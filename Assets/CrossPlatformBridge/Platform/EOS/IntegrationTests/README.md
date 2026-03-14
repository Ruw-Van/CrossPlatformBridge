[日本語](#日本語) | [English](#english)

---

<a id="日本語"></a>
# EOS 統合テスト 実行手順

PlayMode 統合テストを実行すると、実際の EOS サーバーに接続して
Lobby / Room 作成・実績解除などを検証します。

---

## 前提条件

| 項目 | 内容 |
|------|------|
| Epic Games アカウント | EOS 開発者ポータルへのアクセス権限あり |
| EOS DevAuthTool | EOS SDK に同梱 (`Tools/EOS_DevAuthTool`) |
| Unity Editor | EOS Plugin 設定済み (`Tools > EOS Plugin > EOS Configuration`) |
| EOS 開発者ポータル設定 | テスト用の Achievement が定義済み |

---

## 設定ファイルの分類

| 種別 | 保存先 | git 管理 |
|------|--------|---------|
| EOS 認証情報（AppId 等） | `StreamingAssets/EOS/eos_product_config.json` | ✅ 管理対象（EOS Plugin が管理） |
| 統合テスト用設定 | `Assets/CrossPlatformBridgeSettings/Editor/EOS/EOSIntegrationTestSettings.asset` | ❌ `.gitignore` で除外済み |

`Editor/` フォルダ配下に置くことで Unity のビルドにも含まれません。

---

## セットアップ手順

### 1. EOS DevAuthTool の起動

```
[EOS SDK インストール先]/Tools/EOS_DevAuthTool を起動
  ↓
ポート: 8080（デフォルト）で起動
  ↓
Epic Games 開発者アカウントでログイン
  ↓
資格情報名を登録（例: TestUser）
```

> ⚠️ DevAuthTool はテスト実行中も起動したままにしてください。

### 2. テスト設定ファイルの作成

Unity のメニューバーから：

```
Tools > CrossPlatformBridge > EOS > Create Integration Test Settings
```

`Assets/CrossPlatformBridgeSettings/Editor/EOS/EOSIntegrationTestSettings.asset` が自動的に作成されます。
フォルダが存在しない場合も自動作成されます。

`EOSIntegrationTestSettings.asset` を開き、以下を入力：

| フィールド | 説明 | 例 |
|-----------|------|-----|
| Dev Auth Port | DevAuthTool のポート番号 | `8080` |
| Dev Auth Credential Name | DevAuthTool に登録した資格情報名 | `TestUser` |
| Test Achievement Id | EOS 開発者ポータルで定義済みの実績 API 名 | `eos_achievement_001` |

### 3. テストの実行

```
Window > General > Test Runner
  ↓
PlayMode タブ
  ↓
CrossPlatformBridge.EOS.IntegrationTests を展開して実行
```

---

## テストケース一覧

### EOSNetworkIntegrationTests（ネットワーク: 8 本）

各テストの `[UnitySetUp]` で以下が自動実行されます：
- `NetworkHandler.Initialize()` → EOS Plugin の初期化
- DevAuth ログイン（同一 Unity セッション内で初回のみ）
- `NetworkHandler.Connect()` → EOS アカウントへの接続

`[UnityTearDown]` で以下が自動実行されます：
- テスト中に作成した Room / Lobby の解放
- `NetworkHandler.Disconnect()` → 接続解除
- `NetworkHandler.Shutdown()` → リソース解放

| # | テスト名 | 検証内容 | 主な失敗原因 |
|---|---------|---------|------------|
| 1 | `Initialize_WithValidSettings_InitialStateIsNotConnected` | `Initialize()` 直後は `IsConnected=false`、`IsHost=false` | EOS Plugin が未設定 |
| 2 | `Connect_WithDevAuth_SetsIsConnectedAndAccountId` | `Connect()` 後は `IsConnected=true`、`AccountId` が設定される | DevAuthTool 未起動 / 認証情報名の誤り |
| 3 | `Disconnect_Connected_SetsIsConnectedFalse` | `Disconnect()` 後は `IsConnected=false` | — |
| 4 | `CreateLobby_Connected_ReturnsTrueAndSetsStationId` | `CreateLobby()` が `true` を返し、`StationId` が設定される | EOS Lobby サービスが有効でない |
| 5 | `SearchLobby_CreatedLobby_FindsAtLeastOneResult` | 作成したロビーが `SearchLobby()` の結果に含まれる | ロビー作成失敗 / 検索タイムアウト |
| 6 | `DisconnectLobby_FromActiveLobby_ClearsStationId` | `DisconnectLobby()` 後は `StationId=null` | — |
| 7 | `CreateRoom_Connected_ReturnsTrueAndSetsIsHost` | `CreateRoom()` が `true` を返し、`IsHost=true` になる | EOS P2P / Relay サービスが有効でない |
| 8 | `DisconnectRoom_FromActiveRoom_ClearsIsHost` | `DisconnectRoom()` 後は `IsHost=false` | — |

### EOSAchievementIntegrationTests（実績: 3 本）

各テストの `[UnitySetUp]` で以下が自動実行されます：
- DevAuth ログイン（同一 Unity セッション内で初回のみ）
- `NetworkHandler.Connect()` → EpicAccountId の取得に使用

`[TearDown]` はハンドラの参照解放のみ（実績データの巻き戻しなし）。

| # | テスト名 | 検証内容 | 主な失敗原因 |
|---|---------|---------|------------|
| 1 | `UnlockAchievement_ValidId_ReturnsTrueAndAddsToList` | 実績を解除し、`GetUnlockedAchievements()` の結果に含まれる | `TestAchievementId` がポータルに未定義 |
| 2 | `SetProgress_100Percent_TriggersUnlock` | 進捗 100% で実績が解除され、解除済みリストに含まれる | 同上 |
| 3 | `GetUnlockedAchievements_ReturnsNonNullList` | `GetUnlockedAchievements()` が `null` でないリストを返す | EOS Achievements サービスが有効でない |

---

## トラブルシューティング

### `EOSIntegrationTestSettings.asset が見つかりません`

→ [セットアップ手順 2](#2-テスト設定ファイルの作成) に従って設定ファイルを作成してください。
保存先が `Editor/IntegrationTest/` 以外の場合はテストがスキップされます（`Assume.That` による）。

### `DevAuth ログイン失敗: InvalidCredentials`

→ DevAuthTool が起動しているか確認してください。
→ `Dev Auth Credential Name` フィールドが DevAuthTool に登録した名前と一致しているか確認してください。

### `Initialize() が失敗しました`

→ `Tools > EOS Plugin > EOS Configuration` で EOS の AppId / SandboxId / DeploymentId が設定されているか確認してください。

### `CreateLobby() が false を返しました`

→ EOS 開発者ポータル（[dev.epicgames.com](https://dev.epicgames.com)）で該当 Deployment の **Lobby** サービスが有効になっているか確認してください。

### テスト後にロビーが残る

→ テストが途中で失敗した場合、EOS サーバー上にロビーが残ることがあります。通常は数分で自動削除されます。
手動削除が必要な場合は EOS 開発者ポータルの `Deployments > Lobbies` から確認できます。

---

## 注意事項

### Achievement テストについて

- テスト #1・#2 で `TestAchievementId` の実績を解除します。**一度解除した実績は再解除できません**（EOS の仕様）。
- テスト専用の Achievement を EOS 開発者ポータルで別途定義することを推奨します。
- テスト環境（Sandbox）では開発者ポータルから実績リセットが可能です。

### SendData テストについて

- P2P データ送受信テストは 2 クライアントが同じルームに接続している必要があるため、現バージョンでは未実装です。

### セッションの再利用

- 同一 Unity セッション内では DevAuth ログイン結果が再利用されます（`static bool _sessionInitialized`）。
- セッション関連のエラーが発生した場合は Unity Editor を再起動してください。

---

<a id="english"></a>
# EOS Integration Test — Setup Guide

Running PlayMode integration tests connects to real EOS servers to verify Lobby / Room creation, achievement unlocking, and more.

---

## Prerequisites

| Item | Details |
|------|---------|
| Epic Games account | Access to the EOS Developer Portal |
| EOS DevAuthTool | Bundled with the EOS SDK (`Tools/EOS_DevAuthTool`) |
| Unity Editor | EOS Plugin configured (`Tools > EOS Plugin > EOS Configuration`) |
| EOS Developer Portal | Test achievement defined |

---

## Configuration Files

| Type | Location | Git |
|------|----------|-----|
| EOS credentials (AppId, etc.) | `StreamingAssets/EOS/eos_product_config.json` | ✅ Tracked (managed by EOS Plugin) |
| Integration test settings | `Assets/CrossPlatformBridgeSettings/Editor/EOS/EOSIntegrationTestSettings.asset` | ❌ Excluded via `.gitignore` |

The `Editor/` path prevents the settings file from being included in builds.

---

## Setup Steps

### 1. Start EOS DevAuthTool

```
Launch [EOS SDK install dir]/Tools/EOS_DevAuthTool
  ↓
Start on port 8080 (default)
  ↓
Log in with your Epic Games developer account
  ↓
Register a credential name (e.g., TestUser)
```

> ⚠️ Keep DevAuthTool running throughout the test session.

### 2. Create the test settings file

From the Unity menu bar:

```
Tools > CrossPlatformBridge > EOS > Create Integration Test Settings
```

`Assets/CrossPlatformBridgeSettings/Editor/EOS/EOSIntegrationTestSettings.asset` is created automatically,
including any missing folders.

Open `EOSIntegrationTestSettings.asset` and fill in:

| Field | Description | Example |
|-------|-------------|---------|
| Dev Auth Port | DevAuthTool port number | `8080` |
| Dev Auth Credential Name | Credential name registered in DevAuthTool | `TestUser` |
| Test Achievement Id | Achievement API name defined in the EOS Developer Portal | `eos_achievement_001` |

### 3. Run the tests

```
Window > General > Test Runner
  ↓
PlayMode tab
  ↓
Expand CrossPlatformBridge.EOS.IntegrationTests and run
```

---

## Test Cases

### EOSNetworkIntegrationTests (Network: 8 tests)

`[UnitySetUp]` automatically runs before each test:
- `NetworkHandler.Initialize()` — initializes the EOS Plugin
- DevAuth login (once per Unity session)
- `NetworkHandler.Connect()` — connects to EOS account

`[UnityTearDown]` automatically runs after each test:
- Releases any Room / Lobby created during the test
- `NetworkHandler.Disconnect()` — disconnects
- `NetworkHandler.Shutdown()` — releases resources

| # | Test Name | Verifies | Common Failure |
|---|-----------|---------|----------------|
| 1 | `Initialize_WithValidSettings_InitialStateIsNotConnected` | After `Initialize()`: `IsConnected=false`, `IsHost=false` | EOS Plugin not configured |
| 2 | `Connect_WithDevAuth_SetsIsConnectedAndAccountId` | After `Connect()`: `IsConnected=true`, `AccountId` is set | DevAuthTool not running / wrong credential name |
| 3 | `Disconnect_Connected_SetsIsConnectedFalse` | After `Disconnect()`: `IsConnected=false` | — |
| 4 | `CreateLobby_Connected_ReturnsTrueAndSetsStationId` | `CreateLobby()` returns `true`, `StationId` is set | EOS Lobby service not enabled |
| 5 | `SearchLobby_CreatedLobby_FindsAtLeastOneResult` | Created lobby appears in `SearchLobby()` results | Lobby creation failed / search timeout |
| 6 | `DisconnectLobby_FromActiveLobby_ClearsStationId` | After `DisconnectLobby()`: `StationId=null` | — |
| 7 | `CreateRoom_Connected_ReturnsTrueAndSetsIsHost` | `CreateRoom()` returns `true`, `IsHost=true` | EOS P2P / Relay service not enabled |
| 8 | `DisconnectRoom_FromActiveRoom_ClearsIsHost` | After `DisconnectRoom()`: `IsHost=false` | — |

### EOSAchievementIntegrationTests (Achievement: 3 tests)

`[UnitySetUp]` automatically runs before each test:
- DevAuth login (once per Unity session)
- `NetworkHandler.Connect()` — used to obtain `EpicAccountId`

`[TearDown]` only releases handler references (no achievement data rollback).

| # | Test Name | Verifies | Common Failure |
|---|-----------|---------|----------------|
| 1 | `UnlockAchievement_ValidId_ReturnsTrueAndAddsToList` | Unlocks achievement and confirms it appears in `GetUnlockedAchievements()` | `TestAchievementId` not defined in portal |
| 2 | `SetProgress_100Percent_TriggersUnlock` | 100% progress unlocks the achievement and it appears in the unlocked list | Same as above |
| 3 | `GetUnlockedAchievements_ReturnsNonNullList` | `GetUnlockedAchievements()` returns a non-null list (empty list is OK) | EOS Achievements service not enabled |

---

## Troubleshooting

### `EOSIntegrationTestSettings.asset not found`

→ Follow [Setup Step 2](#2-create-the-test-settings-file) to create the settings file.
Tests are skipped (via `Assume.That`) if the file is not at `Assets/CrossPlatformBridgeSettings/Editor/EOS/EOSIntegrationTestSettings.asset`.

### `DevAuth login failed: InvalidCredentials`

→ Verify DevAuthTool is running.
→ Check that `Dev Auth Credential Name` matches the name registered in DevAuthTool.

### `Initialize() failed`

→ Verify AppId / SandboxId / DeploymentId are configured under `Tools > EOS Plugin > EOS Configuration`.

### `CreateLobby() returned false`

→ Check that the **Lobby** service is enabled for the relevant Deployment in the [EOS Developer Portal](https://dev.epicgames.com).

### Lobbies remain after tests

→ If a test fails mid-run, lobbies may remain on the EOS server. They are usually auto-deleted within a few minutes.
For manual deletion, check `Deployments > Lobbies` in the EOS Developer Portal.

---

## Notes

### Achievement tests

- Tests #1 and #2 unlock the `TestAchievementId` achievement. **Once unlocked, achievements cannot be re-unlocked** (EOS behavior).
- Define a dedicated test achievement in the EOS Developer Portal.
- In test environments (Sandbox), achievements can be reset from the developer portal.

### SendData test

- P2P data send/receive requires two clients in the same room, so this test is not yet implemented.

### Session reuse

- Within the same Unity session, the DevAuth login result is reused (`static bool _sessionInitialized`).
- If you encounter session-related errors, restart the Unity Editor.
