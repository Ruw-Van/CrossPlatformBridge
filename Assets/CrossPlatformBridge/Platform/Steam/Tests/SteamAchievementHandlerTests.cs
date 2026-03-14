#if USE_CROSSPLATFORMBRIDGE_STEAM
#if !DISABLESTEAMWORKS
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.Steam.Achievement;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Steamworks;

namespace CrossPlatformBridge.Platform.Steam.Tests
{
	/// <summary>
	/// Steam 実績ハンドラの EditMode テスト。
	/// SteamClient が起動していない場合は Ignore。
	/// </summary>
	public class SteamAchievementHandlerTests
	{
		private SteamAchievementHandler _handler;

		[SetUp]
		public void Setup()
		{
			_handler = new SteamAchievementHandler();
		}

		[TearDown]
		public void TearDown()
		{
			_handler = null;
		}

		[UnityTest]
		[Ignore("Requires SteamClient to be running and steam_appid.txt to be configured.")]
		public IEnumerator UnlockAchievement_NotRunning_ReturnsFalse() => UniTask.ToCoroutine(async () =>
		{
			// Act
			bool result = await _handler.UnlockAchievement("test");

			// Assert
			Assert.IsFalse(result);
		});

		[UnityTest]
		[Ignore("Requires SteamClient to be running and steam_appid.txt to be configured.")]
		public IEnumerator GetUnlockedAchievements_NotRunning_ReturnsEmpty() => UniTask.ToCoroutine(async () =>
		{
			// Act
			var result = await _handler.GetUnlockedAchievements();

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(0, result.Count);
		});
		
		[UnityTest]
		[Ignore("Requires SteamClient to be running and steam_appid.txt to be configured.")]
		public IEnumerator SetProgress_NotRunning_ReturnsFalse() => UniTask.ToCoroutine(async () =>
		{
			// Act
			var result = await _handler.SetProgress("test", 100f);

			// Assert
			Assert.IsFalse(result);
		});
	}
}
#endif
#endif
