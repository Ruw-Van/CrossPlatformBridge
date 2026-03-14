#if USE_CROSSPLATFORMBRIDGE_EOS
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.IntegrationTests
{
    /// <summary>
    /// EOS 統合テスト用の設定ファイル。
    /// DevAuthTool の接続情報とテスト用実績 ID・リーダーボード ID を保持する。
    ///
    /// このアセットは Editor/IntegrationTest/ フォルダに保存してください。
    /// .gitignore で除外されているため認証情報が誤ってコミットされません。
    /// 詳細は IntegrationTests/README.md を参照してください。
    /// </summary>
    [CreateAssetMenu(
        fileName = "EOSIntegrationTestSettings",
        menuName = "CrossPlatformBridge/EOS/Integration Test Settings")]
    public class EOSIntegrationTestSettings : ScriptableObject
    {
        [Header("EOS DevAuthTool 設定")]
        [Tooltip("EOS DevAuthTool のポート番号（デフォルト: 8080）")]
        [SerializeField] private int _devAuthPort = 8080;

        [Tooltip("EOS DevAuthTool に登録した資格情報名")]
        [SerializeField] private string _devAuthCredentialName = "TestUser";

        [Header("テスト用データ")]
        [Tooltip("EOS 開発者ポータルで定義済みの実績 ID")]
        [SerializeField] private string _testAchievementId = "eos_achievement_001";

        [Tooltip("EOS 開発者ポータルで定義済みのリーダーボード ID（Stat 名と一致させること）")]
        [SerializeField] private string _testLeaderboardId = "eos_leaderboard_001";

        public int DevAuthPort => _devAuthPort;
        public string DevAuthCredentialName => _devAuthCredentialName;
        public string TestAchievementId => _testAchievementId;
        public string TestLeaderboardId => _testLeaderboardId;
    }
}
#endif
