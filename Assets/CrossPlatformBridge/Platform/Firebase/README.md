[日本語](#日本語) | [English](#english)

---

<a id="日本語"></a>
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
AccountService.Instance.Use<Firebase>();
bool ok = await AccountService.Instance.InitializeAsync(); // 匿名ログイン
string uid = AccountService.Instance.AccountId;
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
AccountService.Instance.Use<Firebase>();
await AccountService.Instance.InitializeAsync();
// その後クラウドストレージを使用
CloudStorage.Instance.Use<Firebase>();
await CloudStorage.Instance.SaveData("firebase_save", "hello");
string value = await CloudStorage.Instance.LoadData("firebase_save");
```

#### テスト操作（ServiceTestUI）
- Save Data / Load Data / Load All Data / Delete Data

---

<a id="english"></a>
# Firebase — CrossPlatformBridge

## Overview
Google Firebase integration using Authentication and Firestore.

**Supported services:** Account, CloudStorage

## Prerequisites
- CrossPlatformBridge package
- UniTask
- [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup) (Firebase.Auth, Firebase.Firestore, Firebase.App DLLs)
- Firebase project configured

## Installation
1. Download Firebase Unity SDK and import `FirebaseAuth.unitypackage` and `FirebaseFirestore.unitypackage`.
2. Add Scripting Define Symbol: `USE_CROSSPLATFORMBRIDGE_FIREBASE`
3. Place `google-services.json` (Android) or `GoogleService-Info.plist` (iOS) in `Assets/StreamingAssets/`.

## Platform Configuration
Firebase dependencies are checked automatically via `FirebaseApp.CheckAndFixDependenciesAsync()` during initialization.

---

## Services

### Account
Firebase Authentication — anonymous sign-in (UID-based account management).

#### Handler Registration
```csharp
AccountService.Instance.Use<Firebase>();
bool ok = await AccountService.Instance.InitializeAsync(); // Anonymous sign-in
string uid = AccountService.Instance.AccountId;
```

#### Test Operations (ServiceTestUI)
- Initialize (anonymous sign-in)
- Shutdown

---

### CloudStorage
Firebase Firestore cloud save (`users/{uid}/data/{key}` document structure).

#### Configuration
Call `FirebaseAccountHandler.InitializeAsync()` before using cloud storage to obtain a valid UID.

#### Handler Registration
```csharp
// Initialize account first
AccountService.Instance.Use<Firebase>();
await AccountService.Instance.InitializeAsync();
// Then use cloud storage
CloudStorage.Instance.Use<Firebase>();
await CloudStorage.Instance.SaveData("firebase_save", "hello");
string value = await CloudStorage.Instance.LoadData("firebase_save");
```

#### Test Operations (ServiceTestUI)
- Save Data / Load Data / Load All Data / Delete Data
