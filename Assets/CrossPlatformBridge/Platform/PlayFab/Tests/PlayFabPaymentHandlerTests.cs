#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
#if !DISABLE_PLAYFABCLIENT_API
using System.Collections.Generic;
using CrossPlatformBridge.Platform.PlayFab.Payment;
using CrossPlatformBridge.Services.Payment;
using CrossPlatformBridge.Testing;
using NUnit.Framework;

namespace CrossPlatformBridge.Platform.PlayFab.Tests
{
    /// <summary>
    /// PlayFab PaymentHandler の EditMode 単体テスト。
    /// PlayFab サーバーへの実接続を必要としないオフラインテスト群。
    ///
    /// GetCatalog / PurchaseItem 等の実接続が必要なテストは
    /// IntegrationTests/PlayFabAllServicesDummyTests.cs を参照してください。
    /// </summary>
    public class PlayFabPaymentHandlerTests
    {
        private PaymentHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new PaymentHandler();
        }

        // -----------------------------------------------------------------------
        // 生成
        // -----------------------------------------------------------------------

        /// <summary>
        /// PaymentHandler を生成しても例外が発生しないことを確認する。
        /// </summary>
        [Test]
        public void Creation_CanBeInstantiated()
        {
            Assert.IsNotNull(_handler,
                "PaymentHandler を生成できるはずです。");
        }

        // -----------------------------------------------------------------------
        // イベント
        // -----------------------------------------------------------------------

        /// <summary>
        /// OnCurrencyUpdated イベントにサブスクライブ・解除できることを確認する。
        /// </summary>
        [Test]
        public void OnCurrencyUpdated_CanSubscribeAndUnsubscribe()
        {
            string receivedCode = null;
            int receivedBalance = -1;
            void OnUpdated(string code, int balance)
            {
                receivedCode = code;
                receivedBalance = balance;
            }

            _handler.OnCurrencyUpdated += OnUpdated;
            _handler.OnCurrencyUpdated -= OnUpdated;

            Assert.IsNull(receivedCode);
            Assert.AreEqual(-1, receivedBalance);
        }

        /// <summary>
        /// OnPurchaseError イベントにサブスクライブ・解除できることを確認する。
        /// </summary>
        [Test]
        public void OnPurchaseError_CanSubscribeAndUnsubscribe()
        {
            string receivedError = null;
            void OnError(string message) => receivedError = message;

            _handler.OnPurchaseError += OnError;
            _handler.OnPurchaseError -= OnError;

            Assert.IsNull(receivedError);
        }

        // -----------------------------------------------------------------------
        // IServiceTestProvider
        // -----------------------------------------------------------------------

        [Test]
        public void PlayFabPaymentHandler_GetTestOperations_ReturnsNonNullAndNonEmpty()
        {
            var ops = _handler.GetTestOperations();

            Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
            Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
        }

        [Test]
        public void PlayFabPaymentHandler_GetDefaultData_ReturnsExpectedCurrencyCode()
        {
            var data = _handler.GetDefaultData();

            Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
            Assert.AreEqual("GD", data.PaymentCurrencyCode,
                "デフォルトの PaymentCurrencyCode は GD である必要があります");
        }

        // -----------------------------------------------------------------------
        // データモデル: CatalogItemInfo
        // -----------------------------------------------------------------------

        /// <summary>
        /// CatalogItemInfo のフィールドに値を設定・取得できることを確認する。
        /// </summary>
        [Test]
        public void CatalogItemInfo_FieldAssignment_StoresValues()
        {
            var prices = new Dictionary<string, uint> { { "GD", 100u }, { "RG", 50u } };
            var item = new CatalogItemInfo
            {
                ItemId                = "sword_001",
                DisplayName           = "Iron Sword",
                Description           = "A basic iron sword.",
                VirtualCurrencyPrices = prices,
            };

            Assert.AreEqual("sword_001", item.ItemId);
            Assert.AreEqual("Iron Sword", item.DisplayName);
            Assert.AreEqual("A basic iron sword.", item.Description);
            Assert.AreEqual(2, item.VirtualCurrencyPrices.Count);
            Assert.AreEqual(100u, item.VirtualCurrencyPrices["GD"]);
            Assert.AreEqual(50u, item.VirtualCurrencyPrices["RG"]);
        }

        /// <summary>
        /// CatalogItemInfo の VirtualCurrencyPrices が null の場合を確認する。
        /// </summary>
        [Test]
        public void CatalogItemInfo_WithNullPrices_FieldIsNull()
        {
            var item = new CatalogItemInfo
            {
                ItemId      = "free_item",
                DisplayName = "Free Item",
            };

            Assert.IsNull(item.VirtualCurrencyPrices,
                "VirtualCurrencyPrices を設定しない場合は null のはずです。");
        }

        // -----------------------------------------------------------------------
        // データモデル: InventoryItemInfo
        // -----------------------------------------------------------------------

        /// <summary>
        /// InventoryItemInfo のフィールドに値を設定・取得できることを確認する。
        /// </summary>
        [Test]
        public void InventoryItemInfo_FieldAssignment_StoresValues()
        {
            var item = new InventoryItemInfo
            {
                ItemInstanceId = "instance_abc",
                ItemId         = "sword_001",
                DisplayName    = "Iron Sword",
                RemainingUses  = 5,
            };

            Assert.AreEqual("instance_abc", item.ItemInstanceId);
            Assert.AreEqual("sword_001", item.ItemId);
            Assert.AreEqual("Iron Sword", item.DisplayName);
            Assert.AreEqual(5, item.RemainingUses);
        }

        /// <summary>
        /// RemainingUses が null（使用回数無制限）の場合を確認する。
        /// </summary>
        [Test]
        public void InventoryItemInfo_WithNullRemainingUses_IsUnlimited()
        {
            var item = new InventoryItemInfo
            {
                ItemInstanceId = "instance_xyz",
                ItemId         = "unlimited_item",
                DisplayName    = "Unlimited Item",
                RemainingUses  = null,
            };

            Assert.IsNull(item.RemainingUses,
                "RemainingUses が null の場合は使用回数無制限を意味するはずです。");
        }
    }
}
#endif // !DISABLE_PLAYFABCLIENT_API
#endif
