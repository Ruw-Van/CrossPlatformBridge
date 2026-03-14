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
var account = new CrossPlatformBridge.Platform.PlayFab.Account.PlayFabAccount();
bool ok = await account.InitializeAsync(); // デバイス ID 匿名ログイン
string playerId = account.AccountId;
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
var storage = new CrossPlatformBridge.Platform.PlayFab.CloudStorage.CloudStorageHandler();
await storage.SaveData("playfab_save", "hello");
string value = await storage.LoadData("playfab_save");
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
var payment = new CrossPlatformBridge.Platform.PlayFab.Payment.PaymentHandler();
payment.OnCurrencyUpdated += (code, balance) => Debug.Log($"{code}: {balance}");
var currencies = await payment.GetVirtualCurrencies();
bool ok = await payment.PurchaseItem("item_001", "GD", 10);
```

#### テスト操作（ServiceTestUI）
- Get Virtual Currencies / Get Catalog
- Purchase Item / Get Inventory / Consume Item
