namespace CrossPlatformBridge.Services.Account
{
	/// <summary>
	/// アカウントサービスのハンドラーを生成できるプラットフォームを表すインターフェース。
	/// <see cref="AccountService.Use{T}"/> の型引数として使用します。
	/// </summary>
	public interface IAccountPlatform
	{
		/// <summary>プラットフォーム固有のアカウントハンドラーを生成します。</summary>
		IInternalAccountHandler CreateAccountHandler();
	}
}
