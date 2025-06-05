// Assets/Scripts/CrossPlatformBridge/Network/PUN2NetworkHandler/PUN2SettingsFactory.cs
using CrossPlatformBridge.Network; // INetworkSettings を使用するため

namespace CrossPlatformBridge.Network.PUN2NetworkHandler
{
	/// <summary>
	/// Pun2RoomSettings のインスタンスを生成するためのファクトリー。
	/// </summary>
	public class PUN2SettingsFactory : INetworkSettingsFactory
	{
		public INetworkSettings CreateSettings()
		{
			return new PUN2Settings();
		}
	}
}
