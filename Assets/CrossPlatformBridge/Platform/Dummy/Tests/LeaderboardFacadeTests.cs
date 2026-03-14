using System;
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.Dummy.Leaderboard;
using CrossPlatformBridge.Services.Leaderboard;
using LeaderboardService = CrossPlatformBridge.Services.Leaderboard.Leaderboard;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Dummy.Tests
{
	/// <summary>
	/// <see cref="Leaderboard"/> ファサードを経由したリーダーボード機能の動作確認テスト。
	/// <c>LeaderboardService.Instance.Use&lt;DummyLeaderboardPlatform&gt;()</c> でハンドラーを注入し、
	/// CrossPlatformBridge の公開 API をすべてファサード越しに検証します。
	/// </summary>
	public class LeaderboardFacadeTests
	{
		// テストごとに新しいハンドラーを Use<T>() で差し替えるため、
		// モノビヘイビアシングルトン自体は破棄しない。
		private DummyLeaderboardHandler _handler;

		[SetUp]
		public void Setup()
		{
			// Use<T>() でファクトリ経由の新規ハンドラーをファサードに注入する。
			// これにより毎テスト前にインメモリ状態がリセットされる。
			_handler = (DummyLeaderboardHandler)LeaderboardService.Instance.Use<DummyLeaderboardPlatform>();
			_handler.SimulatedDelayMs = 0;
			_handler.LocalPlayerId   = "player_001";
			_handler.LocalPlayerName = "TestPlayer";
		}

		[TearDown]
		public void TearDown()
		{
			_handler = null;
		}

		// ── 初期化 ────────────────────────────────────────────────

		[Test]
		public void Use_DummyLeaderboardPlatform_SetsIsInitializedTrue()
		{
			Assert.IsTrue(LeaderboardService.Instance.IsInitialized,
				"Use<DummyLeaderboardPlatform>() 後は IsInitialized が true である必要があります。");
		}

		// ── グローバルランキング ──────────────────────────────────

		[UnityTest]
		public IEnumerator SubmitScore_ViaFacade_ReturnsTrueAndCanBeRetrieved() => UniTask.ToCoroutine(async () =>
		{
			// Act
			bool ok = await LeaderboardService.Instance.SubmitScore("board_01", 9000L);

			// Assert
			Assert.IsTrue(ok, "SubmitScore() が false を返しました。");
			var entry = await LeaderboardService.Instance.GetPlayerEntry("board_01", "player_001");
			Assert.IsNotNull(entry, "SubmitScore() 後に GetPlayerEntry() で取得できませんでした。");
			Assert.AreEqual(9000L, entry.Score);
		});

		[UnityTest]
		public IEnumerator GetTopEntries_ViaFacade_ReturnsDescendingOrder() => UniTask.ToCoroutine(async () =>
		{
			// Arrange: 3 人分のスコアをそれぞれ別プレイヤーとして送信
			_handler.LocalPlayerId = "p1"; await LeaderboardService.Instance.SubmitScore("board_01", 100L);
			_handler.LocalPlayerId = "p2"; await LeaderboardService.Instance.SubmitScore("board_01", 300L);
			_handler.LocalPlayerId = "p3"; await LeaderboardService.Instance.SubmitScore("board_01", 200L);

			// Act
			List<LeaderboardEntry> top = await LeaderboardService.Instance.GetTopEntries("board_01", 10);

			// Assert
			Assert.AreEqual(3, top.Count);
			Assert.AreEqual(300L, top[0].Score, "1位のスコアが正しくありません。");
			Assert.AreEqual(200L, top[1].Score, "2位のスコアが正しくありません。");
			Assert.AreEqual(100L, top[2].Score, "3位のスコアが正しくありません。");
			Assert.AreEqual(1, top[0].Rank);
			Assert.AreEqual(2, top[1].Rank);
			Assert.AreEqual(3, top[2].Rank);
		});

		[UnityTest]
		public IEnumerator GetTopEntries_ViaFacade_RespectsCountLimit() => UniTask.ToCoroutine(async () =>
		{
			// Arrange: 5 件登録
			for (int i = 1; i <= 5; i++)
			{
				_handler.LocalPlayerId = $"p{i}";
				await LeaderboardService.Instance.SubmitScore("board_01", i * 100L);
			}

			// Act: 上位 3 件のみ
			List<LeaderboardEntry> top3 = await LeaderboardService.Instance.GetTopEntries("board_01", 3);

			Assert.AreEqual(3, top3.Count, "count=3 を指定したとき 3 件のみ返す必要があります。");
		});

		[UnityTest]
		public IEnumerator GetPlayerEntry_ViaFacade_ReturnsCorrectEntry() => UniTask.ToCoroutine(async () =>
		{
			await LeaderboardService.Instance.SubmitScore("board_01", 7777L);

			var entry = await LeaderboardService.Instance.GetPlayerEntry("board_01", "player_001");

			Assert.IsNotNull(entry);
			Assert.AreEqual(7777L, entry.Score);
			Assert.AreEqual("player_001", entry.PlayerId);
		});

		[UnityTest]
		public IEnumerator GetPlayerEntry_ViaFacade_NonExistingPlayer_ReturnsNull() => UniTask.ToCoroutine(async () =>
		{
			var entry = await LeaderboardService.Instance.GetPlayerEntry("board_01", "nobody");
			Assert.IsNull(entry, "存在しないプレイヤーのエントリは null を返す必要があります。");
		});

		[UnityTest]
		public IEnumerator GetEntriesAroundPlayer_ViaFacade_ReturnsExpectedRange() => UniTask.ToCoroutine(async () =>
		{
			// Arrange: 5 人登録（p1=500, p2=400, p3=300, p4=200, p5=100）
			for (int i = 1; i <= 5; i++)
			{
				_handler.LocalPlayerId   = $"p{i}";
				_handler.LocalPlayerName = $"Player{i}";
				await LeaderboardService.Instance.SubmitScore("board_01", (6 - i) * 100L);
			}

			// p3（3位）の前後 1 件 → [p2, p3, p4]
			var entries = await LeaderboardService.Instance.GetEntriesAroundPlayer("board_01", "p3", 1);

			Assert.AreEqual(3, entries.Count, "range=1 のとき前後合わせて 3 件返す必要があります。");
			Assert.AreEqual("p2", entries[0].PlayerId);
			Assert.AreEqual("p3", entries[1].PlayerId);
			Assert.AreEqual("p4", entries[2].PlayerId);
		});

		// ── セッション内ランキング ────────────────────────────────

		[UnityTest]
		public IEnumerator UpdateSessionScore_ViaFacade_ReturnsTrueAndRanksCorrectly() => UniTask.ToCoroutine(async () =>
		{
			bool ok = await LeaderboardService.Instance.UpdateSessionScore("p1", "Alice", 2000L);

			Assert.IsTrue(ok, "UpdateSessionScore() が false を返しました。");
			int rank = await LeaderboardService.Instance.GetPlayerSessionRank("p1");
			Assert.AreEqual(1, rank, "1人のとき順位は 1 である必要があります。");
		});

		[UnityTest]
		public IEnumerator GetSessionLeaderboard_ViaFacade_ReturnsDescendingOrder() => UniTask.ToCoroutine(async () =>
		{
			await LeaderboardService.Instance.UpdateSessionScore("p1", "Alice", 100L);
			await LeaderboardService.Instance.UpdateSessionScore("p2", "Bob",   300L);
			await LeaderboardService.Instance.UpdateSessionScore("p3", "Carol", 200L);

			List<LeaderboardEntry> board = await LeaderboardService.Instance.GetSessionLeaderboard();

			Assert.AreEqual(3, board.Count);
			Assert.AreEqual("p2", board[0].PlayerId, "1位は最高スコアの p2 である必要があります。");
			Assert.AreEqual("p3", board[1].PlayerId);
			Assert.AreEqual("p1", board[2].PlayerId);
		});

		[UnityTest]
		public IEnumerator GetPlayerSessionRank_ViaFacade_NonExistingPlayer_ReturnsMinus1() => UniTask.ToCoroutine(async () =>
		{
			int rank = await LeaderboardService.Instance.GetPlayerSessionRank("nobody");
			Assert.AreEqual(-1, rank, "存在しないプレイヤーの順位は -1 を返す必要があります。");
		});

		[UnityTest]
		public IEnumerator ResetSessionLeaderboard_ViaFacade_ClearsAllEntries() => UniTask.ToCoroutine(async () =>
		{
			await LeaderboardService.Instance.UpdateSessionScore("p1", "Alice", 999L);

			await LeaderboardService.Instance.ResetSessionLeaderboard();

			List<LeaderboardEntry> board = await LeaderboardService.Instance.GetSessionLeaderboard();
			Assert.AreEqual(0, board.Count, "リセット後はエントリが 0 件である必要があります。");
		});

		// ── IsInitialized ──────────────────────────────────────────

		[Test]
		public void IsInitialized_BeforeUse_ReturnsFalse()
		{
			// 新しい GameObject にコンポーネントを追加してハンドラー未注入状態を作る。
			// シングルトンは既に Use<T>() 済みなので、別インスタンスで検証する。
			var go     = new GameObject("LeaderboardTest_Fresh");
			var facade = go.AddComponent<LeaderboardService>();

			Assert.IsFalse(facade.IsInitialized,
				"ハンドラー注入前は IsInitialized が false である必要があります。");

			UnityEngine.Object.DestroyImmediate(go);
		}
	}
}
