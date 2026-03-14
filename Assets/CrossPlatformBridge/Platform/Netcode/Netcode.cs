#if USE_CROSSPLATFORMBRIDGE_NETCODE
using CrossPlatformBridge.Services.Network;

namespace CrossPlatformBridge.Platform.Netcode
{
	/// <summary>
	/// Unity Netcode for GameObjects プラットフォームのマーカークラス。
	/// Network の Use&lt;Netcode&gt;() で使用します。
	/// </summary>
	public class Netcode : INetworkPlatform
	{
		public IInternalNetworkHandler CreateNetworkHandler()
			=> new Network.NetworkHandler();
	}
}
#endif
