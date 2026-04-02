#if USE_CROSSPLATFORMBRIDGE_NETCODE
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using CrossPlatformBridge.Services.Network;

namespace CrossPlatformBridge.Platform.Netcode.Network
{
	public partial class NetworkHandler : IInternalNetworkHandler
	{
		// --------------------------------------------------------------------------------
		// Lobby 追跡用ローカル状態
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 現在の Lobby ホストの PlayerId キャッシュ。
		/// HostId.Changed イベントで更新される。
		/// </summary>
		private string _currentLobbyHostId = null;

		/// <summary>
		/// Lobby.Players の順序を保持する PlayerId リスト。
		/// PlayerLeft の slotIndex から退出プレイヤーを特定するために使用。
		/// </summary>
		private List<string> _lobbyPlayerIdsBySlot = new List<string>();

		// --------------------------------------------------------------------------------
		// CreateRoom / ConnectRoom / DisconnectRoom / SearchRoom
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 新しいネットワークセッション（ルーム）を作成します。
		/// Relay + Lobby の組み合わせで実装します。
		/// </summary>
		public async UniTask<bool> CreateRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (!_isAuthenticated)
			{
				Debug.LogError("NetcodeNetworkHandler: 認証されていません。ルームを作成できません。");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Not authenticated.");
				return false;
			}

			Debug.Log($"NetcodeNetworkHandler: ルーム '{baseSettings.RoomName}' を作成中...");

			if (_connectedLobby != null)
			{
				Debug.LogWarning("NetcodeNetworkHandler: 既に接続済みのルームがあります。");
				return true;
			}

			RoomSettings settings = baseSettings as RoomSettings ?? new RoomSettings(baseSettings);

			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				// 1. Relay 割り当て取得
				_allocation = await RelayService.Instance.CreateAllocationAsync(settings.MaxPlayers);
				_joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);

				// 2. UTP にホスト用 Relay 設定を適用
				var utpTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
				utpTransport.SetRelayServerData(
					_allocation.RelayServer.IpV4,
					(ushort)_allocation.RelayServer.Port,
					_allocation.AllocationIdBytes,
					_allocation.Key,
					_allocation.ConnectionData
				);

				// 3. NetworkManager をホストとして起動
				if (!NetworkManager.Singleton.StartHost())
				{
					Debug.LogError("NetcodeNetworkHandler: NetworkManager ホスト起動失敗。");
					OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Failed to start NetworkManager host.");
					return false;
				}

				// 4. Lobby サービスでルームを作成（JoinCode を Data に埋め込む）
				CreateLobbyOptions createLobbyOptions = settings.ToCreateLobbyOptions();
				createLobbyOptions.Data["JoinCode"] = new DataObject(DataObject.VisibilityOptions.Member, _joinCode);
				createLobbyOptions.Data["HostId"] = new DataObject(DataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId);

				_connectedLobby = await LobbyService.Instance.CreateLobbyAsync(
					settings.RoomName, settings.MaxPlayers, createLobbyOptions);
				await SubscribeToLobbyEvents(_connectedLobby.Id);
				StationId = _connectedLobby.Id;

				// Lobby プレイヤースロット追跡を初期化
					_currentLobbyHostId = _connectedLobby.HostId;
					_lobbyPlayerIdsBySlot.Clear();
					foreach (var p in _connectedLobby.Players)
						_lobbyPlayerIdsBySlot.Add(p.Id);

					foreach (var lobbyPlayer in _connectedLobby.Players)
					{
						string playerName = lobbyPlayer.Id;
						if (lobbyPlayer.Data != null
							&& lobbyPlayer.Data.TryGetValue("PlayerName", out var nameObj))
						{
							playerName = nameObj.Value;
						}

						var playerData = new PlayerData
						{
							Id = lobbyPlayer.Id,
							Name = playerName,
							PlayerProperties = new Dictionary<string, object>()
						};
						ConnectedList.Add(playerData);
						OnPlayerConnected?.Invoke(playerData.Id, playerData.Name);
					}

					Debug.Log($"NetcodeNetworkHandler: ルーム '{_connectedLobby.Name}' (ID: {_connectedLobby.Id}) を作成しました。JoinCode: {_joinCode}");
					OnRoomOperationCompleted?.Invoke("CreateRoom", true, _connectedLobby.Id);
					OnHostStatusChanged?.Invoke(true);
				return true;
			}
			catch (OperationCanceledException)
			{
				Debug.Log("NetcodeNetworkHandler: ルーム作成がキャンセルされました。");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Cancelled");
				await DisconnectRoom();
				return false;
			}
			catch (LobbyServiceException e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ルーム作成失敗（Lobby）: {e.Message}");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, e.Message);
				await DisconnectRoom();
				return false;
			}
			catch (Exception e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ルーム作成失敗: {e.Message}");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, e.Message);
				await DisconnectRoom();
				return false;
			}
		}

		/// <summary>
		/// 既存のネットワークセッション（ルーム）に接続します。
		/// </summary>
		public async UniTask<bool> ConnectRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (!_isAuthenticated)
			{
				Debug.LogError("NetcodeNetworkHandler: 認証されていません。ルームに接続できません。");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Not authenticated.");
				return false;
			}

			Debug.Log($"NetcodeNetworkHandler: ルーム '{baseSettings.Id}' に接続中...");

			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				RoomSettings settings = baseSettings as RoomSettings;
				if (settings == null)
				{
					throw new Exception($"{nameof(baseSettings)} is not a valid RoomSettings instance.");
				}

				// 1. Lobby に参加
				JoinLobbyByIdOptions joinOptions = settings.ToJoinLobbyByIdOptions();
				_connectedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(
					baseSettings.Id.ToString(), joinOptions);
				await SubscribeToLobbyEvents(_connectedLobby.Id);
				StationId = _connectedLobby.Id;

				// Lobby プレイヤースロット追跡を初期化
				_currentLobbyHostId = _connectedLobby.HostId;
				_lobbyPlayerIdsBySlot.Clear();
				foreach (var p in _connectedLobby.Players)
					_lobbyPlayerIdsBySlot.Add(p.Id);

					// 参加時点の既存メンバーを ConnectedList に追加（ホスト含む全員）
					// （OnLobbyChanged は参加後の変化のみ通知するため、ここで初期化が必要）
					foreach (var lobbyPlayer in _connectedLobby.Players)
					{
						string playerName = lobbyPlayer.Id;
						if (lobbyPlayer.Data != null
							&& lobbyPlayer.Data.TryGetValue("PlayerName", out var nameObj))
					{
						playerName = nameObj.Value;
					}
					var playerData = new PlayerData
					{
						Id = lobbyPlayer.Id,
						Name = playerName,
						PlayerProperties = new Dictionary<string, object>()
					};
					ConnectedList.Add(playerData);
					OnPlayerConnected?.Invoke(playerData.Id, playerData.Name);
				}

				// 2. Lobby から JoinCode を取得して Relay に参加
				_joinCode = _connectedLobby.Data["JoinCode"].Value;
				var joinAllocation = await RelayService.Instance.JoinAllocationAsync(_joinCode);

				// 3. UTP にクライアント用 Relay 設定を適用
				var utpTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
				utpTransport.SetClientRelayData(
					joinAllocation.RelayServer.IpV4,
					(ushort)joinAllocation.RelayServer.Port,
					joinAllocation.AllocationIdBytes,
					joinAllocation.Key,
					joinAllocation.ConnectionData,
					joinAllocation.HostConnectionData
				);

				// 4. NetworkManager をクライアントとして起動
				if (!NetworkManager.Singleton.StartClient())
				{
					Debug.LogError("NetcodeNetworkHandler: NetworkManager クライアント起動失敗。");
					OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Failed to start NetworkManager client.");
					await DisconnectRoom();
					return false;
				}

				Debug.Log($"NetcodeNetworkHandler: ルーム '{_connectedLobby.Name}' (ID: {_connectedLobby.Id}) に参加しました。JoinCode: {_joinCode}");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", true, _connectedLobby.Id);
				OnHostStatusChanged?.Invoke(false);
				return true;
			}
			catch (OperationCanceledException)
			{
				Debug.Log("NetcodeNetworkHandler: ルーム接続がキャンセルされました。");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Cancelled");
				await DisconnectRoom();
				return false;
			}
			catch (Exception e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ルーム接続失敗: {e.Message}");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, e.Message);
				await DisconnectRoom();
				return false;
			}
		}

		/// <summary>
		/// 現在接続しているネットワークセッション（ルーム）から切断します。
		/// ホストで他のプレイヤーがいる場合は Lobby を削除せず退出のみ行い、
		/// UGS が自動的に次のプレイヤーをホストへ昇格させてホストマイグレーションを行います。
		/// </summary>
		public async UniTask DisconnectRoom()
		{
			Debug.Log("NetcodeNetworkHandler: ルームから切断中...");

			_pendingReconnect = false;

			// NetworkManager をシャットダウン（意図的 Shutdown フラグを立ててコールバックをスキップ）
			if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
			{
				_isIntentionalShutdown = true;
				NetworkManager.Singleton.Shutdown();
				Debug.Log("NetcodeNetworkHandler: NetworkManager をシャットダウンしました。");
			}

			if (_connectedLobby != null)
			{
				try
				{
					bool isLobbyHost = _connectedLobby.HostId == AuthenticationService.Instance.PlayerId;

					string myPlayerId = AuthenticationService.Instance.PlayerId;
					bool hasOtherPlayers = ConnectedList.Exists(p => p.Id != myPlayerId);
					if (isLobbyHost && hasOtherPlayers)
					{
						// ホストが退室するが他にプレイヤーがいる
						// → Lobby を削除せず退出のみ。UGS が自動的に新ホストへ昇格させる
						await LobbyService.Instance.RemovePlayerAsync(
							_connectedLobby.Id, AuthenticationService.Instance.PlayerId);
						Debug.Log($"NetcodeNetworkHandler: ホストが退出しました（Lobby は維持、ホストマイグレーション発動）。");
					}
					else if (isLobbyHost)
					{
						// ホストのみで他にプレイヤーがいない → Lobby ごと削除
						await LobbyService.Instance.DeleteLobbyAsync(_connectedLobby.Id);
						Debug.Log($"NetcodeNetworkHandler: ルーム '{_connectedLobby.Name}' を削除しました。");
					}
					else
					{
						// クライアントが退室
						await LobbyService.Instance.RemovePlayerAsync(
							_connectedLobby.Id, AuthenticationService.Instance.PlayerId);
						Debug.Log($"NetcodeNetworkHandler: ルーム '{_connectedLobby.Name}' を退出しました。");
					}
				}
				catch (LobbyServiceException e)
				{
					Debug.LogError($"NetcodeNetworkHandler: ルーム切断（Lobby）失敗: {e.Message}");
				}
				finally
				{
					UnsubscribeToLobbyEvents();
					_connectedLobby = null;
					_currentLobbyHostId = null;
					_lobbyPlayerIdsBySlot.Clear();
					StationId = null;
					ConnectedList.Clear();
					OnRoomOperationCompleted?.Invoke("DisconnectRoom", true, "");
				}
			}
			else
			{
				Debug.Log("NetcodeNetworkHandler: 接続中のルームがありません。");
				OnRoomOperationCompleted?.Invoke("DisconnectRoom", true, "");
			}
		}

		/// <summary>
		/// ロビーをマッチメイキングします。
		/// Netcode ではロビーとルームが統合されているため MatchmakeRoom に委譲します。
		/// </summary>
		public UniTask<bool> MatchmakeLobby(IRoomSettings conditions, CancellationToken cancellationToken = default)
			=> MatchmakeRoom(conditions, false, cancellationToken);

		/// <summary>
		/// ルームをマッチメイキングします。
		/// SearchRoom で条件に合うルームを検索し、最初のルームに接続します。
		/// createIfNotFound が true の場合、見つからなければ新規作成します。
		/// </summary>
		public async UniTask<bool> MatchmakeRoom(IRoomSettings conditions, bool createIfNotFound = false, CancellationToken cancellationToken = default)
		{
			Debug.Log($"NetcodeNetworkHandler: ルームをマッチメイキング中... クエリ: '{conditions.RoomName}'");
			var results = await SearchRoom(conditions);
			if (results != null && results.Count > 0 && results[0] is IRoomSettings target)
				return await ConnectRoom(target, cancellationToken);

			if (createIfNotFound)
			{
				Debug.Log("NetcodeNetworkHandler: マッチするルームが見つからないため新規作成します。");
				return await CreateRoom(conditions, cancellationToken);
			}

			OnRoomOperationCompleted?.Invoke("MatchmakeRoom", false, "マッチするルームが見つかりませんでした。");
			return false;
		}

		/// <summary>
		/// 利用可能なネットワークセッション（ルーム）を検索します。
		/// </summary>
		public async UniTask<List<object>> SearchRoom(IRoomSettings baseSettings)
		{
			Debug.Log($"NetcodeNetworkHandler: ルームを検索中... クエリ: '{baseSettings.RoomName}'");

			if (!AuthenticationService.Instance.IsSignedIn)
			{
				Debug.LogError("NetcodeNetworkHandler: 認証されていません。ルームを検索できません。");
				return new List<object>();
			}

			try
			{
				var queryOptions = new QueryLobbiesOptions
				{
					Count = 100,
					Filters = new List<QueryFilter>(),
				};

				if (!string.IsNullOrEmpty(baseSettings.RoomName))
				{
					queryOptions.Filters.Add(new QueryFilter(
						QueryFilter.FieldOptions.Name,
						baseSettings.RoomName,
						QueryFilter.OpOptions.CONTAINS));
				}

				QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
				var lobbies = new List<object>();
				foreach (Lobby lobby in queryResponse.Results)
				{
					if (lobby.IsLocked || lobby.IsPrivate) continue;

					// CustomProperties クライアント側フィルタ
					// （UGS QueryFilter のカスタムフィールドは S1-S5 固定スロット制限があるためクライアント側で対応）
					if (baseSettings.CustomProperties != null && baseSettings.CustomProperties.Count > 0)
					{
						bool propMatch = true;
						foreach (var kv in baseSettings.CustomProperties)
						{
							if (!lobby.Data.TryGetValue(kv.Key, out var dataObj) ||
								dataObj.Value != kv.Value?.ToString())
							{
								propMatch = false;
								break;
							}
						}
						if (!propMatch) continue;
					}

					lobbies.Add(new RoomSettings
					{
						Id = lobby.Id,
						RoomName = lobby.Name,
						IsOpen = true,
						MaxPlayers = lobby.MaxPlayers,
					});
				}
				Debug.Log($"NetcodeNetworkHandler: ルーム検索完了。{lobbies.Count} 件見つかりました。");
				return lobbies;
			}
			catch (LobbyServiceException e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ルーム検索失敗（Lobby）: {e.Message}");
				return new List<object>();
			}
			catch (Exception e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ルーム検索失敗: {e.Message}");
				return new List<object>();
			}
		}

		// --------------------------------------------------------------------------------
		// ホストマイグレーション
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 自身が新しい Lobby ホストになった際に呼ばれます。
		/// 新しい Relay セッションを作成し、Lobby の JoinCode を更新します。
		/// 他クライアントは JoinCode 変更イベント（OnLobbyChanged）で自動再接続します。
		/// </summary>
		private async UniTask HandleHostMigration()
		{
			Debug.Log("NetcodeNetworkHandler: ホストマイグレーション処理を開始します...");
			_pendingReconnect = false;

			try
			{
				// 古い Netcode セッションを停止（クライアントとして接続していた）
				if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
				{
					_isIntentionalShutdown = true;
					NetworkManager.Singleton.Shutdown();
					// シャットダウン完了を少し待つ
					await UniTask.Delay(500);
				}

				int maxPlayers = _connectedLobby?.MaxPlayers ?? 4;

				// 1. 新しい Relay 割り当てを作成
				_allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
				_joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);

				// 2. UTP にホスト用 Relay 設定を適用
				var utpTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
				utpTransport.SetRelayServerData(
					_allocation.RelayServer.IpV4,
					(ushort)_allocation.RelayServer.Port,
					_allocation.AllocationIdBytes,
					_allocation.Key,
					_allocation.ConnectionData
				);

				// 3. NetworkManager をホストとして起動
				if (!NetworkManager.Singleton.StartHost())
				{
					Debug.LogError("NetcodeNetworkHandler: ホストマイグレーション - NetworkManager ホスト起動失敗。");
					return;
				}

				// 4. Lobby の JoinCode と HostId を更新（他クライアントへ通知）
				var updateOptions = new UpdateLobbyOptions
				{
					Data = new Dictionary<string, DataObject>
					{
						["JoinCode"] = new DataObject(DataObject.VisibilityOptions.Member, _joinCode),
						["HostId"]   = new DataObject(DataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId),
					}
				};
				_connectedLobby = await LobbyService.Instance.UpdateLobbyAsync(_connectedLobby.Id, updateOptions);
				_currentLobbyHostId = _connectedLobby.HostId;

				// 5. ConnectedList を現在の Lobby メンバーで再構築（ホスト含む全員）
				ConnectedList.Clear();
				_lobbyPlayerIdsBySlot.Clear();
				foreach (var lobbyPlayer in _connectedLobby.Players)
				{
					_lobbyPlayerIdsBySlot.Add(lobbyPlayer.Id);

					string playerName = lobbyPlayer.Id;
					if (lobbyPlayer.Data != null
						&& lobbyPlayer.Data.TryGetValue("PlayerName", out var nameObj))
					{
						playerName = nameObj.Value;
					}
					ConnectedList.Add(new PlayerData
					{
						Id = lobbyPlayer.Id,
						Name = playerName,
						PlayerProperties = new Dictionary<string, object>()
					});
				}

				Debug.Log($"NetcodeNetworkHandler: ホストマイグレーション完了。新 JoinCode: {_joinCode}");
				OnHostStatusChanged?.Invoke(true);
			}
			catch (Exception e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ホストマイグレーション失敗: {e.Message}");
			}
		}

		/// <summary>
		/// ホストマイグレーション後に新しい JoinCode で再接続します。
		/// </summary>
		private async UniTask ReconnectAfterHostMigration()
		{
			_pendingReconnect = false;
			Debug.Log($"NetcodeNetworkHandler: ホストマイグレーション後の再接続開始。JoinCode: {_joinCode}");

			try
			{
				// 古い Netcode セッションを停止（まだ残っていれば）
				if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
				{
					_isIntentionalShutdown = true;
					NetworkManager.Singleton.Shutdown();
					await UniTask.Delay(200);
				}

				// 新ホストの起動完了を少し待ってから接続
				await UniTask.Delay(500);

				// 新しい JoinCode で Relay に参加
				var joinAllocation = await RelayService.Instance.JoinAllocationAsync(_joinCode);

				var utpTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
				utpTransport.SetClientRelayData(
					joinAllocation.RelayServer.IpV4,
					(ushort)joinAllocation.RelayServer.Port,
					joinAllocation.AllocationIdBytes,
					joinAllocation.Key,
					joinAllocation.ConnectionData,
					joinAllocation.HostConnectionData
				);

				if (!NetworkManager.Singleton.StartClient())
				{
					Debug.LogError("NetcodeNetworkHandler: 再接続失敗 - NetworkManager クライアント起動失敗。");
					OnNetworkConnectionStatusChanged?.Invoke(false);
					return;
				}

				Debug.Log("NetcodeNetworkHandler: ホストマイグレーション後の再接続成功。");
				// OnClientConnected が発火して RegisterDataMessageHandler / OnNetworkConnectionStatusChanged を呼ぶ
			}
			catch (Exception e)
			{
				Debug.LogError($"NetcodeNetworkHandler: 再接続失敗: {e.Message}");
				OnNetworkConnectionStatusChanged?.Invoke(false);
			}
		}

		// --------------------------------------------------------------------------------
		// Lobby イベント購読
		// --------------------------------------------------------------------------------
		private ILobbyEvents _lobbyEvents;

		private async UniTask SubscribeToLobbyEvents(string lobbyId)
		{
			try
			{
				var callbacks = new LobbyEventCallbacks();
				callbacks.LobbyChanged += OnLobbyChanged;
				_lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, callbacks);
				Debug.Log("NetcodeNetworkHandler: Lobby イベント購読完了。");
			}
			catch (LobbyServiceException e)
			{
				Debug.LogError($"NetcodeNetworkHandler: Lobby イベント購読失敗: {e}");
			}
		}

		private void OnLobbyChanged(ILobbyChanges lobby)
		{
			// ----------------------------------------------------------------
			// 1. ホスト変更（ホストマイグレーション）
			// ----------------------------------------------------------------
			if (lobby.HostId.Changed)
			{
				string newHostId = lobby.HostId.Value;
				Debug.Log($"NetcodeNetworkHandler: Lobby ホストが変わりました → {newHostId}");
				_currentLobbyHostId = newHostId;

				if (newHostId == AuthenticationService.Instance.PlayerId)
				{
					// 自分が新ホストに昇格 → ホストマイグレーション実行
					HandleHostMigration().Forget();
				}
			}

			// ----------------------------------------------------------------
			// 2. Lobby データ変更（新 JoinCode による再接続）
			// ----------------------------------------------------------------
			if (lobby.Data.Changed && _pendingReconnect)
			{
				var dataChanges = lobby.Data.Value;
				if (dataChanges != null
					&& dataChanges.TryGetValue("JoinCode", out var joinCodeChange)
					&& joinCodeChange.Changed
					&& joinCodeChange.Value != null)
				{
					_joinCode = joinCodeChange.Value.Value;
					Debug.Log($"NetcodeNetworkHandler: 新しい JoinCode を検出。再接続します: {_joinCode}");
					ReconnectAfterHostMigration().Forget();
				}
			}

			// ----------------------------------------------------------------
			// 3. 参加プレイヤーの処理
			// ----------------------------------------------------------------
			if (lobby.PlayerJoined.Changed)
			{
				foreach (var joined in lobby.PlayerJoined.Value)
				{
					// スロットリストへ追加（PlayerIndex の位置に挿入）
					int idx = joined.PlayerIndex;
					while (_lobbyPlayerIdsBySlot.Count <= idx)
						_lobbyPlayerIdsBySlot.Add(null);
					_lobbyPlayerIdsBySlot[idx] = joined.Player.Id;

					// 重複追加を防ぐ
					if (ConnectedList.Exists(p => p.Id == joined.Player.Id)) continue;

					string playerName = joined.Player.Id;
					if (joined.Player.Data != null
						&& joined.Player.Data.TryGetValue("PlayerName", out var nameObj))
					{
						playerName = nameObj.Value;
					}

					var player = new PlayerData
					{
						Id = joined.Player.Id,
						Name = playerName,
						PlayerProperties = new Dictionary<string, object>()
					};
					ConnectedList.Add(player);
					OnPlayerConnected?.Invoke(player.Id, player.Name);
				}
			}

			// ----------------------------------------------------------------
			// 4. 退出プレイヤーの処理
			// ----------------------------------------------------------------
			if (lobby.PlayerLeft.Changed)
			{
				foreach (int slotIndex in lobby.PlayerLeft.Value)
				{
					// スロットリストから PlayerId を取得して ID 基準で削除
					string leftPlayerId = null;
					if (slotIndex < _lobbyPlayerIdsBySlot.Count)
					{
						leftPlayerId = _lobbyPlayerIdsBySlot[slotIndex];
						_lobbyPlayerIdsBySlot.RemoveAt(slotIndex);
					}

					if (leftPlayerId == null) continue;

					var player = ConnectedList.Find(p => p.Id == leftPlayerId);
					if (player != null)
					{
						ConnectedList.Remove(player);
						DisconnectedList.Add(player);
						OnPlayerDisconnected?.Invoke(player.Id, player.Name);
					}
				}
			}
		}

		private void UnsubscribeToLobbyEvents()
		{
			if (_lobbyEvents != null)
			{
				try
				{
					_lobbyEvents.Callbacks.LobbyChanged -= OnLobbyChanged;
					_ = _lobbyEvents.UnsubscribeAsync();
				}
				catch (LobbyServiceException e)
				{
					Debug.LogError($"NetcodeNetworkHandler: Lobby イベント購読解除失敗: {e}");
				}
				_lobbyEvents = null;
			}
		}
	}
}

#endif
