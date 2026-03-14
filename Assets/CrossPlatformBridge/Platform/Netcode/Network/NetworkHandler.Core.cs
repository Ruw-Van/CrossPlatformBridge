#if USE_CROSSPLATFORMBRIDGE_NETCODE
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Testing;

namespace CrossPlatformBridge.Platform.Netcode.Network
{
	/// <summary>
	/// Unity Netcode for GameObjects を使用した IInternalNetworkHandler の実装。
	/// Unity Gaming Services (Lobby, Relay, Authentication) と連携します。
	/// </summary>
	public partial class NetworkHandler : IInternalNetworkHandler, IServiceTestProvider
	{
		// --------------------------------------------------------------------------------
		// イベント
		// --------------------------------------------------------------------------------
		public event Action<byte[], string> OnDataReceived;
		public event Action<string, string> OnPlayerConnected;
		public event Action<string, string> OnPlayerDisconnected;
		public event Action<bool> OnNetworkConnectionStatusChanged;
		public event Action<bool> OnHostStatusChanged;
		public event Action<string, bool, string> OnLobbyOperationCompleted;
		public event Action<string, bool, string> OnRoomOperationCompleted;

		// --------------------------------------------------------------------------------
		// 内部状態
		// --------------------------------------------------------------------------------
		private Unity.Services.Relay.Models.Allocation _allocation;
		private string _joinCode;
		private Lobby _connectedLobby;

		// ハートビート送信間隔（秒）
		private const float HeartbeatInterval = 15f;
		private float _lastHeartbeatTime = -HeartbeatInterval;

		// UGS 認証完了フラグ（Connect 成功後 true、Shutdown/Disconnect で false）
		private bool _isAuthenticated = false;

		// ホストマイグレーション制御フラグ
		// _pendingReconnect : クライアントがサーバー切断後に Lobby 経由の再接続を待機中
		// _isIntentionalShutdown : DisconnectRoom / HandleHostMigration から意図的に呼んだ Shutdown か否か
		private bool _pendingReconnect = false;
		private bool _isIntentionalShutdown = false;

		// --------------------------------------------------------------------------------
		// プロパティ
		// --------------------------------------------------------------------------------
		public object AccountId { get; private set; }
		public string NickName { get; private set; }
		public object StationId { get; private set; }

		/// <summary>
		/// UGS 匿名認証が完了している状態を「接続済み」とみなします。
		/// NetworkManager のセッション開始（CreateRoom/ConnectRoom）は IsHost で確認できます。
		/// </summary>
		public bool IsConnected => _isAuthenticated;

		public bool IsHost
		{
			get
			{
				if (NetworkManager.Singleton == null) return false;
				return NetworkManager.Singleton.IsHost;
			}
		}

		private List<PlayerData> _connectedList = new List<PlayerData>();
		public List<PlayerData> ConnectedList
		{
			get { return _connectedList; }
			set { _connectedList = value; }
		}

		private List<PlayerData> _disconnectedList = new List<PlayerData>();
		public List<PlayerData> DisconnectedList
		{
			get { return _disconnectedList; }
			set { _disconnectedList = value; }
		}

		public INetworkSettingsFactory SettingsFactory { get; } = new NetcodeSettingsFactory();

		// --------------------------------------------------------------------------------
		// Constructor
		// --------------------------------------------------------------------------------

		/// <summary>
		/// コンストラクタは副作用を持ちません。
		/// NetworkManager の生成とコールバック登録は Initialize() で行います。
		/// </summary>
		public NetworkHandler() { }

		// --------------------------------------------------------------------------------
		// IInternalNetworkHandler インターフェース実装
		// --------------------------------------------------------------------------------

		/// <summary>
		/// NetworkManager を生成（未生成の場合）し、コールバックを登録します。
		/// </summary>
		public bool Initialize(INetworkSettings baseSettings)
		{
			if (NetworkManager.Singleton == null)
			{
				GameObject networkManagerGameObject = new GameObject("NetworkManager");
				networkManagerGameObject.AddComponent<NetworkManager>();
				networkManagerGameObject.AddComponent<UnityTransport>();
			}

			NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
			NetworkManager.Singleton.OnServerStarted += OnServerStarted;
			NetworkManager.Singleton.OnServerStopped += OnServerStopped;
			return true;
		}

		/// <summary>
		/// Unity Services からサインアウトし、関連するリソースをクリーンアップします。
		/// </summary>
		public void Shutdown()
		{
			Debug.Log("NetcodeNetworkHandler: Unity Services をシャットダウン中...");

			// マイグレーション待機をキャンセル
			_pendingReconnect = false;
			_isIntentionalShutdown = false;

			// NetworkManager イベント購読解除
			if (NetworkManager.Singleton != null)
			{
				NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
				NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
				NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
				NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
			}

			DisconnectRoom().Forget();
			DisconnectLobby().Forget();

			if (AuthenticationService.Instance.IsSignedIn)
			{
				AuthenticationService.Instance.SignOut();
				Debug.Log("NetcodeNetworkHandler: サインアウトしました。");
			}

			AccountId = null;
			NickName = null;
			StationId = null;
			_isAuthenticated = false;
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("NetcodeNetworkHandler: シャットダウン完了。");
		}

		/// <summary>
		/// Unity Services を初期化・認証します。
		/// </summary>
		public async UniTask<bool> Connect(INetworkSettings baseSettings)
		{
			try
			{
				// 1. Unity Services を先に初期化（AuthenticationService へのアクセスに必要）
				Debug.Log("NetcodeNetworkHandler: Unity Services を初期化中...");
				if (UnityServices.State == ServicesInitializationState.Uninitialized)
				{
#if UNITY_EDITOR
					// Editor を複数起動した場合にプロセスIDでプロファイルを分離し、
					// 各インスタンスが異なる匿名アカウント（PlayerId）を取得できるようにする
					var options = new Unity.Services.Core.InitializationOptions()
						.SetProfile($"editor_{System.Diagnostics.Process.GetCurrentProcess().Id}");
					await UnityServices.InitializeAsync(options);
#else
					await UnityServices.InitializeAsync();
#endif
				}

				// 2. 既にサインイン済みかつ同一プレイヤーならスキップ
				if (AuthenticationService.Instance.IsSignedIn
					&& AuthenticationService.Instance.PlayerId == (AccountId?.ToString() ?? ""))
				{
					_isAuthenticated = true;
					Debug.Log($"NetcodeNetworkHandler: 既に認証済み。Player ID: {AccountId}");
					OnNetworkConnectionStatusChanged?.Invoke(true);
					return true;
				}

				// 3. 別IDでサインイン済みの場合は一度サインアウト
				if (AuthenticationService.Instance.IsSignedIn)
				{
					AuthenticationService.Instance.SignOut();
				}

				// 4. NetworkManager に UTP を設定
				var utpTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
				if (NetworkManager.Singleton.NetworkConfig == null)
				{
					NetworkManager.Singleton.NetworkConfig = new NetworkConfig();
				}
				NetworkManager.Singleton.NetworkConfig.NetworkTransport = utpTransport;

				// 5. 匿名サインイン
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				AccountId = AuthenticationService.Instance.PlayerId;
				NickName = string.IsNullOrEmpty(baseSettings.NickName)
					? AuthenticationService.Instance.PlayerId
					: baseSettings.NickName;
				_isAuthenticated = true;
				Debug.Log($"NetcodeNetworkHandler: 認証成功。Player ID: {AccountId}");

				OnNetworkConnectionStatusChanged?.Invoke(true);
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError($"NetcodeNetworkHandler: Connect 失敗: {e.Message}");
				_isAuthenticated = false;
				OnNetworkConnectionStatusChanged?.Invoke(false);
				return false;
			}
		}

		/// <summary>
		/// Netcode NetworkManager と Lobby から切断します。
		/// </summary>
		public async UniTask Disconnect()
		{
			Debug.Log("NetcodeNetworkHandler: 全てのネットワーク接続を切断中...");
			await DisconnectRoom();
			await DisconnectLobby();
			_isAuthenticated = false;
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("NetcodeNetworkHandler: 全てのネットワーク接続切断完了。");
		}

		public void UpdateState()
		{
			// Lobby ハートビートをホストのみ、一定間隔で送信
			if (_connectedLobby != null
				&& AuthenticationService.Instance.IsSignedIn
				&& _connectedLobby.HostId == AuthenticationService.Instance.PlayerId)
			{
				float now = Time.realtimeSinceStartup;
				if (now - _lastHeartbeatTime >= HeartbeatInterval)
				{
					_lastHeartbeatTime = now;
					_ = LobbyService.Instance.SendHeartbeatPingAsync(_connectedLobby.Id);
				}
			}
		}

		// --------------------------------------------------------------------------------
		// NetworkManager コールバック
		// --------------------------------------------------------------------------------

		private void OnClientConnected(ulong clientId)
		{
			Debug.Log($"NetcodeNetworkHandler: クライアント接続: ClientId = {clientId}");

			// プレイヤーの追加は Lobby イベント（OnLobbyChanged）で行うため、
			// ここでは NetworkManager レベルの処理のみ実施する
			if (!NetworkManager.Singleton.IsServer
				&& clientId == NetworkManager.Singleton.LocalClientId)
			{
				// クライアント自身の接続確認
				OnNetworkConnectionStatusChanged?.Invoke(true);
				RegisterDataMessageHandler();
			}
		}

		private void OnClientDisconnected(ulong clientId)
		{
			Debug.Log($"NetcodeNetworkHandler: クライアント切断: ClientId = {clientId}");

			if (NetworkManager.Singleton.IsServer)
			{
				// サーバー側: 切断プレイヤーをリストから移動（Lobby イベントでも処理されるが Netcode 側でも同期）
				var player = ConnectedList.Find(p => p.Id == clientId.ToString());
				if (player != null)
				{
					ConnectedList.Remove(player);
					DisconnectedList.Add(player);
					OnPlayerDisconnected?.Invoke(player.Id, player.Name);
				}
			}
			else if (clientId == NetworkManager.Singleton.LocalClientId)
			{
				if (_isIntentionalShutdown)
				{
					// DisconnectRoom / HandleHostMigration / ReconnectAfterHostMigration が呼んだ
					// 意図的な Shutdown → 呼び出し元が後続処理を担う
					_isIntentionalShutdown = false;
				}
				else if (_connectedLobby != null && _lobbyEvents != null)
				{
					// 予期しないサーバー切断 + Lobby にはまだ参加中
					// → ホストマイグレーション（HostId.Changed）または JoinCode 更新を待つ
					Debug.Log("NetcodeNetworkHandler: 予期しない切断。Lobby 経由のホストマイグレーションを待機します...");
					_pendingReconnect = true;
				}
				else
				{
					// Lobby もない通常の切断
					OnNetworkConnectionStatusChanged?.Invoke(false);
					OnHostStatusChanged?.Invoke(false);
				}
			}
		}

		private void OnServerStarted()
		{
			Debug.Log("NetcodeNetworkHandler: NetworkManager サーバー開始。");
			OnHostStatusChanged?.Invoke(true);
			// ホスト自身は ConnectedList に追加しない
			// ホストはサーバー役としてルームを管理するが、接続プレイヤー一覧には含めない
			RegisterDataMessageHandler();
		}

		private void OnServerStopped(bool wasServerRunning)
		{
			Debug.Log("NetcodeNetworkHandler: NetworkManager サーバー停止。");
			OnHostStatusChanged?.Invoke(false);
			UnregisterDataMessageHandler();
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => null;

		public TestDefaultData GetDefaultData() => new TestDefaultData { UserName = "NetcodePlayer", LobbyRoomName = "NetcodeLobby", SendData = "Hello from Netcode!" };
	}
}

#endif
