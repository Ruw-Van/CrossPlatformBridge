#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
#if !DISABLE_PLAYFABCLIENT_API
using System.Collections;
using CrossPlatformBridge.Platform.PlayFab.Account;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.PlayFab.Tests
{
    /// <summary>
    /// PlayFabAccount の EditMode 単体テスト。
    /// PlayFab サーバーへの実接続を必要としないオフラインテスト群。
    ///
    /// LoginAnonymous 等の実接続が必要なテストは
    /// IntegrationTests/PlayFabAllServicesDummyTests.cs を参照してください。
    /// </summary>
    public class PlayFabAccountTests
    {
        private PlayFabAccount _account;

        [SetUp]
        public void SetUp()
        {
            _account = new PlayFabAccount();
        }

        // -----------------------------------------------------------------------
        // 初期状態の検証
        // -----------------------------------------------------------------------

        /// <summary>
        /// 生成直後は IsInitialized が false であることを確認する。
        /// </summary>
        [Test]
        public void Creation_DefaultState_IsNotInitialized()
        {
            Assert.IsFalse(_account.IsInitialized,
                "生成直後は IsInitialized が false のはずです。");
        }

        /// <summary>
        /// 生成直後は AccountId が空文字であることを確認する。
        /// </summary>
        [Test]
        public void Creation_DefaultState_AccountIdIsEmpty()
        {
            Assert.AreEqual(string.Empty, _account.AccountId,
                "生成直後は AccountId が空文字のはずです。");
        }

        /// <summary>
        /// 生成直後は NickName が空文字であることを確認する。
        /// </summary>
        [Test]
        public void Creation_DefaultState_NickNameIsEmpty()
        {
            Assert.AreEqual(string.Empty, _account.NickName,
                "生成直後は NickName が空文字のはずです。");
        }

        /// <summary>
        /// 生成直後は SessionTicket が空文字であることを確認する。
        /// </summary>
        [Test]
        public void Creation_DefaultState_SessionTicketIsEmpty()
        {
            Assert.AreEqual(string.Empty, _account.SessionTicket,
                "生成直後は SessionTicket が空文字のはずです。");
        }

        // -----------------------------------------------------------------------
        // イベント
        // -----------------------------------------------------------------------

        /// <summary>
        /// OnAuthStateChanged イベントにサブスクライブ・解除できることを確認する。
        /// </summary>
        [Test]
        public void OnAuthStateChanged_CanSubscribeAndUnsubscribe()
        {
            bool received = false;
            void OnChanged(bool state) => received = state;

            _account.OnAuthStateChanged += OnChanged;
            _account.OnAuthStateChanged -= OnChanged;

            Assert.IsFalse(received);
        }

        // -----------------------------------------------------------------------
        // ShutdownAsync（初期化前）
        // -----------------------------------------------------------------------

        /// <summary>
        /// 未初期化状態で ShutdownAsync() を呼び出しても例外が発生せず、
        /// IsInitialized が false のままであることを確認する。
        /// </summary>
        [UnityTest]
        public IEnumerator ShutdownAsync_WhenNotInitialized_CompletesWithoutException() => UniTask.ToCoroutine(async () =>
        {
            await _account.ShutdownAsync();

            Assert.IsFalse(_account.IsInitialized,
                "ShutdownAsync() 後も IsInitialized は false のはずです。");
            Assert.AreEqual(string.Empty, _account.AccountId,
                "ShutdownAsync() 後は AccountId が空文字のはずです。");
            Assert.AreEqual(string.Empty, _account.SessionTicket,
                "ShutdownAsync() 後は SessionTicket が空文字のはずです。");
        });

        // -----------------------------------------------------------------------
        // IServiceTestProvider
        // -----------------------------------------------------------------------

        [Test]
        public void PlayFabAccount_GetTestOperations_ReturnsNonNullAndNonEmpty()
        {
            var ops = _account.GetTestOperations();

            Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
            Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
        }

        [Test]
        public void PlayFabAccount_GetDefaultData_IsNotNull()
        {
            var data = _account.GetDefaultData();

            Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
        }

    }
}
#endif // !DISABLE_PLAYFABCLIENT_API
#endif
