namespace CrossPlatformBridge.Services.Network
{
	/// <summary>
	/// INetworkSettings および IRoomSettings のインスタンスを生成するためのファクトリーインターフェース。
	/// </summary>
	public interface INetworkSettingsFactory
	{
		/// <summary>
		/// 新しい INetworkSettings のインスタンスを生成します。
		/// </summary>
		/// <returns>生成された INetworkSettings のインスタンス。</returns>
		NetworkSettingsScriptableObjectBase CreateNetworkSettings();

		/// <summary>
		/// 既存の INetworkSettings ScriptableObject を利用して INetworkSettings のインスタンスを生成します。
		/// </summary>
		/// <param name="existingSettings">既存の INetworkSettings を実装した ScriptableObject。</param>
		/// <returns>既存の設定を利用した INetworkSettings のインスタンス。</returns>
		NetworkSettingsScriptableObjectBase CreateNetworkSettings(NetworkSettingsScriptableObjectBase existingSettings);

		/// <summary>
		/// 新しい IRoomSettings のインスタンスを生成します。
		/// </summary>
		/// <returns>生成された IRoomSettings のインスタンス。</returns>
		IRoomSettings CreateRoomSettings();
	}
}
