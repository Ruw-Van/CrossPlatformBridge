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
	/// PhotonFusion用のルーム操作を実装するクラス。
	/// ルーム = Photon Fusion のセッション（GameMode.Host / Client）。
	/// </summary>
	public partial class NetworkHandler
	{
		/// <summary>
		/// ルームを作成します（ホストとして起動）。
		/// </summary>
		public async UniTask<bool> CreateRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_runner == null)
			{
				Debug.LogError("PhotonFusion: Runnerが初期化されていません");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Runner not initialized");
				return false;
			}

			try
			{
				var settings = baseSettings as RoomSettings;
				Debug.Log($"PhotonFusion: ルーム作成中 name='{settings?.RoomName}', maxPlayers={settings?.MaxPlayers}");

				cancellationToken.ThrowIfCancellationRequested();

				var startArgs = new StartGameArgs
				{
					GameMode = GameMode.Host,
					SessionName = settings?.RoomName ?? "Room",
					PlayerCount = settings?.MaxPlayers ?? 8,
					IsVisible = settings?.IsVisible ?? true,
					IsOpen = settings?.IsOpen ?? true,
					SceneManager = _runner.GetComponent<INetworkSceneManager>()
				};

				var result = await _runner.StartGame(startArgs);
				cancellationToken.ThrowIfCancellationRequested();

				if (result.Ok)
				{
					IsConnected = true;
					IsHost = true;
					AccountId = _runner.LocalPlayer.RawEncoded.ToString();
					NickName = _runner.LocalPlayer.ToString();
					StationId = _runner.SessionInfo?.Name;
					Debug.Log($"PhotonFusion: ルーム作成完了 session={StationId}");
					OnHostStatusChanged?.Invoke(true);
					OnNetworkConnectionStatusChanged?.Invoke(true);
					OnRoomOperationCompleted?.Invoke("CreateRoom", true, StationId?.ToString() ?? "");
				}
				else
				{
					Debug.LogError($"PhotonFusion: ルーム作成失敗 reason={result.ShutdownReason}");
					OnRoomOperationCompleted?.Invoke("CreateRoom", false, result.ShutdownReason.ToString());
				}
				return result.Ok;
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("PhotonFusion: ルーム作成がキャンセルされました。");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Cancelled");
				return false;
			}
		}

		/// <summary>
		/// 既存のルームに参加します（クライアントとして接続）。
		/// </summary>
		public async UniTask<bool> ConnectRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_runner == null)
			{
				Debug.LogError("PhotonFusion: Runnerが初期化されていません");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Runner not initialized");
				return false;
			}

			try
			{
				var settings = baseSettings as RoomSettings;
				Debug.Log($"PhotonFusion: ルームに接続中 name='{settings?.RoomName}'");

				cancellationToken.ThrowIfCancellationRequested();

				var startArgs = new StartGameArgs
				{
					GameMode = GameMode.Client,
					SessionName = settings?.RoomName ?? "Room",
					SceneManager = _runner.GetComponent<INetworkSceneManager>()
				};

				var result = await _runner.StartGame(startArgs);
				cancellationToken.ThrowIfCancellationRequested();

				if (result.Ok)
				{
					IsConnected = true;
					IsHost = false;
					AccountId = _runner.LocalPlayer.RawEncoded.ToString();
					NickName = _runner.LocalPlayer.ToString();
					StationId = _runner.SessionInfo?.Name;
					Debug.Log($"PhotonFusion: ルーム接続完了 session={StationId}");
					OnHostStatusChanged?.Invoke(false);
					OnNetworkConnectionStatusChanged?.Invoke(true);
					OnRoomOperationCompleted?.Invoke("ConnectRoom", true, StationId?.ToString() ?? "");
				}
				else
				{
					Debug.LogError($"PhotonFusion: ルーム接続失敗 reason={result.ShutdownReason}");
					OnRoomOperationCompleted?.Invoke("ConnectRoom", false, result.ShutdownReason.ToString());
				}
				return result.Ok;
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("PhotonFusion: ルーム接続がキャンセルされました。");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Cancelled");
				return false;
			}
		}

		/// <summary>
		/// ルームから切断します。
		/// </summary>
		public async UniTask DisconnectRoom()
		{
			Debug.Log("PhotonFusion: ルームから切断中...");
			if (_runner != null)
			{
				await _runner.Shutdown();
			}
			IsConnected = false;
			IsHost = false;
			AccountId = null;
			NickName = null;
			StationId = null;
			ConnectedList.Clear();
			OnHostStatusChanged?.Invoke(false);
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnRoomOperationCompleted?.Invoke("DisconnectRoom", true, "");
			Debug.Log("PhotonFusion: ルーム切断完了");
		}

		/// <summary>
		/// 利用可能なルームの一覧を返します。
		/// ロビーに参加済みの場合は OnSessionListUpdated で更新された一覧を返します。
		/// baseSettings.RoomName が指定されている場合は名前でフィルタします。
		/// </summary>
		public UniTask<List<object>> SearchRoom(IRoomSettings baseSettings)
		{
			if (_runner == null)
			{
				Debug.LogError("PhotonFusion: Runnerが初期化されていません");
				return UniTask.FromResult(new List<object>());
			}

			var query = baseSettings?.RoomName ?? "";
			IEnumerable<SessionInfo> filtered = string.IsNullOrEmpty(query)
				? roomList
				: roomList.Where(s => s.Name.Contains(query));

			var results = filtered.Cast<object>().ToList();
			Debug.Log($"PhotonFusion: SearchRoom query='{query}', hit={results.Count}件");
			return UniTask.FromResult(results);
		}
	}
}

#endif
