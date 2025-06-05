#if UNITY_SWITCH
using System.Collections;
using UnityEngine;
#if !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace CrossPlatformBridge.ScreenShot.Switch
{
	public class ScreenShot : IInternalScreenShot
	{
#if !UNITY_EDITOR
		[DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
		private static extern void Switch_SaveScreenShot();
#else
		private static void Switch_SaveScreenShot()
		{
			Debug.Log("Switch_SaveScreenShot is not implemented.");
		}
#endif

		public IEnumerator SaveScreenShot()
		{
			try
			{
				yield return new WaitForSeconds(0.3f);
				Switch_SaveScreenShot();
				yield return new WaitForSeconds(0.3f);
				yield break;
			}
			finally
			{
				Debug.Log($"Switch.SaveScreenShot");
			}
		}
	}
}
#endif