[日本語](#日本語) | [English](#english)

---

<a id="日本語"></a>
# Steam — CrossPlatformBridge

## 概要
Steamworks.NET を使用したアカウント・実績・スクリーンショット統合実装。

**対応サービス:** Account, Achievement, ScreenShot

## 前提条件
- CrossPlatformBridge パッケージ
- [Steamworks.NET](https://steamworks.github.io/) Unity プラグイン
- `steam_appid.txt` にアプリ ID を設定（プロジェクトルート）
- テスト環境で Steam クライアントが起動済みであること

## インストール
1. Steamworks.NET をプロジェクトにインポート。
2. Scripting Define Symbol を追加: `USE_CROSSPLATFORMBRIDGE_STEAM`

## プラットフォーム設定
各サービスを使用する前に `SteamAPI.Init()` が呼ばれている必要があります。通常はシーンの `SteamManager` コンポーネントで管理します。

---

## サービス

### Account
SteamID と表示名を取得するアカウントサービス。

#### ハンドラー登録
```csharp
AccountService.Instance.Use<Steam>();
bool ok = await AccountService.Instance.InitializeAsync(); // SteamID と表示名を取得
string steamId = AccountService.Instance.AccountId;
```

#### テスト操作（ServiceTestUI）
- Initialize（Steam ID 取得）
- Shutdown

---

### Achievement
Steamworks.NET の `SteamUserStats` API を使用した Steam 実績管理。

#### 設定
[Steamworks パートナーダッシュボード](https://partner.steamgames.com/) で実績を定義し、API 名（例: `STEAM_ACHIEVEMENT_1`）を実績 ID として使用します。

#### ハンドラー登録
```csharp
Achievement.Instance.Use<Steam>();
bool ok = await Achievement.Instance.UnlockAchievement("STEAM_ACHIEVEMENT_1");
```

#### テスト操作（ServiceTestUI）
- Unlock Achievement
- Get Unlocked Achievements
- Set Progress (50%)

---

### ScreenShot
スクリーンショットを撮影し Steam にアップロード。

#### 設定
コンストラクタで `SteamScreenshotReady_t` コールバックが自動登録されます。

#### ハンドラー登録
```csharp
var handler = ScreenShot.Use<Steam>();
yield return handler.SaveScreenShot(success => Debug.Log($"Screenshot: {success}"));
```

#### テスト操作（ServiceTestUI）
- Save Screenshot

---

<a id="english"></a>
# Steam — CrossPlatformBridge

## Overview
Steamworks.NET integration for account, achievements, and screenshot upload.

**Supported services:** Account, Achievement, ScreenShot

## Prerequisites
- CrossPlatformBridge package
- [Steamworks.NET](https://steamworks.github.io/) Unity plugin
- Steam App ID configured (`steam_appid.txt` in project root)
- Steam client running on the test machine

## Installation
1. Import Steamworks.NET into your project.
2. Add Scripting Define Symbol: `USE_CROSSPLATFORMBRIDGE_STEAM`

## Platform Configuration
Ensure `SteamAPI.Init()` has been called before using any service. Typically managed by a `SteamManager` component in the scene.

---

## Services

### Account
Retrieves SteamID and display name.

#### Handler Registration
```csharp
AccountService.Instance.Use<Steam>();
bool ok = await AccountService.Instance.InitializeAsync(); // Reads SteamID and display name
string steamId = AccountService.Instance.AccountId;
```

#### Test Operations (ServiceTestUI)
- Initialize (read Steam identity)
- Shutdown

---

### Achievement
Steam achievements via Steamworks.NET `SteamUserStats` API.

#### Configuration
Define achievements in the [Steamworks Partner Dashboard](https://partner.steamgames.com/). Use the API names (e.g., `STEAM_ACHIEVEMENT_1`) as achievement IDs.

#### Handler Registration
```csharp
Achievement.Instance.Use<Steam>();
bool ok = await Achievement.Instance.UnlockAchievement("STEAM_ACHIEVEMENT_1");
```

#### Test Operations (ServiceTestUI)
- Unlock Achievement
- Get Unlocked Achievements
- Set Progress (50%)

---

### ScreenShot
Captures and uploads screenshots to Steam.

#### Configuration
`SteamScreenshotReady_t` callback is registered automatically in the constructor.

#### Handler Registration
```csharp
var handler = ScreenShot.Use<Steam>();
yield return handler.SaveScreenShot(success => Debug.Log($"Screenshot: {success}"));
```

#### Test Operations (ServiceTestUI)
- Save Screenshot
