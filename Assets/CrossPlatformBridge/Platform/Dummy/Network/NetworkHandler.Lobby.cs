using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Platform.Dummy.Network
{
	/// <summary>
	/// IInternalNetworkHandler のダミー実装のロビー操作部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class NetworkHandler : IInternalNetworkHandler
	{
		// --------------------------------------------------------------------------------
		// プライベートヘルパー（OnLobbyOperationCompleted / OnRoomOperationCompleted を発火しない）
		// Lobby / Room の両メソッドから共通利用する
		// --------------------------------------------------------------------------------

		private async UniTask<string> CreateSessionCoreAsync(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			await UniTask.Delay(300, cancellationToken: cancellationToken); // セッション作成のシミュレーション

			// DefaultRoomSettings を利用してロビー設定をシミュレート
			Debug.Log($"DummyNetworkHandler: ロビー設定 - MaxPlayers: {DefaultRoomSettings.MaxPlayers}, IsVisible: {DefaultRoomSettings.IsVisible}, IsOpen: {DefaultRoomSettings.IsOpen}");
			foreach (var prop in DefaultRoomSettings.CustomProperties)
			{
				Debug.Log($"DummyNetworkHandler: カスタムプロパティ - {prop.Key}: {prop.Value}");
			}

			_currentLobbyId = baseSettings.RoomName + "_" + Guid.NewGuid().ToString().Substring(0, 4);
			_isHost = true;
			StationId = _currentLobbyId;
			_connectedPlayers.Clear();
			_connectedPlayers.Add(AccountId.ToString()); // ホスト自身を追加

			Debug.Log($"DummyNetworkHandler: セッション '{baseSettings.RoomName}' (ID: {_currentLobbyId}) を作成しました。");
			OnHostStatusChanged?.Invoke(true);
			OnPlayerConnected?.Invoke(AccountId.ToString(), NickName); // ホスト自身が接続したことを通知
			return _currentLobbyId;
		}

		private async UniTask DisconnectSessionCoreAsync()
		{
			await UniTask.Delay(200); // 切断のシミュレーション
			_currentLobbyId = null;
			_isHost = false;
			StationId = null;
			_connectedPlayers.Clear();
			OnHostStatusChanged?.Invoke(false);
		}

		private async UniTask<string> ConnectSessionCoreAsync(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			await UniTask.Delay(300, cancellationToken: cancellationToken); // セッション接続のシミュレーション

			if (_currentLobbyId != null) // 既にセッションにいる場合
			{
				Debug.LogWarning("DummyNetworkHandler: 既に別のセッションに接続しています。");
				await DisconnectSessionCoreAsync();
			}

			_currentLobbyId = baseSettings.Id.ToString();
			_isHost = false;
			StationId = _currentLobbyId;
			_connectedPlayers.Add(AccountId.ToString()); // 自分自身を追加

			Debug.Log($"DummyNetworkHandler: セッション '{baseSettings.RoomName}' に接続しました。");
			OnHostStatusChanged?.Invoke(false);
			OnPlayerConnected?.Invoke(AccountId.ToString(), NickName); // 自分自身が接続したことを通知
			return _currentLobbyId;
		}

		// --------------------------------------------------------------------------------
		// ロビー操作（公開メソッド）
		// --------------------------------------------------------------------------------

		public async UniTask<bool> CreateLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			Debug.Log($"DummyNetworkHandler: ロビー '{baseSettings.RoomName}' を作成中...");
			if (!_isConnected)
			{
				Debug.LogError("DummyNetworkHandler: 接続されていません。ロビーを作成できません。");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Not connected.");
				return false;
			}
			try
			{
				string sessionId = await CreateSessionCoreAsync(baseSettings, cancellationToken);
				OnLobbyOperationCompleted?.Invoke("CreateLobby", true, sessionId);
				return true;
			}
			catch (OperationCanceledException)
			{
				Debug.Log("DummyNetworkHandler: ロビー作成がキャンセルされました。");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Cancelled");
				return false;
			}
		}

		public async UniTask<bool> ConnectLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			Debug.Log($"DummyNetworkHandler: ロビー '{baseSettings.RoomName}' に接続中...");
			if (!_isConnected)
			{
				Debug.LogError("DummyNetworkHandler: 接続されていません。ロビーに接続できません。");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Not connected.");
				return false;
			}
			try
			{
				string sessionId = await ConnectSessionCoreAsync(baseSettings, cancellationToken);
				Debug.Log($"DummyNetworkHandler: ロビー '{baseSettings.RoomName}' に接続しました。");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", true, sessionId);
				return true;
			}
			catch (OperationCanceledException)
			{
				Debug.Log("DummyNetworkHandler: ロビー接続がキャンセルされました。");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Cancelled");
				return false;
			}
		}

		public async UniTask DisconnectLobby()
		{
			Debug.Log("DummyNetworkHandler: ロビーから切断中...");
			await DisconnectSessionCoreAsync();
			OnLobbyOperationCompleted?.Invoke("DisconnectLobby", true, "");
			Debug.Log("DummyNetworkHandler: ロビー切断完了。");
		}

		public async UniTask<List<object>> SearchLobby(IRoomSettings baseSettings)
		{
			Debug.Log($"DummyNetworkHandler: ロビーを検索中... クエリ: '{baseSettings.RoomName}'");
			await UniTask.Delay(150); // 検索のシミュレーション

			var dummyLobbies = new List<RoomSettings>
			{
				new RoomSettings { Id = "DummyLobby1", RoomName = "DummyLobby1", MaxPlayers = 4 },
				new RoomSettings
				{
					Id = "DummyLobby2",
					RoomName = "DummyLobby2",
					MaxPlayers = 4,
					CustomProperties = new Dictionary<string, object> { { "gameMode", "Battle" } },
				},
				new RoomSettings { Id = "TestLobby", RoomName = "TestLobby", MaxPlayers = 2 },
			};

			List<object> results = new();
			foreach (var lobby in dummyLobbies)
			{
				if (!string.IsNullOrEmpty(baseSettings.RoomName) && !lobby.RoomName.Contains(baseSettings.RoomName))
					continue;

				if (baseSettings.CustomProperties != null && baseSettings.CustomProperties.Count > 0)
				{
					bool match = true;
					foreach (var kv in baseSettings.CustomProperties)
					{
						if (!lobby.CustomProperties.TryGetValue(kv.Key, out var val) ||
							val?.ToString() != kv.Value?.ToString())
						{
							match = false;
							break;
						}
					}
					if (!match) continue;
				}

				results.Add(lobby);
			}
			Debug.Log($"DummyNetworkHandler: ロビー検索完了。{results.Count} 件見つかりました。");
			return results;
		}

		public async UniTask<bool> MatchmakeLobby(IRoomSettings conditions, CancellationToken cancellationToken = default)
		{
			Debug.Log($"DummyNetworkHandler: ロビーをマッチメイキング中... クエリ: '{conditions.RoomName}'");
			var results = await SearchLobby(conditions);
			if (results == null || results.Count == 0)
			{
				Debug.Log("DummyNetworkHandler: マッチするロビーが見つかりませんでした。");
				OnLobbyOperationCompleted?.Invoke("MatchmakeLobby", false, "マッチするロビーが見つかりませんでした。");
				return false;
			}
			if (results[0] is not IRoomSettings target)
			{
				OnLobbyOperationCompleted?.Invoke("MatchmakeLobby", false, "検索結果を IRoomSettings に変換できません。");
				return false;
			}
			return await ConnectLobby(target, cancellationToken);
		}
	}
}
