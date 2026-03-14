#if USE_CROSSPLATFORMBRIDGE_NETCODE
using CrossPlatformBridge.Services.Network;
using UnityEngine;

namespace CrossPlatformBridge.Platform.Netcode.Network
{
	/// <summary>
	/// NetcodeSettings のインスタンスを生成するためのファクトリークラス。
	/// </summary>
	public class NetcodeSettingsFactory : INetworkSettingsFactory
	{
		/// <summary>
		/// 新しいネットワーク設定インスタンスを生成します。
		/// </summary>
		/// <returns>NetworkSettingsScriptableObjectBase</returns>
		public NetworkSettingsScriptableObjectBase CreateNetworkSettings()
		{
			return ScriptableObject.CreateInstance<NetworkSettings>();
		}

		/// <summary>
		/// 既存の設定を元にネットワーク設定インスタンスを生成します。
		/// </summary>
		/// <param name="existingSettings">既存の設定</param>
		/// <returns>NetworkSettingsScriptableObjectBase</returns>
		public NetworkSettingsScriptableObjectBase CreateNetworkSettings(NetworkSettingsScriptableObjectBase existingSettings)
		{
			return existingSettings;
		}

		/// <summary>
		/// 新しいルーム設定インスタンスを生成します。
		/// </summary>
		/// <returns>IRoomSettings</returns>
		public IRoomSettings CreateRoomSettings()
		{
			return new RoomSettings();
		}
	}
}

#endif
