using CrossPlatformBridge.Services.Leaderboard;

namespace CrossPlatformBridge.Platform.Dummy.Leaderboard
{
	/// <summary>
	/// Leaderboard サービスの Dummy プラットフォームマーカー。
	/// <see cref="Leaderboard.Use{T}"/> の型引数として使用します。
	/// <code>
	/// var handler = (DummyLeaderboardHandler)Leaderboard.Instance.Use&lt;DummyLeaderboardPlatform&gt;();
	/// </code>
	/// </summary>
	public class DummyLeaderboardPlatform : ILeaderboardPlatform
	{
		/// <inheritdoc/>
		public IInternalLeaderboardHandler CreateLeaderboardHandler()
			=> new DummyLeaderboardHandler();
	}
}
