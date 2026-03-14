namespace CrossPlatformBridge.Services.Leaderboard
{
	/// <summary>
	/// リーダーボードサービスのハンドラーを生成できるプラットフォームを表すインターフェース。
	/// <see cref="Leaderboard.Use{T}"/> の型引数として使用します。
	/// </summary>
	public interface ILeaderboardPlatform
	{
		/// <summary>プラットフォーム固有のリーダーボードハンドラーを生成します。</summary>
		IInternalLeaderboardHandler CreateLeaderboardHandler();
	}
}
