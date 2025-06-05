// Assets/Scripts/CrossPlatformBridge/Network/PUN2NetworkHandler/NetcodeSettingsFactory.cs

namespace CrossPlatformBridge.Network.NetcodeNetworkHandler
{
	/// <summary>
	/// NetcodeSettings のインスタンスを生成するためのファクトリー。
	/// </summary>
	public class NetcodeSettingsFactory : INetworkSettingsFactory
	{
		public INetworkSettings CreateSettings()
		{
			return new NetcodeSettings();
		}
	}
}
