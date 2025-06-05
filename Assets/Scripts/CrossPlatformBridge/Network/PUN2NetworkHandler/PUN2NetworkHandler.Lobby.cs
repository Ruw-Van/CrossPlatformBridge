// Assets/Scripts/CrossPlatformBridge/Network/PUN2NetworkHandler/PUN2NetworkHandler.Lobby.cs
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace CrossPlatformBridge.Network.PUN2NetworkHandler
{
	/// <summary>
	/// Photon Unity Networking 2 (PUN2) を使用した IInternalNetworkHandler の実装のロビー機能部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class PUN2NetworkHandler : MonoBehaviourPunCallbacks, IInternalNetworkHandler
	{
		// --------------------------------------------------------------------------------
		// IInternalNetworkHandler インターフェース実装 - ロビー機能
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 新しいロビー (PUN2のRoomに相当) を作成します。
		/// </summary>
		/// <param name="lobbyName">作成するロビー（ルーム）の名前。</param>
		/// <param name="settings">ルーム作成に使用する設定。</param>
		/// <returns>作成が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> CreateLobby(string lobbyName, INetworkSettings settings)
		{
			Debug.Log($"PUN2NetworkHandler: ルーム '{lobbyName}' を作成中...");

			// INetworkSettings を Pun2RoomSettings に変換
			PUN2Settings pun2Settings = settings as PUN2Settings;
			if (pun2Settings == null)
			{
				Debug.LogWarning("PUN2NetworkHandler: 渡された INetworkSettings は Pun2RoomSettings ではありません。デフォルト設定を使用します。");
				pun2Settings = new PUN2Settings(settings); // INetworkSettings の値で初期化
			}

			RoomOptions roomOptions = pun2Settings.ToRoomOptions();

			// 特定のロビーに参加してからルームを作成することも可能だが、ここではデフォルトロビーで直接ルーム作成
			PhotonNetwork.CreateRoom(lobbyName, roomOptions);

			// ルーム作成完了または失敗を待機
			await UniTask.WaitUntil(() => PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == ClientState.Leaving || PhotonNetwork.NetworkClientState == ClientState.Disconnected);

			if (PhotonNetwork.InRoom)
			{
				Debug.Log($"PUN2NetworkHandler: ルーム '{PhotonNetwork.CurrentRoom.Name}' を作成しました。");
				StationId = PhotonNetwork.CurrentRoom.Name;
				OnLobbyOperationCompleted?.Invoke("CreateLobby", true, PhotonNetwork.CurrentRoom.Name);
				OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
				return true;
			}
			else
			{
				Debug.LogError($"PUN2NetworkHandler: ルーム作成失敗。クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Room creation failed.");
				return false;
			}
		}

		/// <summary>
		/// 既存のロビー (PUN2のRoomに相当) に接続します。
		/// </summary>
		/// <param name="lobbyId">接続するロビー（ルーム）のID。</param>
		/// <returns>接続が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> ConnectLobby(string lobbyId)
		{
			Debug.Log($"PUN2NetworkHandler: ルーム '{lobbyId}' に接続中...");
			PhotonNetwork.JoinRoom(lobbyId);

			// ルーム参加完了または失敗を待機
			await UniTask.WaitUntil(() => PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == ClientState.Leaving || PhotonNetwork.NetworkClientState == ClientState.Disconnected);

			if (PhotonNetwork.InRoom)
			{
				Debug.Log($"PUN2NetworkHandler: ルーム '{PhotonNetwork.CurrentRoom.Name}' に接続しました。");
				StationId = PhotonNetwork.CurrentRoom.Name;
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", true, PhotonNetwork.CurrentRoom.Name);
				OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
				return true;
			}
			else
			{
				Debug.LogError($"PUN2NetworkHandler: ルーム接続失敗。クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Room joining failed.");
				return false;
			}
		}

		/// <summary>
		/// 現在接続しているロビー (PUN2のRoomに相当) から切断します。
		/// </summary>
		public async UniTask DisconnectLobby()
		{
			Debug.Log("PUN2NetworkHandler: ルームから切断中...");
			if (PhotonNetwork.InRoom)
			{
				PhotonNetwork.LeaveRoom();
				await UniTask.WaitUntil(() => !PhotonNetwork.InRoom); // ルーム退出完了を待機
			}
			StationId = null;
			OnLobbyOperationCompleted?.Invoke("DisconnectLobby", true, "");
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("PUN2NetworkHandler: ルーム切断完了。");
		}

		/// <summary>
		/// 利用可能なロビー (PUN2のRoomに相当) を検索します。
		/// </summary>
		/// <param name="query">検索クエリ (部分一致)。</param>
		/// <returns>検索結果のロビーIDのリスト。</returns>
		public async UniTask<List<string>> SearchLobby(string query = "")
		{
			Debug.Log($"PUN2NetworkHandler: ルームを検索中... クエリ: '{query}'");
			List<string> roomNames = new List<string>();

			// ロビーにいない場合はロビーに参加してからルームリストを取得
			if (!PhotonNetwork.InLobby)
			{
				Debug.LogWarning("PUN2NetworkHandler: ロビーに接続していません。ロビーに参加します。");
				PhotonNetwork.JoinLobby();
				await UniTask.WaitUntil(() => PhotonNetwork.InLobby || PhotonNetwork.NetworkClientState == ClientState.Disconnected);
				if (!PhotonNetwork.InLobby)
				{
					Debug.LogError("PUN2NetworkHandler: ロビーへの参加に失敗しました。ルームリストを取得できません。");
					return roomNames;
				}
			}

			// // PhotonNetwork.RoomList は OnRoomListUpdate イベントで更新される
			// // ここで即座にリストを返す場合、最新でない可能性がある
			// foreach (RoomInfo room in PhotonNetwork.RoomList.Values) // RoomList は Dictionary<string, RoomInfo> に変更
			// {
			// 	if (room.IsVisible && room.IsOpen && room.PlayerCount < room.MaxPlayers) // 表示可能で、参加可能で、空きがあるルームのみ
			// 	{
			// 		if (string.IsNullOrEmpty(query) || room.Name.Contains(query))
			// 		{
			// 			roomNames.Add($"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})");
			// 		}
			// 	}
			// }
			// Debug.Log($"PUN2NetworkHandler: ルーム検索完了。{roomNames.Count} 件見つかりました。");
			return roomNames;
		}

		// --------------------------------------------------------------------------------
		// MonoBehaviourPunCallbacks (PUN2のイベントハンドラ) - ロビー関連
		// --------------------------------------------------------------------------------

		/// <summary>
		/// ルーム作成に成功した際に呼び出されます。
		/// </summary>
		public override void OnCreatedRoom()
		{
			Debug.Log($"PUN2NetworkHandler.OnCreatedRoom: ルーム '{PhotonNetwork.CurrentRoom.Name}' を作成しました。");
			StationId = PhotonNetwork.CurrentRoom.Name;
			OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient); // ホスト状態の更新 (マスタークライアントがホスト)
		}

		/// <summary>
		/// ルーム作成に失敗した際に呼び出されます。
		/// </summary>
		/// <param name="returnCode">エラーコード。</param>
		/// <param name="message">エラーメッセージ。</param>
		public override void OnCreateRoomFailed(short returnCode, string message)
		{
			Debug.LogError($"PUN2NetworkHandler.OnCreateRoomFailed: ルーム作成失敗。コード: {returnCode}, メッセージ: {message}");
			OnRoomOperationCompleted?.Invoke("CreateRoom", false, message);
			OnLobbyOperationCompleted?.Invoke("CreateLobby", false, message); // ロビー操作としても失敗を通知
		}

		/// <summary>
		/// ルームに参加した際に呼び出されます。
		/// </summary>
		public override void OnJoinedRoom()
		{
			Debug.Log($"PUN2NetworkHandler.OnJoinedRoom: ルーム '{PhotonNetwork.CurrentRoom.Name}' に参加しました。");
			StationId = PhotonNetwork.CurrentRoom.Name;
			OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);

			// 自身が接続したことを通知
			OnPlayerConnected?.Invoke(PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.LocalPlayer.NickName);

			// ルーム内の既存プレイヤーを ConnectedList に追加
			foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
			{
				if (player.IsLocal) continue;
				OnPlayerConnected?.Invoke(player.UserId, player.NickName);
			}
		}

		/// <summary>
		/// ルーム参加に失敗した際に呼び出されます。
		/// </summary>
		/// <param name="returnCode">エラーコード。</param>
		/// <param name="message">エラーメッセージ。</param>
		public override void OnJoinRoomFailed(short returnCode, string message)
		{
			Debug.LogError($"PUN2NetworkHandler.OnJoinRoomFailed: ルーム参加失敗。コード: {returnCode}, メッセージ: {message}");
			OnRoomOperationCompleted?.Invoke("ConnectRoom", false, message);
			OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, message); // ロビー操作としても失敗を通知
		}

		/// <summary>
		/// ランダムなルーム参加に失敗した際に呼び出されます。
		/// </summary>
		/// <param name="returnCode">エラーコード。</param>
		/// <param name="message">エラーメッセージ。</param>
		public override void OnJoinRandomFailed(short returnCode, string message)
		{
			Debug.LogError($"PUN2NetworkHandler.OnJoinRandomFailed: ランダムルーム参加失敗。コード: {returnCode}, メッセージ: {message}");
			// 通常、ランダム参加失敗後は新しいルームを作成するなどのフォールバック処理を行う
			OnRoomOperationCompleted?.Invoke("ConnectRoom", false, message + " (Random join failed)"); // 汎用的に失敗を通知
			OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, message + " (Random join failed)");
		}

		/// <summary>
		/// ルームを退出した際に呼び出されます。
		/// </summary>
		public override void OnLeftRoom()
		{
			Debug.Log("PUN2NetworkHandler.OnLeftRoom: ルームを退出しました。");
			StationId = null;
			OnHostStatusChanged?.Invoke(false); // ホスト状態をリセット
			OnPlayerDisconnected?.Invoke(PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.LocalPlayer.NickName); // 自身が切断されたことを通知
		}

		/// <summary>
		/// 他のプレイヤーがルームに入室した際に呼び出されます。
		/// </summary>
		/// <param name="newPlayer">入室したプレイヤー。</param>
		public override void OnPlayerEnteredRoom(Player newPlayer)
		{
			Debug.Log($"PUN2NetworkHandler.OnPlayerEnteredRoom: プレイヤー '{newPlayer.NickName}' ({newPlayer.UserId}) が入室しました。");
			OnPlayerConnected?.Invoke(newPlayer.UserId, newPlayer.NickName);
			OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient); // マスタークライアントが変わる可能性があるので更新
		}

		/// <summary>
		/// 他のプレイヤーがルームを退出した際に呼び出されます。
		/// </summary>
		/// <param name="otherPlayer">退出したプレイヤー。</param>
		public override void OnPlayerLeftRoom(Player otherPlayer)
		{
			Debug.Log($"PUN2NetworkHandler.OnPlayerLeftRoom: プレイヤー '{otherPlayer.NickName}' ({otherPlayer.UserId}) が退出しました。");
			OnPlayerDisconnected?.Invoke(otherPlayer.UserId, otherPlayer.NickName);
			OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient); // マスタークライアントが変わる可能性があるので更新
		}

		/// <summary>
		/// ルームのマスタークライアントが変更された際に呼び出されます。
		/// </summary>
		/// <param name="newMasterClient">新しいマスタークライアント。</param>
		public override void OnMasterClientSwitched(Player newMasterClient)
		{
			Debug.Log($"PUN2NetworkHandler.OnMasterClientSwitched: マスタークライアントが '{newMasterClient.NickName}' に変更されました。");
			OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
		}

		/// <summary>
		/// ロビーのルームリストが更新された際に呼び出されます。
		/// </summary>
		/// <param name="roomList">更新されたルーム情報のリスト。</param>
		public override void OnRoomListUpdate(List<RoomInfo> roomList)
		{
			Debug.Log($"PUN2NetworkHandler.OnRoomListUpdate: ルームリストが更新されました。現在 {roomList.Count} 件のルームがあります。");
			// SearchLobby/SearchRoom はこのリストを利用するため、ここで直接イベントを発生させる必要は薄い
			// ただし、UIなどでリアルタイムにルームリストを更新する場合は、ここで処理を行う
		}
	}
}
