// Assets/Scripts/CrossPlatformBridge/Network/Network.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Network
{
	/// <summary>
	/// ネットワーク接続を管理する表側のクラス。
	/// アプリケーションがネットワーク機能を利用するための主要なインターフェースを提供します。
	/// このクラスはpartialで分割され、機能ごとにファイルが分かれています。
	/// </summary>
	public partial class Network : MonoBehaviour // ★ クラス名変更
	{
				// --------------------------------------------------------------------------------
		// イベント (IInternalNetworkHandler)
		// --------------------------------------------------------------------------------
		public event Action<byte[]> OnDataReceived;
		public event Action<string, string> OnPlayerConnected;
		public event Action<string, string> OnPlayerDisconnected;
		public event Action<bool> OnNetworkConnectionStatusChanged;
		public event Action<bool> OnHostStatusChanged;
		public event Action<string, bool, string> OnLobbyOperationCompleted;
		public event Action<string, bool, string> OnRoomOperationCompleted;

		// --------------------------------------------------------------------------------
		// 変数
		// --------------------------------------------------------------------------------

		public string AccountId { get; private set; }
		public string NickName { get; private set; }
		public string StationId { get; private set; }
		public bool IsConnected { get; private set; }
		public bool IsHost { get; private set; }
		public List<string> ConnectedList { get; private set; } = new List<string>();
		public List<string> DisconnectedList { get; private set; } = new List<string>();

		// 内部のプラットフォーム固有ネットワーク管理クラスへの参照
		private IInternalNetworkHandler _internalNetworkHandler;

		// ★追加: 現在の設定を保持するフィールド
		private INetworkSettings _preparedSettings;

		/// <summary>
		/// ロビー/ルーム作成のための設定を準備します。
		/// この設定は、次に CreateLobby または CreateRoom が呼び出された際に使用されます。
		/// </summary>
		public INetworkSettings PrepareRoomSettings() // ★新しいメソッド
		{
			if (_internalNetworkHandler == null)
			{
				Debug.LogError("Network: 内部ネットワークハンドラが設定されていません。設定を準備できません。");
				return null;
			}
			_preparedSettings = _internalNetworkHandler.SettingsFactory.CreateSettings();
			Debug.Log("Network: ルーム設定が準備されました。_preparedRoomSettings をカスタマイズしてください。");
			return _preparedSettings; // カスタマイズのために設定オブジェクトを返す
		}

		// --------------------------------------------------------------------------------
		// Unity ライフサイクル (Networkの初期化とイベント購読)
		// --------------------------------------------------------------------------------

		private void Awake()
		{
			// _internalNetworkHandler は InitializeLibrary を通じて設定されるようになりました。
			// Awake は他の MonoBehaviour 初期化に必要であれば使用できます。
		}

		private void OnDestroy()
		{
			// ハンドラがまだアクティブな場合にイベント購読が解除されることを保証します。
			if (_internalNetworkHandler != null)
			{
				UnsubscribeFromInternalHandlerEvents();
				// 注意: OnDestroy でのハンドラの非同期シャットダウンは複雑です。
				// オブジェクトが破棄される前に ShutdownLibrary が明示的に呼び出されるのが最善です。
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
			if (_internalNetworkHandler != null)
			{
				Debug.LogWarning("Network: 既存のネットワークハンドラがアクティブです。新しいハンドラを初期化する前にシャットダウンします。");
				await ShutdownLibrary(); // これによりイベント購読も解除され、_internalNetworkHandler が null になります
			}

			if (handlerToUse == null)
			{
				Debug.LogError("Network: 提供されたネットワークハンドラが null です。初期化に失敗しました。");
				_internalNetworkHandler = null; // null であることを保証
				return false;
			}

			_internalNetworkHandler = handlerToUse;
			Debug.Log("Network: ライブラリを非同期で初期化中..."); // ★ Debug.Log のメッセージ変更

			SubscribeToInternalHandlerEvents();

			bool success = await _internalNetworkHandler.Initialize();
			Debug.Log($"Network: ライブラリ初期化 {(success ? "成功" : "失敗")}."); // ★ Debug.Log のメッセージ変更

			if (!success)
			{
				UnsubscribeFromInternalHandlerEvents();
				_internalNetworkHandler = null; // 失敗時にクリーンアップ
				return false;
			}

			// ★ 初期化成功時にAccountId, NickName, StationId を取得
			if (success)
			{
				AccountId = _internalNetworkHandler.AccountId;
				NickName = _internalNetworkHandler.NickName;
				StationId = _internalNetworkHandler.StationId;
				Debug.Log($"Network: 初期化完了. AccountId: {AccountId}, NickName: {NickName}, StationId: {StationId}");
			}

			return success;
		}

		public async UniTask ShutdownLibrary()
		{
			Debug.Log("Network: ライブラリを非同期で終了中..."); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null)
			{
				Debug.Log("Network: シャットダウンする内部ネットワークハンドラがありません。");
				return;
			}
			await _internalNetworkHandler.Shutdown();
			UnsubscribeFromInternalHandlerEvents();

			IsConnected = false;
			IsHost = false;
			ConnectedList.Clear();
			DisconnectedList.Clear();
			AccountId = null;
			NickName = null;
			StationId = null;
			_internalNetworkHandler = null; // ハンドラを解放
			Debug.Log("Network: ライブラリ終了完了."); // ★ Debug.Log のメッセージ変更
		}

		public async UniTask<bool> ConnectNetwork(string userId, string userName)
		{
			Debug.Log($"Network: ネットワークに非同期で接続中... UserID: {userId}, UserName: {userName}"); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return false;

			bool success = await _internalNetworkHandler.Connect(userId, userName);
			if (success)
			{
				// ★ NetworkHandler から取得するように変更
				AccountId = _internalNetworkHandler.AccountId;
				NickName = _internalNetworkHandler.NickName;
				StationId = _internalNetworkHandler.StationId; // StationId は Initialize で既に設定されているが、念のため

				Debug.Log($"Network: ネットワーク接続完了. StationId: {StationId}"); // ★ Debug.Log のメッセージ変更
			}
			else
			{
				Debug.LogError("Network: ネットワーク接続失敗."); // ★ Debug.Log のメッセージ変更
			}
			return success;
		}

		public async UniTask DisconnectNetwork()
		{
			Debug.Log("Network: ネットワークから非同期で切断中..."); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return;

			await _internalNetworkHandler.Disconnect();
			IsHost = false;
			ConnectedList.Clear();
			DisconnectedList.Clear();
			// ★ 切断時に情報をクリア
			AccountId = null;
			NickName = null;
			StationId = null;
			Debug.Log("Network: ネットワーク切断完了."); // ★ Debug.Log のメッセージ変更
		}

		public async UniTask<bool> SwitchNetwork(string newUserId, string newUserName)
		{
			Debug.Log($"Network: ネットワークを切り替えます。新しいユーザー: {newUserName} ({newUserId})"); // ★ Debug.Log のメッセージ変更
			if (IsConnected)
			{
				Debug.Log("Network: 現在のネットワークを切断中..."); // ★ Debug.Log のメッセージ変更
				await DisconnectNetwork();
				Debug.Log("Network: 現在のネットワーク切断完了。"); // ★ Debug.Log のメッセージ変更
			}
			else
			{
				Debug.Log("Network: 現在ネットワークに接続していません。直接新しい接続に進みます。"); // ★ Debug.Log のメッセージ変更
			}

			Debug.Log("Network: 新しいネットワークに接続中..."); // ★ Debug.Log のメッセージ変更
			bool success = await ConnectNetwork(newUserId, newUserName);

			if (success)
			{
				Debug.Log("Network: ネットワーク切り替え完了."); // ★ Debug.Log のメッセージ変更
			}
			else
			{
				Debug.LogError("Network: ネットワーク切り替え失敗."); // ★ Debug.Log のメッセージ変更
			}
			return success;
		}

		// これらのメソッドは IInternalNetworkHandler からのイベントを処理し、
		// この Network クラスの公開イベントを発行します。
		private void InternalHandleNetworkConnectionStatusChanged(bool isConnected)
		{
			IsConnected = isConnected;
			OnNetworkConnectionStatusChanged?.Invoke(isConnected);
		}

		private void InternalHandleHostStatusChanged(bool isHost)
		{
			IsHost = isHost;
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
	}
}