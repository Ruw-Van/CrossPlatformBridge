using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.Dummy.Achievement;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Dummy.Tests
{
	public class AchievementHandlerTests
	{
		private DummyAchievementHandler _handler;

		[SetUp]
		public void Setup()
		{
			_handler = new DummyAchievementHandler();
			_handler.SimulatedDelayMs = 0; // テスト実行を早くするため遅延なしにする
		}

		[TearDown]
		public void TearDown()
		{
			_handler.ResetAll();
			_handler = null;
		}

		[UnityTest]
		public IEnumerator UnlockAchievement_ValidId_ReturnsTrueAndAddsToList() => UniTask.ToCoroutine(async () =>
		{
			// Arrange
			string achievementId = "test_achievement_01";

			// Act
			bool result = await _handler.UnlockAchievement(achievementId);
			List<string> unlocked = await _handler.GetUnlockedAchievements();

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(1, unlocked.Count);
			Assert.Contains(achievementId, unlocked);
		});

		[UnityTest]
		public IEnumerator UnlockAchievement_EmptyId_ReturnsFalse() => UniTask.ToCoroutine(async () =>
		{
			// Act
			bool result = await _handler.UnlockAchievement("");
			List<string> unlocked = await _handler.GetUnlockedAchievements();

			// Assert
			Assert.IsFalse(result);
			Assert.AreEqual(0, unlocked.Count);
		});

		[UnityTest]
		public IEnumerator UnlockAchievement_AlreadyUnlocked_ReturnsTrueButDoesNotDuplicate() => UniTask.ToCoroutine(async () =>
		{
			// Arrange
			string achievementId = "test_achievement_dup";
			await _handler.UnlockAchievement(achievementId);

			// Act
			bool result = await _handler.UnlockAchievement(achievementId);
			List<string> unlocked = await _handler.GetUnlockedAchievements();

			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(1, unlocked.Count);
		});

		[UnityTest]
		public IEnumerator SetProgress_ValidId_StoresProgress() => UniTask.ToCoroutine(async () =>
		{
			// Arrange
			string achievementId = "test_achievement_prog";
			float progress = 50.5f;

			// Act
			bool result = await _handler.SetProgress(achievementId, progress);
			
			// Assert
			Assert.IsTrue(result);
			Assert.AreEqual(progress, _handler.GetStoredProgress(achievementId));
			
			List<string> unlocked = await _handler.GetUnlockedAchievements();
			Assert.IsFalse(unlocked.Contains(achievementId));
		});

		[UnityTest]
		public IEnumerator SetProgress_Progress100_UnlocksAutomatically() => UniTask.ToCoroutine(async () =>
		{
			// Arrange
			string achievementId = "test_achievement_prog100";
			float progress = 100f;

			// Act
			bool result = await _handler.SetProgress(achievementId, progress);

			// Assert
			Assert.IsTrue(result);
			
			List<string> unlocked = await _handler.GetUnlockedAchievements();
			Assert.Contains(achievementId, unlocked);
		});

		[UnityTest]
		public IEnumerator SetProgress_AlreadyUnlocked_Ignored() => UniTask.ToCoroutine(async () =>
		{
			// Arrange
			string achievementId = "test_achievement_ignored";
			await _handler.UnlockAchievement(achievementId); // 先に解除する
			_handler.ResetAll(); // 進捗だけリセットして再セットするロジックをシミュレート
			await _handler.UnlockAchievement(achievementId); 

			// Act
			bool result = await _handler.SetProgress(achievementId, 50f);

			// Assert
			Assert.IsTrue(result);
			// Unlock済みなのでProgressはセットされない（またはUnlock時点の100のまま）
			Assert.AreEqual(100f, _handler.GetStoredProgress(achievementId));
		});
	}
}
