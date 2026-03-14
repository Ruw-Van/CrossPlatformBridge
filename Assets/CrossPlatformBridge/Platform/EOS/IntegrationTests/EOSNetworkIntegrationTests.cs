#if USE_CROSSPLATFORMBRIDGE_EOS
using System.Collections;
using System.Collections.Generic;
using NetworkService = CrossPlatformBridge.Services.Network.Network;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Tests.Shared;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using NUnit.Framework;
using PlayEveryWare.EpicOnlineServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using CrossPlatformBridge.Platform.EOS;
using EosRoomSettings = CrossPlatformBridge.Platform.EOS.Network.RoomSettings;

namespace CrossPlatformBridge.Platform.EOS.IntegrationTests
{
    /// <summary>
    /// EOS ネットワーク機能の PlayMode 統合テスト。
    ///
    /// テストの目的は、CrossPlatformBridge の Network ファサードを通じて EOS が
    /// 正しく動作することを確認すること。
    /// - NetworkService.Instance.Use&lt;EOS&gt;() でハンドラを登録し、
    /// - NetworkService.Instance.ConnectNetwork() / CreateLobby() などのファサード API を呼ぶことで
    ///   Bridge 全体のスタックを検証する。
    ///
    /// 実行前に EOS DevAuthTool を起動し、EOSIntegrationTestSettings.asset を
    /// Editor/IntegrationTest/ フォルダに配置してください。詳細は README.md を参照。
    /// </summary>
    public class EOSNetworkIntegrationTests : NetworkIntegrationTestBase
    {
        private const string SettingsPath =
            "Assets/CrossPlatformBridgeSettings/Editor/EOSIntegrationTestSettings.asset";

        // セッションはテストクラス内で共有する（DevAuth ログインは一度のみ）
        private static EOSIntegrationTestSettings _settings;
        private static bool _sessionInitialized;

        // -----------------------------------------------------------------------
        // NetworkIntegrationTestBase 実装
        // -----------------------------------------------------------------------

        protected override async UniTask SetUpPlatform()
        {
            _settings = AssetDatabase.LoadAssetAtPath<EOSIntegrationTestSettings>(SettingsPath);
            Assume.That(_settings, Is.Not.Null,
                "EOSIntegrationTestSettings.asset が見つかりません。IntegrationTests/README.md を参照してください。");

            bool initialized = await NetworkService.Instance.Use<EOS>();
            Assert.IsTrue(initialized, "Network.Use<EOS>() が失敗しました。EOS Plugin の設定を確認してください。");
            Assert.IsTrue(NetworkService.Instance.IsInitialized, "Use<EOS>() 後は IsInitialized が true のはずです。");

            if (!_sessionInitialized)
            {
                await LoginWithDevAuth(_settings);
                _sessionInitialized = true;
            }

            bool connected = await NetworkService.Instance.ConnectNetwork("", _settings.DevAuthCredentialName);
            Assert.IsTrue(connected, "Network.ConnectNetwork() が失敗しました。");
        }

        protected override async UniTask TearDownPlatform()
        {
            // EOS SDK をシャットダウンしたので次回 SetUp で必ずログインし直す
            _sessionInitialized = false;
            await UniTask.CompletedTask;
        }

        // -----------------------------------------------------------------------
        // EOS 固有テスト: SearchLobby（リトライあり）
        // -----------------------------------------------------------------------

        /// <summary>
        /// 作成したロビーが SearchLobby() で見つかることを確認する。
        /// EOS はロビー検索インデックスが結果整合性のため、最大 5 回・2 秒間隔でリトライする。
        /// </summary>
        [UnityTest]
        public IEnumerator SearchLobby_CreatedLobby_FindsAtLeastOneResult() => UniTask.ToCoroutine(async () =>
        {
            var createSettings = NetworkService.Instance.PrepareRoomSettings() as EosRoomSettings;
            createSettings.RoomName = "SearchTestLobby";
            createSettings.MaxPlayers = 4;

            bool created = await NetworkService.Instance.CreateLobby(createSettings);
            _testCreatedLobby = created;
            Assert.IsTrue(created, "ロビーの作成に失敗しました。");

            var searchSettings = NetworkService.Instance.PrepareRoomSettings() as EosRoomSettings;
            searchSettings.RoomName = "SearchTestLobby";

            List<object> results = new List<object>();
            for (int retry = 0; retry < 5; retry++)
            {
                results = await NetworkService.Instance.SearchLobby(searchSettings);
                if (results.Count > 0) break;
                if (retry < 4) await UniTask.Delay(System.TimeSpan.FromSeconds(2));
            }

            Assert.IsNotNull(results, "Network.SearchLobby() が null を返しました。");
            Assert.Greater(results.Count, 0, "作成したロビーが検索結果に含まれていません。");
        });

        // -----------------------------------------------------------------------
        // ヘルパー
        // -----------------------------------------------------------------------

        private static async UniTask LoginWithDevAuth(EOSIntegrationTestSettings settings)
        {
            var authInterface = EOSManager.Instance.GetEOSPlatformInterface().GetAuthInterface();
            if (authInterface.GetLoggedInAccountsCount() > 0)
            {
                Debug.Log("[EOS Integration] 既存の Auth セッションを再利用します");
                return;
            }

            var loginOptions = new LoginOptions
            {
                Credentials = new Credentials
                {
                    Type = LoginCredentialType.Developer,
                    Id = $"localhost:{settings.DevAuthPort}",
                    Token = settings.DevAuthCredentialName,
                },
                ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence,
            };

            var tcs = new UniTaskCompletionSource<LoginCallbackInfo>();
            authInterface.Login(ref loginOptions, null,
                (ref LoginCallbackInfo info) => tcs.TrySetResult(info));

            var result = await tcs.Task;
            Assert.AreEqual(Result.Success, result.ResultCode,
                $"DevAuth ログイン失敗: {result.ResultCode}。DevAuthTool (port:{settings.DevAuthPort}) が起動していることを確認してください。");
        }
    }
}
#endif
