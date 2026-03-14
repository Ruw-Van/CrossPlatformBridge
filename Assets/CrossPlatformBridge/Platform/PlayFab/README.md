# PlayFab — CrossPlatformBridge

## Overview
Microsoft PlayFab integration using anonymous login, Player Data Storage, and virtual currency/catalog.

**Supported services:** Account, CloudStorage, Payment

## Prerequisites
- CrossPlatformBridge package
- UniTask
- [PlayFab Unity SDK](https://github.com/PlayFab/UnitySDK) (via UPM or Asset Store)

## Installation
1. Import PlayFab Unity SDK.
2. Add Scripting Define Symbol: `USE_CROSSPLATFORMBRIDGE_PLAYFAB`
3. Set your PlayFab Title ID:
   ```csharp
   PlayFab.PlayFabSettings.staticSettings.TitleId = "YOUR_TITLE_ID";
   ```

## Platform Configuration
Set Title ID before calling `InitializeAsync()`.

---

## Services

### Account
Anonymous device-ID login.

#### Handler Registration
```csharp
var account = new CrossPlatformBridge.Platform.PlayFab.Account.PlayFabAccount();
bool ok = await account.InitializeAsync(); // Anonymous login with device ID
string playerId = account.AccountId;
```

#### Test Operations (ServiceTestUI)
- Initialize (anonymous login)
- Shutdown

---

### CloudStorage
PlayFab Player Data (server-side key-value storage).

#### Configuration
Call `PlayFabAccount.InitializeAsync()` before using cloud storage to establish a session.

#### Handler Registration
```csharp
var storage = new CrossPlatformBridge.Platform.PlayFab.CloudStorage.CloudStorageHandler();
await storage.SaveData("playfab_save", "hello");
string value = await storage.LoadData("playfab_save");
```

#### Test Operations (ServiceTestUI)
- Save Data / Load Data / Load All Data / Delete Data

---

### Payment
Virtual currency, catalog, and inventory management for in-app purchases.

#### Configuration
1. In [PlayFab Game Manager](https://developer.playfab.com/), configure:
   - Virtual Currencies (e.g., `GD` for Gold)
   - Item Catalog (catalog version, item IDs)
2. Call `PlayFabAccount.InitializeAsync()` before payment operations.

#### Handler Registration
```csharp
var payment = new CrossPlatformBridge.Platform.PlayFab.Payment.PaymentHandler();
payment.OnCurrencyUpdated += (code, balance) => Debug.Log($"{code}: {balance}");
var currencies = await payment.GetVirtualCurrencies();
bool ok = await payment.PurchaseItem("item_001", "GD", 10);
```

#### Test Operations (ServiceTestUI)
- Get Virtual Currencies / Get Catalog
- Purchase Item / Get Inventory / Consume Item
