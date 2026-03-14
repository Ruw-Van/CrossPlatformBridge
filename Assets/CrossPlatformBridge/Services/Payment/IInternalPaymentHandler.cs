using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CrossPlatformBridge.Services.Payment
{
	/// <summary>
	/// プラットフォーム固有の決済・仮想通貨処理を抽象化するインターフェース。
	/// </summary>
	public interface IInternalPaymentHandler
	{
		// --------------------------------------------------------------------------------
		// イベント
		// --------------------------------------------------------------------------------

		/// <summary>仮想通貨残高が変化したときに発火。引数は (currencyCode, newBalance)。</summary>
		event Action<string, int> OnCurrencyUpdated;

		/// <summary>購入エラーが発生したときに発火。引数はエラーメッセージ。</summary>
		event Action<string> OnPurchaseError;

		// --------------------------------------------------------------------------------
		// 仮想通貨
		// --------------------------------------------------------------------------------

		/// <summary>保有する仮想通貨の一覧と残高を取得します。</summary>
		UniTask<Dictionary<string, int>> GetVirtualCurrencies();

		// --------------------------------------------------------------------------------
		// カタログ
		// --------------------------------------------------------------------------------

		/// <summary>アイテムカタログを取得します。</summary>
		/// <param name="catalogVersion">カタログバージョン。空文字でデフォルトを使用。</param>
		UniTask<List<CatalogItemInfo>> GetCatalog(string catalogVersion = "");

		// --------------------------------------------------------------------------------
		// 購入
		// --------------------------------------------------------------------------------

		/// <summary>仮想通貨でアイテムを購入します。</summary>
		/// <param name="itemId">購入するアイテムの ID。</param>
		/// <param name="currencyCode">支払いに使用する仮想通貨コード (例: "GD")。</param>
		/// <param name="price">支払い金額。</param>
		/// <param name="catalogVersion">カタログバージョン。空文字でデフォルトを使用。</param>
		UniTask<bool> PurchaseItem(string itemId, string currencyCode, int price, string catalogVersion = "");

		// --------------------------------------------------------------------------------
		// インベントリ
		// --------------------------------------------------------------------------------

		/// <summary>プレイヤーのインベントリ一覧を取得します。</summary>
		UniTask<List<InventoryItemInfo>> GetInventory();

		/// <summary>インベントリ内のアイテムを消費します。</summary>
		/// <param name="itemInstanceId">アイテムインスタンス ID。</param>
		/// <param name="count">消費数。</param>
		UniTask<bool> ConsumeItem(string itemInstanceId, int count = 1);
	}
}
