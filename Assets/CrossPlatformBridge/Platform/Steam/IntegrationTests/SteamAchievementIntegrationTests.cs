#if USE_CROSSPLATFORMBRIDGE_STEAM
#if !DISABLESTEAMWORKS
using CrossPlatformBridge.Platform.Steam;
using CrossPlatformBridge.Tests.Shared;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using AccountService = CrossPlatformBridge.Services.Account.AccountService;
using AchievementService = CrossPlatformBridge.Services.Achievement.Achievement;

namespace CrossPlatformBridge.Platform.Steam.IntegrationTests
{
    /// <summary>
    /// Steam 実績機能の PlayMode 統合テスト。
    ///
    /// CrossPlatformBridge の Achievement ファサードを通じて Steam が正しく動作することを確認する。
    /// - AccountService.Instance.Use&lt;Steam&gt;() + InitializeAsync() で SteamAPI を初期化し、
    /// - AchievementService.Instance.Use&lt;Steam&gt;() で実績ハンドラを注入する。
    ///
    /// 実行前に Steam クライアントを起動し、SteamIntegrationTestSettings.asset を
    /// Editor/IntegrationTest/ フォルダに配置してください。詳細は README.md を参照。
    /// </summary>
    public class SteamAchievementIntegrationTests : AchievementIntegrationTestBase
    {
        private const string SettingsPath =
            "Assets/CrossPlatformBridgeSettings/Editor/SteamIntegrationTestSettings.asset";

        // セッションはテストクラス内で共有する（SteamAPI.Init() は一度のみ）
        private static SteamIntegrationTestSettings _settings;
        private static bool _initialized;

        protected override string TestAchievementId => _settings?.TestAchievementId;

        // -----------------------------------------------------------------------
        // AchievementIntegrationTestBase 実装
        // -----------------------------------------------------------------------

        protected override async UniTask SetUpPlatform()
        {
            _settings = AssetDatabase.LoadAssetAtPath<SteamIntegrationTestSettings>(SettingsPath);
            Assume.That(_settings, Is.Not.Null,
                "SteamIntegrationTestSettings.asset が見つかりません。IntegrationTests/README.md を参照してください。");

            if (!_initialized)
            {
                AccountService.Instance.Use<Steam>();
                try
                {
                    bool ok = await AccountService.Instance.InitializeAsync();
                    Assume.That(ok, Is.True,
                        "SteamAPI.Init() が失敗しました。Steam クライアントが起動していること、および " +
                        "steam_appid.txt がプロジェクトルートに配置されていることを確認してください。");
                }
                catch (CrossPlatformBridge.Services.Account.AccountServiceException e)
                {
                    Assume.That(false, e.Message);
                }
                _initialized = true;
            }

            AchievementService.Instance.Use<Steam>();
            Assert.IsTrue(AchievementService.Instance.IsInitialized, "Achievement.Use<Steam>() 後は IsInitialized が true のはずです。");
        }
    }
}
#endif
#endif
