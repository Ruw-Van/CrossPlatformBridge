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
var account = new CrossPlatformBridge.Platform.Steam.SteamAccountService();
bool ok = await account.InitializeAsync(); // SteamID と表示名を取得
string steamId = account.AccountId;
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
var achievement = new CrossPlatformBridge.Platform.Steam.Achievement.SteamAchievementHandler();
bool ok = await achievement.UnlockAchievement("STEAM_ACHIEVEMENT_1");
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
var screenshot = new CrossPlatformBridge.Platform.Steam.ScreenShot.ScreenShot();
yield return screenshot.SaveScreenShot(success => Debug.Log($"Screenshot: {success}"));
```

#### テスト操作（ServiceTestUI）
- Save Screenshot
