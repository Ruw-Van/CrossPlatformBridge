#if USE_CROSSPLATFORMBRIDGE_EOS
using System.Collections.Generic;
using System.Linq;
using CrossPlatformBridge.Services.Leaderboard;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Leaderboards;
using Epic.OnlineServices.Stats;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Leaderboard
{
	/// <summary>
	/// EOS リーダーボード用の内部実装。
	/// グローバルランキングは EOS Stats Interface（スコア送信）と Leaderboards Interface（ランキング取得）を使用します。
	/// セッション内ランキングはインメモリで管理します。
	/// </summary>
	public class EOSLeaderboardHandler : IInternalLeaderboardHandler, IServiceTestProvider
	{
		private StatsInterface Stats =>
			EOSManager.Instance.GetEOSPlatformInterface().GetStatsInterface();

		private LeaderboardsInterface LeaderboardsI =>
			EOSManager.Instance.GetEOSPlatformInterface().GetLeaderboardsInterface();

		private ProductUserId LocalUserId
		{
			get
			{
				var connect = EOSManager.Instance.GetEOSPlatformInterface()?.GetConnectInterface();
				if (connect == null || connect.GetLoggedInUsersCount() == 0) return null;
				return connect.GetLoggedInUserByIndex(0);
			}
		}

		// セッション内ランキングはインメモリ管理
		private readonly Dictionary<string, LeaderboardEntry> _sessionScores
			= new Dictionary<string, LeaderboardEntry>();

		// ── グローバルランキング ──────────────────────────────────

		/// <inheritdoc/>
		public UniTask<bool> SubmitScore(string leaderboardName, long score)
		{
			// EOS のリーダーボードはサーバーサイドで Stat に基づいて算出される。
			// Stat 名をリーダーボード名と同じにする運用を前提とする。
			var tcs = new UniTaskCompletionSource<bool>();

			var options = new IngestStatOptions
			{
				LocalUserId = LocalUserId,
				TargetUserId = LocalUserId,
				Stats = new IngestData[]
				{
					new IngestData
					{
						StatName = leaderboardName,
						IngestAmount = (int)score
					}
				}
			};

			Stats.IngestStat(ref options, null, (ref IngestStatCompleteCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
				{
					Debug.Log($"[EOS Leaderboard] SubmitScore: {leaderboardName} = {score}");
					tcs.TrySetResult(true);
				}
				else
				{
					Debug.LogError($"[EOS Leaderboard] SubmitScore failed: {leaderboardName}, Result: {info.ResultCode}");
					tcs.TrySetResult(false);
				}
			});

			return tcs.Task;
		}

		/// <inheritdoc/>
		public UniTask<List<LeaderboardEntry>> GetTopEntries(string leaderboardName, int count)
		{
			var tcs = new UniTaskCompletionSource<List<LeaderboardEntry>>();

			var queryOptions = new QueryLeaderboardRanksOptions
			{
				LeaderboardId = leaderboardName,
				LocalUserId = LocalUserId
			};

			LeaderboardsI.QueryLeaderboardRanks(ref queryOptions, null, (ref OnQueryLeaderboardRanksCompleteCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
				{
					var entries = CopyLeaderboardRecords(count);
					tcs.TrySetResult(entries);
				}
				else
				{
					Debug.LogError($"[EOS Leaderboard] GetTopEntries failed: {leaderboardName}, Result: {info.ResultCode}");
					tcs.TrySetResult(new List<LeaderboardEntry>());
				}
			});

			return tcs.Task;
		}

		/// <inheritdoc/>
		public UniTask<LeaderboardEntry> GetPlayerEntry(string leaderboardName, string playerId)
		{
			if (string.IsNullOrEmpty(playerId))
				return UniTask.FromResult<LeaderboardEntry>(null);

			var tcs = new UniTaskCompletionSource<LeaderboardEntry>();

			var queryOptions = new QueryLeaderboardRanksOptions
			{
				LeaderboardId = leaderboardName,
				LocalUserId = LocalUserId
			};

			LeaderboardsI.QueryLeaderboardRanks(ref queryOptions, null, (ref OnQueryLeaderboardRanksCompleteCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
				{
					var all = CopyLeaderboardRecords();
					var entry = all.FirstOrDefault(e => e.PlayerId == playerId);
					tcs.TrySetResult(entry);
				}
				else
				{
					Debug.LogError($"[EOS Leaderboard] GetPlayerEntry failed: {leaderboardName}, Result: {info.ResultCode}");
					tcs.TrySetResult(null);
				}
			});

			return tcs.Task;
		}

		/// <inheritdoc/>
		public async UniTask<List<LeaderboardEntry>> GetEntriesAroundPlayer(string leaderboardName, string playerId, int range)
		{
			// EOS は「プレイヤー周辺」の直接APIがないため、全件取得後にメモリ上でスライスする。
			var playerEntry = await GetPlayerEntry(leaderboardName, playerId);
			if (playerEntry == null) return new List<LeaderboardEntry>();

			int startRank = Mathf.Max(1, playerEntry.Rank - range);
			int endRank = playerEntry.Rank + range;

			var tcs = new UniTaskCompletionSource<List<LeaderboardEntry>>();

			var queryOptions = new QueryLeaderboardRanksOptions
			{
				LeaderboardId = leaderboardName,
				LocalUserId = LocalUserId
			};

			LeaderboardsI.QueryLeaderboardRanks(ref queryOptions, null, (ref OnQueryLeaderboardRanksCompleteCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
				{
					var all = CopyLeaderboardRecords();
					var slice = all.Where(e => e.Rank >= startRank && e.Rank <= endRank).ToList();
					tcs.TrySetResult(slice);
				}
				else
				{
					Debug.LogError($"[EOS Leaderboard] GetEntriesAroundPlayer failed: {leaderboardName}, Result: {info.ResultCode}");
					tcs.TrySetResult(new List<LeaderboardEntry>());
				}
			});

			return await tcs.Task;
		}

		// ── セッション内ランキング（インメモリ） ──────────────────

		/// <inheritdoc/>
		public UniTask<bool> UpdateSessionScore(string playerId, string playerName, long score)
		{
			if (string.IsNullOrEmpty(playerId))
			{
				Debug.LogWarning("[EOS Leaderboard] UpdateSessionScore: playerId is null or empty.");
				return UniTask.FromResult(false);
			}

			_sessionScores[playerId] = new LeaderboardEntry
			{
				PlayerId = playerId,
				PlayerName = playerName ?? playerId,
				Score = score,
				Rank = 0
			};

			RecalculateSessionRanks();
			Debug.Log($"[EOS Leaderboard] UpdateSessionScore: {playerId} = {score}");
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

		private List<LeaderboardEntry> CopyLeaderboardRecords(int maxCount = int.MaxValue)
		{
			var getCountOptions = new GetLeaderboardRecordCountOptions();
			uint count = LeaderboardsI.GetLeaderboardRecordCount(ref getCountOptions);
			var result = new List<LeaderboardEntry>((int)count);

			for (uint i = 0; i < count; i++)
			{
				if (result.Count >= maxCount) break;

				var copyOptions = new CopyLeaderboardRecordByIndexOptions { LeaderboardRecordIndex = i };
				if (LeaderboardsI.CopyLeaderboardRecordByIndex(ref copyOptions, out LeaderboardRecord? record) == Result.Success
				    && record.HasValue)
				{
					result.Add(new LeaderboardEntry
					{
						PlayerId = record.Value.UserId?.ToString() ?? "",
						PlayerName = record.Value.UserDisplayName ?? "",
						Score = (long)record.Value.Score,
						Rank = (int)record.Value.Rank
					});
				}
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
			new TestOperation { SectionLabel = "グローバルランキング (EOS)" },
			new TestOperation
			{
				Label = "Submit Score",
				Action = async ctx =>
				{
					long score = long.TryParse(ctx.LeaderboardScore, out long s) ? s : 1000L;
					bool ok = await SubmitScore(ctx.LeaderboardName, score);
					ctx.ReportResult(ok ? $"スコア送信完了: {ctx.LeaderboardName} = {score}" : "送信失敗");
					ctx.AppendLog($"SubmitScore({ctx.LeaderboardName}, {score}) → {ok}");
				}
			},
			new TestOperation
			{
				Label = "Get Top 10",
				Action = async ctx =>
				{
					var list = await GetTopEntries(ctx.LeaderboardName, 10);
					string result = list?.Count > 0
						? string.Join("\n", list.Select(e => $"#{e.Rank} {e.PlayerName}: {e.Score}"))
						: "（エントリなし）";
					ctx.ReportResult(result);
					ctx.AppendLog($"GetTopEntries({ctx.LeaderboardName}, 10) → {list?.Count ?? 0} 件");
				}
			},
			new TestOperation { SectionLabel = "セッション内ランキング (EOS)" },
			new TestOperation
			{
				Label = "Update Session Score",
				Action = async ctx =>
				{
					var uid = LocalUserId?.ToString() ?? "unknown";
					long score = long.TryParse(ctx.LeaderboardScore, out long s) ? s : 500L;
					bool ok = await UpdateSessionScore(uid, ctx.UserName, score);
					ctx.ReportResult(ok ? $"セッションスコア更新: {uid} = {score}" : "更新失敗");
					ctx.AppendLog($"UpdateSessionScore({uid}, {score}) → {ok}");
				}
			},
			new TestOperation
			{
				Label = "Get Session Leaderboard",
				Action = async ctx =>
				{
					var list = await GetSessionLeaderboard();
					string result = list?.Count > 0
						? string.Join("\n", list.Select(e => $"#{e.Rank} {e.PlayerName}: {e.Score}"))
						: "（エントリなし）";
					ctx.ReportResult(result);
					ctx.AppendLog($"GetSessionLeaderboard → {list?.Count ?? 0} 件");
				}
			},
		};

		public TestDefaultData GetDefaultData() => new TestDefaultData
		{
			LeaderboardName = "eos_leaderboard_001",
			LeaderboardScore = "1000",
		};
	}
}
#endif
