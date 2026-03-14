#if USE_CROSSPLATFORMBRIDGE_EOS
using CrossPlatformBridge.Services.Network;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Network
{
	/// <summary>
	/// EOS用のネットワーク設定・ルーム設定インスタンスを生成するファクトリークラス。
	/// </summary>
	public class EOSSettingsFactory : INetworkSettingsFactory
	{
		public NetworkSettingsScriptableObjectBase CreateNetworkSettings()
		{
			return ScriptableObject.CreateInstance<NetworkSettings>();
		}

		public NetworkSettingsScriptableObjectBase CreateNetworkSettings(NetworkSettingsScriptableObjectBase existingSettings)
		{
			return existingSettings;
		}

		public IRoomSettings CreateRoomSettings()
		{
			return new RoomSettings();
		}
	}
}

#endif
