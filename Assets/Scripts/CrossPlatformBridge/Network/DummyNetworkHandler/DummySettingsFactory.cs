// Assets/Scripts/CrossPlatformBridge/Network/DummyNetworkHandler/DummySettingsFactory.cs
namespace CrossPlatformBridge.Network.DummyNetworkHandler
{
	/// <summary>
	/// DummyRoomSettings のインスタンスを生成するためのファクトリー。
	/// </summary>
	public class DummySettingsFactory : INetworkSettingsFactory
	{
		public INetworkSettings CreateSettings()
		{
			return new DummySettings();
		}
	}
}
