#if USE_CROSSPLATFORMBRIDGE_EOS
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Network
{
	/// <summary>
	/// EOS LobbyInterface をルームとして使うクラス。
	/// ロビー（セッションリスト）とルーム（プレイセッション）を同じ LobbyInterface で管理する。
	/// </summary>
	public partial class NetworkHandler
	{
		private string _currentRoomId = null;

		public async UniTask<bool> CreateRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_platform == null || _productUserId == null)
			{
				Debug.LogError("EOS: Platform または ProductUserId が未初期化です");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Not initialized");
				return false;
			}

			var settings = baseSettings as RoomSettings;
			var lobbyInterface = _platform.GetLobbyInterface();

			var options = new CreateLobbyOptions
			{
				LocalUserId = _productUserId,
				MaxLobbyMembers = (uint)(settings?.MaxPlayers ?? 8),
				PermissionLevel = settings?.IsVisible == true
					? LobbyPermissionLevel.Publicadvertised
					: LobbyPermissionLevel.Inviteonly,
				BucketId = settings?.RoomName ?? "room",
				// TODO: カスタムプロパティを LobbyAttribute として追加
			};

			var tcs = new UniTaskCompletionSource<CreateLobbyCallbackInfo>();
			using var reg = cancellationToken.Register(() => tcs.TrySetCanceled());
			lobbyInterface.CreateLobby(ref options, null, (ref CreateLobbyCallbackInfo info) =>
			{
				tcs.TrySetResult(info);
			});

			try
			{
				var result = await tcs.Task;
				if (result.ResultCode == Result.Success)
				{
					_currentRoomId = result.LobbyId;
					StationId = _currentRoomId;
					IsHost = true;
					Debug.Log($"EOS: ルーム作成完了 LobbyId={_currentRoomId}");
					OnHostStatusChanged?.Invoke(true);
					OnRoomOperationCompleted?.Invoke("CreateRoom", true, _currentRoomId);
					return true;
				}
				else
				{
					Debug.LogError($"EOS: ルーム作成失敗 result={result.ResultCode}");
					OnRoomOperationCompleted?.Invoke("CreateRoom", false, result.ResultCode.ToString());
					return false;
				}
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("EOS: ルーム作成がキャンセルされました。");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Cancelled");
				return false;
			}
		}

		public async UniTask<bool> ConnectRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_platform == null || _productUserId == null)
			{
				Debug.LogError("EOS: Platform または ProductUserId が未初期化です");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Not initialized");
				return false;
			}

			var settings = baseSettings as RoomSettings;
			if (string.IsNullOrEmpty(settings?.LobbyId))
			{
				Debug.LogWarning("EOS: ConnectRoom には RoomSettings.LobbyId を指定してください");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "LobbyId not specified");
				return false;
			}

			var lobbyInterface = _platform.GetLobbyInterface();
			var options = new JoinLobbyByIdOptions
			{
				LobbyId = settings.LobbyId,
				LocalUserId = _productUserId,
			};

			var tcs = new UniTaskCompletionSource<JoinLobbyByIdCallbackInfo>();
			using var reg = cancellationToken.Register(() => tcs.TrySetCanceled());
			lobbyInterface.JoinLobbyById(ref options, null, (ref JoinLobbyByIdCallbackInfo info) =>
			{
				tcs.TrySetResult(info);
			});

			try
			{
				var result = await tcs.Task;
				if (result.ResultCode == Result.Success)
				{
					_currentRoomId = settings.LobbyId;
					StationId = _currentRoomId;
					IsHost = false;
					Debug.Log($"EOS: ルーム参加完了 LobbyId={_currentRoomId}");
					OnHostStatusChanged?.Invoke(false);
					OnRoomOperationCompleted?.Invoke("ConnectRoom", true, _currentRoomId);
					return true;
				}
				else
				{
					Debug.LogError($"EOS: ルーム参加失敗 result={result.ResultCode}");
					OnRoomOperationCompleted?.Invoke("ConnectRoom", false, result.ResultCode.ToString());
					return false;
				}
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("EOS: ルーム参加がキャンセルされました。");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Cancelled");
				return false;
			}
		}

		public async UniTask DisconnectRoom()
		{
			if (_platform == null || _productUserId == null || string.IsNullOrEmpty(_currentRoomId))
			{
				Debug.LogWarning("EOS: ルームに参加していません");
				OnRoomOperationCompleted?.Invoke("DisconnectRoom", true, "");
				return;
			}

			var lobbyInterface = _platform.GetLobbyInterface();
			var options = new LeaveLobbyOptions
			{
				LocalUserId = _productUserId,
				LobbyId = _currentRoomId,
			};

			var tcs = new UniTaskCompletionSource<LeaveLobbyCallbackInfo>();
			lobbyInterface.LeaveLobby(ref options, null, (ref LeaveLobbyCallbackInfo info) =>
			{
				tcs.TrySetResult(info);
			});

			var result = await tcs.Task;
			bool success = result.ResultCode == Result.Success;
			if (success)
			{
				_currentRoomId = null;
				StationId = null;
				IsHost = false;
				ConnectedList.Clear();
			}
			OnHostStatusChanged?.Invoke(false);
			OnRoomOperationCompleted?.Invoke("DisconnectRoom", success, success ? "" : result.ResultCode.ToString());
			Debug.Log($"EOS: ルーム退出完了 result={result.ResultCode}");
		}

		public UniTask<List<object>> SearchRoom(IRoomSettings baseSettings)
		{
			// Room 検索は Lobby 検索と同じロジックを使用
			return SearchLobby(baseSettings);
		}

		/// <summary>
		/// 条件に合うルームを検索して接続します。
		/// createIfNotFound が true の場合、見つからなければ conditions でルームを作成します。
		/// </summary>
		public async UniTask<bool> MatchmakeRoom(IRoomSettings conditions, bool createIfNotFound = false, CancellationToken cancellationToken = default)
		{
			var results = await SearchRoom(conditions);
			if (results != null && results.Count > 0 && results[0] is IRoomSettings target)
				return await ConnectRoom(target, cancellationToken);

			if (createIfNotFound)
			{
				Debug.Log("EOS: マッチするルームが見つからないため新規作成します。");
				return await CreateRoom(conditions, cancellationToken);
			}

			OnRoomOperationCompleted?.Invoke("MatchmakeRoom", false, "マッチするルームが見つかりませんでした。");
			return false;
		}
	}
}

#endif
