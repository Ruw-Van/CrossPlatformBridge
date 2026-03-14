#if USE_CROSSPLATFORMBRIDGE_STEAM
#if !DISABLESTEAMWORKS
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Services.Achievement;
using CrossPlatformBridge.Services.ScreenShot;

namespace CrossPlatformBridge.Platform.Steam
{
	/// <summary>
	/// Steam プラットフォームのマーカークラス。
	/// AccountService / Achievement / ScreenShot の Use&lt;Steam&gt;() で使用します。
	/// </summary>
	public class Steam : IAccountPlatform, IAchievementPlatform, IScreenShotPlatform
	{
		public IInternalAccountHandler CreateAccountHandler()
			=> new Account.SteamAccountService();

		public IInternalAchievementHandler CreateAchievementHandler()
			=> new Achievement.SteamAchievementHandler();

		public IInternalScreenShot CreateScreenShot()
			=> new ScreenShot.ScreenShot();
	}
}
#endif
#endif
