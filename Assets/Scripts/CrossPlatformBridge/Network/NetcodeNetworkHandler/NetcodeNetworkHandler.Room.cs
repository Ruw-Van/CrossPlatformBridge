// Assets/Scripts/CrossPlatformBridge/Services/NetcodeNetworkHandler/NetcodeNetworkHandler.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;

namespace CrossPlatformBridge.Network.NetcodeNetworkHandler
{
	/// <summary>
	/// Unity Netcode for GameObjects を使用した IInternalNetworkHandler の実装。
	/// Unity Gaming Services (Lobby, Relay, Authentication) と連携します。
	/// </summary>
	public partial class NetcodeNetworkHandler : IInternalNetworkHandler
	{
		/// <summary>
		/// 新しいネットワークセッション（ルーム）を作成します。
		/// Netcode for GameObjectsでは、通常CreateLobbyとRelayの組み合わせで行われるため、CreateLobbyを呼び出します。
		/// </summary>
		/// <param name="roomName">作成するルームの名前。</param>
		/// <param name="settings">ルーム作成に使用する設定オブジェクト。</param> // ★変更
		/// <returns>ルーム作成が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> CreateRoom(string roomName, INetworkSettings settings) // ★シグネチャ変更
		{
			Debug.Log($"NetcodeNetworkHandler: ルーム (セッション) '{roomName}' を作成中... CreateLobbyを呼び出して処理されます。");

			// ファクトリーからデフォルト設定を取得し、maxPlayers を上書き
			NetcodeSettings netcodeSettings = SettingsFactory.CreateSettings() as NetcodeSettings;
			if (netcodeSettings == null)
			{
				Debug.LogError("NetcodeNetworkHandler: NetcodeSettingsFactory が NetcodeSettings を返しませんでした。");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Invalid settings factory.");
				return false;
			}
			netcodeSettings.MaxPlayers = settings.MaxPlayers; // maxPlayersを上書き

			return await CreateLobby(roomName, netcodeSettings); // ロビー作成と同じロジック (settingsは内部で処理)
		}

		/// <summary>
		/// 既存のネットワークセッション（ルーム）に接続します。
		/// Netcode for GameObjectsでは、通常ConnectLobbyを呼び出して処理されます。
		/// </summary>
		/// <param name="roomId">接続するルームのID（ロビーIDまたはジョインコード）。</param>
		/// <returns>ルーム接続が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> ConnectRoom(string roomId)
		{
			Debug.Log($"NetcodeNetworkHandler: ルーム (セッション) '{roomId}' に接続中... ConnectLobbyを呼び出して処理されます。");
			return await ConnectLobby(roomId); // ロビー接続と同じロジック
		}

		/// <summary>
		/// 現在接続しているネットワークセッション（ルーム）から切断します。
		/// Netcode for GameObjectsでは、通常DisconnectLobbyを呼び出して処理されます。
		/// </summary>
		public async UniTask DisconnectRoom()
		{
			Debug.Log("NetcodeNetworkHandler: ルーム (セッション) から切断中...");
			if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
			{
				NetworkManager.Singleton.Shutdown();
				await UniTask.WaitUntil(() => !NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsClient);
				Debug.Log("NetcodeNetworkHandler: NetworkManager シャットダウン完了。");
			}
			StationId = null;
			OnRoomOperationCompleted?.Invoke("DisconnectRoom", true, "");
			OnHostStatusChanged?.Invoke(false);
		}

		/// <summary>
		/// 利用可能なネットワークセッション（ルーム）を検索します。
		/// Netcode for GameObjectsでは、SearchLobbyを呼び出して処理されます。
		/// </summary>
		/// <param name="query">検索クエリ（オプション）。</param>
		/// <returns>検索結果のルームIDのリスト。</returns>
		public async UniTask<List<string>> SearchRoom(string query = "")
		{
			Debug.Log($"NetcodeNetworkHandler: ルーム (セッション) を検索中... SearchLobby を呼び出して処理されます。");
			return await SearchLobby(query); // ロビー検索と同じロジック
		}
	}
}
