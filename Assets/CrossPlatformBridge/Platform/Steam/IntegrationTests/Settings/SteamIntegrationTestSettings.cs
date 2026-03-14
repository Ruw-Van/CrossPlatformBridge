#if USE_CROSSPLATFORMBRIDGE_STEAM
using UnityEngine;

namespace CrossPlatformBridge.Platform.Steam.IntegrationTests
{
    /// <summary>
    /// Steam 統合テスト用の設定ファイル。
    /// テスト対象の実績 ID を保持する。
    ///
    /// このアセットは Editor/IntegrationTest/ フォルダに保存してください。
    /// .gitignore で除外されているため実績 ID が誤ってコミットされません。
    /// 詳細は IntegrationTests/README.md を参照してください。
    /// </summary>
    [CreateAssetMenu(
        fileName = "SteamIntegrationTestSettings",
        menuName = "CrossPlatformBridge/Steam/Integration Test Settings")]
    public class SteamIntegrationTestSettings : ScriptableObject
    {
        [Header("テスト用データ")]
        [Tooltip("Steamworks Partner Dashboard で定義済みの実績 API 名")]
        [SerializeField] private string _testAchievementId = "ACH_WIN_ONE_GAME";

        public string TestAchievementId => _testAchievementId;
    }
}
#endif
