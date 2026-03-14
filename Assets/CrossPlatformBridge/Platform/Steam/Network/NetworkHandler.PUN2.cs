#if USE_CROSSPLATFORMBRIDGE_STEAM && USE_CROSSPLATFORMBRIDGE_PUN2
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Platform.PUN2.Network;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;
using UnityEngine;

namespace CrossPlatformBridge.Platform.Steam.Network
{
	/// <summary>
	/// Steam 向け PUN2 認証の登録。
	/// RuntimeInitializeOnLoadMethod により起動時に PreConnectAuthAsync フックへ登録されます。
	/// </summary>
	internal static class PUN2NetworkHandlerSteamInitializer
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			NetworkHandler.PreConnectAuthAsync = async () =>
			{
				PhotonNetwork.AuthValues = new AuthenticationValues()
				{
					UserId = SteamUser.GetSteamID().ToString(),
					AuthType = CustomAuthenticationType.Steam,
				};
				await UniTask.CompletedTask;
			};
		}
	}
}
#endif
