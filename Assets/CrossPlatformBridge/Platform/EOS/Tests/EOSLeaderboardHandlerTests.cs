#if USE_CROSSPLATFORMBRIDGE_EOS
using CrossPlatformBridge.Platform.EOS.Leaderboard;
using CrossPlatformBridge.Testing;
using NUnit.Framework;

namespace CrossPlatformBridge.Platform.EOS.Tests
{
	/// <summary>
	/// EOS リーダーボードハンドラの EditMode テスト。
	/// EOS 接続が不要なコンストラクタ・初期状態・IServiceTestProvider の検証を行う。
	/// </summary>
	public class EOSLeaderboardHandlerTests
	{
		private EOSLeaderboardHandler _handler;

		[SetUp]
		public void Setup()
		{
			_handler = new EOSLeaderboardHandler();
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
		public void EOSLeaderboardHandler_CanBeInstantiated()
		{
			Assert.IsNotNull(_handler, "EOSLeaderboardHandler は new でインスタンス化できる必要があります");
		}

		// ----------------------------------------------------------------
		// IServiceTestProvider
		// ----------------------------------------------------------------

		[Test]
		public void EOSLeaderboardHandler_GetTestOperations_ReturnsNonNullAndNonEmpty()
		{
			var ops = _handler.GetTestOperations();

			Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
			Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
		}

		[Test]
		public void EOSLeaderboardHandler_GetDefaultData_ReturnsExpectedLeaderboardName()
		{
			var data = _handler.GetDefaultData();

			Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
			Assert.AreEqual("eos_leaderboard_001", data.LeaderboardName,
				"デフォルトの LeaderboardName は eos_leaderboard_001 である必要があります");
		}

		// ----------------------------------------------------------------
		// TODO: 統合テスト（実 EOS 接続・DevAuthTool 起動が必要）
		// ----------------------------------------------------------------
		// - SubmitScore / GetTopEntries / GetPlayerEntry などは
		//   IntegrationTests/EOSLeaderboardIntegrationTests.cs を参照
	}
}
#endif
