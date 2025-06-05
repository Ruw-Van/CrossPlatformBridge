// Assets/Scripts/CrossPlatformBridge/Network/INetworkRoomSettingsFactory.cs
namespace CrossPlatformBridge.Network
{
	/// <summary>
	/// INetworkRoomSettings のインスタンスを生成するためのファクトリーインターフェース。
	/// </summary>
	public interface INetworkSettingsFactory
	{
		/// <summary>
		/// 新しい INetworkRoomSettings のインスタンスを生成します。
		/// </summary>
		/// <returns>生成された INetworkRoomSettings のインスタンス。</returns>
		INetworkSettings CreateSettings();
	}
}
