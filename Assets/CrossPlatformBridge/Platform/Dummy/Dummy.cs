using CrossPlatformBridge.Services.Achievement;
using CrossPlatformBridge.Services.Network;

namespace CrossPlatformBridge.Platform.Dummy
{
	/// <summary>
	/// Dummy プラットフォームのマーカークラス（開発・テスト用）。
	/// Achievement / Network の Use&lt;Dummy&gt;() で使用します。
	/// </summary>
	public class Dummy : IAchievementPlatform, INetworkPlatform
	{
		public IInternalAchievementHandler CreateAchievementHandler()
			=> new Achievement.DummyAchievementHandler();

		public IInternalNetworkHandler CreateNetworkHandler()
			=> new Network.NetworkHandler();
	}
}
