using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Testing;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Platform.Dummy.Network
{
	/// <summary>
	/// IInternalNetworkHandler のダミー実装のコア部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class NetworkHandler : IInternalNetworkHandler, IServiceTestProvider
	{
		// --------------------------------------------------------------------------------
		// イベント (IInternalNetworkHandler)
		// --------------------------------------------------------------------------------
		public event Action<byte[], string> OnDataReceived;
		public event Action<string, string> OnPlayerConnected;
#pragma warning disable CS0067
		public event Action<string, string> OnPlayerDisconnected;
#pragma warning restore CS0067
		public event Action<bool> OnNetworkConnectionStatusChanged;
		public event Action<bool> OnHostStatusChanged;
		public event Action<string, bool, string> OnLobbyOperationCompleted;
		public event Action<string, bool, string> OnRoomOperationCompleted;

		// --------------------------------------------------------------------------------
		// プロパティ (IInternalNetworkHandler)
		// --------------------------------------------------------------------------------
		public object AccountId { get; private set; }
		public string NickName { get; private set; }
		public object StationId { get; private set; }
		public bool IsConnected { get => _isConnected; private set => _isConnected = value; }
		public bool IsHost { get => _isHost; private set => _isHost = value; }
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
		/// このハンドラが提供するINetworkRoomSettingsのファクトリーを取得します。
		/// </summary>
		public INetworkSettingsFactory SettingsFactory { get; } = new DummySettingsFactory(); // ★ 追加

		// --------------------------------------------------------------------------------
		// 内部設定可能なプロパティ
		// --------------------------------------------------------------------------------
		/// <summary>
		/// ルーム作成時に使用されるデフォルトのDummyRoomSettings。
		/// 外部から事前に設定することが可能です。
		/// </summary>
		public RoomSettings DefaultRoomSettings { get; set; } = new RoomSettings();


		// --------------------------------------------------------------------------------
		// 内部状態
		// --------------------------------------------------------------------------------
		private bool _isConnected = false;
		private bool _isHost = false;
		private string _currentLobbyId = null;
		private List<string> _connectedPlayers = new List<string>();

		// --------------------------------------------------------------------------------
		// コンストラクタ
		// --------------------------------------------------------------------------------
		public NetworkHandler()
		{
			Debug.Log("DummyNetworkHandler: インスタンスが作成されました。");
		}

		public bool Initialize(INetworkSettings baseSettings)
		{
			Debug.Log("DummyNetworkHandler: 初期化中...");
			NetworkSettings setting = baseSettings as NetworkSettings;
			_isConnected = true;
			AccountId = "dummyUser_" + Guid.NewGuid().ToString().Substring(0, 8);
			NickName = Application.productName;
			OnNetworkConnectionStatusChanged?.Invoke(true);
			Debug.Log($"DummyNetworkHandler: 初期化完了. AccountId: {AccountId}, NickName: {NickName}, StationId: {StationId}");
			return true;
		}

		public void Shutdown()
		{
			Debug.Log("DummyNetworkHandler: シャットダウン中...");
			_isConnected = false;
			_isHost = false;
			_currentLobbyId = null;
			_connectedPlayers.Clear();
			AccountId = null;
			NickName = null;
			StationId = null;
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("DummyNetworkHandler: シャットダウン完了。");
		}

		public async UniTask<bool> Connect(INetworkSettings baseSettings)
		{
			NetworkSettings setting = baseSettings as NetworkSettings;
			Debug.Log($"DummyNetworkHandler: 接続中... UserID: {AccountId}, UserName: {NickName}");
			await UniTask.Delay(200); // 接続のシミュレーション
			if (_isConnected)
			{
				Debug.Log("DummyNetworkHandler: 既に接続済みです。");
				return true;
			}

			_isConnected = true;
			StationId = "dummySession_" + Guid.NewGuid().ToString().Substring(0, 8);
			OnNetworkConnectionStatusChanged?.Invoke(true);
			Debug.Log($"DummyNetworkHandler: 接続完了. AccountId: {AccountId}, NickName: {NickName}, StationId: {StationId}");
			return true;
		}

		public async UniTask Disconnect()
		{
			Debug.Log("DummyNetworkHandler: 切断中...");
			await UniTask.Delay(200); // 切断のシミュレーション
			_isConnected = false;
			_isHost = false;
			_currentLobbyId = null;
			_connectedPlayers.Clear();
			AccountId = null;
			NickName = null;
			StationId = null;
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("DummyNetworkHandler: 切断完了。");
		}

		public void UpdateState()
		{
			// ダミーなので特別な更新処理は不要
			// Debug.Log("DummyNetworkHandler: UpdateState 呼び出し。");
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		/// <summary>Network 操作は ServiceTestUI 側の従来実装で行うため null を返します。</summary>
		public IReadOnlyList<TestOperation> GetTestOperations() => null;

		public TestDefaultData GetDefaultData() => new TestDefaultData
		{
			UserName = "DummyPlayer",
			LobbyRoomName = "DummyLobby",
			SendData = "Hello from Dummy!",
		};
	}
}
