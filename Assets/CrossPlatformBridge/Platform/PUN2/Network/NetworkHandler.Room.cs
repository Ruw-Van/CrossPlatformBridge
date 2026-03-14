#if USE_CROSSPLATFORMBRIDGE_PUN2
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;

namespace CrossPlatformBridge.Platform.PUN2.Network
{
	/// <summary>
	/// Photon Unity Networking 2 (PUN2) を使用した IInternalNetworkHandler の実装のルーム機能部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class NetworkHandler : MonoBehaviourPunCallbacks, IInternalNetworkHandler
	{
		// --------------------------------------------------------------------------------
		// IInternalNetworkHandler インターフェース実装 - ルーム機能
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 新しいルームを作成します。
		/// </summary>
		/// <param name="baseSettings">ルーム作成に使用する設定。</param>
		/// <returns>ルーム作成が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> CreateRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			Debug.Log($"PUN2NetworkHandler: ルーム '{baseSettings.RoomName}' を作成中...");
			try
			{
				// INetworkSettings を Pun2RoomSettings に変換
				RoomSettings settings = baseSettings as RoomSettings;
				if (settings == null)
				{
					Debug.LogWarning("PUN2NetworkHandler: 渡された INetworkSettings は Pun2RoomSettings ではありません。デフォルト設定を使用します。");
					settings = new RoomSettings(settings); // INetworkSettings の値で初期化
				}

				RoomOptions roomOptions = settings.ToRoomOptions();
				PhotonNetwork.CreateRoom(settings.RoomName, roomOptions);

				// ユーザーキャンセルとタイムアウトを統合
				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
					cancellationToken, timeoutController.Timeout(TimeSpan.FromSeconds(10)));

				await UniTask.WaitUntil(() => PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == ClientState.Leaving || PhotonNetwork.NetworkClientState == ClientState.Disconnected, cancellationToken: linkedCts.Token);

				if (PhotonNetwork.InRoom)
				{
					Debug.Log($"PUN2NetworkHandler: ルーム '{PhotonNetwork.CurrentRoom.Name}' を作成しました。");
					StationId = PhotonNetwork.CurrentRoom.Name;
					OnRoomOperationCompleted?.Invoke("CreateRoom", true, PhotonNetwork.CurrentRoom.Name);
					OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
				}
				return true;
			}
			catch (OperationCanceledException)
			{
				Debug.Log($"PUN2NetworkHandler: ルーム作成がキャンセルされました。クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Cancelled");
				return false;
			}
			catch (Exception e)
			{
				Debug.LogError($"PUN2NetworkHandler: ルーム作成失敗。{e.Message} クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Room creation failed.");
				return false;
			}
			finally
			{
				timeoutController.Reset();
			}
		}

		/// <summary>
		/// 既存のルームに接続します。
		/// </summary>
		/// <param name="roomId">接続するルームのID。</param>
		/// <returns>ルーム接続が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> ConnectRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			try
			{
				Debug.Log($"PUN2NetworkHandler: ルーム '{baseSettings.RoomName}' に接続中...");

				RoomSettings settings = baseSettings as RoomSettings;
				if (settings == null)
				{
					Debug.LogWarning("PUN2NetworkHandler: 渡された INetworkSettings は Pun2RoomSettings ではありません。デフォルト設定を使用します。");
					settings = new RoomSettings(settings); // INetworkSettings の値で初期化
				}
				PhotonNetwork.JoinRoom(settings.RoomName);

				// ユーザーキャンセルとタイムアウトを統合
				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
					cancellationToken, timeoutController.Timeout(TimeSpan.FromSeconds(10)));

				await UniTask.WaitUntil(() => PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == ClientState.Leaving || PhotonNetwork.NetworkClientState == ClientState.Disconnected, cancellationToken: linkedCts.Token);

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
			catch (OperationCanceledException)
			{
				Debug.Log($"PUN2NetworkHandler: ルーム接続がキャンセルされました。クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Cancelled");
				return false;
			}
			catch (Exception e)
			{
				Debug.LogError($"PUN2NetworkHandler: ルーム接続失敗。{e.Message} クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Room creation failed.");
				return false;
			}
			finally
			{
				timeoutController.Reset();
			}
		}

		/// <summary>
		/// 現在接続しているルームから切断します。
		/// </summary>
		public async UniTask DisconnectRoom()
		{
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
		/// 利用可能なルームを検索します。
		/// ロビー接続（ConnectLobby）後に OnRoomListUpdate で更新されたリストをフィルタして返します。
		/// </summary>
		/// <param name="baseSettings">RoomName・CustomProperties でフィルタ条件を指定します。</param>
		/// <returns>条件に合う RoomSettings のリスト。</returns>
		public UniTask<List<object>> SearchRoom(IRoomSettings baseSettings)
		{
			Debug.Log($"PUN2NetworkHandler: ルームを検索中... クエリ: '{baseSettings.RoomName}'");

			List<object> results = new List<object>();
			foreach (RoomInfo room in roomList)
			{
				if (!string.IsNullOrEmpty(baseSettings.RoomName) && !room.Name.Contains(baseSettings.RoomName))
					continue;

				if (baseSettings.CustomProperties != null && baseSettings.CustomProperties.Count > 0)
				{
					bool match = true;
					foreach (var kv in baseSettings.CustomProperties)
					{
						if (!room.CustomProperties.ContainsKey(kv.Key) ||
							room.CustomProperties[kv.Key]?.ToString() != kv.Value?.ToString())
						{
							match = false;
							break;
						}
					}
					if (!match) continue;
				}

				results.Add(new RoomSettings
				{
					Id = room.Name,
					RoomName = room.Name,
					MaxPlayers = room.MaxPlayers,
					IsOpen = room.IsOpen,
					IsVisible = room.IsVisible,
				});
			}
			Debug.Log($"PUN2NetworkHandler: ルーム検索完了。{results.Count} 件見つかりました。");
			return UniTask.FromResult(results);
		}

		/// <summary>
		/// ルームをマッチメイキングします。
		/// PUN2 ネイティブの JoinRandomRoom を使用して条件に合うルームを検索・接続します。
		/// createIfNotFound が true の場合、見つからなければ新規作成します。
		/// </summary>
		public async UniTask<bool> MatchmakeRoom(IRoomSettings conditions, bool createIfNotFound = false, CancellationToken cancellationToken = default)
		{
			Debug.Log($"PUN2NetworkHandler: MatchmakeRoom クエリ: '{conditions.RoomName}'");
			try
			{
				var expectedProps = new ExitGames.Client.Photon.Hashtable();
				if (conditions.CustomProperties != null)
					foreach (var kv in conditions.CustomProperties)
						expectedProps[kv.Key] = kv.Value;

				byte maxPlayers = conditions.MaxPlayers > 0 ? (byte)conditions.MaxPlayers : (byte)0;
				PhotonNetwork.JoinRandomRoom(expectedProps.Count > 0 ? expectedProps : null, maxPlayers);

				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
					cancellationToken, timeoutController.Timeout(TimeSpan.FromSeconds(10)));

				// JoinedLobby = JoinRandom 失敗後に戻る状態、InRoom = 成功
				await UniTask.WaitUntil(
					() => PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == ClientState.JoinedLobby,
					cancellationToken: linkedCts.Token);

				if (PhotonNetwork.InRoom)
				{
					StationId = PhotonNetwork.CurrentRoom.Name;
					OnRoomOperationCompleted?.Invoke("MatchmakeRoom", true, PhotonNetwork.CurrentRoom.Name);
					OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
					return true;
				}

				if (createIfNotFound) return await CreateRoom(conditions, cancellationToken);

				OnRoomOperationCompleted?.Invoke("MatchmakeRoom", false, "マッチするルームが見つかりませんでした。");
				return false;
			}
			catch (OperationCanceledException)
			{
				Debug.Log($"PUN2NetworkHandler: MatchmakeRoom がキャンセルされました。");
				OnRoomOperationCompleted?.Invoke("MatchmakeRoom", false, "Cancelled");
				return false;
			}
			finally
			{
				timeoutController.Reset();
			}
		}

		#region MonoBehaviourPunCallbacks
		// --------------------------------------------------------------------------------
		// MonoBehaviourPunCallbacks (PUN2のイベントハンドラ) - ルーム関連
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
		public override void OnRoomListUpdate(List<RoomInfo> changedRoomList)
		{
			roomList.Update(changedRoomList); // ルームリストを更新
			Debug.Log($"PUN2NetworkHandler.OnRoomListUpdate: ルームリストが更新されました。現在 {roomList.Count} 件のルームがあります。");
		}
		#endregion
	}
}

#endif
