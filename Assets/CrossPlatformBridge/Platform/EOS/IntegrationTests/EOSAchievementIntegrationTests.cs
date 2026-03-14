#if USE_CROSSPLATFORMBRIDGE_EOS
using CrossPlatformBridge.Platform.EOS;
using CrossPlatformBridge.Tests.Shared;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using NUnit.Framework;
using PlayEveryWare.EpicOnlineServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using NetworkService = CrossPlatformBridge.Services.Network.Network;
using AchievementService = CrossPlatformBridge.Services.Achievement.Achievement;

namespace CrossPlatformBridge.Platform.EOS.IntegrationTests
{
    /// <summary>
    /// EOS 実績機能の PlayMode 統合テスト。
    ///
    /// CrossPlatformBridge の Achievement ファサードを通じて EOS が正しく動作することを確認する。
    /// - NetworkService.Instance.Use&lt;EOS&gt;() で EOS SDK を初期化し、
    /// - AchievementService.Instance.Use&lt;EOS&gt;() で実績ハンドラを注入する。
    ///
    /// 実行前に EOS DevAuthTool を起動し、EOSIntegrationTestSettings.asset を
    /// Editor/IntegrationTest/ フォルダに配置してください。詳細は README.md を参照。
    /// </summary>
    public class EOSAchievementIntegrationTests : AchievementIntegrationTestBase
    {
        private const string SettingsPath =
            "Assets/CrossPlatformBridgeSettings/Editor/EOSIntegrationTestSettings.asset";

        // セッションはテストクラス内で共有する（DevAuth ログインは一度のみ）
        private static EOSIntegrationTestSettings _settings;
        private static bool _sessionInitialized;

        protected override string TestAchievementId => _settings?.TestAchievementId;

        // -----------------------------------------------------------------------
        // AchievementIntegrationTestBase 実装
        // -----------------------------------------------------------------------

        protected override async UniTask SetUpPlatform()
        {
            _settings = AssetDatabase.LoadAssetAtPath<EOSIntegrationTestSettings>(SettingsPath);
            Assume.That(_settings, Is.Not.Null,
                "EOSIntegrationTestSettings.asset が見つかりません。IntegrationTests/README.md を参照してください。");

            if (!_sessionInitialized)
            {
                bool networkInitialized = await NetworkService.Instance.Use<EOS>();
                Assert.IsTrue(networkInitialized, "Network.Use<EOS>() が失敗しました。EOS Plugin の設定を確認してください。");

                await LoginWithDevAuth(_settings);

                bool connected = await NetworkService.Instance.ConnectNetwork("", _settings.DevAuthCredentialName);
                Assert.IsTrue(connected, "Network.ConnectNetwork() が失敗しました。");

                _sessionInitialized = true;
            }

            AchievementService.Instance.Use<EOS>();
            Assert.IsTrue(AchievementService.Instance.IsInitialized, "Achievement.Use<EOS>() 後は IsInitialized が true のはずです。");
        }

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
