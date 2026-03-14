#if USE_CROSSPLATFORMBRIDGE_EOS
using System.Collections.Generic;
using CrossPlatformBridge.Services.Achievement;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Achievements;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Achievement
{
	/// <summary>
	/// EOS 実績・トロフィー用の内部実装。
	/// Epic.OnlineServices.Achievements のAPIを利用して実績を操作する。
	/// </summary>
	public class EOSAchievementHandler : IInternalAchievementHandler, IServiceTestProvider
	{
		private AchievementsInterface Achievements =>
			EOSManager.Instance.GetEOSPlatformInterface().GetAchievementsInterface();

		private ProductUserId LocalUserId =>
			EOSManager.Instance.GetProductUserId();

		/// <inheritdoc/>
		public UniTask<bool> UnlockAchievement(string achievementId)
		{
			// EOS の実績は進捗を完了させることで解除する
			var tcs = new UniTaskCompletionSource<bool>();
			var options = new UnlockAchievementsOptions
			{
				UserId = LocalUserId,
				AchievementIds = new Utf8String[] { achievementId }
			};

			Achievements.UnlockAchievements(ref options, null, (ref OnUnlockAchievementsCompleteCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
				{
					Debug.Log($"[EOS Achievement] Unlocked achievement: {achievementId}");
					tcs.TrySetResult(true);
				}
				else
				{
					Debug.LogError($"[EOS Achievement] Failed to unlock achievement: {achievementId}, Result: {info.ResultCode}");
					tcs.TrySetResult(false);
				}
			});

			return tcs.Task;
		}

		/// <inheritdoc/>
		public UniTask<List<string>> GetUnlockedAchievements()
		{
			var tcs = new UniTaskCompletionSource<List<string>>();

			var queryOptions = new QueryPlayerAchievementsOptions
			{
				LocalUserId = LocalUserId,
				TargetUserId = LocalUserId
			};

			Achievements.QueryPlayerAchievements(ref queryOptions, null, (ref OnQueryPlayerAchievementsCompleteCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
				{
					var getCountOptions = new GetPlayerAchievementCountOptions { UserId = LocalUserId };
					var count = Achievements.GetPlayerAchievementCount(ref getCountOptions);
					var unlockedList = new List<string>();

					for (uint i = 0; i < count; i++)
					{
						var copyOptions = new CopyPlayerAchievementByIndexOptions { LocalUserId = LocalUserId, TargetUserId = LocalUserId, AchievementIndex = i };
						if (Achievements.CopyPlayerAchievementByIndex(ref copyOptions, out var achievement) == Result.Success && achievement.HasValue)
						{
							// 進捗完了、または解除時刻があるものを解放済みとみなす
							if (achievement.Value.Progress >= 1f || achievement.Value.UnlockTime.HasValue)
							{
								unlockedList.Add(achievement.Value.AchievementId);
							}
						}
					}

					tcs.TrySetResult(unlockedList);
				}
				else
				{
					Debug.LogError($"[EOS Achievement] Failed to query achievements. Result: {info.ResultCode}");
					tcs.TrySetResult(new List<string>());
				}
			});

			return tcs.Task;
		}

		/// <inheritdoc/>
		public UniTask<bool> SetProgress(string achievementId, float progress)
		{
			// EOS は通常 Stats 連携で進捗を更新する想定。
			// 任意の進捗を直接設定するAPIは用意されていないため、
			// 進捗が完了したら Unlock を呼ぶ運用に寄せる。
			// progress >= 100 の場合は Unlock を実行。
			if (progress >= 100f)
			{
				return UnlockAchievement(achievementId);
			}
			
			Debug.LogWarning("[EOS Achievement] SetProgress is not natively supported without Stats API. Use UnlockAchievement or bind progress to EOS Stats.");
			return UniTask.FromResult(false);
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

		public TestDefaultData GetDefaultData() => new TestDefaultData { AchievementId = "eos_achievement_001" };
	}
}
#endif
