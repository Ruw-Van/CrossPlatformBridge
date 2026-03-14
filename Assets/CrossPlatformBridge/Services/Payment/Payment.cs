using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Services.Payment
{
	/// <summary>
	/// 決済・仮想通貨・インベントリ機能の公開ファサード。
	/// IInternalPaymentHandler の実装を差し替えることで、バックエンドを切り替えられます。
	/// 使用前に InitializeHandler() で実装を注入してください。
	/// </summary>
	public class Payment : MonoBehaviour
	{
		private static Payment _instance;
		public static Payment Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindFirstObjectByType<Payment>();
					if (_instance == null)
					{
						var go = new GameObject(typeof(Payment).Name);
						_instance = go.AddComponent<Payment>();
					}
					DontDestroyOnLoad(_instance.gameObject);
				}
				return _instance;
			}
		}

		// --------------------------------------------------------------------------------
		// イベント
		// --------------------------------------------------------------------------------

		/// <summary>仮想通貨残高が変化したときに発火。引数は (currencyCode, newBalance)。</summary>
		public event Action<string, int> OnCurrencyUpdated;

		/// <summary>購入エラーが発生したときに発火。引数はエラーメッセージ。</summary>
		public event Action<string> OnPurchaseError;

		// --------------------------------------------------------------------------------
		// プロパティ
		// --------------------------------------------------------------------------------

		/// <summary>ハンドラが初期化済みかどうか。</summary>
		public bool IsInitialized => _handler != null;

		// --------------------------------------------------------------------------------
		// フィールド
		// --------------------------------------------------------------------------------

		private IInternalPaymentHandler _handler;

		// --------------------------------------------------------------------------------
		// Unity ライフサイクル
		// --------------------------------------------------------------------------------

		private void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else if (_instance != this)
			{
				Destroy(gameObject);
			}
		}

		// --------------------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 使用する決済実装を注入します。
		/// </summary>
		/// <param name="handler">決済ハンドラーの実装。</param>
		public void InitializeHandler(IInternalPaymentHandler handler)
		{
			if (_handler != null)
			{
				_handler.OnCurrencyUpdated -= RaiseCurrencyUpdated;
				_handler.OnPurchaseError   -= RaisePurchaseError;
			}

			_handler = handler;

			if (_handler != null)
			{
				_handler.OnCurrencyUpdated += RaiseCurrencyUpdated;
				_handler.OnPurchaseError   += RaisePurchaseError;
			}
		}

		// --------------------------------------------------------------------------------
		// 仮想通貨
		// --------------------------------------------------------------------------------

		/// <summary>保有する仮想通貨の一覧と残高を取得します。</summary>
		public async UniTask<Dictionary<string, int>> GetVirtualCurrencies()
		{
			AssertInitialized();
			return await _handler.GetVirtualCurrencies();
		}

		// --------------------------------------------------------------------------------
		// カタログ
		// --------------------------------------------------------------------------------

		/// <summary>アイテムカタログを取得します。</summary>
		/// <param name="catalogVersion">カタログバージョン。空文字でデフォルトを使用。</param>
		public async UniTask<List<CatalogItemInfo>> GetCatalog(string catalogVersion = "")
		{
			AssertInitialized();
			return await _handler.GetCatalog(catalogVersion);
		}

		// --------------------------------------------------------------------------------
		// 購入
		// --------------------------------------------------------------------------------

		/// <summary>仮想通貨でアイテムを購入します。</summary>
		public async UniTask<bool> PurchaseItem(string itemId, string currencyCode, int price, string catalogVersion = "")
		{
			AssertInitialized();
			return await _handler.PurchaseItem(itemId, currencyCode, price, catalogVersion);
		}

		// --------------------------------------------------------------------------------
		// インベントリ
		// --------------------------------------------------------------------------------

		/// <summary>プレイヤーのインベントリ一覧を取得します。</summary>
		public async UniTask<List<InventoryItemInfo>> GetInventory()
		{
			AssertInitialized();
			return await _handler.GetInventory();
		}

		/// <summary>インベントリ内のアイテムを消費します。</summary>
		public async UniTask<bool> ConsumeItem(string itemInstanceId, int count = 1)
		{
			AssertInitialized();
			return await _handler.ConsumeItem(itemInstanceId, count);
		}

		// --------------------------------------------------------------------------------
		// プライベート
		// --------------------------------------------------------------------------------

		private void RaiseCurrencyUpdated(string code, int balance) => OnCurrencyUpdated?.Invoke(code, balance);
		private void RaisePurchaseError(string error) => OnPurchaseError?.Invoke(error);

		private void AssertInitialized()
		{
			if (_handler == null)
				throw new InvalidOperationException("[Payment] InitializeHandler() を先に呼び出してください。");
		}
	}
}
