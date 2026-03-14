#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
#if !DISABLE_PLAYFABCLIENT_API

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using CrossPlatformBridge.Services.Payment;
using CrossPlatformBridge.Testing;

namespace CrossPlatformBridge.Platform.PlayFab.Payment
{
	/// <summary>
	/// PlayFab を使用した決済・仮想通貨・インベントリ管理の実装。
	/// IInternalPaymentHandler を実装し、PlayFab SDK のコールバック API を UniTask でラップします。
	/// </summary>
	public class PaymentHandler : IInternalPaymentHandler, IServiceTestProvider
	{
		// --------------------------------------------------------------------------------
		// イベント
		// --------------------------------------------------------------------------------

		public event Action<string, int> OnCurrencyUpdated;
		public event Action<string>      OnPurchaseError;

		// --------------------------------------------------------------------------------
		// 仮想通貨
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<Dictionary<string, int>> GetVirtualCurrencies()
		{
			var tcs = new UniTaskCompletionSource<Dictionary<string, int>>();

			PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
				result =>
				{
					var currencies = new Dictionary<string, int>();
					if (result.VirtualCurrency != null)
					{
						foreach (var kv in result.VirtualCurrency)
							currencies[kv.Key] = kv.Value;
					}
					tcs.TrySetResult(currencies);
				},
				error =>
				{
					OnPurchaseError?.Invoke(error.GenerateErrorReport());
					tcs.TrySetResult(new Dictionary<string, int>());
				});

			return await tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// カタログ
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<List<CatalogItemInfo>> GetCatalog(string catalogVersion = "")
		{
			var tcs     = new UniTaskCompletionSource<List<CatalogItemInfo>>();
			var request = new GetCatalogItemsRequest
			{
				CatalogVersion = string.IsNullOrEmpty(catalogVersion) ? null : catalogVersion,
			};

			PlayFabClientAPI.GetCatalogItems(request,
				result =>
				{
					var items = new List<CatalogItemInfo>();
					if (result.Catalog != null)
					{
						foreach (var item in result.Catalog)
						{
							items.Add(new CatalogItemInfo
							{
								ItemId                = item.ItemId,
								DisplayName           = item.DisplayName,
								Description           = item.Description,
								VirtualCurrencyPrices = item.VirtualCurrencyPrices != null
									? new Dictionary<string, uint>(item.VirtualCurrencyPrices)
									: new Dictionary<string, uint>(),
							});
						}
					}
					tcs.TrySetResult(items);
				},
				error =>
				{
					OnPurchaseError?.Invoke(error.GenerateErrorReport());
					tcs.TrySetResult(new List<CatalogItemInfo>());
				});

			return await tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// 購入
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<bool> PurchaseItem(string itemId, string currencyCode, int price, string catalogVersion = "")
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var request = new PurchaseItemRequest
			{
				ItemId          = itemId,
				VirtualCurrency = currencyCode,
				Price           = price,
				CatalogVersion  = string.IsNullOrEmpty(catalogVersion) ? null : catalogVersion,
			};

			PlayFabClientAPI.PurchaseItem(request,
				_ =>
				{
					// 購入後は GetVirtualCurrencies() で残高を改めて取得することを推奨
					OnCurrencyUpdated?.Invoke(currencyCode, -1);
					tcs.TrySetResult(true);
				},
				error =>
				{
					OnPurchaseError?.Invoke(error.GenerateErrorReport());
					tcs.TrySetResult(false);
				});

			return await tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// インベントリ
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<List<InventoryItemInfo>> GetInventory()
		{
			var tcs = new UniTaskCompletionSource<List<InventoryItemInfo>>();

			PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
				result =>
				{
					var inventory = new List<InventoryItemInfo>();
					if (result.Inventory != null)
					{
						foreach (var item in result.Inventory)
						{
							inventory.Add(new InventoryItemInfo
							{
								ItemInstanceId = item.ItemInstanceId,
								ItemId         = item.ItemId,
								DisplayName    = item.DisplayName,
								RemainingUses  = item.RemainingUses,
							});
						}
					}
					tcs.TrySetResult(inventory);
				},
				error =>
				{
					OnPurchaseError?.Invoke(error.GenerateErrorReport());
					tcs.TrySetResult(new List<InventoryItemInfo>());
				});

			return await tcs.Task;
		}

		/// <inheritdoc/>
		public async UniTask<bool> ConsumeItem(string itemInstanceId, int count = 1)
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var request = new ConsumeItemRequest
			{
				ItemInstanceId = itemInstanceId,
				ConsumeCount   = count,
			};

			PlayFabClientAPI.ConsumeItem(request,
				_ => tcs.TrySetResult(true),
				error =>
				{
					OnPurchaseError?.Invoke(error.GenerateErrorReport());
					tcs.TrySetResult(false);
				});

			return await tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => new TestOperation[]
		{
			new TestOperation { SectionLabel = "仮想通貨" },
			new TestOperation { Label = "Get Virtual Currencies", Action = async ctx => { var currencies = await GetVirtualCurrencies(); if (currencies == null || currencies.Count == 0) ctx.ReportResult("（通貨なし）"); else { var sb = new System.Text.StringBuilder(); foreach (var kv in currencies) sb.AppendLine($"{kv.Key}: {kv.Value}"); ctx.ReportResult(sb.ToString()); } ctx.AppendLog($"GetVirtualCurrencies → {currencies?.Count ?? 0} 件"); } },
			new TestOperation { SectionLabel = "カタログ" },
			new TestOperation { Label = "Get Catalog", Action = async ctx => { var catalog = await GetCatalog(); if (catalog == null || catalog.Count == 0) ctx.ReportResult("（カタログなし）"); else { var sb = new System.Text.StringBuilder(); foreach (var item in catalog) sb.AppendLine($"[{item.ItemId}] {item.DisplayName}"); ctx.ReportResult(sb.ToString()); } ctx.AppendLog($"GetCatalog → {catalog?.Count ?? 0} 件"); } },
			new TestOperation { SectionLabel = "購入" },
			new TestOperation { Label = "Purchase Item", Action = async ctx => { if (!int.TryParse(ctx.PaymentPrice, out int price)) { ctx.ReportResult("Price が不正です"); return; } bool ok = await PurchaseItem(ctx.PaymentItemId, ctx.PaymentCurrencyCode, price); ctx.ReportResult(ok ? $"購入完了: {ctx.PaymentItemId}" : "購入失敗"); ctx.AppendLog($"PurchaseItem({ctx.PaymentItemId}, {ctx.PaymentCurrencyCode}, {price}) → {ok}"); } },
			new TestOperation { SectionLabel = "インベントリ" },
			new TestOperation { Label = "Get Inventory", Action = async ctx => { var inventory = await GetInventory(); if (inventory == null || inventory.Count == 0) ctx.ReportResult("（インベントリなし）"); else { var sb = new System.Text.StringBuilder(); foreach (var item in inventory) sb.AppendLine($"[{item.ItemInstanceId}] {item.DisplayName}"); ctx.ReportResult(sb.ToString()); } ctx.AppendLog($"GetInventory → {inventory?.Count ?? 0} 件"); } },
			new TestOperation { Label = "Consume Item", Action = async ctx => { if (string.IsNullOrEmpty(ctx.PaymentItemInstanceId)) { ctx.ReportResult("Item Instance ID を入力してください"); return; } bool ok = await ConsumeItem(ctx.PaymentItemInstanceId); ctx.ReportResult(ok ? "消費完了" : "消費失敗"); ctx.AppendLog($"ConsumeItem({ctx.PaymentItemInstanceId}) → {ok}"); } },
		};

		public TestDefaultData GetDefaultData() => new TestDefaultData { PaymentCurrencyCode = "GD", PaymentPrice = "0" };
	}
}

#endif // !DISABLE_PLAYFABCLIENT_API

#endif
