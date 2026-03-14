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

		/// <summary>
		/// プラットフォーム固有の実装を登録します。
		/// 各プラットフォームアセンブリの RuntimeInitializeOnLoadMethod から呼び出してください。
		/// </summary>
		[System.Obsolete("Use Use<T>() instead for parameterless implementations. " +
		                 "SetImplementation remains available when constructor arguments are required.")]
		public static void SetImplementation(IInternalScreenShot impl)
		{
			_internalScreenShot = impl;
		}

		/// <summary>
		/// 指定したプラットフォームの実装を生成して設定し、返します。
		/// </summary>
		/// <typeparam name="T"><see cref="IScreenShotPlatform"/> を実装し、パラメーターなしコンストラクターを持つプラットフォーム型。</typeparam>
		/// <returns>生成された実装。</returns>
		public static IInternalScreenShot Use<T>() where T : IScreenShotPlatform, new()
		{
			var impl = new T().CreateScreenShot();
#pragma warning disable CS0618
			SetImplementation(impl);
#pragma warning restore CS0618
			return impl;
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
