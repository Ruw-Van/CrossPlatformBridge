#if USE_CROSSPLATFORMBRIDGE_STEAM
#if !DISABLESTEAMWORKS
using UnityEngine;

namespace CrossPlatformBridge.Platform.Steam.ScreenShot
{
	internal static class SteamScreenShotBootstrap
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Register()
		{
			var impl = CrossPlatformBridge.Services.ScreenShot.ScreenShot.Use<CrossPlatformBridge.Platform.Steam.Steam>();
			if (impl is System.IDisposable disposable)
			{
				Application.quitting += () =>
				{
					Debug.Log("[ScreenShot] App Quitting: Disposing Steam callback.");
					disposable.Dispose();
				};
			}
		}
	}
}
#endif

#endif
