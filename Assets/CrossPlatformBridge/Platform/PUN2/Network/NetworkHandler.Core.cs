#if USE_CROSSPLATFORMBRIDGE_PUN2
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Testing;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace CrossPlatformBridge.Platform.PUN2.Network
{
	/// <summary>
	/// Photon Unity Networking 2 (PUN2) を使用した IInternalNetworkHandler の実装のコア部分。
	/// MonoBehaviourPunCallbacks を継承し、PUN2のイベントを処理します。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class NetworkHandler : MonoBehaviourPunCallbacks, IInternalNetworkHandler, IServiceTestProvider
	{
		// --------------------------------------------------------------------------------
		// イベント (IInternalNetworkHandler)
		// --------------------------------------------------------------------------------
		public event Action<byte[], string> OnDataReceived;
		public event Action<string, string> OnPlayerConnected;
		public event Action<string, string> OnPlayerDisconnected;
		public event Action<bool> OnNetworkConnectionStatusChanged;
		public event Action<bool> OnHostStatusChanged;
		public event Action<string, bool, string> OnLobbyOperationCompleted;
		public event Action<string, bool, string> OnRoomOperationCompleted;

		// --------------------------------------------------------------------------------
		// プロパティ (IInternalNetworkHandler)
		// --------------------------------------------------------------------------------
		public object AccountId { get; private set; }
		public string NickName { get; private set; }
		public object StationId { get; private set; } // PUN2ではRoom.Nameを使用
		public bool IsConnected
		{
			get { return PhotonNetwork.IsConnectedAndReady; }
			set { return; }
		}
		public bool IsHost
		{
			get { return PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient; }
			set { return; }
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


		/// <summary>
		/// このハンドラが提供するINetworkSettingsのファクトリーを取得します。
		/// </summary>
		public INetworkSettingsFactory SettingsFactory { get; } = new PUN2SettingsFactory();

		// --------------------------------------------------------------------------------
		// Unity ライフサイクル (MonoBehaviourPunCallbacks の継承)
		// --------------------------------------------------------------------------------

		/// <summary>
		/// MonoBehaviourが有効になったときにコールバックを登録します。
		/// </summary>
		public override void OnEnable()
		{
			base.OnEnable();
			PhotonNetwork.AddCallbackTarget(this);
			// IOnEventCallback は Data.cs で登録
		}

		/// <summary>
		/// MonoBehaviourが無効になったときにコールバックを解除します。
		/// </summary>
		public override void OnDisable()
		{
			base.OnDisable();
			PhotonNetwork.RemoveCallbackTarget(this);
			// IOnEventCallback は Data.cs で登録解除
		}

		// --------------------------------------------------------------------------------
		// IInternalNetworkHandler インターフェース実装 - コア機能
		// --------------------------------------------------------------------------------

		/// <summary>
		/// Photon を初期化します。
		/// </summary>
		/// <returns>初期化と接続が成功した場合は true、それ以外は false。</returns>
		public bool Initialize(INetworkSettings baseSettings)
		{
			Debug.Log("PUN2NetworkHandler: Photon を初期化中...");

			PhotonNetwork.AutomaticallySyncScene = true; // マスタークライアントがロードしたシーンを自動的に同期
			PhotonNetwork.GameVersion = Application.version; // ゲームのバージョンを設定

			return true;
		}

		/// <summary>
		/// Photon から切断し、ライブラリをシャットダウンします。
		/// </summary>
		public void Shutdown()
		{
			Debug.Log("PUN2NetworkHandler: Photon をシャットダウン中...");
			if (PhotonNetwork.IsConnected)
			{
				PhotonNetwork.Disconnect();
			}
			AccountId = null;
			NickName = null;
			StationId = null;
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("PUN2NetworkHandler: シャットダウン完了。");
		}

		/// <summary>
		/// Photon に接続し、ユーザー情報を設定します。
		/// Initialize が既に接続を行っているため、ここではロビーへの参加も試みます。
		/// </summary>
		/// <param name="baseSettings"></param>
		/// <returns>接続とロビー参加が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> Connect(INetworkSettings baseSettings)
		{
			NetworkSettings settings = baseSettings as NetworkSettings;
			// 既に接続済みであれば、現在の状態を通知して終了
			if (PhotonNetwork.IsConnectedAndReady)
			{
				Debug.Log("PUN2NetworkHandler: Photon は既に接続済みです。");
				AccountId = PhotonNetwork.LocalPlayer.UserId;
				NickName = PhotonNetwork.LocalPlayer.NickName;
				StationId = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "Not in Room";
				OnNetworkConnectionStatusChanged?.Invoke(true);
				OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
				return true;
			}

#if UNITY_SWITCH
			//// Photon NintendoSerivceAccount認証
			//await SwitchNetwork.GetNsaIdTokenString();
			//if (string.IsNullOrEmpty(SwitchNetwork.NsaIdTokenString))
			//	throw new UnauthorizedAccessException("ネットワークの認証がキャンセルされました。");
			if (!Application.isEditor)
			{
				PhotonNetwork.AuthValues = new AuthenticationValues()
				{
					UserId = NNAccount.NXCurrentUid.ToString(),
					AuthType = CustomAuthenticationType.NintendoSwitch,
				};
				//PhotonNetwork.AuthValues.AddAuthParameter("token", SwitchNetwork.NsaIdTokenString);
			}
#endif

			// NickName を接続前に設定する（OnJoinedRoom で LocalPlayer.NickName が空にならないよう）
			if (!string.IsNullOrEmpty(settings?.NickName))
				PhotonNetwork.LocalPlayer.NickName = settings.NickName;

			// Photon に接続
			PhotonNetwork.ConnectUsingSettings();

			// 接続が完了するまで待機
			await UniTask.WaitUntil(() => PhotonNetwork.IsConnectedAndReady || PhotonNetwork.NetworkClientState == ClientState.Disconnected);

			if (PhotonNetwork.IsConnectedAndReady)
			{
				Debug.Log("PUN2NetworkHandler: Photon 初期化・接続成功。");
				AccountId = PhotonNetwork.LocalPlayer.UserId;
				NickName = PhotonNetwork.LocalPlayer.NickName;
				StationId = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "Not in Room";
				OnNetworkConnectionStatusChanged?.Invoke(true);
				OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
				return true;
			}
			else
			{
				Debug.LogError("PUN2NetworkHandler: Photon 初期化・接続失敗。クライアント状態: " + PhotonNetwork.NetworkClientState);
				OnNetworkConnectionStatusChanged?.Invoke(false);
				OnHostStatusChanged?.Invoke(false);
				return false;
			}
		}

		/// <summary>
		/// Photon から切断します。
		/// </summary>
		public async UniTask Disconnect()
		{
			Debug.Log("PUN2NetworkHandler: Photon から切断中...");
			if (PhotonNetwork.IsConnected)
			{
				PhotonNetwork.Disconnect();
				await UniTask.WaitUntil(() => !PhotonNetwork.IsConnected);
			}
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			AccountId = null;
			NickName = null;
			StationId = null;
			Debug.Log("PUN2NetworkHandler: 切断完了。");
		}

		/// <summary>
		/// 内部のネットワーク状態を更新します。
		/// PUN2はイベント駆動型なので、このメソッドで特別な処理は不要ですが、
		/// 必要に応じて、定期的なチェックや同期処理をここに記述できます。
		/// </summary>
		public void UpdateState()
		{
			// イベント駆動型なので通常は何もせず、Photonのコールバックに任せる
		}

		#region MonoBehaviourPunCallbacks
		// --------------------------------------------------------------------------------
		// MonoBehaviourPunCallbacks (PUN2のイベントハンドラ) - コア関連
		// --------------------------------------------------------------------------------

		/// <summary>
		/// Photon Cloud に接続した際に呼び出されます。
		/// </summary>
		public override void OnConnectedToMaster()
		{
			Debug.Log("PUN2NetworkHandler.OnConnectedToMaster: Photon Master Server に接続しました。");
			OnNetworkConnectionStatusChanged?.Invoke(true); // 接続状態が変更されたことを通知
			AccountId = PhotonNetwork.LocalPlayer.UserId;
			NickName = PhotonNetwork.LocalPlayer.NickName;
		}

		/// <summary>
		/// Photon Cloud から切断された際に呼び出されます。
		/// </summary>
		/// <param name="cause">切断の理由。</param>
		public override void OnDisconnected(DisconnectCause cause)
		{
			Debug.LogWarning($"PUN2NetworkHandler.OnDisconnected: Photon から切断されました。原因: {cause}");
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			AccountId = null;
			NickName = null;
			StationId = null;
		}
		#endregion

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => null;

		public TestDefaultData GetDefaultData() => new TestDefaultData { UserName = "PUN2Player", LobbyRoomName = "PUN2Lobby", SendData = "Hello from PUN2!" };
	}
}

#endif
