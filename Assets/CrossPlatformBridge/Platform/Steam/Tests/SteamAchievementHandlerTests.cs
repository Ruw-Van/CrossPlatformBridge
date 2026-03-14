#if USE_CROSSPLATFORMBRIDGE_STEAM
#if !DISABLESTEAMWORKS
using CrossPlatformBridge.Platform.Steam.Achievement;
using CrossPlatformBridge.Testing;
using NUnit.Framework;

namespace CrossPlatformBridge.Platform.Steam.Tests
{
	/// <summary>
	/// Steam 実績ハンドラの EditMode テスト。
	/// Steam クライアントへの接続を必要としないコンストラクタ・IServiceTestProvider の検証を行う。
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

		// ----------------------------------------------------------------
		// コンストラクタ
		// ----------------------------------------------------------------

		[Test]
		public void SteamAchievementHandler_CanBeInstantiated()
		{
			Assert.IsNotNull(_handler, "SteamAchievementHandler は new でインスタンス化できる必要があります");
		}

		// ----------------------------------------------------------------
		// IServiceTestProvider
		// ----------------------------------------------------------------

		[Test]
		public void SteamAchievementHandler_GetTestOperations_ReturnsNonNullAndNonEmpty()
		{
			var ops = _handler.GetTestOperations();

			Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
			Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
		}

		[Test]
		public void SteamAchievementHandler_GetDefaultData_ReturnsExpectedAchievementId()
		{
			var data = _handler.GetDefaultData();

			Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
			Assert.AreEqual("STEAM_ACHIEVEMENT_1", data.AchievementId,
				"デフォルトの AchievementId は STEAM_ACHIEVEMENT_1 である必要があります");
		}

		// ----------------------------------------------------------------
		// TODO: 統合テスト（Steam クライアント起動・steam_appid.txt 設定が必要）
		// ----------------------------------------------------------------
		// - UnlockAchievement / GetUnlockedAchievements / SetProgress は
		//   IntegrationTests/SteamAchievementIntegrationTests.cs を参照
	}
}
#endif
#endif
