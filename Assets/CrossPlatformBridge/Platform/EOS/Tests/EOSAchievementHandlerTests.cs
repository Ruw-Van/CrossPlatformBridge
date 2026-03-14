#if USE_CROSSPLATFORMBRIDGE_EOS
using CrossPlatformBridge.Platform.EOS.Achievement;
using CrossPlatformBridge.Testing;
using NUnit.Framework;

namespace CrossPlatformBridge.Platform.EOS.Tests
{
	/// <summary>
	/// EOS 実績ハンドラの EditMode テスト。
	/// EOS 接続が不要なコンストラクタ・初期状態・IServiceTestProvider の検証を行う。
	/// </summary>
	public class EOSAchievementHandlerTests
	{
		private EOSAchievementHandler _handler;

		[SetUp]
		public void Setup()
		{
			_handler = new EOSAchievementHandler();
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
		public void EOSAchievementHandler_CanBeInstantiated()
		{
			Assert.IsNotNull(_handler, "EOSAchievementHandler は new でインスタンス化できる必要があります");
		}

		// ----------------------------------------------------------------
		// IServiceTestProvider
		// ----------------------------------------------------------------

		[Test]
		public void EOSAchievementHandler_GetTestOperations_ReturnsNonNullAndNonEmpty()
		{
			var ops = _handler.GetTestOperations();

			Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
			Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
		}

		[Test]
		public void EOSAchievementHandler_GetDefaultData_ReturnsExpectedAchievementId()
		{
			var data = _handler.GetDefaultData();

			Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
			Assert.AreEqual("eos_achievement_001", data.AchievementId,
				"デフォルトの AchievementId は eos_achievement_001 である必要があります");
		}

		// ----------------------------------------------------------------
		// TODO: 統合テスト（実 EOS 接続・DevAuthTool 起動が必要）
		// ----------------------------------------------------------------
		// - UnlockAchievement / GetUnlockedAchievements / SetProgress は
		//   IntegrationTests/EOSAchievementIntegrationTests.cs を参照
	}
}
#endif
