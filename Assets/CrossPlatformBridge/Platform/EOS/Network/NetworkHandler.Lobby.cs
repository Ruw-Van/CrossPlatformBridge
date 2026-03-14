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
	/// EOS LobbyInterface を使ったロビー操作を実装するクラス。
	/// </summary>
	public partial class NetworkHandler
	{
		private string _currentLobbyId = null;

		public async UniTask<bool> CreateLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_platform == null || _productUserId == null)
			{
				Debug.LogError("EOS: Platform または ProductUserId が未初期化です");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Not initialized");
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
				BucketId = settings?.RoomName ?? "default",
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
					_currentLobbyId = result.LobbyId;
					StationId = _currentLobbyId;
					Debug.Log($"EOS: ロビー作成完了 LobbyId={_currentLobbyId}");
					OnLobbyOperationCompleted?.Invoke("CreateLobby", true, _currentLobbyId);
					return true;
				}
				else
				{
					Debug.LogError($"EOS: ロビー作成失敗 result={result.ResultCode}");
					OnLobbyOperationCompleted?.Invoke("CreateLobby", false, result.ResultCode.ToString());
					return false;
				}
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("EOS: ロビー作成がキャンセルされました。");
				OnLobbyOperationCompleted?.Invoke("CreateLobby", false, "Cancelled");
				return false;
			}
		}

		public async UniTask<bool> ConnectLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_platform == null || _productUserId == null)
			{
				Debug.LogError("EOS: Platform または ProductUserId が未初期化です");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Not initialized");
				return false;
			}

			var settings = baseSettings as RoomSettings;
			var lobbyInterface = _platform.GetLobbyInterface();

			// LobbyId が指定されている場合は JoinLobbyById、そうでなければ JoinLobby
			if (!string.IsNullOrEmpty(settings?.LobbyId))
			{
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
						_currentLobbyId = settings.LobbyId;
						StationId = _currentLobbyId;
						Debug.Log($"EOS: ロビー参加完了 LobbyId={_currentLobbyId}");
						OnLobbyOperationCompleted?.Invoke("ConnectLobby", true, _currentLobbyId);
						return true;
					}
					Debug.LogError($"EOS: ロビー参加失敗 result={result.ResultCode}");
					OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, result.ResultCode.ToString());
					return false;
				}
				catch (System.OperationCanceledException)
				{
					Debug.Log("EOS: ロビー参加がキャンセルされました。");
					OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "Cancelled");
					return false;
				}
			}
			else
			{
				// TODO: SearchLobby で取得した LobbyDetails を使って JoinLobby を呼ぶ実装に拡張してください
				Debug.LogWarning("EOS: ConnectLobby には RoomSettings.LobbyId を指定してください");
				OnLobbyOperationCompleted?.Invoke("ConnectLobby", false, "LobbyId not specified");
				return false;
			}
		}

		public async UniTask DisconnectLobby()
		{
			if (_platform == null || _productUserId == null || string.IsNullOrEmpty(_currentLobbyId))
			{
				Debug.LogWarning("EOS: ロビーに参加していません");
				OnLobbyOperationCompleted?.Invoke("DisconnectLobby", true, "");
				return;
			}

			var lobbyInterface = _platform.GetLobbyInterface();
			var options = new LeaveLobbyOptions
			{
				LocalUserId = _productUserId,
				LobbyId = _currentLobbyId,
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
				_currentLobbyId = null;
				StationId = null;
			}
			Debug.Log($"EOS: ロビー退出完了 result={result.ResultCode}");
			OnLobbyOperationCompleted?.Invoke("DisconnectLobby", success, success ? "" : result.ResultCode.ToString());
		}

		public async UniTask<List<object>> SearchLobby(IRoomSettings baseSettings)
		{
			if (_platform == null || _productUserId == null)
			{
				Debug.LogError("EOS: Platform または ProductUserId が未初期化です");
				return new List<object>();
			}

			var settings = baseSettings as RoomSettings;
			var lobbyInterface = _platform.GetLobbyInterface();

			var createSearchOptions = new CreateLobbySearchOptions { MaxResults = 50 };
			lobbyInterface.CreateLobbySearch(ref createSearchOptions, out var search);

			if (search == null)
			{
				Debug.LogError("EOS: LobbySearch 作成失敗");
				return new List<object>();
			}

			// BucketId（ルーム名）でフィルタ
			if (!string.IsNullOrEmpty(settings?.RoomName))
			{
				var param = new LobbySearchSetParameterOptions
				{
					ComparisonOp = ComparisonOp.Equal,
					Parameter = new AttributeData
					{
						Key = "bucket",
						Value = new AttributeDataValue { AsUtf8 = settings.RoomName },
					},
				};
				search.SetParameter(ref param);
			}

			var findOptions = new LobbySearchFindOptions { LocalUserId = _productUserId };
			var tcs = new UniTaskCompletionSource<LobbySearchFindCallbackInfo>();
			search.Find(ref findOptions, null, (ref LobbySearchFindCallbackInfo info) =>
			{
				tcs.TrySetResult(info);
			});

			var result = await tcs.Task;
			var list = new List<object>();
			try
			{
				if (result.ResultCode == Result.Success)
				{
					var countOptions = new LobbySearchGetSearchResultCountOptions();
					uint count = search.GetSearchResultCount(ref countOptions);
					for (uint i = 0; i < count; i++)
					{
						var copyOptions = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i };
						search.CopySearchResultByIndex(ref copyOptions, out var details);
						if (details != null) list.Add(details);
					}
				}
				else
				{
					Debug.LogWarning($"EOS: SearchLobby 失敗 result={result.ResultCode}");
				}
			}
			finally
			{
				search.Release();
			}
			Debug.Log($"EOS: SearchLobby 結果 {list.Count} 件");
			return list;
		}
	}
}

#endif
