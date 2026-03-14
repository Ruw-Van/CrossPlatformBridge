#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Services.CloudStorage;
using CrossPlatformBridge.Services.Leaderboard;
using CrossPlatformBridge.Services.Payment;

namespace CrossPlatformBridge.Platform.PlayFab
{
	/// <summary>
	/// Azure PlayFab プラットフォームのマーカークラス。
	/// AccountService / CloudStorage / Payment / Leaderboard の Use&lt;PlayFab&gt;() で使用します。
	/// </summary>
	public class PlayFab : IAccountPlatform, ICloudStoragePlatform, IPaymentPlatform, ILeaderboardPlatform
	{
		public IInternalAccountHandler CreateAccountHandler()
			=> new Account.PlayFabAccount();

		public IInternalCloudStorageHandler CreateCloudStorageHandler()
			=> new CloudStorage.CloudStorageHandler();

		public IInternalPaymentHandler CreatePaymentHandler()
			=> new Payment.PaymentHandler();

		public IInternalLeaderboardHandler CreateLeaderboardHandler()
			=> new Leaderboard.LeaderboardHandler();
	}
}
#endif
