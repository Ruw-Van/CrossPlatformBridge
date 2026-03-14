[日本語](#日本語) | [English](#english)

---

<a id="日本語"></a>
# Steam 統合テスト 実行手順

PlayMode 統合テストを実行すると、実際の Steam サーバーに接続して実績操作を検証します。

## 前提条件

- Steam クライアントが起動していること
- `steam_appid.txt` がプロジェクトルートに配置済みであること
- Steamworks Partner Dashboard でテスト用の Achievement が定義済みであること

## 設定ファイルの分類

| 種別 | 保存先 | 説明 |
|------|--------|------|
| Steam AppId | `steam_appid.txt`（プロジェクトルート） | 既存ファイル（変更不要） |
| 統合テスト用設定 | `Assets/CrossPlatformBridgeSettings/Editor/Steam/SteamIntegrationTestSettings.asset` | **Tools メニューで自動作成** |

`Editor/` フォルダ配下に置くことで Unity のビルドに含まれません。`.gitignore` により git にもコミットされません。

## 手順

### 1. Steam クライアントの起動

テスト実行前に Steam クライアントを起動してください。
`steam_appid.txt` が存在しない場合は、プロジェクトルートに作成して AppId を記載してください。

```
480
```
（上記は Spacewar のデモ AppId。実際のプロジェクト AppId に置き換えてください）

### 2. テスト設定ファイルの作成

Unity のメニューバーから：

```
Tools > CrossPlatformBridge > Steam > Create Integration Test Settings
```

`Assets/CrossPlatformBridgeSettings/Editor/Steam/SteamIntegrationTestSettings.asset` が自動的に作成されます。
フォルダが存在しない場合も自動作成されます。

作成した `SteamIntegrationTestSettings.asset` に以下を入力：

| フィールド | 説明 | 例 |
|-----------|------|----|
| Test Achievement Id | Steamworks Partner Dashboard で定義済みの実績 API 名 | `ACH_WIN_ONE_GAME` |

### 3. テストの実行

```
Window > General > Test Runner
  ↓
PlayMode タブ
  ↓
CrossPlatformBridge.Steam.IntegrationTests を展開して実行
```

## 注意事項

### Achievement テストについて
- 実績 API 名は Steamworks Partner Dashboard の **「統計と実績」** セクションで確認できます。
- **一度解除した実績は再解除できません**（Steam の仕様）。テスト用には専用の実績 ID を定義してください。
- Steamworks Partner Dashboard でテスト実績を設定したら、「変更を公開」が必要です（内部テスト状態でも動作します）。

### Network テストについて
- Steam のネットワーク機能（Lobby / Room / P2P）は現バージョンでは未実装のため、ネットワーク統合テストは対象外です。

### テスト後のクリーンアップ
- Steam 実績のリセットは Steamworks Partner Dashboard の「ユーティリティ」→「実績のリセット」で行えます（開発用アカウントのみ）。

---

<a id="english"></a>
# Steam Integration Test — Setup Guide

Running PlayMode integration tests connects to real Steam servers to verify achievement operations.

## Prerequisites

- Steam client running
- `steam_appid.txt` present in the project root
- Test achievement defined in Steamworks Partner Dashboard

## Configuration Files

| Type | Location | Description |
|------|----------|-------------|
| Steam AppId | `steam_appid.txt` (project root) | Existing file (no changes needed) |
| Integration test settings | `Assets/CrossPlatformBridgeSettings/Editor/Steam/SteamIntegrationTestSettings.asset` | **Auto-created via Tools menu** |

The `Editor/` path prevents the settings file from being included in builds and from being committed to git (excluded via `.gitignore`).

## Setup Steps

### 1. Start Steam client

Launch the Steam client before running tests.
If `steam_appid.txt` does not exist, create it in the project root with your App ID:

```
480
```
(The above is the Spacewar demo App ID — replace with your actual project App ID.)

### 2. Create the test settings file

From the Unity menu bar:

```
Tools > CrossPlatformBridge > Steam > Create Integration Test Settings
```

`Assets/CrossPlatformBridgeSettings/Editor/Steam/SteamIntegrationTestSettings.asset` is created automatically,
including any missing folders.

Open `SteamIntegrationTestSettings.asset` and fill in:

| Field | Description | Example |
|-------|-------------|---------|
| Test Achievement Id | Achievement API name defined in Steamworks Partner Dashboard | `ACH_WIN_ONE_GAME` |

### 3. Run the tests

```
Window > General > Test Runner
  ↓
PlayMode tab
  ↓
Expand CrossPlatformBridge.Steam.IntegrationTests and run
```

## Notes

### Achievement tests
- Achievement API names can be found in the **Stats & Achievements** section of Steamworks Partner Dashboard.
- **Once unlocked, achievements cannot be re-unlocked** (Steam behavior). Define dedicated test achievements.
- After configuring test achievements in Steamworks Partner Dashboard, you must **Publish** the changes (internal test state also works).

### Network tests
- Steam network features (Lobby / Room / P2P) are not yet implemented, so network integration tests are out of scope.

### Post-test cleanup
- Steam achievement resets can be performed in Steamworks Partner Dashboard under **Utilities → Reset Achievements** (developer accounts only).
