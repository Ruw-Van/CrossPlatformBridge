// Assets/Scripts/CrossPlatformBridge/Network/DummyNetworkHandler/DummyNetworkHandler.Core.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Network.DummyNetworkHandler
{
	/// <summary>
	/// IInternalNetworkHandler のダミー実装のコア部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class DummyNetworkHandler : IInternalNetworkHandler
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
		// プロパティ (IInternalNetworkHandler)
		// --------------------------------------------------------------------------------
		public string AccountId { get; private set; }
		public string NickName { get; private set; }
		public string StationId { get; private set; }

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
		public DummySettings DefaultRoomSettings { get; set; } = new DummySettings();

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
		public DummyNetworkHandler()
		{
			Debug.Log("DummyNetworkHandler: インスタンスが作成されました。");
		}

		public async UniTask<bool> Initialize()
		{
			Debug.Log("DummyNetworkHandler: 初期化中...");
			await UniTask.Delay(100); // 初期化のシミュレーション
			_isConnected = true;
			AccountId = "dummyUser_" + Guid.NewGuid().ToString().Substring(0, 8);
			NickName = "DummyPlayer";
			StationId = "dummyStation_" + Guid.NewGuid().ToString().Substring(0, 8);
			OnNetworkConnectionStatusChanged?.Invoke(true);
			Debug.Log($"DummyNetworkHandler: 初期化完了. AccountId: {AccountId}, NickName: {NickName}, StationId: {StationId}");
			return true;
		}

		public async UniTask Shutdown()
		{
			Debug.Log("DummyNetworkHandler: シャットダウン中...");
			await UniTask.Delay(100); // シャットダウンのシミュレーション
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

		public async UniTask<bool> Connect(string userId, string userName)
		{
			Debug.Log($"DummyNetworkHandler: 接続中... UserID: {userId}, UserName: {userName}");
			await UniTask.Delay(200); // 接続のシミュレーション
			if (_isConnected)
			{
				Debug.Log("DummyNetworkHandler: 既に接続済みです。");
				return true;
			}

			_isConnected = true;
			AccountId = userId;
			NickName = userName;
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
	}
}
