#if USE_CROSSPLATFORMBRIDGE_PHOTONFUSION
using CrossPlatformBridge.Services.Network;

namespace CrossPlatformBridge.Platform.PhotonFusion
{
	/// <summary>
	/// Photon Fusion プラットフォームのマーカークラス。
	/// Network の Use&lt;Fusion&gt;() で使用します。
	/// </summary>
	public class Fusion : INetworkPlatform
	{
		public IInternalNetworkHandler CreateNetworkHandler()
			=> new Network.NetworkHandler();
	}
}
#endif
