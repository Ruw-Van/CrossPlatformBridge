#if USE_CROSSPLATFORMBRIDGE_FIREBASE
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Services.CloudStorage;

namespace CrossPlatformBridge.Platform.Firebase
{
	/// <summary>
	/// Firebase プラットフォームのマーカークラス。
	/// AccountService / CloudStorage の Use&lt;Firebase&gt;() で使用します。
	/// </summary>
	public class Firebase : IAccountPlatform, ICloudStoragePlatform
	{
		public IInternalAccountHandler CreateAccountHandler()
			=> new Account.FirebaseAccountHandler();

		public IInternalCloudStorageHandler CreateCloudStorageHandler()
			=> new CloudStorage.FirebaseCloudStorageHandler();
	}
}
#endif
