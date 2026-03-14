# Firebase — CrossPlatformBridge

## 概要
Google Firebase Authentication と Firestore を使用した統合実装。

**対応サービス:** Account, CloudStorage

## 前提条件
- CrossPlatformBridge パッケージ
- UniTask
- [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup)（Firebase.Auth, Firebase.Firestore, Firebase.App DLL）
- Firebase プロジェクトの作成・設定済み

## インストール
1. Firebase Unity SDK の `FirebaseAuth.unitypackage` と `FirebaseFirestore.unitypackage` をインポート。
2. Scripting Define Symbol を追加: `USE_CROSSPLATFORMBRIDGE_FIREBASE`
3. `google-services.json`（Android）または `GoogleService-Info.plist`（iOS）を `Assets/StreamingAssets/` に配置。

## プラットフォーム設定
初期化時に `FirebaseApp.CheckAndFixDependenciesAsync()` が自動実行されます。

---

## サービス

### Account
Firebase Authentication の匿名ログイン（UID ベースのアカウント管理）。

#### ハンドラー登録
```csharp
var account = new CrossPlatformBridge.Platform.Firebase.Account.FirebaseAccountHandler();
bool ok = await account.InitializeAsync(); // 匿名ログイン
string uid = account.AccountId;
```

#### テスト操作（ServiceTestUI）
- Initialize（匿名ログイン）
- Shutdown

---

### CloudStorage
Firebase Firestore を使用したクラウドセーブ（`users/{uid}/data/{key}` ドキュメント構造）。

#### 設定
クラウドストレージ使用前に `FirebaseAccountHandler.InitializeAsync()` を呼び出して UID を取得してください。

#### ハンドラー登録
```csharp
// まずアカウントを初期化
var account = new FirebaseAccountHandler();
await account.InitializeAsync();
// その後クラウドストレージを使用
var storage = new CrossPlatformBridge.Platform.Firebase.CloudStorage.FirebaseCloudStorageHandler();
await storage.SaveData("firebase_save", "hello");
string value = await storage.LoadData("firebase_save");
```

#### テスト操作（ServiceTestUI）
- Save Data / Load Data / Load All Data / Delete Data
