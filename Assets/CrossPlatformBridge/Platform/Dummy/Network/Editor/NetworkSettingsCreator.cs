#if UNITY_EDITOR
using UnityEditor;
using CrossPlatformBridge.Services.Network.Editor;

namespace CrossPlatformBridge.Platform.Dummy.Network.Editor
{
	/// <summary>
	/// NetworkSettingsScriptableObject を特定のフォルダに生成するための専用エディタスクリプト。
	/// </summary>
	public class NetworkSettingsCreator : NetworkSettingsCreatorBase
	{
		private const string TARGET_NAME = "Dummy";

		private const string MENU_ITEM_PATH = "Assets/Create/CrossPlatformBridgeSettings/Network/" + TARGET_NAME + "NetworkSettings";

		[MenuItem(MENU_ITEM_PATH, false, 0)]
		public static void CreateNetworkSettingsAsset()
			=> CreateNetworkSettingsAsset<NetworkSettings>(TARGET_NAME);
	}
}
#endif
