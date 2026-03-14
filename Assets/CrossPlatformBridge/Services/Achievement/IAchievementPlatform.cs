namespace CrossPlatformBridge.Services.Achievement
{
	/// <summary>
	/// 実績サービスのハンドラーを生成できるプラットフォームを表すインターフェース。
	/// <see cref="Achievement.Use{T}"/> の型引数として使用します。
	/// </summary>
	public interface IAchievementPlatform
	{
		/// <summary>プラットフォーム固有の実績ハンドラーを生成します。</summary>
		IInternalAchievementHandler CreateAchievementHandler();
	}
}
