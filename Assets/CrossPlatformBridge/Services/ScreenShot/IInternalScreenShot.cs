using System;
using System.Collections;

namespace CrossPlatformBridge.Services.ScreenShot
{
	/// <summary>
	/// プラットフォーム固有のスクリーンショット処理を抽象化するためのインターフェース。
	/// </summary>
	public interface IInternalScreenShot
	{
		/// <summary>
		/// スクリーンショットを非同期で保存します。
		/// </summary>
		/// <param name="onCompleted">完了時に呼ばれるコールバック。成功なら true、失敗なら false。</param>
		public abstract IEnumerator SaveScreenShot(Action<bool> onCompleted = null);
	}
}
