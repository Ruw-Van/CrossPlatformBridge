using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CrossPlatformBridge.Services.Achievement
{
	/// <summary>
	/// プラットフォーム固有の実績・トロフィー処理を抽象化するインターフェース。
	/// </summary>
	public interface IInternalAchievementHandler
	{
		/// <summary>
		/// 指定したIDの実績を解除します。
		/// </summary>
		/// <param name="achievementId">解除する実績のID</param>
		UniTask<bool> UnlockAchievement(string achievementId);

		/// <summary>
		/// 現在解除済みの実績ID一覧を取得します。
		/// </summary>
		UniTask<List<string>> GetUnlockedAchievements();

		/// <summary>
		/// 実績の進行度を更新します。
		/// 一部のプラットフォームでは進行度ではなく達成可否のみをサポートする場合があります。
		/// </summary>
		/// <param name="achievementId">進行度を更新する実績ID</param>
		/// <param name="progress">進捗率（0.0 〜 100.0、または 0.0 〜 1.0 などプラットフォーム依存。通常はパーセンテージが多い）</param>
		UniTask<bool> SetProgress(string achievementId, float progress);
	}
}
