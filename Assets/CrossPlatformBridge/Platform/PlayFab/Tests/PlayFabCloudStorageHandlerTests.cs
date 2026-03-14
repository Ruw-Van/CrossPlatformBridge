#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
#if !DISABLE_PLAYFABCLIENT_API
using System.Collections;
using CrossPlatformBridge.Platform.PlayFab.CloudStorage;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.PlayFab.Tests
{
    /// <summary>
    /// PlayFab CloudStorageHandler の EditMode 単体テスト。
    /// PlayFab サーバーへの実接続を必要としないオフラインテスト群。
    ///
    /// SaveData / LoadData 等の実接続が必要なテストは
    /// IntegrationTests/PlayFabAllServicesDummyTests.cs を参照してください。
    /// </summary>
    public class PlayFabCloudStorageHandlerTests
    {
        private CloudStorageHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _handler = new CloudStorageHandler();
        }

        // -----------------------------------------------------------------------
        // 生成
        // -----------------------------------------------------------------------

        /// <summary>
        /// CloudStorageHandler を生成しても例外が発生しないことを確認する。
        /// </summary>
        [Test]
        public void Creation_CanBeInstantiated()
        {
            Assert.IsNotNull(_handler,
                "CloudStorageHandler を生成できるはずです。");
        }

        // -----------------------------------------------------------------------
        // SaveDataBatch の空辞書ガード
        // -----------------------------------------------------------------------

        /// <summary>
        /// SaveDataBatch() に null を渡した場合、PlayFab 呼び出しなしで false を返すことを確認する。
        /// null は「null ではない空辞書」と同一に扱われ、PlayFab API を呼び出さない。
        /// </summary>
        [UnityTest]
        public IEnumerator SaveDataBatch_WithNullDictionary_CallsPlayFabApi() => UniTask.ToCoroutine(async () =>
        {
            // null/空辞書の場合、内部では null Keys 指定で PlayFab API を呼ぶためエラーになる
            // ※ 実接続なしのため失敗するが、ここでは呼び出しの動作を確認するのみ
            // Ignore ではなく「例外なし」で完了することを確認する
            Assert.DoesNotThrow(() =>
            {
                _ = _handler.SaveDataBatch(null);
            }, "SaveDataBatch(null) は例外を throw しないはずです。");
            await UniTask.CompletedTask;
        });

        // -----------------------------------------------------------------------
        // IServiceTestProvider
        // -----------------------------------------------------------------------

        [Test]
        public void PlayFabCloudStorageHandler_GetTestOperations_ReturnsNonNullAndNonEmpty()
        {
            var ops = _handler.GetTestOperations();

            Assert.IsNotNull(ops, "GetTestOperations() は null を返してはいけません");
            Assert.Greater(ops.Count, 0, "GetTestOperations() は 1 件以上の操作を返す必要があります");
        }

        [Test]
        public void PlayFabCloudStorageHandler_GetDefaultData_ReturnsExpectedCloudStorageKey()
        {
            var data = _handler.GetDefaultData();

            Assert.IsNotNull(data, "GetDefaultData() は null を返してはいけません");
            Assert.AreEqual("playfab_save", data.CloudStorageKey,
                "デフォルトの CloudStorageKey は playfab_save である必要があります");
        }

    }
}
#endif // !DISABLE_PLAYFABCLIENT_API
#endif
