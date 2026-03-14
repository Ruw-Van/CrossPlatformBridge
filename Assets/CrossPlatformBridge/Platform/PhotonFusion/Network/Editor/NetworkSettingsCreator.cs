#if USE_CROSSPLATFORMBRIDGE_PHOTONFUSION
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CrossPlatformBridge.Platform.PhotonFusion.Network.Editor
{
	/// <summary>
	/// PhotonFusion用ネットワーク設定ScriptableObjectをエディタから作成するためのユーティリティ。
	/// </summary>
	public static class NetworkSettingsCreator
	{
		[MenuItem("Assets/Create/CrossPlatformBridge/PhotonFusion/NetworkSettings", false, 1001)]
		public static void CreateNetworkSettingsAsset()
		{
			var asset = ScriptableObject.CreateInstance<NetworkSettings>();
			ProjectWindowUtil.CreateAsset(asset, "PhotonFusionNetworkSettings.asset");
		}
	}
}
#endif

#endif
