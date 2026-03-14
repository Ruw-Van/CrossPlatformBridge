[日本語](#日本語) | [English](#english)

---

<a id="日本語"></a>
# PlayFab — CrossPlatformBridge

## 概要
Microsoft PlayFab の匿名ログイン・Player Data Storage・仮想通貨/カタログを使用した統合実装。

**対応サービス:** Account, CloudStorage, Payment

## 前提条件
- CrossPlatformBridge パッケージ
- UniTask
- [PlayFab Unity SDK](https://github.com/PlayFab/UnitySDK)（UPM または Asset Store 経由）

## インストール
1. PlayFab Unity SDK をインポート。
2. Scripting Define Symbol を追加: `USE_CROSSPLATFORMBRIDGE_PLAYFAB`
3. PlayFab Title ID を設定:
   ```csharp
   PlayFab.PlayFabSettings.staticSettings.TitleId = "YOUR_TITLE_ID";
   ```

## プラットフォーム設定
`InitializeAsync()` 呼び出し前に Title ID を設定してください。

---

## サービス

### Account
デバイス ID による匿名ログイン。

#### ハンドラー登録
```csharp
AccountService.Instance.Use<PlayFab>();
bool ok = await AccountService.Instance.InitializeAsync(); // デバイス ID 匿名ログイン
string playerId = AccountService.Instance.AccountId;
```

#### テスト操作（ServiceTestUI）
- Initialize（匿名ログイン）
- Shutdown

---

### CloudStorage
PlayFab Player Data（サーバーサイドキーバリューストレージ）を使用したクラウドセーブ。

#### 設定
クラウドストレージ使用前に `PlayFabAccount.InitializeAsync()` を呼び出してセッションを確立してください。

#### ハンドラー登録
```csharp
CloudStorage.Instance.Use<PlayFab>();
await CloudStorage.Instance.SaveData("playfab_save", "hello");
string value = await CloudStorage.Instance.LoadData("playfab_save");
```

#### テスト操作（ServiceTestUI）
- Save Data / Load Data / Load All Data / Delete Data

---

### Payment
仮想通貨・カタログ・インベントリを使ったアプリ内購入管理。

#### 設定
1. [PlayFab Game Manager](https://developer.playfab.com/) で設定:
   - 仮想通貨（例: `GD` = ゴールド）
   - アイテムカタログ（カタログバージョン、アイテム ID）
2. `PlayFabAccount.InitializeAsync()` を呼び出してから Payment 操作を実行。

#### ハンドラー登録
```csharp
Payment.Instance.Use<PlayFab>();
Payment.Instance.OnCurrencyUpdated += (code, balance) => Debug.Log($"{code}: {balance}");
var currencies = await Payment.Instance.GetVirtualCurrencies();
bool ok = await Payment.Instance.PurchaseItem("item_001", "GD", 10);
```

#### テスト操作（ServiceTestUI）
- Get Virtual Currencies / Get Catalog
- Purchase Item / Get Inventory / Consume Item

---

<a id="english"></a>
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
AccountService.Instance.Use<PlayFab>();
bool ok = await AccountService.Instance.InitializeAsync(); // Anonymous login with device ID
string playerId = AccountService.Instance.AccountId;
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
CloudStorage.Instance.Use<PlayFab>();
await CloudStorage.Instance.SaveData("playfab_save", "hello");
string value = await CloudStorage.Instance.LoadData("playfab_save");
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
Payment.Instance.Use<PlayFab>();
Payment.Instance.OnCurrencyUpdated += (code, balance) => Debug.Log($"{code}: {balance}");
var currencies = await Payment.Instance.GetVirtualCurrencies();
bool ok = await Payment.Instance.PurchaseItem("item_001", "GD", 10);
```

#### Test Operations (ServiceTestUI)
- Get Virtual Currencies / Get Catalog
- Purchase Item / Get Inventory / Consume Item
