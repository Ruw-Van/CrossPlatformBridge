namespace CrossPlatformBridge.Services.ScreenShot
{
	/// <summary>
	/// スクリーンショットサービスの実装を生成できるプラットフォームを表すインターフェース。
	/// <see cref="ScreenShot.Use{T}"/> の型引数として使用します。
	/// </summary>
	public interface IScreenShotPlatform
	{
		/// <summary>プラットフォーム固有のスクリーンショット実装を生成します。</summary>
		IInternalScreenShot CreateScreenShot();
	}
}
