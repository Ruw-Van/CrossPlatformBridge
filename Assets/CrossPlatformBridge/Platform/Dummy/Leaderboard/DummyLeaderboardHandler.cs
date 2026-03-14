using System.Collections.Generic;
using System.Linq;
using CrossPlatformBridge.Services.Leaderboard;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Platform.Dummy.Leaderboard
{
	/// <summary>
	/// リーダーボード機能のダミー実装。
	/// インメモリでグローバルランキングとセッション内ランキングを管理します。
	/// 開発およびテスト用に使用します。
	/// </summary>
	public class DummyLeaderboardHandler : IInternalLeaderboardHandler, IServiceTestProvider
	{
		// グローバルランキング: leaderboardName → (playerId → entry)
		private readonly Dictionary<string, Dictionary<string, LeaderboardEntry>> _globalScores
			= new Dictionary<string, Dictionary<string, LeaderboardEntry>>();

		// セッション内ランキング: playerId → entry
		private readonly Dictionary<string, LeaderboardEntry> _sessionScores
			= new Dictionary<string, LeaderboardEntry>();

		/// <summary>擬似的なネットワーク遅延（ミリ秒）</summary>
		public int SimulatedDelayMs { get; set; } = 100;

		/// <summary>SubmitScore 時に使用するプレイヤー名（テスト用）</summary>
		public string LocalPlayerName { get; set; } = "Player";

		/// <summary>SubmitScore 時に使用するプレイヤーID（テスト用）</summary>
		public string LocalPlayerId { get; set; } = "local_player";

		// ── グローバルランキング ──────────────────────────────────

		public async UniTask<bool> SubmitScore(string leaderboardName, long score)
		{
			if (SimulatedDelayMs > 0) await UniTask.Delay(SimulatedDelayMs);

			if (string.IsNullOrEmpty(leaderboardName))
			{
				Debug.LogWarning("[DummyLeaderboard] SubmitScore: leaderboardName is null or empty.");
				return false;
			}

			if (!_globalScores.TryGetValue(leaderboardName, out var board))
			{
				board = new Dictionary<string, LeaderboardEntry>();
				_globalScores[leaderboardName] = board;
			}

			board[LocalPlayerId] = new LeaderboardEntry
			{
				PlayerId = LocalPlayerId,
				PlayerName = LocalPlayerName,
				Score = score,
				Rank = 0 // 後で再計算
			};

			RecalculateGlobalRanks(leaderboardName);
			Debug.Log($"[DummyLeaderboard] SubmitScore: {leaderboardName} / {LocalPlayerId} = {score}");
			return true;
		}

		public async UniTask<List<LeaderboardEntry>> GetTopEntries(string leaderboardName, int count)
		{
			if (SimulatedDelayMs > 0) await UniTask.Delay(SimulatedDelayMs);

			if (!_globalScores.TryGetValue(leaderboardName, out var board))
				return new List<LeaderboardEntry>();

			return board.Values
				.OrderBy(e => e.Rank)
				.Take(count)
				.ToList();
		}

		public async UniTask<LeaderboardEntry> GetPlayerEntry(string leaderboardName, string playerId)
		{
			if (SimulatedDelayMs > 0) await UniTask.Delay(SimulatedDelayMs);

			if (string.IsNullOrEmpty(playerId)) return null;
			if (!_globalScores.TryGetValue(leaderboardName, out var board)) return null;

			return board.TryGetValue(playerId, out var entry) ? entry : null;
		}

		public async UniTask<List<LeaderboardEntry>> GetEntriesAroundPlayer(string leaderboardName, string playerId, int range)
		{
			if (SimulatedDelayMs > 0) await UniTask.Delay(SimulatedDelayMs);

			if (!_globalScores.TryGetValue(leaderboardName, out var board))
				return new List<LeaderboardEntry>();

			var sorted = board.Values.OrderBy(e => e.Rank).ToList();
			int idx = sorted.FindIndex(e => e.PlayerId == playerId);
			if (idx < 0) return new List<LeaderboardEntry>();

			int start = Mathf.Max(0, idx - range);
			int end = Mathf.Min(sorted.Count - 1, idx + range);
			return sorted.GetRange(start, end - start + 1);
		}

		// ── セッション内ランキング ────────────────────────────────

		public async UniTask<bool> UpdateSessionScore(string playerId, string playerName, long score)
		{
			if (SimulatedDelayMs > 0) await UniTask.Delay(SimulatedDelayMs);

			if (string.IsNullOrEmpty(playerId))
			{
				Debug.LogWarning("[DummyLeaderboard] UpdateSessionScore: playerId is null or empty.");
				return false;
			}

			_sessionScores[playerId] = new LeaderboardEntry
			{
				PlayerId = playerId,
				PlayerName = playerName ?? playerId,
				Score = score,
				Rank = 0
			};

			RecalculateSessionRanks();
			Debug.Log($"[DummyLeaderboard] UpdateSessionScore: {playerId} = {score}");
			return true;
		}

		public async UniTask<List<LeaderboardEntry>> GetSessionLeaderboard()
		{
			if (SimulatedDelayMs > 0) await UniTask.Delay(SimulatedDelayMs);

			return _sessionScores.Values
				.OrderBy(e => e.Rank)
				.ToList();
		}

		public async UniTask<int> GetPlayerSessionRank(string playerId)
		{
			if (SimulatedDelayMs > 0) await UniTask.Delay(SimulatedDelayMs);

			if (_sessionScores.TryGetValue(playerId, out var entry))
				return entry.Rank;

			return -1;
		}

		public async UniTask ResetSessionLeaderboard()
		{
			if (SimulatedDelayMs > 0) await UniTask.Delay(SimulatedDelayMs);

			_sessionScores.Clear();
			Debug.Log("[DummyLeaderboard] Session leaderboard reset.");
		}

		// --------------------------------------------------------------------------------
		// テスト用ヘルパー
		// --------------------------------------------------------------------------------

		/// <summary>
		/// テスト用にすべてのスコアをリセットします。
		/// </summary>
		public void ResetAll()
		{
			_globalScores.Clear();
			_sessionScores.Clear();
		}

		/// <summary>
		/// テスト用に指定リーダーボードのスコアを直接取得します。
		/// </summary>
		public LeaderboardEntry GetStoredGlobalEntry(string leaderboardName, string playerId)
		{
			if (_globalScores.TryGetValue(leaderboardName, out var board) &&
			    board.TryGetValue(playerId, out var entry))
				return entry;
			return null;
		}

		// --------------------------------------------------------------------------------
		// プライベート
		// --------------------------------------------------------------------------------

		private void RecalculateGlobalRanks(string leaderboardName)
		{
			if (!_globalScores.TryGetValue(leaderboardName, out var board)) return;
			int rank = 1;
			foreach (var entry in board.Values.OrderByDescending(e => e.Score))
			{
				entry.Rank = rank++;
			}
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
			new TestOperation { SectionLabel = "グローバルランキング" },
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
			new TestOperation
			{
				Label = "Get My Entry",
				Action = async ctx =>
				{
					var entry = await GetPlayerEntry(ctx.LeaderboardName, LocalPlayerId);
					ctx.ReportResult(entry != null ? $"#{entry.Rank} {entry.PlayerName}: {entry.Score}" : "（エントリなし）");
					ctx.AppendLog($"GetPlayerEntry({ctx.LeaderboardName}, {LocalPlayerId}) → {(entry != null ? "found" : "null")}");
				}
			},
			new TestOperation { SectionLabel = "セッション内ランキング" },
			new TestOperation
			{
				Label = "Update Session Score",
				Action = async ctx =>
				{
					long score = long.TryParse(ctx.LeaderboardScore, out long s) ? s : 500L;
					bool ok = await UpdateSessionScore(LocalPlayerId, LocalPlayerName, score);
					ctx.ReportResult(ok ? $"セッションスコア更新: {LocalPlayerId} = {score}" : "更新失敗");
					ctx.AppendLog($"UpdateSessionScore({LocalPlayerId}, {score}) → {ok}");
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
			new TestOperation
			{
				Label = "Reset Session",
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
			LeaderboardName = "dummy_leaderboard_001",
			LeaderboardScore = "1000",
		};
	}
}
