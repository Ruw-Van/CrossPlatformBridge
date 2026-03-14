#if USE_CROSSPLATFORMBRIDGE_PUN2
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using static UnityEngine.Rendering.RayTracingAccelerationStructure;
using System;

namespace CrossPlatformBridge.Platform.PUN2.Network
{
	/// <summary>
	/// Photon Unity Networking 2 (PUN2) を使用した IInternalNetworkHandler の実装のロビー機能部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class NetworkHandler : MonoBehaviourPunCallbacks, IInternalNetworkHandler
	{
		// --------------------------------------------------------------------------------
		// IInternalNetworkHandler インターフェース実装 - ロビー機能
		// --------------------------------------------------------------------------------

		public UniTask<bool> CreateLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
			=> ConnectLobby(baseSettings, cancellationToken);

		/// <summary>
		/// 新しいロビー (PUN2のRoomに相当) に接続します。
		/// </summary>
		/// <param name="baseSettings"></param>
		/// <param name="cancellationToken">操作をキャンセルするためのトークン（省略可）</param>
		/// <returns>接続が成功した場合は true、それ以外は false。</returns>
		public async UniTask<bool> ConnectLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			RoomSettings settings = baseSettings as RoomSettings;
			Debug.Log($"PUN2NetworkHandler: ロビー '{settings.RoomName}' に接続中...");

			try
			{
				TypedLobby lobby = settings.ToTypedLobby(LobbyType.SqlLobby);
				PhotonNetwork.JoinLobby(lobby);

				// ユーザーキャンセルとタイムアウトを統合
				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
					cancellationToken, timeoutController.Timeout(TimeSpan.FromSeconds(10)));

				// ロビー接続完了または失敗を待機
				await UniTask.WaitUntil(() => PhotonNetwork.InLobby, cancellationToken: linkedCts.Token);

				Debug.Log($"PUN2NetworkHandler: ロビー '{PhotonNetwork.CurrentLobby.Name}' に接続しました。");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", true, PhotonNetwork.CurrentLobby.Name);
				OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient);
				return true;
			}
			catch (OperationCanceledException)
			{
				Debug.Log($"PUN2NetworkHandler: ロビー接続がキャンセルされました。クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Cancelled");
				return false;
			}
			catch (Exception e)
			{
				Debug.LogError($"PUN2NetworkHandler: ロビー接続に失敗しました。{e.Message} クライアント状態: {PhotonNetwork.NetworkClientState}");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Lobby creation failed.");
				return false;
			}
			finally
			{
				timeoutController.Reset();
			}
		}


		/// <summary>
		/// 現在接続しているロビー (PUN2のRoomに相当) から切断します。
		/// </summary>
		public async UniTask DisconnectLobby()
		{
			Debug.Log("PUN2NetworkHandler: ロビーから切断中...");
			try
			{
				if (PhotonNetwork.InLobby)
				{
					PhotonNetwork.LeaveLobby();
					await UniTask.WaitUntil(() => !PhotonNetwork.InRoom, cancellationToken: timeoutController.Timeout(TimeSpan.FromSeconds(10))); // ロビー退出完了を待機
				}
				StationId = null;
				OnLobbyOperationCompleted?.Invoke("DisconnectLobby", true, "");
				OnHostStatusChanged?.Invoke(false);
				Debug.Log("PUN2NetworkHandler: ロビー切断完了。");
			}
			finally
			{
				timeoutController.Reset();
			}
		}

		/// <summary>
		/// 利用可能なロビーを検索します。Photonではロビー検索機能は無いので無効とします。
		/// </summary>
		/// <param name="baseSettings"></param>
		/// <returns>検索結果のロビーIDのリスト。</returns>
		public UniTask<List<object>> SearchLobby(IRoomSettings baseSettings)
		{
			return UniTask.FromResult(new List<object>());
		}

		/// <summary>
		/// ロビーをマッチメイキングします。
		/// PUN2 にはロビー検索機能がないため ConnectLobby に委譲します。
		/// </summary>
		public UniTask<bool> MatchmakeLobby(IRoomSettings conditions, CancellationToken cancellationToken = default)
			=> ConnectLobby(conditions, cancellationToken);

		#region MonoBehaviourPunCallbacks
		// --------------------------------------------------------------------------------
		// MonoBehaviourPunCallbacks (PUN2のイベントハンドラ) - ロビー関連
		// --------------------------------------------------------------------------------

		/// <summary>
		/// ロビーに参加した際に呼び出されます。
		/// </summary>
		public override void OnJoinedLobby()
		{
			Debug.Log($"PUN2NetworkHandler.OnJoinedLobby: ロビー '{PhotonNetwork.CurrentLobby.Name}' に接続しました。");
			StationId = PhotonNetwork.CurrentLobby.Name;
			roomList.Clear(); // ロビー接続時にロビーリストをクリア
			OnHostStatusChanged?.Invoke(PhotonNetwork.IsMasterClient); // ホスト状態の更新 (マスタークライアントがホスト)
		}

		/// <summary>
		/// ロビーから退出した際に呼び出されます。
		/// </summary>
		public override void OnLeftLobby()
		{
			Debug.Log($"PUN2NetworkHandler.OnLeftLobby: ロビー '{PhotonNetwork.CurrentLobby.Name}' から離脱しました。");
			StationId = null;
			roomList.Clear(); // ロビー接続時にロビーリストをクリア
		}
		#endregion
	}
}

#endif
