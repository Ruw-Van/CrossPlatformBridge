namespace CrossPlatformBridge.Services.Payment
{
	/// <summary>
	/// 決済サービスのハンドラーを生成できるプラットフォームを表すインターフェース。
	/// <see cref="Payment.Use{T}"/> の型引数として使用します。
	/// </summary>
	public interface IPaymentPlatform
	{
		/// <summary>プラットフォーム固有の決済ハンドラーを生成します。</summary>
		IInternalPaymentHandler CreatePaymentHandler();
	}
}
