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
var account = new CrossPlatformBridge.Platform.EOS.Account.EOSAccount();
bool ok = await account.InitializeAsync(); // ブラウザで AccountPortal ログイン
string userId = account.AccountId;
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
var achievement = new CrossPlatformBridge.Platform.EOS.Achievement.EOSAchievementHandler();
bool ok = await achievement.UnlockAchievement("eos_achievement_001");
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
var account = new EOSAccount();
await account.InitializeAsync();
// その後クラウドストレージを使用
var storage = new CrossPlatformBridge.Platform.EOS.CloudStorage.CloudStorageHandler();
await storage.SaveData("myKey", "myValue");
```

#### テスト操作（ServiceTestUI）
- Save Data / Load Data / Load All Data / Delete Data

---

### Network
EOS Lobby・Relay サービスを使用したネットワーク実装。

#### ハンドラー登録
```csharp
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.EOS.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### テスト操作（ServiceTestUI）
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby
- Create / Join / Leave Room
- Search Lobbies / Search Rooms
- Send Data
