using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Services.Network
{
	/// <summary>
	/// ネットワーク操作の進行状態を示します。
	/// これにより、UIなどで連続した操作を防ぐために使用できます。
	/// </summary>
	public enum NetworkOperationStatus
	{
		Idle,             // アイドル状態、操作可能
		Initializing,     // 初期化中
		Connecting,       // 接続中
		Disconnecting,    // 切断中
		CreatingLobby,    // ロビー作成中
		ConnectingLobby,  // ロビー接続中
		DisconnectingLobby,// ロビー切断中
		SearchingLobby,   // ロビー検索中
		CreatingRoom,     // ルーム作成中
		ConnectingRoom,   // ルーム接続中
		DisconnectingRoom,// ルーム切断中
		SearchingRoom,    // ルーム検索中
		ShuttingDown      // 終了処理中
	}

	/// <summary>
	/// ネットワーク接続を管理する表側のクラス。
	/// アプリケーションがネットワーク機能を利用するための主要なインターフェースを提供します。
	/// このクラスはpartialで分割され、機能ごとにファイルが分かれています。
	/// </summary>
	public partial class Network : MonoBehaviour, IInternalNetworkHandler // ★ クラス名変更
	{
		private static Network _instance;
		public static Network Instance
		{
			get
			{
				if (_instance == null)
				{
					// シーン内にインスタンスを探す
					_instance = FindFirstObjectByType<Network>();

					if (_instance == null)
					{
						// シーンに存在しない場合は、新しいGameObjectを作成してアタッチ
						GameObject singletonObject = new GameObject(typeof(Network).Name);
						_instance = singletonObject.AddComponent<Network>();
					}

					// シーンの切り替え時に破棄されないようにする
					DontDestroyOnLoad(_instance.gameObject);
				}
				return _instance;
			}
		}

		// ★追加: 現在のネットワーク操作状態
		private NetworkOperationStatus _currentOperationStatus = NetworkOperationStatus.Idle;
		public NetworkOperationStatus CurrentOperationStatus => _currentOperationStatus;

		[SerializeReference]
		[Tooltip("利用可能なネットワーク設定のリスト。")]
		private List<NetworkSettingsScriptableObjectBase> _availableNetworkSettings = new List<NetworkSettingsScriptableObjectBase>();

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
		// 変数
		// --------------------------------------------------------------------------------

		public object AccountId => _internalNetworkHandler?.AccountId ?? null;

		public string NickName => _internalNetworkHandler?.NickName ?? "";

		public object StationId => _internalNetworkHandler?.StationId ?? null;

		public INetworkSettingsFactory SettingsFactory => _internalNetworkHandler?.SettingsFactory;

		/// <summary>
		/// ネットワークハンドラが初期化済みかどうか。
		/// Initialize 後 true になり、Shutdown 後 false に戻る。
		/// AccountId は Connect 完了後にセットされるため初期化判定には使用しないこと。
		/// </summary>
		public bool IsInitialized => _internalNetworkHandler != null;

		public bool IsConnected => _internalNetworkHandler?.IsConnected ?? false;

		public bool IsHost => _internalNetworkHandler?.IsHost ?? false;

		public List<PlayerData> ConnectedList => _internalNetworkHandler?.ConnectedList ?? null;

		public List<PlayerData> DisconnectedList => _internalNetworkHandler?.DisconnectedList ?? null;

		// 内部のプラットフォーム固有ネットワーク管理クラスへの参照
		private IInternalNetworkHandler _internalNetworkHandler;

		// ★追加: 現在の設定を保持するフィールド
		private IRoomSettings _preparedSettings;

		// 進行中のロビー/ルーム操作をキャンセルするための CancellationTokenSource
		private CancellationTokenSource _operationCts;

		/// <summary>
		/// 現在進行中のロビー/ルーム操作（CreateLobby, ConnectLobby, CreateRoom, ConnectRoom）をキャンセルします。
		/// </summary>
		public void CancelCurrentOperation()
		{
			_operationCts?.Cancel();
			Debug.Log("Network: 現在の操作をキャンセルしました。");
		}

		/// <summary>
		/// ロビー/ルーム作成のための設定を準備します。
		/// この設定は、次に CreateLobby または CreateRoom が呼び出された際に使用されます。
		/// </summary>
		public IRoomSettings PrepareRoomSettings() // ★新しいメソッド
		{
			if (_internalNetworkHandler == null)
			{
				Debug.LogError("Network: 内部ネットワークハンドラが設定されていません。設定を準備できません。");
				return null;
			}
			_preparedSettings = _internalNetworkHandler.SettingsFactory.CreateRoomSettings();
			Debug.Log("Network: ルーム設定が準備されました。_preparedRoomSettings をカスタマイズしてください。");
			return _preparedSettings; // カスタマイズのために設定オブジェクトを返す
		}

		// --------------------------------------------------------------------------------
		// Unity ライフサイクル (Networkの初期化とイベント購読)
		// --------------------------------------------------------------------------------

		private void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else if (_instance != this)
			{
				Destroy(gameObject);
				return; // 既にインスタンスが存在する場合はここで処理を終了
			}
		}

		private void OnDestroy()
		{
			// ハンドラがまだアクティブな場合にイベント購読が解除されることを保証します。
			if (_internalNetworkHandler != null)
			{
				UnsubscribeFromInternalHandlerEvents();
				// 注意: OnDestroy でのハンドラの非同期シャットダウンは複雑です。
				// オブジェクトが破棄される前に ShutdownLibrary が明示的に呼び出されるのが最善です。
				_internalNetworkHandler.Shutdown();
				_internalNetworkHandler = null;
			}
		}

		private void Update()
		{
			_internalNetworkHandler?.UpdateState();
		}

		// --------------------------------------------------------------------------------
		// 内部イベントハンドラと接続
		// --------------------------------------------------------------------------------
		private void SubscribeToInternalHandlerEvents()
		{
			if (_internalNetworkHandler == null) return;

			_internalNetworkHandler.OnDataReceived += HandleReceivedData; // Network.Data.cs で実装
			_internalNetworkHandler.OnPlayerConnected += HandlePlayerConnected; // Network.Data.cs で実装
			_internalNetworkHandler.OnPlayerDisconnected += HandlePlayerDisconnected; // Network.Data.cs で実装
			_internalNetworkHandler.OnNetworkConnectionStatusChanged += InternalHandleNetworkConnectionStatusChanged;
			_internalNetworkHandler.OnHostStatusChanged += InternalHandleHostStatusChanged;
			_internalNetworkHandler.OnLobbyOperationCompleted += InternalHandleLobbyOperationCompleted;
			_internalNetworkHandler.OnRoomOperationCompleted += InternalHandleRoomOperationCompleted;
		}

		private void UnsubscribeFromInternalHandlerEvents()
		{
			if (_internalNetworkHandler == null) return;

			_internalNetworkHandler.OnDataReceived -= HandleReceivedData;
			_internalNetworkHandler.OnPlayerConnected -= HandlePlayerConnected;
			_internalNetworkHandler.OnPlayerDisconnected -= HandlePlayerDisconnected;
			_internalNetworkHandler.OnNetworkConnectionStatusChanged -= InternalHandleNetworkConnectionStatusChanged;
			_internalNetworkHandler.OnHostStatusChanged -= InternalHandleHostStatusChanged;
			_internalNetworkHandler.OnLobbyOperationCompleted -= InternalHandleLobbyOperationCompleted;
			_internalNetworkHandler.OnRoomOperationCompleted -= InternalHandleRoomOperationCompleted;
		}

		// --------------------------------------------------------------------------------
		// コアネットワーク機能 (初期化、接続、切断、切り替え)
		// --------------------------------------------------------------------------------

		public async UniTask<bool> InitializeLibrary(IInternalNetworkHandler handlerToUse)
		{
			try
			{
				if (_internalNetworkHandler != null)
				{
					Debug.LogWarning("Network: 既存のネットワークハンドラがアクティブです。新しいハンドラを初期化する前にシャットダウンします。");
					await ShutdownLibrary(); // これによりイベント購読も解除され、_internalNetworkHandler が null になります
				}

				if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
				{
					Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
					return false;
				}
				_currentOperationStatus = NetworkOperationStatus.Initializing;

				if (handlerToUse == null)
				{
					Debug.LogError("Network: 提供されたネットワークハンドラが null です。初期化に失敗しました。");
					_internalNetworkHandler = null; // null であることを保証
					return false;
				}

				_internalNetworkHandler = handlerToUse;
				Debug.Log("Network: ライブラリを非同期で初期化中...");

				SubscribeToInternalHandlerEvents();


				NetworkSettingsScriptableObjectBase settings = _internalNetworkHandler.SettingsFactory.CreateNetworkSettings();
				settings = _availableNetworkSettings.FirstOrDefault(s =>
				{
					Debug.Log($"{s.name} == {settings.GetType()}");
					return s.GetType() == settings.GetType();
				}) ?? settings;
				bool success = _internalNetworkHandler.Initialize(settings);
				Debug.Log($"Network: ライブラリ初期化 {(success ? "成功" : "失敗")}.");

				if (!success)
				{
					UnsubscribeFromInternalHandlerEvents();
					_internalNetworkHandler = null; // 失敗時にクリーンアップ
					return false;
				}

				// ★ 初期化成功時にAccountId, NickName, StationId を取得
				if (success)
				{
					Debug.Log($"Network: 初期化完了. AccountId: {AccountId}, NickName: {NickName}, StationId: {StationId}");
				}

				return success;
			}
			finally
			{
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
		}

		public UniTask ShutdownLibrary()
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return UniTask.CompletedTask;
			}
			_currentOperationStatus = NetworkOperationStatus.ShuttingDown;

			try
			{
				Debug.Log("Network: ライブラリを非同期で終了中...");
				if (_internalNetworkHandler == null)
				{
					Debug.Log("Network: シャットダウンする内部ネットワークハンドラがありません。");
					return UniTask.CompletedTask;
				}
				_internalNetworkHandler.Shutdown();
				UnsubscribeFromInternalHandlerEvents();

				_internalNetworkHandler = null; // ハンドラを解放
				Debug.Log("Network: ライブラリ終了完了.");
			}
			finally
			{
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
			return UniTask.CompletedTask;
		}

		public async UniTask<bool> ConnectNetwork(string userId, string userName)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return false;
			}
			_currentOperationStatus = NetworkOperationStatus.Connecting;

			try
			{
				Debug.Log($"Network: ネットワークに非同期で接続中... UserID: {userId}, UserName: {userName}");
				if (_internalNetworkHandler == null) return false;

				INetworkSettings settings = _internalNetworkHandler.SettingsFactory.CreateNetworkSettings();
				settings.NickName = userName;
				bool success = await _internalNetworkHandler.Connect(settings);
				if (success)
				{
					Debug.Log($"Network: ネットワーク接続完了. StationId: {StationId}");
				}
				else
				{
					Debug.LogError("Network: ネットワーク接続失敗.");
				}
				return success;
			}
			finally
			{
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
		}

		public async UniTask DisconnectNetwork()
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return;
			}
			_currentOperationStatus = NetworkOperationStatus.Disconnecting;

			try
			{
				Debug.Log("Network: ネットワークから非同期で切断中...");
				if (_internalNetworkHandler == null) return;

				await _internalNetworkHandler.Disconnect();
				Debug.Log("Network: ネットワーク切断完了.");
			}
			finally
			{
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
		}

		public async UniTask<bool> SwitchNetwork(string newUserId, string newUserName)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return false;
			}

			Debug.Log($"Network: ネットワークを切り替えます。新しいユーザー: {newUserName} ({newUserId})");
			if (IsConnected)
			{
				Debug.Log("Network: 現在のネットワークを切断中...");
				await DisconnectNetwork();
				Debug.Log("Network: 現在のネットワーク切断完了。");
			}
			else
			{
				Debug.Log("Network: 現在ネットワークに接続していません。直接新しい接続に進みます。");
			}

			Debug.Log("Network: 新しいネットワークに接続中...");
			bool success = await ConnectNetwork(newUserId, newUserName);

			if (success)
			{
				Debug.Log("Network: ネットワーク切り替え完了.");
			}
			else
			{
				Debug.LogError("Network: ネットワーク切り替え失敗.");
			}
			return success;
		}

		// これらのメソッドは IInternalNetworkHandler からのイベントを処理し、
		// この Network クラスの公開イベントを発行します。
		private void InternalHandleNetworkConnectionStatusChanged(bool isConnected)
		{
			OnNetworkConnectionStatusChanged?.Invoke(isConnected);
		}

		private void InternalHandleHostStatusChanged(bool isHost)
		{
			OnHostStatusChanged?.Invoke(isHost);
		}

		private void InternalHandleLobbyOperationCompleted(string operation, bool success, string message)
		{
			OnLobbyOperationCompleted?.Invoke(operation, success, message);
		}

		private void InternalHandleRoomOperationCompleted(string operation, bool success, string message)
		{
			OnRoomOperationCompleted?.Invoke(operation, success, message);
		}

		public bool Initialize(INetworkSettings settings)
		{
			return _internalNetworkHandler.Initialize(settings);
		}

		public void Shutdown()
		{
			_internalNetworkHandler.Shutdown();
		}

		public UniTask<bool> Connect(INetworkSettings baseSettings)
		{
			return _internalNetworkHandler.Connect(baseSettings);
		}

		public UniTask Disconnect()
		{
			return _internalNetworkHandler.Disconnect();
		}

		public void UpdateState()
		{
			_internalNetworkHandler.UpdateState();
		}
	}
}
