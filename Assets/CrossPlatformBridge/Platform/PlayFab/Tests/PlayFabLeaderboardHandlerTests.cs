#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
#if !DISABLE_PLAYFABCLIENT_API

using CrossPlatformBridge.Platform.PlayFab.Leaderboard;
using NUnit.Framework;

namespace CrossPlatformBridge.Platform.PlayFab.Tests
{
	/// <summary>
	/// PlayFab LeaderboardHandler の EditMode 単体テスト。
	/// PlayFab サーバーへの実接続を必要としないオフラインテスト群。
	///
	/// SubmitScore / GetTopEntries 等の実接続が必要なテストは
	/// IntegrationTests/PlayFabLeaderboardIntegrationTests.cs を参照してください。
	/// </summary>
	public class PlayFabLeaderboardHandlerTests
	{
		private LeaderboardHandler _handler;

		[SetUp]
		public void SetUp()
		{
			_handler = new LeaderboardHandler();
		}

		// -----------------------------------------------------------------------
		// 生成
		// -----------------------------------------------------------------------

		/// <summary>
		/// LeaderboardHandler を生成しても例外が発生しないことを確認する。
		/// </summary>
		[Test]
		public void Creation_CanBeInstantiated()
		{
			Assert.IsNotNull(_handler,
				"LeaderboardHandler を生成できるはずです。");
		}

		// -----------------------------------------------------------------------
		// IServiceTestProvider
		// -----------------------------------------------------------------------

		/// <summary>
		/// GetTestOperations() が null でなく 1 件以上の操作を返すことを確認する。
		/// </summary>
		[Test]
		public void GetTestOperations_ReturnsNonNullAndNonEmpty()
		{
			var ops = _handler.GetTestOperations();

			Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
			Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
		}

		/// <summary>
		/// GetDefaultData() が期待するリーダーボード名を返すことを確認する。
		/// </summary>
		[Test]
		public void GetDefaultData_ReturnsExpectedLeaderboardName()
		{
			var data = _handler.GetDefaultData();

			Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
			Assert.AreEqual("playfab_leaderboard_001", data.LeaderboardName,
				"デフォルトの LeaderboardName は playfab_leaderboard_001 である必要があります");
		}
	}
}

#endif // !DISABLE_PLAYFABCLIENT_API
#endif // USE_CROSSPLATFORMBRIDGE_PLAYFAB
