#if USE_CROSSPLATFORMBRIDGE_EOS
using CrossPlatformBridge.Platform.EOS.Payment;
using CrossPlatformBridge.Testing;
using NUnit.Framework;

namespace CrossPlatformBridge.Platform.EOS.Tests
{
	/// <summary>
	/// EOS 決済ハンドラの EditMode テスト。
	/// EOS 接続が不要なコンストラクタ・初期状態・IServiceTestProvider の検証を行う。
	/// </summary>
	public class EOSPaymentHandlerTests
	{
		private PaymentHandler _handler;

		[SetUp]
		public void Setup()
		{
			_handler = new PaymentHandler();
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
		public void EOSPaymentHandler_CanBeInstantiated()
		{
			Assert.IsNotNull(_handler, "PaymentHandler は new でインスタンス化できる必要があります");
		}

		// ----------------------------------------------------------------
		// イベント
		// ----------------------------------------------------------------

		[Test]
		public void EOSPaymentHandler_EventsAreSubscribable()
		{
			// イベントの購読・解除ができることを確認
			Assert.DoesNotThrow(() =>
			{
				_handler.OnCurrencyUpdated += (_, __) => { };
				_handler.OnCurrencyUpdated -= (_, __) => { };
				_handler.OnPurchaseError   += _ => { };
				_handler.OnPurchaseError   -= _ => { };
			}, "イベントの購読・解除ができる必要があります");
		}

		// ----------------------------------------------------------------
		// IServiceTestProvider
		// ----------------------------------------------------------------

		[Test]
		public void EOSPaymentHandler_GetTestOperations_ReturnsNonNullAndNonEmpty()
		{
			var ops = _handler.GetTestOperations();

			Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
			Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
		}

		[Test]
		public void EOSPaymentHandler_GetDefaultData_ReturnsNonNull()
		{
			var data = _handler.GetDefaultData();

			Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
		}

		// ----------------------------------------------------------------
		// TODO: 統合テスト（実 EOS 接続・DevAuthTool 起動が必要）
		// ----------------------------------------------------------------
		// - GetCatalog / PurchaseItem / GetInventory / ConsumeItem は
		//   IntegrationTests への追加を検討
	}
}
#endif
