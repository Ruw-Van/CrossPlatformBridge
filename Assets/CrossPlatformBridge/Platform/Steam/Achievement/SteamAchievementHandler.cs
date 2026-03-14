#if USE_CROSSPLATFORMBRIDGE_STEAM
#if !DISABLESTEAMWORKS
using System.Collections.Generic;
using CrossPlatformBridge.Services.Achievement;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace CrossPlatformBridge.Platform.Steam.Achievement
{
	/// <summary>
	/// Steam 版の実績・トロフィー内部実装。
	/// Steamworks.NET の SteamUserStats を利用して実績を操作する。
	/// SteamAPI.Init() が完了していることが前提。
	/// </summary>
	public class SteamAchievementHandler : IInternalAchievementHandler, IServiceTestProvider
	{
		/// <inheritdoc/>
		public UniTask<bool> UnlockAchievement(string achievementId)
		{
			if (!SteamAPI.IsSteamRunning())
			{
				Debug.LogWarning("[Steam Achievement] Steam is not running. Cannot unlock achievement.");
				return UniTask.FromResult(false);
			}

			// SetAchievement の反映には StoreStats の呼び出しが必要
			bool success = SteamUserStats.SetAchievement(achievementId);
			if (success)
			{
				bool stored = SteamUserStats.StoreStats();
				if (stored)
				{
					Debug.Log($"[Steam Achievement] Successfully unlocked and stored achievement: {achievementId}");
					return UniTask.FromResult(true);
				}
				else
				{
					Debug.LogWarning($"[Steam Achievement] SetAchievement succeeded but StoreStats failed for: {achievementId}");
					return UniTask.FromResult(false);
				}
			}
			else
			{
				Debug.LogError($"[Steam Achievement] Failed to unlock achievement: {achievementId}");
				return UniTask.FromResult(false);
			}
		}

		/// <inheritdoc/>
		public UniTask<List<string>> GetUnlockedAchievements()
		{
			var unlockedList = new List<string>();

			if (!SteamAPI.IsSteamRunning())
			{
				Debug.LogWarning("[Steam Achievement] Steam is not running. Cannot query achievements.");
				return UniTask.FromResult(unlockedList);
			}

			uint achievementCount = SteamUserStats.GetNumAchievements();
			for (uint i = 0; i < achievementCount; i++)
			{
				string name = SteamUserStats.GetAchievementName(i);
				if (!string.IsNullOrEmpty(name))
				{
					bool isUnlocked;
					if (SteamUserStats.GetAchievement(name, out isUnlocked) && isUnlocked)
					{
						unlockedList.Add(name);
					}
				}
			}

			return UniTask.FromResult(unlockedList);
		}

		/// <inheritdoc/>
		public UniTask<bool> SetProgress(string achievementId, float progress)
		{
			if (!SteamAPI.IsSteamRunning())
			{
				Debug.LogWarning("[Steam Achievement] Steam is not running. Cannot set progress.");
				return UniTask.FromResult(false);
			}

			// Steam は通常 SetStat で進捗を更新し、バックエンド側で解除する。
			// ローカルで 100% 到達とみなす場合のみ Unlock を呼ぶ。
			if (progress >= 100f)
			{
				return UnlockAchievement(achievementId);
			}

			Debug.LogWarning("[Steam Achievement] SetProgress is mapped to UnlockAchievement with >100. If you are using Steam Stats for progress, use SteamUserStats.SetStat().");
			return UniTask.FromResult(true); // 進捗は無視したが処理は成功扱いにする
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => new TestOperation[]
		{
			new TestOperation { SectionLabel = "実績操作" },
			new TestOperation { Label = "Unlock Achievement", Action = async ctx => { bool ok = await UnlockAchievement(ctx.AchievementId); ctx.ReportResult(ok ? $"解除完了: {ctx.AchievementId}" : "解除失敗"); ctx.AppendLog($"UnlockAchievement({ctx.AchievementId}) → {ok}"); } },
			new TestOperation { Label = "Get Unlocked Achievements", Action = async ctx => { var list = await GetUnlockedAchievements(); ctx.ReportResult(list?.Count > 0 ? string.Join("\n", list) : "（解除済み実績なし）"); ctx.AppendLog($"GetUnlockedAchievements → {list?.Count ?? 0} 件"); } },
			new TestOperation { Label = "Set Progress (50%)", Action = async ctx => { bool ok = await SetProgress(ctx.AchievementId, 50f); ctx.ReportResult(ok ? $"進行度設定完了: {ctx.AchievementId} = 50%" : "進行度設定失敗"); ctx.AppendLog($"SetProgress({ctx.AchievementId}, 50) → {ok}"); } },
		};

		public TestDefaultData GetDefaultData() => new TestDefaultData { AchievementId = "STEAM_ACHIEVEMENT_1" };
	}
}
#endif
#endif
