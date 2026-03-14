#if USE_CROSSPLATFORMBRIDGE_PHOTONFUSION
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Testing;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace CrossPlatformBridge.Platform.PhotonFusion.Network
{
	/// <summary>
	/// PhotonFusion用のネットワークコア機能を実装するクラス。
	/// </summary>
	public partial class NetworkHandler : IInternalNetworkHandler, INetworkRunnerCallbacks, IServiceTestProvider
	{
		private NetworkRunner _runner;
		private NetworkObject _localPlayer;

		// LocalPlayer.RawEncoded を文字列化して AccountId に使用
		public object AccountId { get; private set; }
		public string NickName { get; private set; }
		// セッション名（ロビー名 / ルーム名）を StationId として保持
		public object StationId { get; private set; }

		public INetworkSettingsFactory SettingsFactory { get; } = new PhotonFusionSettingsFactory();
		public bool IsConnected { get; private set; } = false;
		public bool IsHost { get; private set; } = false;
		public List<PlayerData> ConnectedList { get; } = new List<PlayerData>();
		public List<PlayerData> DisconnectedList { get; } = new List<PlayerData>();

		public event Action<byte[], string> OnDataReceived;
		public event Action<string, string> OnPlayerConnected;
		public event Action<string, string> OnPlayerDisconnected;
		public event Action<bool> OnNetworkConnectionStatusChanged;
		public event Action<bool> OnHostStatusChanged;
		public event Action<string, bool, string> OnLobbyOperationCompleted;
		public event Action<string, bool, string> OnRoomOperationCompleted;

		public bool Initialize(INetworkSettings settings)
		{
			if (_runner == null)
			{
				var runnerObj = new GameObject("FusionRunner");
				_runner = runnerObj.AddComponent<NetworkRunner>();
				_runner.ProvideInput = true;
				_runner.AddCallbacks(this);
				UnityEngine.Object.DontDestroyOnLoad(runnerObj);
				Debug.Log("PhotonFusion: NetworkRunner生成");
			}
			return true;
		}

		public void Shutdown()
		{
			if (_runner != null)
			{
				_runner.RemoveCallbacks(this);
				UnityEngine.Object.Destroy(_runner.gameObject);
				_runner = null;
			}
			IsConnected = false;
			IsHost = false;
			AccountId = null;
			NickName = null;
			StationId = null;
			ConnectedList.Clear();
			DisconnectedList.Clear();
			Debug.Log("PhotonFusion: Shutdown完了");
		}

		public async UniTask<bool> Connect(INetworkSettings baseSettings)
		{
			if (_runner == null)
			{
				Debug.LogError("PhotonFusion: Runnerが初期化されていません");
				return false;
			}
			// Fusion では StartGame でセッションに参加するとロビー購読ができなくなるため、
			// Connect では JoinSessionLobby で Master Server に接続するだけにする。
			// CreateLobby/CreateRoom でそれぞれ適切な操作を行う。
			var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
			if (result.Ok)
			{
				IsConnected = true;
				AccountId = _runner.LocalPlayer.RawEncoded.ToString();
				NickName = _runner.LocalPlayer.ToString();
				Debug.Log($"PhotonFusion: 接続完了 AccountId={AccountId}");
			}
			else
			{
				IsConnected = false;
				Debug.LogError($"PhotonFusion: 接続失敗 reason={result.ShutdownReason}");
			}
			OnNetworkConnectionStatusChanged?.Invoke(IsConnected);
			return IsConnected;
		}

		public async UniTask Disconnect()
		{
			if (_runner != null)
			{
				await _runner.Shutdown();
			}
			IsConnected = false;
			IsHost = false;
			AccountId = null;
			NickName = null;
			StationId = null;
			ConnectedList.Clear();
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("PhotonFusion: 切断完了");
		}

		public void UpdateState() { }

		// --- INetworkRunnerCallbacks ---

		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
		{
			Debug.Log($"PhotonFusion: PlayerJoined player={player}");
			string playerId = player.RawEncoded.ToString();
			string playerName = player.ToString();
			if (!ConnectedList.Exists(p => p.Id == playerId))
			{
				ConnectedList.Add(new PlayerData { Id = playerId, Name = playerName });
				OnPlayerConnected?.Invoke(playerId, playerName);
			}
		}

		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
		{
			Debug.Log($"PhotonFusion: PlayerLeft player={player}");
			string playerId = player.RawEncoded.ToString();
			string playerName = player.ToString();
			var found = ConnectedList.Find(p => p.Id == playerId);
			if (found != null)
			{
				ConnectedList.Remove(found);
				DisconnectedList.Add(found);
			}
			OnPlayerDisconnected?.Invoke(playerId, playerName);
		}

		public void OnConnectedToServer(NetworkRunner runner)
		{
			Debug.Log("PhotonFusion: OnConnectedToServer");
			IsConnected = true;
			AccountId = runner.LocalPlayer.RawEncoded.ToString();
			NickName = runner.LocalPlayer.ToString();
			StationId = runner.SessionInfo?.Name;
			OnNetworkConnectionStatusChanged?.Invoke(true);
		}

		public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
		{
			Debug.Log($"PhotonFusion: OnDisconnectedFromServer reason={reason}");
			IsConnected = false;
			IsHost = false;
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
		}

		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
		{
			Debug.Log($"PhotonFusion: OnShutdown reason={shutdownReason}");
			IsConnected = false;
			IsHost = false;
		}

		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
		{
			Debug.Log($"PhotonFusion: OnSessionListUpdated count={sessionList.Count}");
			roomList.Update(sessionList);
		}

		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
		{
			Debug.LogError($"PhotonFusion: OnConnectFailed reason={reason}");
		}

		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
		{
			Debug.Log($"PhotonFusion: ReliableDataReceived from={player}, size={data.Count}");
			OnDataReceived?.Invoke(data.ToArray(), player.RawEncoded.ToString());
		}

		public void OnInput(NetworkRunner runner, NetworkInput input) { }
		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
		public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
		public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnDisconnectedFromServer(NetworkRunner runner) { }

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => null;

		public TestDefaultData GetDefaultData() => new TestDefaultData { UserName = "FusionPlayer", LobbyRoomName = "FusionLobby", SendData = "Hello from Fusion!" };
	}
}

#endif
