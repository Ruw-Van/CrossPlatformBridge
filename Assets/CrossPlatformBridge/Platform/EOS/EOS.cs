#if USE_CROSSPLATFORMBRIDGE_EOS
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Services.Achievement;
using CrossPlatformBridge.Services.CloudStorage;
using CrossPlatformBridge.Services.Leaderboard;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Services.Payment;

namespace CrossPlatformBridge.Platform.EOS
{
	/// <summary>
	/// Epic Online Services プラットフォームのマーカークラス。
	/// AccountService / Achievement / CloudStorage / Leaderboard / Network / Payment の Use&lt;EOS&gt;() で使用します。
	/// </summary>
	public class EOS : IAccountPlatform, IAchievementPlatform, ICloudStoragePlatform, ILeaderboardPlatform, INetworkPlatform, IPaymentPlatform
	{
		public IInternalAccountHandler CreateAccountHandler()
			=> new Account.EOSAccount();

		public IInternalAchievementHandler CreateAchievementHandler()
			=> new Achievement.EOSAchievementHandler();

		public IInternalCloudStorageHandler CreateCloudStorageHandler()
			=> new CloudStorage.CloudStorageHandler();

		public IInternalLeaderboardHandler CreateLeaderboardHandler()
			=> new Leaderboard.EOSLeaderboardHandler();

		public IInternalNetworkHandler CreateNetworkHandler()
			=> new Network.NetworkHandler();

		public IInternalPaymentHandler CreatePaymentHandler()
			=> new Payment.PaymentHandler();
	}
}
#endif
