#if USE_CROSSPLATFORMBRIDGE_PLAYFAB && !DISABLE_PLAYFABCLIENT_API
using System.Collections;
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Services.Leaderboard;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using PlayFab;
using CrossPlatformBridge.Platform.PlayFab;
using UnityEngine;
using UnityEngine.TestTools;
using LeaderboardService = CrossPlatformBridge.Services.Leaderboard.Leaderboard;

namespace CrossPlatformBridge.Platform.PlayFab.IntegrationTests
{
	/// <summary>
	/// PlayFab リーダーボードサービスの PlayMode 統合テスト。
	/// CrossPlatformBridge ファサード（Leaderboard）経由で PlayFab の
	/// スコア送信・ランキング取得を検証する。
	///
	/// 実行前提条件:
	/// - PlayFab TitleId が設定済みであること。
	/// - PlayFab ダッシュボードでリーダーボード（Statistic）が作成済みであること。
	/// </summary>
	public class PlayFabLeaderboardIntegrationTests
	{
		private const string TestLeaderboardName = "playfab_leaderboard_001";

		private GameObject _accountServiceGo;
		private GameObject _leaderboardServiceGo;

		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
		{
			Assume.That(
				!string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId),
				"PlayFab の TitleId が設定されていません。");

			_accountServiceGo = new GameObject("AccountService");
			_accountServiceGo.AddComponent<AccountService>();
			AccountService.Instance.Use<CrossPlatformBridge.Platform.PlayFab.PlayFab>();

			bool initialized = await AccountService.Instance.InitializeAsync();
			Assume.That(initialized, Is.True,
				"PlayFab の初期化に失敗しました。TitleId と PlayFab Console の設定を確認してください。");

			_leaderboardServiceGo = new GameObject("LeaderboardService");
			_leaderboardServiceGo.AddComponent<LeaderboardService>();
			LeaderboardService.Instance.Use<CrossPlatformBridge.Platform.PlayFab.PlayFab>();
		});

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			if (LeaderboardService.Instance != null)
				Object.Destroy(LeaderboardService.Instance.gameObject);

			if (AccountService.Instance != null)
				await AccountService.Instance.ShutdownAsync();

			if (_accountServiceGo != null)
				Object.Destroy(_accountServiceGo);

			if (_leaderboardServiceGo != null)
				Object.Destroy(_leaderboardServiceGo);
		});

		// -----------------------------------------------------------------------
		// SubmitScore
		// -----------------------------------------------------------------------

		/// <summary>
		/// SubmitScore() がログイン後に true を返すことを確認する。
		/// PlayFab ダッシュボードで対応する Statistic が作成されている必要があります。
		/// </summary>
		[UnityTest]
		public IEnumerator SubmitScore_AfterLogin_ReturnsTrue() => UniTask.ToCoroutine(async () =>
		{
			bool result = await LeaderboardService.Instance.SubmitScore(TestLeaderboardName, 1000L);

			Assert.IsTrue(result,
				$"SubmitScore() が false を返しました。PlayFab ダッシュボードで Statistic '{TestLeaderboardName}' が設定されているか確認してください。");
		});

		// -----------------------------------------------------------------------
		// GetTopEntries
		// -----------------------------------------------------------------------

		/// <summary>
		/// GetTopEntries() がログイン後に null でない結果を返すことを確認する（空リストは可）。
		/// </summary>
		[UnityTest]
		public IEnumerator GetTopEntries_AfterLogin_ReturnsNotNull() => UniTask.ToCoroutine(async () =>
		{
			var entries = await LeaderboardService.Instance.GetTopEntries(TestLeaderboardName, 10);

			Assert.IsNotNull(entries,
				"GetTopEntries() が null を返しました。");
		});

		// -----------------------------------------------------------------------
		// GetEntriesAroundPlayer
		// -----------------------------------------------------------------------

		/// <summary>
		/// GetEntriesAroundPlayer() がログイン後に null でない結果を返すことを確認する。
		/// スコアを事前に送信しているためエントリが存在するはずです。
		/// </summary>
		[UnityTest]
		public IEnumerator GetEntriesAroundPlayer_AfterLogin_ReturnsNotNull() => UniTask.ToCoroutine(async () =>
		{
			// 事前にスコアを送信してエントリを作成
			await LeaderboardService.Instance.SubmitScore(TestLeaderboardName, 2000L);

			string playerId = AccountService.Instance.AccountId;
			var entries = await LeaderboardService.Instance.GetEntriesAroundPlayer(TestLeaderboardName, playerId, 2);

			Assert.IsNotNull(entries,
				"GetEntriesAroundPlayer() が null を返しました。");
		});
	}
}
#endif
