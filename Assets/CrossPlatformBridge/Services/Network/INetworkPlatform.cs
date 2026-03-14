namespace CrossPlatformBridge.Services.Network
{
	/// <summary>
	/// ネットワークサービスのハンドラーを生成できるプラットフォームを表すインターフェース。
	/// <see cref="Network.Use{T}"/> の型引数として使用します。
	/// </summary>
	public interface INetworkPlatform
	{
		/// <summary>プラットフォーム固有のネットワークハンドラーを生成します。</summary>
		IInternalNetworkHandler CreateNetworkHandler();
	}
}
