#if USE_CROSSPLATFORMBRIDGE_EOS
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Network.Editor
{
	/// <summary>
	/// EOS用ネットワーク設定ScriptableObjectをエディタから作成するためのユーティリティ。
	/// </summary>
	public static class NetworkSettingsCreator
	{
		[MenuItem("Assets/Create/CrossPlatformBridge/EOS/NetworkSettings", false, 1001)]
		public static void CreateNetworkSettingsAsset()
		{
			var asset = ScriptableObject.CreateInstance<NetworkSettings>();
			ProjectWindowUtil.CreateAsset(asset, "EOSNetworkSettings.asset");
		}
	}
}
#endif

#endif
