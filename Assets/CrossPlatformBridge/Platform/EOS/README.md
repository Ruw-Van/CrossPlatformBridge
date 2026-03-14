[日本語](#日本語) | [English](#english)

---

<a id="日本語"></a>
# EOS — CrossPlatformBridge

## 概要
Epic Online Services (EOS) の Lobby・Relay・Player Data Storage・Achievements を使用した統合実装。

**対応サービス:** Account, Achievement, CloudStorage, Network

## 前提条件
- CrossPlatformBridge パッケージ
- UniTask
- [EOS Unity Plugin (PlayEveryWare)](https://github.com/PlayEveryWare/eos_plugin_for_unity)
- EOS 認証情報: Product ID, Sandbox ID, Deployment ID, Client ID / Client Secret

## インストール
1. UPM で CrossPlatformBridge をインポート。
2. EOS Unity Plugin をプロジェクトにインポート。
3. Scripting Define Symbol を追加: `USE_CROSSPLATFORMBRIDGE_EOS`

## プラットフォーム設定
1. Unity メニューから **EOS Plugin → EOS Configuration** を開く。
2. EOS 認証情報を入力:
   - Product ID / Sandbox ID / Deployment ID / Client ID / Client Secret
3. シーンの GameObject に `EOSManager` コンポーネントを追加（または EOSAccount が自動生成）。

---

## サービス

### Account
AccountPortal（ブラウザ OAuth）によるアカウント認証。

#### ハンドラー登録
```csharp
AccountService.Instance.Use<EOS>();
bool ok = await AccountService.Instance.InitializeAsync(); // ブラウザで AccountPortal ログイン
string userId = AccountService.Instance.AccountId;
```

#### テスト操作（ServiceTestUI）
- Initialize（AccountPortal ブラウザログイン）
- Shutdown

---

### Achievement
EOS Achievements の実装。Stats API 経由で進行度を更新し、目標値到達で解除。

#### 設定
1. [EOS Developer Portal](https://dev.epicgames.com/) に実績を定義。
2. `EOSAccount.InitializeAsync()` を呼び出してから実績機能を使用。

#### ハンドラー登録
```csharp
Achievement.Instance.Use<EOS>();
bool ok = await Achievement.Instance.UnlockAchievement("eos_achievement_001");
```

#### テスト操作（ServiceTestUI）
- Unlock Achievement
- Get Unlocked Achievements
- Set Progress (50%)（注：EOS のネイティブ進行度には Stats API が必要）

---

### CloudStorage
EOS Player Data Storage を使用したクラウドセーブ（キー = ファイル名、値 = UTF-8 ファイル内容）。

#### 設定
クラウドストレージ使用前に `EOSAccount.InitializeAsync()` で `ProductUserId` を取得してください。

#### ハンドラー登録
```csharp
// まずアカウントを初期化
AccountService.Instance.Use<EOS>();
await AccountService.Instance.InitializeAsync();
// その後クラウドストレージを使用
CloudStorage.Instance.Use<EOS>();
await CloudStorage.Instance.SaveData("myKey", "myValue");
```

#### テスト操作（ServiceTestUI）
- Save Data / Load Data / Load All Data / Delete Data

---

### Network
EOS Lobby・Relay サービスを使用したネットワーク実装。

#### ハンドラー登録
```csharp
await Network.Instance.Use<EOS>();
await Network.Instance.ConnectNetwork(userId, userName);
```

#### テスト操作（ServiceTestUI）
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby
- Create / Join / Leave Room
- Search Lobbies / Search Rooms
- Send Data

---

<a id="english"></a>
# EOS — CrossPlatformBridge

## Overview
Epic Online Services (EOS) integration using Lobby, Relay, Player Data Storage, and Achievements.

**Supported services:** Account, Achievement, CloudStorage, Network

## Prerequisites
- CrossPlatformBridge package
- UniTask
- [EOS Unity Plugin (PlayEveryWare)](https://github.com/PlayEveryWare/eos_plugin_for_unity)
- EOS credentials: Product ID, Sandbox ID, Deployment ID, Client ID / Client Secret

## Installation
1. Import CrossPlatformBridge via UPM.
2. Import the EOS Unity Plugin into your project.
3. Add Scripting Define Symbol: `USE_CROSSPLATFORMBRIDGE_EOS`

## Platform Configuration
1. In Unity, open **EOS Plugin → EOS Configuration**.
2. Fill in your EOS credentials:
   - Product ID / Sandbox ID / Deployment ID / Client ID / Client Secret
3. Add `EOSManager` component to a scene GameObject (or let `EOSAccount` auto-create it).

---

## Services

### Account
EOS AccountPortal (browser-based OAuth) authentication.

#### Handler Registration
```csharp
AccountService.Instance.Use<EOS>();
bool ok = await AccountService.Instance.InitializeAsync(); // Opens browser for AccountPortal login
string userId = AccountService.Instance.AccountId;
```

#### Test Operations (ServiceTestUI)
- Initialize (AccountPortal browser login)
- Shutdown

---

### Achievement
EOS Achievements via Stats API. Progress updates when stats reach target values.

#### Configuration
1. Define achievements in the [EOS Developer Portal](https://dev.epicgames.com/).
2. Call `EOSAccount.InitializeAsync()` before using achievements.

#### Handler Registration
```csharp
Achievement.Instance.Use<EOS>();
bool ok = await Achievement.Instance.UnlockAchievement("eos_achievement_001");
```

#### Test Operations (ServiceTestUI)
- Unlock Achievement
- Get Unlocked Achievements
- Set Progress (50%) — Note: EOS requires Stats API for native progress

---

### CloudStorage
EOS Player Data Storage (key = filename, value = UTF-8 file content).

#### Configuration
Call `EOSAccount.InitializeAsync()` before using cloud storage to obtain a valid `ProductUserId`.

#### Handler Registration
```csharp
// Initialize account first
AccountService.Instance.Use<EOS>();
await AccountService.Instance.InitializeAsync();
// Then use cloud storage
CloudStorage.Instance.Use<EOS>();
await CloudStorage.Instance.SaveData("myKey", "myValue");
```

#### Test Operations (ServiceTestUI)
- Save Data / Load Data / Load All Data / Delete Data

---

### Network
EOS Lobby and Relay services.

#### Handler Registration
```csharp
await Network.Instance.Use<EOS>();
await Network.Instance.ConnectNetwork(userId, userName);
```

#### Test Operations (ServiceTestUI)
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby
- Create / Join / Leave Room
- Search Lobbies / Search Rooms
- Send Data
