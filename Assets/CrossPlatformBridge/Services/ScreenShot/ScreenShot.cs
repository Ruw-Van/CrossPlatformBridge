using System;
using System.Collections;
using UnityEngine;

namespace CrossPlatformBridge.Services.ScreenShot
{
	/// <summary>
	/// プラットフォームごとの内部実装に委譲してスクリーンショット保存を提供する静的クラス。
	/// </summary>
	public static class ScreenShot
	{
		private static IInternalScreenShot _internalScreenShot;

#if UNITY_SWITCH
		static ScreenShot()
		{
			_internalScreenShot = new CrossPlatformBridge.Platform.Switch.ScreenShot.ScreenShot();
		}
#endif

		/// <summary>
		/// プラットフォーム固有の実装を登録します。
		/// 各プラットフォームアセンブリの RuntimeInitializeOnLoadMethod から呼び出してください。
		/// </summary>
		public static void SetImplementation(IInternalScreenShot impl)
		{
			_internalScreenShot = impl;
		}

		/// <summary>
		/// スクリーンショット保存処理を呼び出します。
		/// </summary>
		/// <param name="onCompleted">完了時に呼ばれるコールバック。成功なら true、失敗なら false。</param>
		/// <returns>IEnumerator</returns>
		public static IEnumerator SaveScreenShot(Action<bool> onCompleted = null)
			=> _internalScreenShot?.SaveScreenShot(onCompleted) ?? null;
	}
}
