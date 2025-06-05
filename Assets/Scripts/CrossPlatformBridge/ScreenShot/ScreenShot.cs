using System.Collections;

namespace CrossPlatformBridge.ScreenShot
{
	public static class ScreenShot
	{
		private static IInternalScreenShot _internalScreenShot;
		static ScreenShot()
		{
#if UNITY_SWITCH
			_internalScreenShot = new Switch.ScreenShot();
#elif !DISABLESTEAMWORKS
			_internalScreenShot = new Steam.ScreenShot();
#else
			_internalScreenShot = null
#endif
		}

		public static IEnumerator SaveScreenShot()
			=> _internalScreenShot?.SaveScreenShot() ?? null;
	}
}
