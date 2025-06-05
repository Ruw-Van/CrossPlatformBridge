// Assets/Scripts/CrossPlatformBridge/Services/NetcodeNetworkHandler/NetcodeNetworkHandler.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace CrossPlatformBridge.Network.NetcodeNetworkHandler
{
	/// <summary>
	/// Unity Netcode for GameObjects を使用した IInternalNetworkHandler の実装。
	/// Unity Gaming Services (Lobby, Relay, Authentication) と連携します。
	/// </summary>
	public partial class NetcodeNetworkHandler : IInternalNetworkHandler
	{
		/// <summary>
		/// 新しいLobbyを作成し、Relayホストとして起動します。
		/// </summary>
		/// <param name="lobbyName">作成するロビーの名前。</param>
		/// <param name="baseSettings">ロビー作成に使用する設定オブジェクト。</param> // ★追加
		/// <returns>ロビー作成が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> CreateLobby(string lobbyName, INetworkSettings baseSettings) // ★シグネチャ変更
		{
			Debug.Log($"NetcodeNetworkHandler: ロビー '{lobbyName}' を作成中...");
			if (!AuthenticationService.Instance.IsSignedIn)
			{
				Debug.LogError("NetcodeNetworkHandler: 認証されていません。ロビーを作成できません。");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Not authenticated.");
				return false;
			}

			// 渡された設定オブジェクトを NetcodeSettings にキャストまたは変換
			NetcodeSettings netcodeSettings = baseSettings as NetcodeSettings;
			if (netcodeSettings == null)
			{
				// もし baseSettings が NetcodeSettings ではない場合、互換性のある新しいインスタンスを作成
				netcodeSettings = new NetcodeSettings(baseSettings);
			}

			try
			{
				// 1. Relayサーバーから割り当てを取得 (MaxPlayersはnetcodeSettingsから取得)
				_allocation = await RelayService.Instance.CreateAllocationAsync(netcodeSettings.MaxPlayers); // ★netcodeSettings.MaxPlayersを使用
				_joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);

				// 2. NetworkManagerをホストとして開始
				var utpTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
				if (NetworkManager.Singleton.NetworkConfig == null)
				{
					NetworkManager.Singleton.NetworkConfig = new NetworkConfig();
					NetworkManager.Singleton.NetworkConfig.NetworkTransport = utpTransport;
				}
				utpTransport.SetRelayServerData(
					_allocation.RelayServer.IpV4,
					(ushort)_allocation.RelayServer.Port,
					_allocation.AllocationIdBytes,
					_allocation.Key,
					_allocation.ConnectionData
				);

				if (!NetworkManager.Singleton.StartHost())
				{
					Debug.LogError("NetcodeNetworkHandler: NetworkManager ホスト起動失敗。");
					OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Failed to start NetworkManager host.");
					return false;
				}

				// 3. Lobbyサービスでロビーを作成
				// PlayerDataを設定
				netcodeSettings.PlayerData[AuthenticationService.Instance.PlayerId] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public);

				CreateLobbyOptions createLobbyOptions = netcodeSettings.ToCreateLobbyOptions();
				createLobbyOptions.Data.Add("JoinCode", new DataObject(DataObject.VisibilityOptions.Member, _joinCode));
				createLobbyOptions.Data.Add("HostId", new DataObject(DataObject.VisibilityOptions.Member, AuthenticationService.Instance.PlayerId));


				_connectedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, netcodeSettings.MaxPlayers, createLobbyOptions);
				StationId = _connectedLobby.Id;
				Debug.Log($"NetcodeNetworkHandler: ロビー '{_connectedLobby.Name}' (ID: {_connectedLobby.Id}) を作成しました。JoinCode: {_joinCode}");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", true, _connectedLobby.Id);
				OnHostStatusChanged?.Invoke(true); // ホストになったことを通知
				return true;
			}
			catch (LobbyServiceException e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ロビー作成失敗: {e.Message}");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, e.Message);
				await DisconnectRoom();
				return false;
			}
			catch (Exception e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ロビー作成時の予期せぬエラー: {e.Message}");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, e.Message);
				await DisconnectRoom();
				return false;
			}
		}

		/// <summary>
		/// 既存のLobbyに接続し、Relayクライアントとして参加します。
		/// </summary>
		/// <param name="lobbyId">接続するロビーのIDまたはジョインコード。</param>
		/// <returns>接続が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> ConnectLobby(string lobbyId)
		{
			Debug.Log($"NetcodeNetworkHandler: ロビー '{lobbyId}' に接続中...");
			if (!AuthenticationService.Instance.IsSignedIn)
			{
				Debug.LogError("NetcodeNetworkHandler: 認証されていません。ロビーに接続できません。");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Not authenticated.");
				return false;
			}

			try
			{
				_connectedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

				// ロビーからRelayのJoinCodeを取得
				_joinCode = _connectedLobby.Data["JoinCode"].Value;
				StationId = _connectedLobby.Id;
				Debug.Log($"NetcodeNetworkHandler: ロビー '{_connectedLobby.Name}' (ID: {_connectedLobby.Id}) に参加しました。JoinCode: {_joinCode}");

				// Relayクライアントとして参加
				var _allocation = await RelayService.Instance.JoinAllocationAsync(_joinCode);

				var utpTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
				utpTransport.SetClientRelayData(
					_allocation.RelayServer.IpV4,
					(ushort)_allocation.RelayServer.Port,
					_allocation.AllocationIdBytes,
					_allocation.Key,
					_allocation.ConnectionData,
					_allocation.HostConnectionData
				);

				if (!NetworkManager.Singleton.StartClient())
				{
					Debug.LogError("NetcodeNetworkHandler: NetworkManager クライアント起動失敗。");
					OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Failed to start NetworkManager client.");
					return false;
				}

				OnLobbyOperationCompleted?.Invoke("ConnectLobby", true, _connectedLobby.Id);
				OnHostStatusChanged?.Invoke(false); // クライアントなのでホストではない
				return true;
			}
			catch (LobbyServiceException e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ロビー接続失敗: {e.Message}");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, e.Message);
				return false;
			}
			catch (RelayServiceException e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ロビー接続失敗: {e.Message}");
				OnLobbyOperationCompleted?.Invoke("RelayServiceException", false, e.Message);
				return false;
			}
			catch (Exception e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ロビー接続時の予期せぬエラー: {e.Message}");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, e.Message);
				return false;
			}
		}

		/// <summary>
		/// 現在接続しているロビーから切断します。
		/// </summary>
		public async UniTask DisconnectLobby()
		{
			Debug.Log("NetcodeNetworkHandler: ロビーから切断中...");
			if (_connectedLobby != null)
			{
				try
				{
					if (_connectedLobby.HostId == AuthenticationService.Instance.PlayerId)
					{
						// ホストの場合、ロビーを削除
						await LobbyService.Instance.DeleteLobbyAsync(_connectedLobby.Id);
						Debug.Log($"NetcodeNetworkHandler: ロビー '{_connectedLobby.Name}' を削除しました。");
						NetworkManager.Singleton.Shutdown(); // ホストの場合はNetworkManagerをシャットダウンする
					}
					else
					{
						// クライアントの場合、ロビーを退出
						await LobbyService.Instance.RemovePlayerAsync(_connectedLobby.Id, AuthenticationService.Instance.PlayerId);
						Debug.Log($"NetcodeNetworkHandler: ロビー '{_connectedLobby.Name}' を退出しました。");
					}
				}
				catch (LobbyServiceException e)
				{
					Debug.LogError($"NetcodeNetworkHandler: ロビー切断失敗: {e.Message}");
				}
				finally
				{
					_connectedLobby = null;
					StationId = null;
					OnLobbyOperationCompleted?.Invoke("DisconnectLobby", true, "");
				}
			}
			else
			{
				Debug.Log("NetcodeNetworkHandler: 接続中のロビーがありません。");
				OnLobbyOperationCompleted?.Invoke("DisconnectLobby", true, "");
			}
		}

		/// <summary>
		/// 利用可能なロビーを検索します。
		/// </summary>
		/// <param name="query">検索クエリ (部分一致)。</param>
		/// <returns>検索結果のロビーIDのリスト。</returns>
		public async UniTask<List<string>> SearchLobby(string query = "")
		{
			Debug.Log($"NetcodeNetworkHandler: ロビーを検索中... クエリ: '{query}'");
			if (!AuthenticationService.Instance.IsSignedIn)
			{
				Debug.LogError("NetcodeNetworkHandler: 認証されていません。ロビーを検索できません。");
				return new List<string>();
			}

			try
			{
				var queryOptions = new QueryLobbiesOptions
				{
					Count = 25, // 最大取得数
					Filters = new List<QueryFilter>
					{
						new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT), // 空きがあるルームのみ
					}
				};

				if (!string.IsNullOrEmpty(query))
				{
					queryOptions.Filters.Add(new QueryFilter(QueryFilter.FieldOptions.Name, query, QueryFilter.OpOptions.CONTAINS));
				}

				QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
				List<string> lobbyNames = new List<string>();
				foreach (Lobby lobby in queryResponse.Results)
				{
					if (lobby.IsLocked || lobby.IsPrivate) continue; // ロックされている、またはプライベートなロビーは表示しない

					string hostName = "Unknown";
					if (lobby.HostId != null && lobby.Players.Exists(p => p.Id == lobby.HostId))
					{
						hostName = lobby.Name;
					}
					lobbyNames.Add($"{lobby.Name} by {hostName} ({lobby.Players.Count}/{lobby.MaxPlayers}) - Code: {lobby.LobbyCode}");
				}
				Debug.Log($"NetcodeNetworkHandler: ロビー検索完了。{lobbyNames.Count} 件見つかりました。");
				return lobbyNames;
			}
			catch (LobbyServiceException e)
			{
				Debug.LogError($"NetcodeNetworkHandler: ロビー検索失敗: {e.Message}");
				return new List<string>();
			}
		}
	}
}
