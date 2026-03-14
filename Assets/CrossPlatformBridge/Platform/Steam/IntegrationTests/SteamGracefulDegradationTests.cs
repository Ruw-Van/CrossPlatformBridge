#if USE_CROSSPLATFORMBRIDGE_STEAM
#if !DISABLESTEAMWORKS
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.Steam.Account;
using CrossPlatformBridge.Platform.Steam.Achievement;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Steam.IntegrationTests
{
    /// <summary>
    /// Steam 未起動（グレースフルデグラデーション）テスト。
    /// Steam クライアントなし（CI / 開発環境）でも実行可能な、
    /// 各サービスの「失敗時の安全な返り値」を検証するテスト群。
    ///
    /// Steam 接続環境での実績テストは SteamAchievementIntegrationTests を参照。
    /// </summary>
    public class SteamGracefulDegradationTests
    {
        // ══════════════════════════════════════════════════════════
        // Account Service
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 未初期化状態で ShutdownAsync() を呼んでも例外が発生せず、
        /// IsInitialized が false のままであることを検証する。
        /// </summary>
        [UnityTest]
        public IEnumerator Account_Shutdown_WhenNotInitialized_IsNoOp() => UniTask.ToCoroutine(async () =>
        {
            var account = new SteamAccountService();
            await account.ShutdownAsync();

            Assert.IsFalse(account.IsInitialized, "未初期化状態での Shutdown は IsInitialized を false のまま保つ");
        });

        // ══════════════════════════════════════════════════════════
        // Achievement Handler
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Steam 未起動時に UnlockAchievement は false を返すことを検証する。
        /// </summary>
        [UnityTest]
        public IEnumerator Achievement_UnlockAchievement_WhenSteamNotRunning_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            var handler = new SteamAchievementHandler();
            bool result = await handler.UnlockAchievement("ACH_DUMMY");

            Assert.IsFalse(result, "Steam 未起動時は false を返す");
        });

        /// <summary>
        /// Steam 未起動時に GetUnlockedAchievements は null でない空リストを返すことを検証する。
        /// </summary>
        [UnityTest]
        public IEnumerator Achievement_GetUnlockedAchievements_WhenSteamNotRunning_ReturnsEmptyList() => UniTask.ToCoroutine(async () =>
        {
            var handler = new SteamAchievementHandler();
            List<string> result = await handler.GetUnlockedAchievements();

            Assert.IsNotNull(result, "null ではなく空リストを返す");
            Assert.AreEqual(0, result.Count, "Steam 未起動時は 0 件");
        });

        /// <summary>
        /// Steam 未起動時に SetProgress は false を返すことを検証する。
        /// </summary>
        [UnityTest]
        public IEnumerator Achievement_SetProgress_WhenSteamNotRunning_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            var handler = new SteamAchievementHandler();
            bool result = await handler.SetProgress("ACH_DUMMY", 50f);

            Assert.IsFalse(result, "Steam 未起動時は false を返す");
        });
    }
}
#endif
#endif
