namespace CrossPlatformBridge.Services.CloudStorage
{
	/// <summary>
	/// クラウドストレージサービスのハンドラーを生成できるプラットフォームを表すインターフェース。
	/// <see cref="CloudStorage.Use{T}"/> の型引数として使用します。
	/// </summary>
	public interface ICloudStoragePlatform
	{
		/// <summary>プラットフォーム固有のクラウドストレージハンドラーを生成します。</summary>
		IInternalCloudStorageHandler CreateCloudStorageHandler();
	}
}
