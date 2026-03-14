#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
#if !DISABLE_PLAYFABCLIENT_API

using System.Collections.Generic;
using System.Linq;
using CrossPlatformBridge.Services.Leaderboard;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace CrossPlatformBridge.Platform.PlayFab.Leaderboard
{
	/// <summary>
	/// PlayFab を使用したリーダーボード実装。
	/// グローバルランキングは PlayFab Statistics API を使用し、
	/// セッション内ランキングはインメモリで管理します。
	/// </summary>
	public class LeaderboardHandler : IInternalLeaderboardHandler, IServiceTestProvider
	{
		// セッション内ランキング: playerId → entry
		private readonly Dictionary<string, LeaderboardEntry> _sessionScores
			= new Dictionary<string, LeaderboardEntry>();

		// ── グローバルランキング ──────────────────────────────────

		/// <inheritdoc/>
		public UniTask<bool> SubmitScore(string leaderboardName, long score)
		{
			var tcs = new UniTaskCompletionSource<bool>();

			var request = new UpdatePlayerStatisticsRequest
			{
				Statistics = new List<StatisticUpdate>
				{
					new StatisticUpdate
					{
						StatisticName = leaderboardName,
						Value         = (int)score,
					}
				}
			};

			PlayFabClientAPI.UpdatePlayerStatistics(request,
				_ =>
				{
					Debug.Log($"[PlayFab Leaderboard] SubmitScore: {leaderboardName} = {score}");
					tcs.TrySetResult(true);
				},
				error =>
				{
					Debug.LogError($"[PlayFab Leaderboard] SubmitScore failed: {leaderboardName}, Error: {error.GenerateErrorReport()}");
					tcs.TrySetResult(false);
				});

			return tcs.Task;
		}

		/// <inheritdoc/>
		public UniTask<List<LeaderboardEntry>> GetTopEntries(string leaderboardName, int count)
		{
			var tcs = new UniTaskCompletionSource<List<LeaderboardEntry>>();

			var request = new GetLeaderboardRequest
			{
				StatisticName  = leaderboardName,
				StartPosition  = 0,
				MaxResultsCount = count,
			};

			PlayFabClientAPI.GetLeaderboard(request,
				result =>
				{
					var entries = ConvertLeaderboard(result.Leaderboard);
					tcs.TrySetResult(entries);
				},
				error =>
				{
					Debug.LogError($"[PlayFab Leaderboard] GetTopEntries failed: {leaderboardName}, Error: {error.GenerateErrorReport()}");
					tcs.TrySetResult(new List<LeaderboardEntry>());
				});

			return tcs.Task;
		}

		/// <inheritdoc/>
		public UniTask<LeaderboardEntry> GetPlayerEntry(string leaderboardName, string playerId)
		{
			if (string.IsNullOrEmpty(playerId))
				return UniTask.FromResult<LeaderboardEntry>(null);

			var tcs = new UniTaskCompletionSource<LeaderboardEntry>();

			var request = new GetLeaderboardAroundPlayerRequest
			{
				StatisticName   = leaderboardName,
				PlayFabId       = playerId,
				MaxResultsCount = 1,
			};

			PlayFabClientAPI.GetLeaderboardAroundPlayer(request,
				result =>
				{
					var entries = ConvertLeaderboard(result.Leaderboard);
					var entry   = entries.FirstOrDefault(e => e.PlayerId == playerId);
					tcs.TrySetResult(entry);
				},
				error =>
				{
					Debug.LogError($"[PlayFab Leaderboard] GetPlayerEntry failed: {leaderboardName}, Error: {error.GenerateErrorReport()}");
					tcs.TrySetResult(null);
				});

			return tcs.Task;
		}

		/// <inheritdoc/>
		public UniTask<List<LeaderboardEntry>> GetEntriesAroundPlayer(string leaderboardName, string playerId, int range)
		{
			if (string.IsNullOrEmpty(playerId))
				return UniTask.FromResult(new List<LeaderboardEntry>());

			var tcs = new UniTaskCompletionSource<List<LeaderboardEntry>>();

			var request = new GetLeaderboardAroundPlayerRequest
			{
				StatisticName   = leaderboardName,
				PlayFabId       = playerId,
				MaxResultsCount = range * 2 + 1,
			};

			PlayFabClientAPI.GetLeaderboardAroundPlayer(request,
				result =>
				{
					var entries = ConvertLeaderboard(result.Leaderboard);
					tcs.TrySetResult(entries);
				},
				error =>
				{
					Debug.LogError($"[PlayFab Leaderboard] GetEntriesAroundPlayer failed: {leaderboardName}, Error: {error.GenerateErrorReport()}");
					tcs.TrySetResult(new List<LeaderboardEntry>());
				});

			return tcs.Task;
		}

		// ── セッション内ランキング（インメモリ） ──────────────────

		/// <inheritdoc/>
		public UniTask<bool> UpdateSessionScore(string playerId, string playerName, long score)
		{
			if (string.IsNullOrEmpty(playerId))
			{
				Debug.LogWarning("[PlayFab Leaderboard] UpdateSessionScore: playerId is null or empty.");
				return UniTask.FromResult(false);
			}

			_sessionScores[playerId] = new LeaderboardEntry
			{
				PlayerId   = playerId,
				PlayerName = playerName ?? playerId,
				Score      = score,
				Rank       = 0,
			};

			RecalculateSessionRanks();
			Debug.Log($"[PlayFab Leaderboard] UpdateSessionScore: {playerId} = {score}");
			return UniTask.FromResult(true);
		}

		/// <inheritdoc/>
		public UniTask<List<LeaderboardEntry>> GetSessionLeaderboard()
		{
			return UniTask.FromResult(
				_sessionScores.Values.OrderBy(e => e.Rank).ToList()
			);
		}

		/// <inheritdoc/>
		public UniTask<int> GetPlayerSessionRank(string playerId)
		{
			if (_sessionScores.TryGetValue(playerId, out var entry))
				return UniTask.FromResult(entry.Rank);

			return UniTask.FromResult(-1);
		}

		/// <inheritdoc/>
		public UniTask ResetSessionLeaderboard()
		{
			_sessionScores.Clear();
			return UniTask.CompletedTask;
		}

		// --------------------------------------------------------------------------------
		// プライベート
		// --------------------------------------------------------------------------------

		private static List<LeaderboardEntry> ConvertLeaderboard(List<PlayerLeaderboardEntry> source)
		{
			if (source == null) return new List<LeaderboardEntry>();

			var result = new List<LeaderboardEntry>(source.Count);
			foreach (var e in source)
			{
				result.Add(new LeaderboardEntry
				{
					PlayerId   = e.PlayFabId ?? "",
					PlayerName = e.DisplayName ?? e.PlayFabId ?? "",
					Score      = (long)e.StatValue,
					Rank       = e.Position + 1,   // PlayFab は 0-based
				});
			}
			return result;
		}

		private void RecalculateSessionRanks()
		{
			int rank = 1;
			foreach (var entry in _sessionScores.Values.OrderByDescending(e => e.Score))
			{
				entry.Rank = rank++;
			}
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => new TestOperation[]
		{
			new TestOperation { SectionLabel = "グローバルランキング (PlayFab)" },
			new TestOperation
			{
				Label  = "Submit Score",
				Action = async ctx =>
				{
					long score = long.TryParse(ctx.LeaderboardScore, out long s) ? s : 1000L;
					bool ok    = await SubmitScore(ctx.LeaderboardName, score);
					ctx.ReportResult(ok ? $"スコア送信完了: {ctx.LeaderboardName} = {score}" : "送信失敗");
					ctx.AppendLog($"SubmitScore({ctx.LeaderboardName}, {score}) → {ok}");
				}
			},
			new TestOperation
			{
				Label  = "Get Top 10",
				Action = async ctx =>
				{
					var list   = await GetTopEntries(ctx.LeaderboardName, 10);
					string res = list?.Count > 0
						? string.Join("\n", list.Select(e => $"#{e.Rank} {e.PlayerName}: {e.Score}"))
						: "（エントリなし）";
					ctx.ReportResult(res);
					ctx.AppendLog($"GetTopEntries({ctx.LeaderboardName}, 10) → {list?.Count ?? 0} 件");
				}
			},
			new TestOperation
			{
				Label  = "Get My Entry",
				Action = async ctx =>
				{
					var myId   = global::PlayFab.PlayFabSettings.staticPlayer?.PlayFabId ?? "";
					var entry  = await GetPlayerEntry(ctx.LeaderboardName, myId);
					ctx.ReportResult(entry != null
						? $"#{entry.Rank} {entry.PlayerName}: {entry.Score}"
						: "（エントリなし）");
					ctx.AppendLog($"GetPlayerEntry({ctx.LeaderboardName}, {myId}) → {(entry != null ? "found" : "null")}");
				}
			},
			new TestOperation { SectionLabel = "セッション内ランキング (PlayFab)" },
			new TestOperation
			{
				Label  = "Update Session Score",
				Action = async ctx =>
				{
					var myId   = global::PlayFab.PlayFabSettings.staticPlayer?.PlayFabId ?? ctx.UserName;
					long score = long.TryParse(ctx.LeaderboardScore, out long s) ? s : 500L;
					bool ok    = await UpdateSessionScore(myId, ctx.UserName, score);
					ctx.ReportResult(ok ? $"セッションスコア更新: {myId} = {score}" : "更新失敗");
					ctx.AppendLog($"UpdateSessionScore({myId}, {score}) → {ok}");
				}
			},
			new TestOperation
			{
				Label  = "Get Session Leaderboard",
				Action = async ctx =>
				{
					var list   = await GetSessionLeaderboard();
					string res = list?.Count > 0
						? string.Join("\n", list.Select(e => $"#{e.Rank} {e.PlayerName}: {e.Score}"))
						: "（エントリなし）";
					ctx.ReportResult(res);
					ctx.AppendLog($"GetSessionLeaderboard → {list?.Count ?? 0} 件");
				}
			},
			new TestOperation
			{
				Label  = "Reset Session",
				Action = async ctx =>
				{
					await ResetSessionLeaderboard();
					ctx.ReportResult("セッションリセット完了");
					ctx.AppendLog("ResetSessionLeaderboard → done");
				}
			},
		};

		public TestDefaultData GetDefaultData() => new TestDefaultData
		{
			LeaderboardName  = "playfab_leaderboard_001",
			LeaderboardScore = "1000",
		};
	}
}

#endif // !DISABLE_PLAYFABCLIENT_API
#endif // USE_CROSSPLATFORMBRIDGE_PLAYFAB
