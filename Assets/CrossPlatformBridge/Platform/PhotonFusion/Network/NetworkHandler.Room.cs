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
		/// SearchLobby に委譲します（PhotonFusion ではロビーとルームのリストは共通）。
		/// </summary>
		public UniTask<List<object>> SearchRoom(IRoomSettings baseSettings)
		{
			if (_runner == null)
			{
				Debug.LogError("PhotonFusion: Runnerが初期化されていません");
				return UniTask.FromResult(new List<object>());
			}
			return SearchLobby(baseSettings);
		}

		/// <summary>
		/// ルームをマッチメイキングします。
		/// ロビー接続（ConnectLobby）後に SearchRoom でフィルタし、最初のルームに接続します。
		/// createIfNotFound が true の場合、見つからなければ新規作成します。
		/// </summary>
		public async UniTask<bool> MatchmakeRoom(IRoomSettings conditions, bool createIfNotFound = false, CancellationToken cancellationToken = default)
		{
			if (!_isInLobby)
			{
				Debug.LogWarning("PhotonFusion: MatchmakeRoom にはロビー接続が必要です。先に ConnectLobby を呼んでください。");
				OnRoomOperationCompleted?.Invoke("MatchmakeRoom", false, "Not in lobby.");
				return false;
			}

			var results = await SearchRoom(conditions);
			if (results != null && results.Count > 0 && results[0] is IRoomSettings target)
				return await ConnectRoom(target, cancellationToken);

			if (createIfNotFound)
			{
				Debug.Log("PhotonFusion: マッチするルームが見つからないため新規作成します。");
				return await CreateRoom(conditions, cancellationToken);
			}

			OnRoomOperationCompleted?.Invoke("MatchmakeRoom", false, "マッチするルームが見つかりませんでした。");
			return false;
		}
	}
}

#endif
