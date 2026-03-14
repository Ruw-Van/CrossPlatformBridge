#if USE_CROSSPLATFORMBRIDGE_PHOTONFUSION
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CrossPlatformBridge.Services.Network;
using Fusion;
using UnityEngine;

namespace CrossPlatformBridge.Platform.PhotonFusion.Network
{
	/// <summary>
	/// PhotonFusion用のロビー操作を実装するクラス。
	/// Photon Fusion のロビー = JoinSessionLobby でセッションリストの購読を開始する仕組み。
	/// ロビー参加中は OnSessionListUpdated が定期的に呼ばれ、セッション一覧が更新される。
	/// </summary>
	public partial class NetworkHandler
	{
		// ロビー参加中かどうかのフラグ
		private bool _isInLobby = false;

		/// <summary>
		/// ロビーを作成します。
		/// Photon Fusion にはロビー「作成」の概念がないため、
		/// ロビーへの参加（セッションリスト購読開始）として実装します。
		/// </summary>
		public async UniTask<bool> CreateLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_runner == null)
			{
				Debug.LogError("PhotonFusion: Runnerが初期化されていません");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Runner not initialized");
				return false;
			}

			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				Debug.Log("PhotonFusion: ロビーに参加中（セッションリスト購読開始）...");
				var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
				cancellationToken.ThrowIfCancellationRequested();
				if (result.Ok)
				{
					_isInLobby = true;
					StationId = "ClientServer";
					Debug.Log("PhotonFusion: ロビー参加完了");
					OnLobbyOperationCompleted?.Invoke("CreateLobby", true, "ClientServer");
					return true;
				}
				else
				{
					Debug.LogError($"PhotonFusion: ロビー参加失敗 reason={result.ShutdownReason}");
					OnLobbyOperationCompleted?.Invoke("CreateLobby", false, result.ShutdownReason.ToString());
					return false;
				}
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("PhotonFusion: ロビー作成がキャンセルされました。");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Cancelled");
				return false;
			}
		}

		/// <summary>
		/// 既存のロビーに参加します。
		/// Photon Fusion では CreateLobby と同じ JoinSessionLobby で実装します。
		/// </summary>
		public async UniTask<bool> ConnectLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_runner == null)
			{
				Debug.LogError("PhotonFusion: Runnerが初期化されていません");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Runner not initialized");
				return false;
			}

			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				Debug.Log("PhotonFusion: ロビーに接続中...");
				var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);
				cancellationToken.ThrowIfCancellationRequested();
				if (result.Ok)
				{
					_isInLobby = true;
					StationId = "ClientServer";
					Debug.Log("PhotonFusion: ロビー接続完了");
					OnLobbyOperationCompleted?.Invoke("ConnectLobby", true, "ClientServer");
					return true;
				}
				else
				{
					Debug.LogError($"PhotonFusion: ロビー接続失敗 reason={result.ShutdownReason}");
					OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, result.ShutdownReason.ToString());
					return false;
				}
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("PhotonFusion: ロビー接続がキャンセルされました。");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Cancelled");
				return false;
			}
		}

		/// <summary>
		/// ロビーから切断します。
		/// Runner をシャットダウンしてセッションリスト購読を終了します。
		/// </summary>
		public async UniTask DisconnectLobby()
		{
			Debug.Log("PhotonFusion: ロビーから切断中...");
			if (_runner != null)
			{
				await _runner.Shutdown();
			}
			_isInLobby = false;
			StationId = null;
			roomList.Clear();
			OnLobbyOperationCompleted?.Invoke("DisconnectLobby", true, "");
			Debug.Log("PhotonFusion: ロビー切断完了");
		}

		/// <summary>
		/// ロビー内のセッション（ルーム）一覧を返します。
		/// OnSessionListUpdated で更新された roomList をフィルタして RoomSettings にラップして返します。
		/// baseSettings.RoomName や CustomProperties が指定されている場合はクライアント側でフィルタします。
		/// </summary>
		public UniTask<List<object>> SearchLobby(IRoomSettings baseSettings)
		{
			if (!_isInLobby)
				Debug.LogWarning("PhotonFusion: ロビーに参加していません。先に CreateLobby / ConnectLobby を呼んでください。");

			var query = baseSettings?.RoomName ?? "";
			var results = new List<object>();

			foreach (var session in roomList)
			{
				if (!string.IsNullOrEmpty(query) && !session.Name.Contains(query)) continue;

				if (baseSettings?.CustomProperties != null && baseSettings.CustomProperties.Count > 0)
				{
					bool match = true;
					foreach (var kv in baseSettings.CustomProperties)
					{
						if (!session.Properties.TryGetValue(kv.Key, out SessionProperty val) ||
							val.ToString() != kv.Value?.ToString())
						{
							match = false;
							break;
						}
					}
					if (!match) continue;
				}

				results.Add(new RoomSettings
				{
					Id = session.Name,
					RoomName = session.Name,
					MaxPlayers = session.MaxPlayers,
					IsOpen = session.IsOpen,
					IsVisible = session.IsVisible,
				});
			}

			Debug.Log($"PhotonFusion: SearchLobby query='{query}', hit={results.Count}件");
			return UniTask.FromResult(results);
		}

		/// <summary>
		/// ロビーをマッチメイキングします。
		/// PhotonFusion にはロビー選択の概念がないため ConnectLobby に委譲します。
		/// </summary>
		public UniTask<bool> MatchmakeLobby(IRoomSettings conditions, CancellationToken cancellationToken = default)
			=> ConnectLobby(conditions, cancellationToken);
	}
}

#endif
