#if USE_CROSSPLATFORMBRIDGE_PUN2
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.PUN2;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Tests.Shared;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Photon.Pun;
using UnityEngine.TestTools;
using NetworkService = CrossPlatformBridge.Services.Network.Network;

namespace CrossPlatformBridge.Platform.PUN2.IntegrationTests
{
    /// <summary>
    /// PUN2 ネットワーク機能の PlayMode 統合テスト。
    ///
    /// CrossPlatformBridge の Network ファサードを通じて PUN2 が正しく動作することを確認する。
    /// - NetworkService.Instance.Use&lt;PUN2&gt;() でハンドラを登録し、
    /// - NetworkService.Instance.ConnectNetwork() / CreateLobby() などのファサード API を呼ぶことで
    ///   Bridge 全体のスタックを検証する。
    ///
    /// 実行前に PhotonServerSettings.asset の AppIdRealtime を設定してください。
    /// Settings: Edit > Project Settings > Photon Unity Networking > PUN 2
    /// </summary>
    public class PUN2NetworkIntegrationTests : NetworkIntegrationTestBase
    {
        // -----------------------------------------------------------------------
        // NetworkIntegrationTestBase 実装
        // -----------------------------------------------------------------------

        protected override async UniTask SetUpPlatform()
        {
            Assume.That(
                !string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime),
                "PhotonServerSettings の AppIdRealtime が設定されていません。" +
                "Edit > Project Settings > Photon Unity Networking > PUN 2 で設定してください。");

            bool initialized = await NetworkService.Instance.Use<PUN2>();
            Assert.IsTrue(initialized, "Network.Use<PUN2>() が失敗しました。PhotonServerSettings を確認してください。");
            Assert.IsTrue(NetworkService.Instance.IsInitialized, "Use<PUN2>() 後は IsInitialized が true のはずです。");

            bool connected = await NetworkService.Instance.ConnectNetwork("", "");
            Assert.IsTrue(connected, "Network.ConnectNetwork() が失敗しました。");
        }

        /// <summary>PUN2 は Photon の仕様によりルーム名が 20 文字制限。</summary>
        protected override string TrimRoomName(string name) =>
            name.Length > 20 ? name[..20] : name;

        // -----------------------------------------------------------------------
        // PUN2 固有テスト
        // -----------------------------------------------------------------------

        /// <summary>
        /// PUN2 は Photon の仕様によりロビー検索をサポートしないため、
        /// SearchLobby() が常に空リストを返すことを確認する。
        /// </summary>
        [UnityTest]
        public IEnumerator SearchLobby_Always_ReturnsEmptyList() => UniTask.ToCoroutine(async () =>
        {
            IRoomSettings searchSettings = NetworkService.Instance.PrepareRoomSettings();
            searchSettings.RoomName = "SearchTestLobby";
            List<object> results = await NetworkService.Instance.SearchLobby(searchSettings);

            Assert.IsNotNull(results, "SearchLobby() が null を返しました。");
            Assert.AreEqual(0, results.Count, "PUN2 の SearchLobby() は常に空リストを返すはずです。");
        });
    }
}
#endif
