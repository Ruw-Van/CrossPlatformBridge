#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
using CrossPlatformBridge.Services.Leaderboard;

namespace CrossPlatformBridge.Platform.PlayFab.Leaderboard
{
	/// <summary>
	/// Leaderboard サービスの PlayFab プラットフォームマーカー。
	/// <see cref="Services.Leaderboard.Leaderboard.Use{T}"/> の型引数として使用します。
	/// <code>
	/// var handler = Leaderboard.Instance.Use&lt;LeaderboardPlatform&gt;();
	/// </code>
	/// </summary>
	public class LeaderboardPlatform : ILeaderboardPlatform
	{
		/// <inheritdoc/>
		public IInternalLeaderboardHandler CreateLeaderboardHandler()
			=> new LeaderboardHandler();
	}
}
#endif
