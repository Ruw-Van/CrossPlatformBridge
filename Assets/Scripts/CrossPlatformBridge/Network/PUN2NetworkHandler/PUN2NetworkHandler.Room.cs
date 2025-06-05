// Assets/Scripts/CrossPlatformBridge/Network/PUN2NetworkHandler/PUN2NetworkHandler.Room.cs
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace CrossPlatformBridge.Network.PUN2NetworkHandler
{
	/// <summary>
	/// Photon Unity Networking 2 (PUN2) を使用した IInternalNetworkHandler の実装のルーム機能部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class PUN2NetworkHandler : MonoBehaviourPunCallbacks, IInternalNetworkHandler
	{
		// --------------------------------------------------------------------------------
		// IInternalNetworkHandler インターフェース実装 - ルーム機能
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 新しいルームを作成します。
		/// </summary>
		/// <param name="roomName">作成するルームの名前。</param>
		/// <param name="settings">ルーム作成に使用する設定。</param>
		/// <returns>ルーム作成が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> CreateRoom(string roomName, INetworkSettings settings)
		{
			// CreateLobby と同じ実装を共有
			Debug.Log($"PUN2NetworkHandler: ルーム '{roomName}' を作成中...");

			// INetworkSettings を Pun2RoomSettings に変換
			PUN2Settings pun2Settings = settings as PUN2Settings;
			if (pun2Settings == null)
			{
				Debug.LogWarning("PUN2NetworkHandler: 渡された INetworkSettings は Pun2RoomSettings ではありません。デフォルト設定を使用します。");
				pun2Settings = new PUN2Settings(settings); // INetworkSettings の値で初期化
			}

			RoomOptions roomOptions = pun2Settings.ToRoomOptions();

			PhotonNetwork.CreateRoom(roomName, roomOptions);

			await UniTask.WaitUntil(() => PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == ClientState.Leaving || PhotonNetwork.NetworkClientState == ClientState.Disconnected);

			if (PhotonNetwork.InRoom)
			{
				Debug.Log($"PUN2NetworkHandler: ルーム '{PhotonNetwork.CurrentRoom.Name}' を作成しました。");
				StationId = PhotonNetwork.CurrentRoom.Name;
				OnRoomOperationCompleted?.Invoke("CreateRoom", true, PhotonNetwork.CurrentRoom.Name);
				OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
				return true;
			}
			else
			{
				Debug.LogError($"PUN2NetworkHandler: ルーム作成失敗。クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Room creation failed.");
				return false;
			}
		}

		/// <summary>
		/// 既存のルームに接続します。ConnectLobby と同じロジックを共有します。
		/// </summary>
		/// <param name="roomId">接続するルームのID。</param>
		/// <returns>ルーム接続が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> ConnectRoom(string roomId)
		{
			// ConnectLobby と同じ実装を共有
			Debug.Log($"PUN2NetworkHandler: ルーム '{roomId}' に接続中...");
			PhotonNetwork.JoinRoom(roomId);

			await UniTask.WaitUntil(() => PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == ClientState.Leaving || PhotonNetwork.NetworkClientState == ClientState.Disconnected);

			if (PhotonNetwork.InRoom)
			{
				Debug.Log($"PUN2NetworkHandler: ルーム '{PhotonNetwork.CurrentRoom.Name}' に接続しました。");
				StationId = PhotonNetwork.CurrentRoom.Name;
				OnRoomOperationCompleted?.Invoke("ConnectRoom", true, PhotonNetwork.CurrentRoom.Name);
				OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
				return true;
			}
			else
			{
				Debug.LogError($"PUN2NetworkHandler: ルーム接続失敗。クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Room joining failed.");
				return false;
			}
		}

		/// <summary>
		/// 現在接続しているルームから切断します。DisconnectLobby と同じロジックを共有します。
		/// </summary>
		public async UniTask DisconnectRoom()
		{
			// DisconnectLobby と同じ実装を共有
			Debug.Log("PUN2NetworkHandler: ルームから切断中...");
			if (PhotonNetwork.InRoom)
			{
				PhotonNetwork.LeaveRoom();
				await UniTask.WaitUntil(() => !PhotonNetwork.InRoom);
			}
			StationId = null;
			OnRoomOperationCompleted?.Invoke("DisconnectRoom", true, "");
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("PUN2NetworkHandler: ルーム切断完了。");
		}

		/// <summary>
		/// 利用可能なルームを検索します。SearchLobby と同じロジックを共有します。
		/// </summary>
		/// <param name="query">検索クエリ (部分一致)。</param>
		/// <returns>検索結果のルームIDのリスト。</returns>
		public async UniTask<List<string>> SearchRoom(string query = "")
		{
			// SearchLobby と同じ実装を共有
			Debug.Log($"PUN2NetworkHandler: ルームを検索中... クエリ: '{query}'");
			return await SearchLobby(query);
		}
	}
}
