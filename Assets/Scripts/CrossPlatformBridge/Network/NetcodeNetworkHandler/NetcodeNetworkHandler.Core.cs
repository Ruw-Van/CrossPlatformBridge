// Assets/Scripts/CrossPlatformBridge/Services/NetcodeNetworkHandler/NetcodeNetworkHandler.cs
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;

namespace CrossPlatformBridge.Network.NetcodeNetworkHandler
{
	/// <summary>
	/// Unity Netcode for GameObjects を使用した IInternalNetworkHandler の実装。
	/// Unity Gaming Services (Lobby, Relay, Authentication) と連携します。
	/// </summary>
	public partial class NetcodeNetworkHandler : IInternalNetworkHandler
	{
		// --------------------------------------------------------------------------------
		// イベント
		// --------------------------------------------------------------------------------
		public event Action<byte[]> OnDataReceived;
		public event Action<string, string> OnPlayerConnected;
		public event Action<string, string> OnPlayerDisconnected;
		public event Action<bool> OnNetworkConnectionStatusChanged;
		public event Action<bool> OnHostStatusChanged;
		public event Action<string, bool, string> OnLobbyOperationCompleted;
		public event Action<string, bool, string> OnRoomOperationCompleted; // Netcodeでは'ルーム'は'セッション'や'ゲーム'と解釈

		// --------------------------------------------------------------------------------
		// 内部状態
		// --------------------------------------------------------------------------------
		private Allocation _allocation;
		private string _joinCode;
		private Lobby _connectedLobby; // 現在接続中のロビー情報

		// --------------------------------------------------------------------------------
		// プロパティ
		// --------------------------------------------------------------------------------
		public string AccountId { get; private set; }
		public string NickName { get; private set; }
		public string StationId { get; private set; } // LobbyIDを使用

		/// <summary>
		/// このハンドラが提供するINetworkSettingsのファクトリーを取得します。
		/// </summary>
		public INetworkSettingsFactory SettingsFactory { get; } = new NetcodeSettingsFactory(); // ★ 追加

		// --------------------------------------------------------------------------------
		// Constructor
		// --------------------------------------------------------------------------------
		public NetcodeNetworkHandler()
		{
			// NetworkManager のシングルトンインスタンスが存在しない場合は、GameObjectを作成してアタッチ
			if (NetworkManager.Singleton == null)
			{
				GameObject networkManagerGameObject = new GameObject("NetworkManager");
				// NetworkManagerはMonoBehaviourなので、AddComponentで追加するとSingletonプロパティが自動的に設定されます。
				// NetworkManager.Singletonに直接代入する必要はありません。
				networkManagerGameObject.AddComponent<NetworkManager>(); // ★修正: NetworkManager.Singletonへの直接代入を削除
				networkManagerGameObject.AddComponent<UnityTransport>(); // UTPをアタッチ
			}

			// NetworkManagerのイベント購読
			NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
			NetworkManager.Singleton.OnServerStarted += OnServerStarted;
			NetworkManager.Singleton.OnServerStopped += OnServerStopped;
		}

		// --------------------------------------------------------------------------------
		// Unity Netcode イベントハンドラ
		// --------------------------------------------------------------------------------

		private void HandleClientConnected(ulong clientId)
		{
			Debug.Log($"NetcodeNetworkHandler: クライアント接続: {clientId}");
			OnNetworkConnectionStatusChanged?.Invoke(NetworkManager.Singleton.IsConnectedClient);
			OnPlayerConnected?.Invoke(clientId.ToString(), $"Player_{clientId}"); // 仮の名前
		}

		private void HandleClientDisconnected(ulong clientId)
		{
			Debug.Log($"NetcodeNetworkHandler: クライアント切断: {clientId}");
			OnNetworkConnectionStatusChanged?.Invoke(NetworkManager.Singleton.IsConnectedClient);
			OnPlayerDisconnected?.Invoke(clientId.ToString(), $"Player_{clientId}"); // 仮の名前
																					 // もしホストが切断された場合、クライアントも切断される
			if (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsClient)
			{
				OnNetworkConnectionStatusChanged?.Invoke(false);
			}
		}

		private void HandleServerStarted()
		{
			Debug.Log("NetcodeNetworkHandler: サーバー開始。");
			OnNetworkConnectionStatusChanged?.Invoke(true);
			OnHostStatusChanged?.Invoke(true); // ホストであると通知
											   // OnPlayerConnected?.Invoke(NetworkManager.Singleton.LocalClientId.ToString(), NickName); // NickNameはNetworkが持つ情報なので、ここでは使えない
		}

		private void HandleClientStarted()
		{
			Debug.Log("NetcodeNetworkHandler: クライアント開始。");
			OnNetworkConnectionStatusChanged?.Invoke(true);
			OnHostStatusChanged?.Invoke(NetworkManager.Singleton.IsHost);
		}

		// 接続承認コールバック (サーバー側でクライアントの接続を許可するかどうかを判断)
		private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
		{
			// ここでクライアントの認証情報などをチェックし、接続を許可するかどうかを判断する
			// request.Payload に ConnectNetwork() で渡された認証データなどを含めることができます。

			// 例: payload を読み込み、認証ロジックを実行
			// var connectionData = System.Text.Encoding.UTF8.GetString(request.Payload);
			// Debug.Log($"Connection approval request from {request.ClientId} with payload: {connectionData}");

			response.Approved = true; // とりあえず全ての接続を許可
			response.CreatePlayerObject = true; // プレイヤーオブジェクトを生成
												// response.PlayerPrefabHash = null; // 特定のプレイヤープレハブをスポーンしたい場合はここにハッシュを設定
			response.Position = Vector3.zero; // スポーン位置
			response.Rotation = Quaternion.identity; // スポーン向き
													 // response.Pending = false; // 非同期で承認する場合は true にする
		}


		// --------------------------------------------------------------------------------
		// IInternalNetworkHandler インターフェース実装
		// --------------------------------------------------------------------------------

		/// <summary>
		/// Unity Services を初期化し、認証を行います。
		/// </summary>
		public async UniTask<bool> Initialize()
		{
			Debug.Log("NetcodeNetworkHandler: Unity Services を初期化中...");
			try
			{
				if (UnityServices.State == ServicesInitializationState.Uninitialized)
				{
					await UnityServices.InitializeAsync();
				}

				if (!AuthenticationService.Instance.IsSignedIn)
				{
					await AuthenticationService.Instance.SignInAnonymouslyAsync();
					AccountId = AuthenticationService.Instance.PlayerId;
					Debug.Log($"NetcodeNetworkHandler: 認証成功。Player ID: {AccountId}");
				}
				else
				{
					AccountId = AuthenticationService.Instance.PlayerId;
					Debug.Log($"NetcodeNetworkHandler: 既に認証済み。Player ID: {AccountId}");
				}

				OnNetworkConnectionStatusChanged?.Invoke(true); // サービスへの接続を「ネットワーク接続」とみなす
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError($"NetcodeNetworkHandler: Unity Services 初期化失敗: {e.Message}");
				OnNetworkConnectionStatusChanged?.Invoke(false);
				return false;
			}
		}

		/// <summary>
		/// Unity Services からサインアウトし、関連するリソースをクリーンアップします。
		/// </summary>
		public async UniTask Shutdown()
		{
			Debug.Log("NetcodeNetworkHandler: Unity Services をシャットダウン中...");
			await DisconnectRoom(); // ルームやロビーから切断
			await DisconnectLobby(); // ロビーから切断

			if (AuthenticationService.Instance.IsSignedIn)
			{
				AuthenticationService.Instance.SignOut();
				Debug.Log("NetcodeNetworkHandler: サインアウトしました。");
			}

			AccountId = null;
			NickName = null;
			StationId = null;
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("NetcodeNetworkHandler: シャットダウン完了。");
		}

		/// <summary>
		/// ユーザー情報を設定し、必要であればUnity Servicesに再接続します。
		/// Netcode for GameObjectsでは、認証時にPlayerIdが設定されます。
		/// </summary>
		/// <param name="userId">接続に使用するユーザーID。</param>
		/// <param name="userName">接続に使用するユーザー名。</param>
		/// <returns>接続と認証が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> Connect(string userId, string userName)
		{
			Debug.Log($"NetcodeNetworkHandler: Connect呼び出し. UserID: {userId}, UserName: {userName}");

			if (!AuthenticationService.Instance.IsSignedIn || AuthenticationService.Instance.PlayerId != userId)
			{
				// 既に認証済みでIDが異なる場合、一度サインアウトして再認証
				if (AuthenticationService.Instance.IsSignedIn)
				{
					AuthenticationService.Instance.SignOut();
				}

				// 特定のUserIdでSignInが必要な場合は、AuthenticationService.Instance.SignInWithCustomIdAsync(userId) を使用
				// 今回は匿名認証なので、UserID は AuthenticationService.Instance.PlayerId になります。
				// 渡されたuserIdはNickNameとして使用します。
				await AuthenticationService.Instance.SignInAnonymouslyAsync();
				AccountId = AuthenticationService.Instance.PlayerId;
				Debug.Log($"NetcodeNetworkHandler: 新しいユーザーで認証成功。Player ID: {AccountId}");
			}
			else
			{
				AccountId = AuthenticationService.Instance.PlayerId;
				Debug.Log($"NetcodeNetworkHandler: 既存のユーザーで認証済み。Player ID: {AccountId}");
			}

			NickName = userName; // NickNameを設定

			OnNetworkConnectionStatusChanged?.Invoke(AuthenticationService.Instance.IsSignedIn);
			return AuthenticationService.Instance.IsSignedIn;
		}

		/// <summary>
		/// Netcode NetworkManager と Lobby から切断します。
		/// </summary>
		public async UniTask Disconnect()
		{
			Debug.Log("NetcodeNetworkHandler: 全てのネットワーク接続を切断中...");
			await DisconnectRoom();
			await DisconnectLobby();
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("NetcodeNetworkHandler: 全てのネットワーク接続切断完了。");
		}

		public void UpdateState()
		{
			// Netcode for GameObjects はイベント駆動型なので、通常ここで特別な更新処理は不要です。
			// Lobby Heartbeat の更新などが必要な場合はここで行います。
			// Unity Services LobbyのHeartbeatは、Hostが定期的に呼び出す必要があります。
			if (_connectedLobby != null && _connectedLobby.HostId == AuthenticationService.Instance.PlayerId)
			{
				_ = LobbyService.Instance.SendHeartbeatPingAsync(_connectedLobby.Id);
			}
		}

		// --------------------------------------------------------------------------------
		// NetworkManager コールバック
		// --------------------------------------------------------------------------------

		private void OnClientConnected(ulong clientId)
		{
			// NetworkManager.Singleton.LocalClientId はローカルクライアントのID。
			// NetworkManager.Singleton.ConnectedClients は接続中のクライアントリスト。
			Debug.Log($"NetcodeNetworkHandler: クライアント接続: ClientId = {clientId}");

			// クライアントがホスト自身の場合（NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId）
			// または他のクライアントの場合で処理を分ける
			if (NetworkManager.Singleton.IsServer) // サーバー側から見た接続
			{
				string playerId = "Unknown"; // Unity ServicesのPlayerIdは直接NetworkManagerからは取得できない
				string playerName = "Unknown";

				// TODO: 実際のプレイヤーデータはNetworkManagerのCustomMessagingManagerなどで同期する必要があります。
				// ロビーから取得した情報を使用する
				Player connectedPlayer = _connectedLobby?.Players?.Find(p => p.Id == AuthenticationService.Instance.PlayerId); // 仮の取得
				if (connectedPlayer != null)
				{
					playerId = connectedPlayer.Id;
					//playerName = connectedPlayer.Data["DisplayName"].Value;
				}

				// サーバーは自分自身もクライアントとして認識するため、ここでは他クライアントのみをConnectedとみなす
				if (clientId != NetworkManager.Singleton.LocalClientId)
				{
					OnPlayerConnected?.Invoke(playerId, playerName);
				}
			}
			else // クライアント側から見た接続
			{
				// クライアント自身が接続した場合
				OnNetworkConnectionStatusChanged?.Invoke(true);
			}
		}

		private void OnClientDisconnected(ulong clientId)
		{
			Debug.Log($"NetcodeNetworkHandler: クライアント切断: ClientId = {clientId}");
			if (NetworkManager.Singleton.IsServer)
			{
				// サーバーから見たクライアント切断 (プレイヤーがルームを退出)
				string playerId = "Unknown"; // Unity ServicesのPlayerIdは直接NetworkManagerからは取得できない
				string playerName = "Unknown";
				// TODO: 実際のプレイヤーデータはNetworkManagerのCustomMessagingManagerなどで同期する必要があります。
				// ロビーから取得した情報を使用する
				Player disconnectedPlayer = _connectedLobby?.Players?.Find(p => p.Id == AuthenticationService.Instance.PlayerId); // 仮の取得
				if (disconnectedPlayer != null)
				{
					playerId = disconnectedPlayer.Id;
					//playerName = disconnectedPlayer.Data["DisplayName"].Value;
				}
				OnPlayerDisconnected?.Invoke(playerId, playerName);
			}
			else // クライアント自身が切断した場合
			{
				OnNetworkConnectionStatusChanged?.Invoke(false);
				OnHostStatusChanged?.Invoke(false);
			}
		}

		private void OnServerStarted()
		{
			Debug.Log("NetcodeNetworkHandler: NetworkManager サーバー開始。");
			OnHostStatusChanged?.Invoke(true);
			// サーバーは自身もクライアントとして接続されるため、ここで自身をConnectedとする
			OnPlayerConnected?.Invoke(AccountId, NickName);
		}

		private void OnServerStopped(bool arg0)
		{
			Debug.Log("NetcodeNetworkHandler: NetworkManager サーバー停止。");
			OnHostStatusChanged?.Invoke(false);
		}
	}
}
