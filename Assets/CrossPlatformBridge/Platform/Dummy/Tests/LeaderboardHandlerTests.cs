using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.Dummy.Leaderboard;
using CrossPlatformBridge.Services.Leaderboard;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Dummy.Tests
{
	public class LeaderboardHandlerTests
	{
		private DummyLeaderboardHandler _handler;

		[SetUp]
		public void Setup()
		{
			_handler = new DummyLeaderboardHandler();
			_handler.SimulatedDelayMs = 0; // テスト実行を早くするため遅延なしにする
			_handler.LocalPlayerId = "player_001";
			_handler.LocalPlayerName = "TestPlayer";
		}

		[TearDown]
		public void TearDown()
		{
			_handler.ResetAll();
			_handler = null;
		}

		// ── グローバルランキング ──────────────────────────────────

		[UnityTest]
		public IEnumerator SubmitScore_ValidArgs_ReturnsTrueAndStoresEntry() => UniTask.ToCoroutine(async () =>
		{
			// Act
			bool result = await _handler.SubmitScore("board_01", 5000L);

			// Assert
			Assert.IsTrue(result);
			var entry = _handler.GetStoredGlobalEntry("board_01", "player_001");
			Assert.IsNotNull(entry);
			Assert.AreEqual(5000L, entry.Score);
			Assert.AreEqual("TestPlayer", entry.PlayerName);
		});

		[UnityTest]
		public IEnumerator SubmitScore_EmptyLeaderboardName_ReturnsFalse() => UniTask.ToCoroutine(async () =>
		{
			bool result = await _handler.SubmitScore("", 1000L);
			Assert.IsFalse(result);
		});

		[UnityTest]
		public IEnumerator GetTopEntries_AfterMultipleSubmits_ReturnsDescendingOrder() => UniTask.ToCoroutine(async () =>
		{
			// Arrange: 3人分のスコアをセット
			_handler.LocalPlayerId = "p1";
			await _handler.SubmitScore("board_01", 300L);
			_handler.LocalPlayerId = "p2";
			await _handler.SubmitScore("board_01", 100L);
			_handler.LocalPlayerId = "p3";
			await _handler.SubmitScore("board_01", 200L);

			// Act
			List<LeaderboardEntry> top = await _handler.GetTopEntries("board_01", 10);

			// Assert: スコア降順
			Assert.AreEqual(3, top.Count);
			Assert.AreEqual(300L, top[0].Score);
			Assert.AreEqual(200L, top[1].Score);
			Assert.AreEqual(100L, top[2].Score);
			Assert.AreEqual(1, top[0].Rank);
			Assert.AreEqual(2, top[1].Rank);
			Assert.AreEqual(3, top[2].Rank);
		});

		[UnityTest]
		public IEnumerator GetTopEntries_CountLimit_RespectsLimit() => UniTask.ToCoroutine(async () =>
		{
			// Arrange: 5件登録
			for (int i = 1; i <= 5; i++)
			{
				_handler.LocalPlayerId = $"p{i}";
				await _handler.SubmitScore("board_01", i * 100L);
			}

			// Act: 上位3件のみ取得
			List<LeaderboardEntry> top = await _handler.GetTopEntries("board_01", 3);

			Assert.AreEqual(3, top.Count);
		});

		[UnityTest]
		public IEnumerator GetPlayerEntry_ExistingPlayer_ReturnsEntry() => UniTask.ToCoroutine(async () =>
		{
			await _handler.SubmitScore("board_01", 9999L);

			var entry = await _handler.GetPlayerEntry("board_01", "player_001");

			Assert.IsNotNull(entry);
			Assert.AreEqual(9999L, entry.Score);
			Assert.AreEqual("player_001", entry.PlayerId);
		});

		[UnityTest]
		public IEnumerator GetPlayerEntry_NonExistingPlayer_ReturnsNull() => UniTask.ToCoroutine(async () =>
		{
			var entry = await _handler.GetPlayerEntry("board_01", "nobody");
			Assert.IsNull(entry);
		});

		[UnityTest]
		public IEnumerator GetEntriesAroundPlayer_Range1_ReturnsCorrectEntries() => UniTask.ToCoroutine(async () =>
		{
			// Arrange: 5人登録（rank 1-5）
			for (int i = 1; i <= 5; i++)
			{
				_handler.LocalPlayerId = $"p{i}";
				_handler.LocalPlayerName = $"Player{i}";
				await _handler.SubmitScore("board_01", (6 - i) * 100L); // p1=500, p2=400, ...
			}

			// p3 の前後 1 件 → [p2, p3, p4]
			var entries = await _handler.GetEntriesAroundPlayer("board_01", "p3", 1);

			Assert.AreEqual(3, entries.Count);
			Assert.AreEqual("p2", entries[0].PlayerId);
			Assert.AreEqual("p3", entries[1].PlayerId);
			Assert.AreEqual("p4", entries[2].PlayerId);
		});

		// ── セッション内ランキング ────────────────────────────────

		[UnityTest]
		public IEnumerator UpdateSessionScore_ValidArgs_ReturnsTrueAndStoresEntry() => UniTask.ToCoroutine(async () =>
		{
			bool result = await _handler.UpdateSessionScore("p1", "Alice", 1500L);

			Assert.IsTrue(result);
			int rank = await _handler.GetPlayerSessionRank("p1");
			Assert.AreEqual(1, rank);
		});

		[UnityTest]
		public IEnumerator UpdateSessionScore_EmptyPlayerId_ReturnsFalse() => UniTask.ToCoroutine(async () =>
		{
			bool result = await _handler.UpdateSessionScore("", "Ghost", 100L);
			Assert.IsFalse(result);
		});

		[UnityTest]
		public IEnumerator GetSessionLeaderboard_MultiPlayers_ReturnsDescendingOrder() => UniTask.ToCoroutine(async () =>
		{
			await _handler.UpdateSessionScore("p1", "Alice", 100L);
			await _handler.UpdateSessionScore("p2", "Bob", 300L);
			await _handler.UpdateSessionScore("p3", "Carol", 200L);

			List<LeaderboardEntry> board = await _handler.GetSessionLeaderboard();

			Assert.AreEqual(3, board.Count);
			Assert.AreEqual("p2", board[0].PlayerId); // 300
			Assert.AreEqual("p3", board[1].PlayerId); // 200
			Assert.AreEqual("p1", board[2].PlayerId); // 100
		});

		[UnityTest]
		public IEnumerator GetPlayerSessionRank_NonExistingPlayer_ReturnsMinusOne() => UniTask.ToCoroutine(async () =>
		{
			int rank = await _handler.GetPlayerSessionRank("nobody");
			Assert.AreEqual(-1, rank);
		});

		[UnityTest]
		public IEnumerator ResetSessionLeaderboard_ClearsAllEntries() => UniTask.ToCoroutine(async () =>
		{
			await _handler.UpdateSessionScore("p1", "Alice", 999L);
			await _handler.ResetSessionLeaderboard();

			List<LeaderboardEntry> board = await _handler.GetSessionLeaderboard();
			Assert.AreEqual(0, board.Count);
		});

		[UnityTest]
		public IEnumerator UpdateSessionScore_OverwritesExistingScore() => UniTask.ToCoroutine(async () =>
		{
			await _handler.UpdateSessionScore("p1", "Alice", 100L);
			await _handler.UpdateSessionScore("p1", "Alice", 999L);

			List<LeaderboardEntry> board = await _handler.GetSessionLeaderboard();
			Assert.AreEqual(1, board.Count);
			Assert.AreEqual(999L, board[0].Score);
		});
	}
}
