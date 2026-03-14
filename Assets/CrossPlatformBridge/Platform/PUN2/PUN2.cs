#if USE_CROSSPLATFORMBRIDGE_PUN2
using CrossPlatformBridge.Services.Network;
using UnityEngine;

namespace CrossPlatformBridge.Platform.PUN2
{
	/// <summary>
	/// Photon Unity Networking 2 プラットフォームのマーカークラス。
	/// Network の Use&lt;PUN2&gt;() で使用します。
	/// </summary>
	public class PUN2 : INetworkPlatform
	{
		public IInternalNetworkHandler CreateNetworkHandler()
		{
			// NetworkHandler は MonoBehaviourPunCallbacks を継承しているため
			// new() ではなく AddComponent() で生成する必要がある。
			var go = new GameObject("[CrossPlatformBridge] PUN2NetworkHandler");
			Object.DontDestroyOnLoad(go);
			return go.AddComponent<Network.NetworkHandler>();
		}
	}
}
#endif
