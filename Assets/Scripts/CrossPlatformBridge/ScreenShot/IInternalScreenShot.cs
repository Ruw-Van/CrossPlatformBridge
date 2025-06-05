// Assets/Scripts/CrossPlatformBridge/ScreenShot/IInternalScreenShot.cs
using System.Collections;

namespace CrossPlatformBridge.ScreenShot
{
	/// <summary>
	/// プラットフォーム固有のスクリーンショット処理を抽象化するためのインターフェース。
	/// </summary>
	public interface IInternalScreenShot
	{
		/// <summary>
		/// スクリーンショットを非同期で保存します。
		/// </summary>
		/// <returns>スクリーンショットの保存が成功した場合は true、それ以外は false。</returns>
		public abstract IEnumerator SaveScreenShot();
	}
}
