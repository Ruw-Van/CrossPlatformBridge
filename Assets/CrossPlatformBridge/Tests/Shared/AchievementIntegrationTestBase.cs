using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using AchievementService = CrossPlatformBridge.Services.Achievement.Achievement;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Tests.Shared
{
	/// <summary>
	/// 実績統合テストの共通基底クラス。
	/// CrossPlatformBridge ファサード（Achievement.Instance）を通じた
	/// UnlockAchievement / SetProgress / GetUnlockedAchievements 操作を
	/// 各プラットフォームで共通テストします。
	/// </summary>
	public abstract class AchievementIntegrationTestBase
	{
		// -----------------------------------------------------------------------
		// 派生クラスで実装
		// -----------------------------------------------------------------------

		/// <summary>プラットフォーム固有のテスト実績 ID。</summary>
		protected abstract string TestAchievementId { get; }

		/// <summary>
		/// プラットフォーム固有の初期化。
		/// Achievement.Instance.Use&lt;T&gt;() を呼び、必要に応じてアカウント認証も行う。
		/// </summary>
		protected abstract UniTask SetUpPlatform();

		/// <summary>プラットフォーム固有の後処理（デフォルト: 何もしない）。</summary>
		protected virtual UniTask TearDownPlatform() => UniTask.CompletedTask;

		// -----------------------------------------------------------------------
		// SetUp / TearDown
		// -----------------------------------------------------------------------

		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(SetUpPlatform);

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			await TearDownPlatform();

			if (AchievementService.Instance != null)
			{
				Object.Destroy(AchievementService.Instance.gameObject);
				await UniTask.NextFrame();
			}
		});

		// -----------------------------------------------------------------------
		// 共通テスト
		// -----------------------------------------------------------------------

		/// <summary>
		/// 実績を解除し、GetUnlockedAchievements で取得できることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator UnlockAchievement_ValidId_ReturnsTrueAndAddsToList() => UniTask.ToCoroutine(async () =>
		{
			bool result = await AchievementService.Instance.UnlockAchievement(TestAchievementId);
			List<string> unlocked = await AchievementService.Instance.GetUnlockedAchievements();

			Assert.IsTrue(result, $"UnlockAchievement({TestAchievementId}) が false を返しました。");
			Assert.Contains(TestAchievementId, unlocked,
				$"解除後も GetUnlockedAchievements に {TestAchievementId} が含まれていません。");
		});

		/// <summary>
		/// 100% 進捗を設定すると UnlockAchievement と同等に動作することを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator SetProgress_100Percent_TriggersUnlock() => UniTask.ToCoroutine(async () =>
		{
			bool result = await AchievementService.Instance.SetProgress(TestAchievementId, 100f);

			Assert.IsTrue(result,
				$"SetProgress({TestAchievementId}, 100) が false を返しました。");
			List<string> unlocked = await AchievementService.Instance.GetUnlockedAchievements();
			Assert.Contains(TestAchievementId, unlocked,
				$"100% 進捗設定後も GetUnlockedAchievements に {TestAchievementId} が含まれていません。");
		});

		/// <summary>
		/// GetUnlockedAchievements が null でなく List を返すことを確認する（実績 0 件でも OK）。
		/// </summary>
		[UnityTest]
		public IEnumerator GetUnlockedAchievements_ReturnsNonNullList() => UniTask.ToCoroutine(async () =>
		{
			List<string> result = await AchievementService.Instance.GetUnlockedAchievements();

			Assert.IsNotNull(result, "GetUnlockedAchievements() が null を返しました。");
		});
	}
}
