using System.Collections.Generic;

namespace CrossPlatformBridge.Services.Payment
{
	/// <summary>
	/// カタログアイテムの情報。
	/// </summary>
	public class CatalogItemInfo
	{
		/// <summary>アイテム ID。</summary>
		public string ItemId;

		/// <summary>表示名。</summary>
		public string DisplayName;

		/// <summary>説明文。</summary>
		public string Description;

		/// <summary>仮想通貨ごとの価格。key: 通貨コード、value: 価格。</summary>
		public Dictionary<string, uint> VirtualCurrencyPrices;
	}

	/// <summary>
	/// インベントリ内のアイテムインスタンス情報。
	/// </summary>
	public class InventoryItemInfo
	{
		/// <summary>インベントリ上の一意なインスタンス ID。</summary>
		public string ItemInstanceId;

		/// <summary>アイテム ID。</summary>
		public string ItemId;

		/// <summary>表示名。</summary>
		public string DisplayName;

		/// <summary>残り使用回数。null の場合は無制限。</summary>
		public int? RemainingUses;
	}
}
