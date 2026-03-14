#if USE_CROSSPLATFORMBRIDGE_EOS
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.EOS.Achievement;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Epic.OnlineServices;

namespace CrossPlatformBridge.Platform.EOS.Tests
{
	/// <summary>
	/// EOS 実績ハンドラの EditMode テスト。
	/// EOSManager の初期化とネットワーク接続が必要なため通常は Ignore。
	/// </summary>
	public class EOSAchievementHandlerTests
	{
		private EOSAchievementHandler _handler;

		[SetUp]
		public void Setup()
		{
			// EOSManager の初期化や認証が必要なため、実行環境に応じて有効化する
			_handler = new EOSAchievementHandler();
		}

		[TearDown]
		public void TearDown()
		{
			_handler = null;
		}
		
		[UnityTest]
		[Ignore("Requires EOSManager initialization and network connection.")]
		public IEnumerator UnlockAchievement_ValidId_ReturnsTrueAndAddsToList() => UniTask.ToCoroutine(async () =>
		{
			// Arrange
			string achievementId = "test_achievement_01";

			// Act
			bool result = await _handler.UnlockAchievement(achievementId);
			List<string> unlocked = await _handler.GetUnlockedAchievements();

			// Assert
			Assert.IsTrue(result);
			Assert.Contains(achievementId, unlocked);
		});

		[UnityTest]
		[Ignore("Requires EOSManager initialization and network connection.")]
		public IEnumerator SetProgress_100Percent_TriggersUnlock() => UniTask.ToCoroutine(async () =>
		{
			// Arrange
			string achievementId = "test_achievement_prog_100";
			float progress = 100f; // EOS 実装では 100% で解除する

			// Act
			bool result = await _handler.SetProgress(achievementId, progress);

			// Assert
			Assert.IsTrue(result);
			List<string> unlocked = await _handler.GetUnlockedAchievements();
			Assert.Contains(achievementId, unlocked);
		});
	}
}
#endif
