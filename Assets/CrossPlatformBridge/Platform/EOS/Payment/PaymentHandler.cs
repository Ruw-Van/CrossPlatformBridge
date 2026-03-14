#if USE_CROSSPLATFORMBRIDGE_EOS
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Ecom;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;
using CrossPlatformBridge.Services.Payment;
using CrossPlatformBridge.Testing;

namespace CrossPlatformBridge.Platform.EOS.Payment
{
	/// <summary>
	/// EOS Ecom インターフェースを使用した決済・インベントリ管理の実装。
	/// EOS は実貨幣決済のみサポートするため、仮想通貨は常に空を返します。
	/// 使用前に EOSManager が初期化済みで EpicAccountId が取得済みであることが必要です。
	/// </summary>
	public class PaymentHandler : IInternalPaymentHandler, IServiceTestProvider
	{
		private EcomInterface Ecom =>
			EOSManager.Instance.GetEOSPlatformInterface().GetEcomInterface();

		/// <summary>
		/// Auth インターフェースからログイン中の EpicAccountId を取得します。
		/// </summary>
		private EpicAccountId LocalEpicAccountId
		{
			get
			{
				var auth = EOSManager.Instance.GetEOSPlatformInterface()?.GetAuthInterface();
				if (auth == null || auth.GetLoggedInAccountsCount() == 0) return null;
				return auth.GetLoggedInAccountByIndex(0);
			}
		}

		// --------------------------------------------------------------------------------
		// イベント
		// --------------------------------------------------------------------------------

		public event Action<string, int> OnCurrencyUpdated;
		public event Action<string>      OnPurchaseError;

		// --------------------------------------------------------------------------------
		// 仮想通貨（EOS は仮想通貨非対応）
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public UniTask<Dictionary<string, int>> GetVirtualCurrencies()
		{
			// EOS は実貨幣決済のみのため仮想通貨は存在しない
			return UniTask.FromResult(new Dictionary<string, int>());
		}

		// --------------------------------------------------------------------------------
		// カタログ
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<List<CatalogItemInfo>> GetCatalog(string catalogVersion = "")
		{
			var epicAccountId = LocalEpicAccountId;
			if (epicAccountId == null)
			{
				Debug.LogError("[EOS Payment] GetCatalog: EpicAccountId が取得できません。Auth ログインを先に行ってください。");
				return new List<CatalogItemInfo>();
			}

			// Step 1: オファー一覧をクエリ
			var queryTcs     = new UniTaskCompletionSource<Result>();
			var queryOptions = new QueryOffersOptions
			{
				LocalUserId                = epicAccountId,
				OverrideCatalogNamespace   = string.IsNullOrEmpty(catalogVersion) ? null : catalogVersion,
			};

			Ecom.QueryOffers(ref queryOptions, null, (ref QueryOffersCallbackInfo info) =>
			{
				queryTcs.TrySetResult(info.ResultCode);
			});

			var queryResult = await queryTcs.Task;
			if (queryResult != Result.Success)
			{
				Debug.LogError($"[EOS Payment] QueryOffers 失敗: result={queryResult}");
				OnPurchaseError?.Invoke($"QueryOffers 失敗: {queryResult}");
				return new List<CatalogItemInfo>();
			}

			// Step 2: オファー数を取得し、各オファーをコピー
			var countOptions = new GetOfferCountOptions { LocalUserId = epicAccountId };
			var count        = Ecom.GetOfferCount(ref countOptions);
			var items        = new List<CatalogItemInfo>();

			for (var i = 0u; i < count; i++)
			{
				var copyOptions = new CopyOfferByIndexOptions
				{
					LocalUserId = epicAccountId,
					OfferIndex  = i,
				};

				if (Ecom.CopyOfferByIndex(ref copyOptions, out var offer) == Result.Success && offer.HasValue)
				{
					items.Add(new CatalogItemInfo
					{
						ItemId                = offer.Value.Id,
						DisplayName           = offer.Value.TitleText,
						Description           = offer.Value.DescriptionText,
						VirtualCurrencyPrices = new Dictionary<string, uint>(), // EOS は仮想通貨なし
					});
				}
			}

			return items;
		}

		// --------------------------------------------------------------------------------
		// 購入
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		/// <remarks>
		/// EOS Checkout は実貨幣のみです。<paramref name="currencyCode"/> と <paramref name="price"/>
		/// は互換性のため引数として受け取りますが、EOS 側では使用されません。
		/// </remarks>
		public async UniTask<bool> PurchaseItem(string itemId, string currencyCode, int price, string catalogVersion = "")
		{
			var epicAccountId = LocalEpicAccountId;
			if (epicAccountId == null)
			{
				Debug.LogError("[EOS Payment] PurchaseItem: EpicAccountId が取得できません。Auth ログインを先に行ってください。");
				return false;
			}

			var tcs     = new UniTaskCompletionSource<bool>();
			var entries = new CheckoutEntry[] { new CheckoutEntry { OfferId = itemId } };
			var options = new CheckoutOptions
			{
				LocalUserId              = epicAccountId,
				Entries                  = entries,
				OverrideCatalogNamespace = string.IsNullOrEmpty(catalogVersion) ? null : catalogVersion,
			};

			Ecom.Checkout(ref options, null, (ref CheckoutCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
				{
					Debug.Log($"[EOS Payment] Checkout 完了: transactionId={info.TransactionId}");
					tcs.TrySetResult(true);
				}
				else
				{
					Debug.LogError($"[EOS Payment] Checkout 失敗: itemId={itemId} result={info.ResultCode}");
					OnPurchaseError?.Invoke($"Checkout 失敗: {info.ResultCode}");
					tcs.TrySetResult(false);
				}
			});

			return await tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// インベントリ（エンタイトルメント）
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<List<InventoryItemInfo>> GetInventory()
		{
			var epicAccountId = LocalEpicAccountId;
			if (epicAccountId == null)
			{
				Debug.LogError("[EOS Payment] GetInventory: EpicAccountId が取得できません。Auth ログインを先に行ってください。");
				return new List<InventoryItemInfo>();
			}

			// Step 1: エンタイトルメント一覧をクエリ
			var queryTcs     = new UniTaskCompletionSource<Result>();
			var queryOptions = new QueryEntitlementsOptions
			{
				LocalUserId      = epicAccountId,
				EntitlementNames = Array.Empty<Utf8String>(),
				IncludeRedeemed  = false,
			};

			Ecom.QueryEntitlements(ref queryOptions, null, (ref QueryEntitlementsCallbackInfo info) =>
			{
				queryTcs.TrySetResult(info.ResultCode);
			});

			var queryResult = await queryTcs.Task;
			if (queryResult != Result.Success)
			{
				Debug.LogError($"[EOS Payment] QueryEntitlements 失敗: result={queryResult}");
				OnPurchaseError?.Invoke($"QueryEntitlements 失敗: {queryResult}");
				return new List<InventoryItemInfo>();
			}

			// Step 2: エンタイトルメント数を取得し、各エントリをコピー
			var countOptions = new GetEntitlementsCountOptions { LocalUserId = epicAccountId };
			var count        = Ecom.GetEntitlementsCount(ref countOptions);
			var inventory    = new List<InventoryItemInfo>();

			for (var i = 0u; i < count; i++)
			{
				var copyOptions = new CopyEntitlementByIndexOptions
				{
					LocalUserId      = epicAccountId,
					EntitlementIndex = i,
				};

				if (Ecom.CopyEntitlementByIndex(ref copyOptions, out var entitlement) == Result.Success && entitlement.HasValue)
				{
					inventory.Add(new InventoryItemInfo
					{
						ItemInstanceId = entitlement.Value.EntitlementId,
						ItemId         = entitlement.Value.CatalogItemId,
						DisplayName    = entitlement.Value.EntitlementName,
						RemainingUses  = null, // EOS エンタイトルメントには残り使用回数の概念がない
					});
				}
			}

			return inventory;
		}

		/// <inheritdoc/>
		public async UniTask<bool> ConsumeItem(string itemInstanceId, int count = 1)
		{
			var epicAccountId = LocalEpicAccountId;
			if (epicAccountId == null)
			{
				Debug.LogError("[EOS Payment] ConsumeItem: EpicAccountId が取得できません。Auth ログインを先に行ってください。");
				return false;
			}

			var tcs     = new UniTaskCompletionSource<bool>();
			var options = new RedeemEntitlementsOptions
			{
				LocalUserId    = epicAccountId,
				EntitlementIds = new Utf8String[] { itemInstanceId },
			};

			Ecom.RedeemEntitlements(ref options, null, (ref RedeemEntitlementsCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
				{
					Debug.Log($"[EOS Payment] Redeem 完了: entitlementId={itemInstanceId}");
					tcs.TrySetResult(true);
				}
				else
				{
					Debug.LogError($"[EOS Payment] Redeem 失敗: entitlementId={itemInstanceId} result={info.ResultCode}");
					OnPurchaseError?.Invoke($"Redeem 失敗: {info.ResultCode}");
					tcs.TrySetResult(false);
				}
			});

			return await tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => new TestOperation[]
		{
			new TestOperation { SectionLabel = "仮想通貨" },
			new TestOperation { Label = "Get Virtual Currencies", Action = async ctx => { var currencies = await GetVirtualCurrencies(); ctx.ReportResult("（EOS は仮想通貨非対応）"); ctx.AppendLog($"GetVirtualCurrencies → {currencies?.Count ?? 0} 件"); } },
			new TestOperation { SectionLabel = "カタログ" },
			new TestOperation { Label = "Get Catalog", Action = async ctx => { var catalog = await GetCatalog(); if (catalog == null || catalog.Count == 0) ctx.ReportResult("（カタログなし）"); else { var sb = new System.Text.StringBuilder(); foreach (var item in catalog) sb.AppendLine($"[{item.ItemId}] {item.DisplayName}"); ctx.ReportResult(sb.ToString()); } ctx.AppendLog($"GetCatalog → {catalog?.Count ?? 0} 件"); } },
			new TestOperation { SectionLabel = "購入" },
			new TestOperation { Label = "Purchase Item (Checkout)", Action = async ctx => { bool ok = await PurchaseItem(ctx.PaymentItemId, "", 0); ctx.ReportResult(ok ? $"購入完了: {ctx.PaymentItemId}" : "購入失敗"); ctx.AppendLog($"PurchaseItem({ctx.PaymentItemId}) → {ok}"); } },
			new TestOperation { SectionLabel = "インベントリ" },
			new TestOperation { Label = "Get Inventory", Action = async ctx => { var inventory = await GetInventory(); if (inventory == null || inventory.Count == 0) ctx.ReportResult("（エンタイトルメントなし）"); else { var sb = new System.Text.StringBuilder(); foreach (var item in inventory) sb.AppendLine($"[{item.ItemInstanceId}] {item.DisplayName}"); ctx.ReportResult(sb.ToString()); } ctx.AppendLog($"GetInventory → {inventory?.Count ?? 0} 件"); } },
			new TestOperation { Label = "Consume Item (Redeem)", Action = async ctx => { if (string.IsNullOrEmpty(ctx.PaymentItemInstanceId)) { ctx.ReportResult("Item Instance ID を入力してください"); return; } bool ok = await ConsumeItem(ctx.PaymentItemInstanceId); ctx.ReportResult(ok ? "Redeem 完了" : "Redeem 失敗"); ctx.AppendLog($"ConsumeItem({ctx.PaymentItemInstanceId}) → {ok}"); } },
		};

		public TestDefaultData GetDefaultData() => new TestDefaultData();
	}
}

#endif
