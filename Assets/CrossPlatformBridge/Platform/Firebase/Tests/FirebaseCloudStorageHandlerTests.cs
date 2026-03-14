#if USE_CROSSPLATFORMBRIDGE_FIREBASE
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.Firebase.CloudStorage;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Firebase.Tests
{
    /// <summary>
    /// FirebaseCloudStorageHandler の EditMode 単体テスト。
    /// Firebase SDK / AccountService への実接続を必要としないオフラインテスト群。
    ///
    /// Firebase Firestore への実接続が必要なテストは
    /// IntegrationTests/FirebaseAllServicesDummyTests.cs を参照してください。
    /// </summary>
    public class FirebaseCloudStorageHandlerTests
    {
        private FirebaseCloudStorageHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new FirebaseCloudStorageHandler();
        }

        // -----------------------------------------------------------------------
        // 生成
        // -----------------------------------------------------------------------

        /// <summary>
        /// FirebaseCloudStorageHandler を生成しても例外が発生しないことを確認する。
        /// </summary>
        [Test]
        public void Creation_CanBeInstantiated()
        {
            Assert.IsNotNull(_handler,
                "FirebaseCloudStorageHandler を生成できるはずです。");
        }

        // -----------------------------------------------------------------------
        // AccountService 未初期化時のガード動作
        // -----------------------------------------------------------------------

        /// <summary>
        /// AccountService が未初期化のとき SaveData() が false を返すことを確認する。
        /// 内部で InvalidOperationException が発生するが、try-catch で捕捉されて false を返す。
        /// </summary>
        [UnityTest]
        public IEnumerator SaveData_WithoutAccountService_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex(@"\[FirebaseCloudStorageHandler\]"));

            bool result = await _handler.SaveData("test_key", "test_value");

            Assert.IsFalse(result,
                "AccountService が未初期化のとき SaveData() は false を返すはずです。");
        });

        /// <summary>
        /// AccountService が未初期化のとき SaveDataBatch() が false を返すことを確認する。
        /// </summary>
        [UnityTest]
        public IEnumerator SaveDataBatch_WithoutAccountService_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex(@"\[FirebaseCloudStorageHandler\]"));

            var data = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };
            bool result = await _handler.SaveDataBatch(data);

            Assert.IsFalse(result,
                "AccountService が未初期化のとき SaveDataBatch() は false を返すはずです。");
        });

        /// <summary>
        /// SaveDataBatch() に空の辞書を渡した場合、AccountService チェック前に true を返すことを確認する。
        /// </summary>
        [UnityTest]
        public IEnumerator SaveDataBatch_WithEmptyDictionary_ReturnsTrueWithoutFirebase() => UniTask.ToCoroutine(async () =>
        {
            // 空辞書は早期リターン（Firebase 呼び出しなし）
            bool result = await _handler.SaveDataBatch(new Dictionary<string, string>());

            Assert.IsTrue(result,
                "SaveDataBatch() に空の辞書を渡した場合は true を返すはずです。");
        });

        /// <summary>
        /// AccountService が未初期化のとき LoadData() が null を返すことを確認する。
        /// </summary>
        [UnityTest]
        public IEnumerator LoadData_WithoutAccountService_ReturnsNull() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex(@"\[FirebaseCloudStorageHandler\]"));

            string result = await _handler.LoadData("test_key");

            Assert.IsNull(result,
                "AccountService が未初期化のとき LoadData() は null を返すはずです。");
        });

        /// <summary>
        /// AccountService が未初期化のとき DeleteData() が false を返すことを確認する。
        /// </summary>
        [UnityTest]
        public IEnumerator DeleteData_WithoutAccountService_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error,
                new System.Text.RegularExpressions.Regex(@"\[FirebaseCloudStorageHandler\]"));

            bool result = await _handler.DeleteData("test_key");

            Assert.IsFalse(result,
                "AccountService が未初期化のとき DeleteData() は false を返すはずです。");
        });

        // -----------------------------------------------------------------------
        // IServiceTestProvider
        // -----------------------------------------------------------------------

        [Test]
        public void FirebaseCloudStorageHandler_GetTestOperations_ReturnsNonNullAndNonEmpty()
        {
            var ops = _handler.GetTestOperations();

            Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
            Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
        }

        [Test]
        public void FirebaseCloudStorageHandler_GetDefaultData_ReturnsExpectedCloudStorageKey()
        {
            var data = _handler.GetDefaultData();

            Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
            Assert.AreEqual("firebase_save", data.CloudStorageKey,
                "デフォルトの CloudStorageKey は firebase_save である必要があります");
        }

    }
}
#endif
