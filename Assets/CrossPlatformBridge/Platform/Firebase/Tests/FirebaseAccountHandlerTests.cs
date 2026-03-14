#if USE_CROSSPLATFORMBRIDGE_FIREBASE
using System.Collections;
using CrossPlatformBridge.Platform.Firebase.Account;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Firebase.Tests
{
    /// <summary>
    /// FirebaseAccountHandler の EditMode 単体テスト。
    /// Firebase SDK への実接続を必要としないオフラインテスト群。
    ///
    /// Firebase への実接続が必要なテスト（InitializeAsync 等）は
    /// IntegrationTests/FirebaseAllServicesDummyTests.cs を参照してください。
    /// </summary>
    public class FirebaseAccountHandlerTests
    {
        private FirebaseAccountHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new FirebaseAccountHandler();
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
            Assert.IsFalse(_handler.IsInitialized,
                "生成直後は IsInitialized が false のはずです。");
        }

        /// <summary>
        /// 生成直後は AccountId が空文字であることを確認する。
        /// Firebase ユーザーが未設定の場合は string.Empty を返す。
        /// </summary>
        [Test]
        public void Creation_DefaultState_AccountIdIsEmpty()
        {
            Assert.AreEqual(string.Empty, _handler.AccountId,
                "生成直後は AccountId が空文字のはずです。");
        }

        /// <summary>
        /// 生成直後は NickName が "AnonymousUser" であることを確認する。
        /// Firebase ユーザーが未設定の場合は "AnonymousUser" を返す。
        /// </summary>
        [Test]
        public void Creation_DefaultState_NickNameIsAnonymousUser()
        {
            Assert.AreEqual("AnonymousUser", _handler.NickName,
                "生成直後は NickName が \"AnonymousUser\" のはずです。");
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

            _handler.OnAuthStateChanged += OnChanged;
            _handler.OnAuthStateChanged -= OnChanged;

            // サブスクライブ解除後にイベントが発火しないことを確認
            Assert.IsFalse(received);
        }

        // -----------------------------------------------------------------------
        // ShutdownAsync（初期化前）
        // -----------------------------------------------------------------------

        /// <summary>
        /// 未初期化状態で ShutdownAsync() を呼び出しても例外が発生しないことを確認する。
        /// </summary>
        [UnityTest]
        public IEnumerator ShutdownAsync_WhenNotInitialized_CompletesWithoutException() => UniTask.ToCoroutine(async () =>
        {
            // 未初期化でも例外なく完了するはず
            await _handler.ShutdownAsync();

            Assert.IsFalse(_handler.IsInitialized,
                "ShutdownAsync() 後も IsInitialized は false のはずです。");
        });

        // -----------------------------------------------------------------------
        // InitializeAsync（Firebase SDK が必要）
        // -----------------------------------------------------------------------

        /// <summary>
        /// InitializeAsync() は Firebase SDK（google-services.json）が必要なため Ignore。
        /// 統合テストは IntegrationTests/ で実行してください。
        /// </summary>
        // -----------------------------------------------------------------------
        // IServiceTestProvider
        // -----------------------------------------------------------------------

        [Test]
        public void FirebaseAccountHandler_GetTestOperations_ReturnsNonNullAndNonEmpty()
        {
            var ops = _handler.GetTestOperations();

            Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
            Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
        }

        [Test]
        public void FirebaseAccountHandler_GetDefaultData_IsNotNull()
        {
            var data = _handler.GetDefaultData();

            Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
        }

    }
}
#endif
