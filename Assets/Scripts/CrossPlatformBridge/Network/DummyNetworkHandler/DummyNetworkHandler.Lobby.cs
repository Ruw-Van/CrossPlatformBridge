// Assets/Scripts/CrossPlatformBridge/Network/DummyNetworkHandler/DummyNetworkHandler.Lobby.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Network.DummyNetworkHandler
{
	/// <summary>
	/// IInternalNetworkHandler のダミー実装のロビー操作部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class DummyNetworkHandler : IInternalNetworkHandler
	{
		public async UniTask<bool> CreateLobby(string lobbyName, INetworkSettings settings)
		{
			Debug.Log($"DummyNetworkHandler: ロビー '{lobbyName}' を作成中...");
			await UniTask.Delay(300); // ロビー作成のシミュレーション

			if (!_isConnected)
			{
				Debug.LogError("DummyNetworkHandler: 接続されていません。ロビーを作成できません。");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Not connected.");
				return false;
			}

			// DefaultRoomSettings を利用してロビー設定をシミュレート
			Debug.Log($"DummyNetworkHandler: ロビー設定 - MaxPlayers: {DefaultRoomSettings.MaxPlayers}, IsVisible: {DefaultRoomSettings.IsVisible}, IsOpen: {DefaultRoomSettings.IsOpen}");
			foreach (var prop in DefaultRoomSettings.CustomProperties)
			{
				Debug.Log($"DummyNetworkHandler: カスタムプロパティ - {prop.Key}: {prop.Value}");
			}

			_currentLobbyId = lobbyName + "_" + Guid.NewGuid().ToString().Substring(0, 4);
			_isHost = true;
			StationId = _currentLobbyId;
			_connectedPlayers.Clear();
			_connectedPlayers.Add(AccountId); // ホスト自身を追加

			Debug.Log($"DummyNetworkHandler: ロビー '{lobbyName}' (ID: {_currentLobbyId}) を作成しました。");
			OnLobbyOperationCompleted?.Invoke("CreateLobby", true, _currentLobbyId);
			OnHostStatusChanged?.Invoke(true);
			OnPlayerConnected?.Invoke(AccountId, NickName); // ホスト自身が接続したことを通知
			return true;
		}

		public async UniTask<bool> ConnectLobby(string lobbyId)
		{
			Debug.Log($"DummyNetworkHandler: ロビー '{lobbyId}' に接続中...");
			await UniTask.Delay(300); // ロビー接続のシミュレーション

			if (!_isConnected)
			{
				Debug.LogError("DummyNetworkHandler: 接続されていません。ロビーに接続できません。");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Not connected.");
				return false;
			}

			if (_currentLobbyId != null) // 既にロビーにいる場合
			{
				Debug.LogWarning("DummyNetworkHandler: 既に別のロビーに接続しています。");
				await DisconnectLobby();
			}

			_currentLobbyId = lobbyId;
			_isHost = false;
			StationId = _currentLobbyId;
			_connectedPlayers.Add(AccountId); // 自分自身を追加

			Debug.Log($"DummyNetworkHandler: ロビー '{lobbyId}' に接続しました。");
			OnLobbyOperationCompleted?.Invoke("ConnectLobby", true, lobbyId);
			OnHostStatusChanged?.Invoke(false);
			OnPlayerConnected?.Invoke(AccountId, NickName); // 自分自身が接続したことを通知
			return true;
		}

		public async UniTask DisconnectLobby()
		{
			Debug.Log("DummyNetworkHandler: ロビーから切断中...");
			await UniTask.Delay(200); // 切断のシミュレーション
			_currentLobbyId = null;
			_isHost = false;
			StationId = null;
			_connectedPlayers.Clear();
			OnLobbyOperationCompleted?.Invoke("DisconnectLobby", true, "");
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("DummyNetworkHandler: ロビー切断完了。");
		}

		public async UniTask<List<string>> SearchLobby(string query = "")
		{
			Debug.Log($"DummyNetworkHandler: ロビーを検索中... クエリ: '{query}'");
			await UniTask.Delay(150); // 検索のシミュレーション
			List<string> dummyLobbies = new List<string>
			{
				"DummyLobby1 (1/4)",
				"DummyLobby2 (3/4)",
				"TestLobby (0/2)"
			};

			List<string> results = new List<string>();
			foreach (var lobby in dummyLobbies)
			{
				if (string.IsNullOrEmpty(query) || lobby.Contains(query))
				{
					results.Add(lobby);
				}
			}
			Debug.Log($"DummyNetworkHandler: ロビー検索完了。{results.Count} 件見つかりました。");
			return results;
		}
	}
}
