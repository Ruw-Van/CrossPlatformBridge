using System;
using System.Collections.Generic;
using System.Linq;
using CrossPlatformBridge.Services.Achievement;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Platform.Dummy.Achievement
{
	/// <summary>
	/// 実績・トロフィー機能のダミー実装。
	/// インメモリで実績の解除状態と進行度を管理します。
	/// 開発およびテスト用に使用します。
	/// </summary>
	public class DummyAchievementHandler : IInternalAchievementHandler, IServiceTestProvider
	{
		private readonly HashSet<string> _unlockedAchievements = new HashSet<string>();
		private readonly Dictionary<string, float> _achievementProgress = new Dictionary<string, float>();

		/// <summary>
		/// 擬似的なネットワーク遅延（ミリ秒）
		/// </summary>
		public int SimulatedDelayMs { get; set; } = 100;

		public async UniTask<bool> UnlockAchievement(string achievementId)
		{
			if (SimulatedDelayMs > 0)
			{
				await UniTask.Delay(SimulatedDelayMs);
			}

			if (string.IsNullOrEmpty(achievementId))
			{
				Debug.LogWarning("[DummyAchievement] UnlockAchievement: achievementId is null or empty.");
				return false;
			}

			if (_unlockedAchievements.Add(achievementId))
			{
				Debug.Log($"[DummyAchievement] Unlocked achievement: {achievementId}");
			}
			else
			{
				Debug.Log($"[DummyAchievement] Achievement already unlocked: {achievementId}");
			}

			// 進行度があれば100%（1.0表記にするか用途次第だが、一旦削除または維持）として扱う
			_achievementProgress[achievementId] = 100f;

			return true;
		}

		public async UniTask<List<string>> GetUnlockedAchievements()
		{
			if (SimulatedDelayMs > 0)
			{
				await UniTask.Delay(SimulatedDelayMs);
			}

			return _unlockedAchievements.ToList();
		}

		public async UniTask<bool> SetProgress(string achievementId, float progress)
		{
			if (SimulatedDelayMs > 0)
			{
				await UniTask.Delay(SimulatedDelayMs);
			}

			if (string.IsNullOrEmpty(achievementId))
			{
				Debug.LogWarning("[DummyAchievement] SetProgress: achievementId is null or empty.");
				return false;
			}

			// すでに解除済みの場合は更新しないなどの制御は、実際のプラットフォームの挙動に合わせる
			if (_unlockedAchievements.Contains(achievementId))
			{
				Debug.Log($"[DummyAchievement] SetProgress: Achievement {achievementId} is already unlocked. Ignored.");
				return true;
			}

			_achievementProgress[achievementId] = progress;
			Debug.Log($"[DummyAchievement] Set progress for {achievementId}: {progress}");

			// 進行度が100(%)以上になったら自動的に解除扱いとする実装例
			if (progress >= 100f)
			{
				await UnlockAchievement(achievementId);
			}

			return true;
		}

		/// <summary>
		/// テスト用にすべての進捗と解除状態をリセットします。
		/// </summary>
		public void ResetAll()
		{
			_unlockedAchievements.Clear();
			_achievementProgress.Clear();
		}
		
		/// <summary>
		/// テスト用に現在の進行度を取得します。
		/// </summary>
		public float GetStoredProgress(string achievementId)
		{
			return _achievementProgress.TryGetValue(achievementId, out float progress) ? progress : 0f;
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => new TestOperation[]
		{
			new TestOperation { SectionLabel = "実績操作" },
			new TestOperation
			{
				Label = "Unlock Achievement",
				Action = async ctx =>
				{
					bool ok = await UnlockAchievement(ctx.AchievementId);
					ctx.ReportResult(ok ? $"解除完了: {ctx.AchievementId}" : "解除失敗");
					ctx.AppendLog($"UnlockAchievement({ctx.AchievementId}) → {ok}");
				}
			},
			new TestOperation
			{
				Label = "Get Unlocked Achievements",
				Action = async ctx =>
				{
					var list = await GetUnlockedAchievements();
					ctx.ReportResult(list?.Count > 0 ? string.Join("\n", list) : "（解除済み実績なし）");
					ctx.AppendLog($"GetUnlockedAchievements → {list?.Count ?? 0} 件");
				}
			},
			new TestOperation
			{
				Label = "Set Progress (50%)",
				Action = async ctx =>
				{
					bool ok = await SetProgress(ctx.AchievementId, 50f);
					ctx.ReportResult(ok ? $"進行度設定完了: {ctx.AchievementId} = 50%" : "進行度設定失敗");
					ctx.AppendLog($"SetProgress({ctx.AchievementId}, 50) → {ok}");
				}
			},
		};

		public TestDefaultData GetDefaultData() => new TestDefaultData
		{
			AchievementId = "dummy_achievement_001",
		};
	}
}
