# CrossPlatformBridge

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A cross-platform game services framework for Unity that provides a **single unified interface** across networking, authentication, cloud storage, payments, achievements, and more.
Switch between backends (PUN2, Photon Fusion, Unity Netcode, EOS, PlayFab, Steam…) without changing your game logic.

---

ネットワーク・認証・クラウドセーブ・決済・実績など、複数のバックエンドを **統一インターフェース** で扱う Unity クロスプラットフォームフレームワークです。
PUN2・Photon Fusion・Unity Netcode・EOS・PlayFab・Steam など、プラットフォームを切り替えても上位ロジックは変わりません。

---

## Features / 特徴

- **Single API** — One facade per service for all platforms / 各サービスを1つの Facade で統一操作
- **Async-first** — All operations use [UniTask](https://github.com/Cysharp/UniTask) / 全非同期処理は UniTask ベース
- **Offline-friendly** — Built-in `Dummy` handler for development & testing / Dummy ハンドラーでオフライン開発・テスト対応
- **Networking** — Lobby/room lifecycle, data send/receive across 4+ backends / ロビー・ルームの作成・検索・データ送受信を統一管理
- **Authentication** — Cross-platform account login and identity / クロスプラットフォームのアカウント認証・プロフィール管理
- **Cloud Storage** — Save and load game data by key / キー指定でゲームデータを保存・読み込み
- **Payment** — Virtual currencies, catalog browsing, item purchase, and inventory / 仮想通貨・カタログ・購入・インベントリ管理
- **Achievements** — Unlock and track achievement progress / 実績の解除・進行度の記録
- **Leaderboard** — Global rankings (cloud-persistent) and in-session rankings (room-scoped) / グローバルランキング（クラウド永続）とセッション内ランキング（ルーム内一時）
- **Screenshot** — Platform-specific screenshot capture / プラットフォーム固有のスクリーンショット保存
- **Event-driven** — Typed events for each service / 各サービスに型付きイベント
- **Extensible** — Add your own handler with 6 files / 6ファイルで新しいハンドラーを追加可能

---

## Supported Platforms / 対応プラットフォーム

✅ Implementation + tests complete &nbsp;|&nbsp; 🚧 Implemented, tests pending &nbsp;|&nbsp; — Not implemented

| Platform | Network | Account | Cloud Storage | Payment | Screenshot | Achievements | Leaderboard |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **Dummy** (dev/test) | ✅ | — | — | — | — | ✅ | ✅ |
| **Photon PUN2** | ✅ | — | — | — | — | — | — |
| **Photon Fusion** | ✅ | — | — | — | — | — | — |
| **Unity Netcode + UGS** | ✅ | — | — | — | — | — | — |
| **Epic Online Services (EOS)** | ✅ | ✅ | ✅ | ✅ | — | ✅ | ✅ |
| **Microsoft PlayFab** | — | 🚧 | ✅ | 🚧 | — | — | ✅ |
| **Steam** | — | ✅ | — | — | ✅ | 🚧 | — |
| **Firebase** | — | 🚧 | ✅ | — | — | — | — |

---

## Architecture / アーキテクチャ

```
Your Game Code
    ├── Network.Instance.Use<Dummy>() / Use<PUN2>() / Use<EOS>() ...
    │       │   Platform marker creates handler internally
    │       └── IInternalNetworkHandler
    │               ├── Dummy    → Dummy.Network.NetworkHandler    ← dev / test
    │               ├── PUN2     → PUN2.Network.NetworkHandler
    │               ├── PhotonFusion → PhotonFusion.Network.NetworkHandler
    │               ├── Netcode  → Netcode.Network.NetworkHandler
    │               └── EOS      → EOS.Network.NetworkHandler
    │
    ├── AccountService.Instance.Use<PlayFab>() / Use<EOS>() / Use<Steam>() ...
    │       └── IInternalAccountHandler
    │               ├── PlayFab  → PlayFab.Account.PlayFabAccount
    │               ├── EOS      → EOS.Account.EOSAccount
    │               ├── Steam    → Steam.SteamAccountService
    │               └── Firebase → Firebase.Account.FirebaseAccountHandler
    │
    ├── CloudStorage.Instance.Use<PlayFab>() / Use<EOS>() / Use<Firebase>() ...
    │       └── IInternalCloudStorageHandler
    │               ├── PlayFab  → PlayFab.CloudStorage.CloudStorageHandler
    │               ├── EOS      → EOS.CloudStorage.CloudStorageHandler
    │               └── Firebase → Firebase.CloudStorage.FirebaseCloudStorageHandler
    │
    ├── Payment.Instance.Use<PlayFab>() / Use<EOS>() ...
    │       └── IInternalPaymentHandler
    │               ├── PlayFab  → PlayFab.Payment.PaymentHandler
    │               └── EOS      → EOS.Payment.PaymentHandler
    │
    ├── Achievement.Instance.Use<Dummy>() / Use<Steam>() / Use<EOS>() ...
    │       └── IInternalAchievementHandler
    │               ├── Dummy    → Dummy.Achievement.DummyAchievementHandler  ← dev / test
    │               ├── Steam    → Steam.Achievement.SteamAchievementHandler
    │               └── EOS      → EOS.Achievement.EOSAchievementHandler
    │
    ├── Leaderboard.Instance.Use<DummyLeaderboardPlatform>() / Use<EOS>() / Use<PlayFab>() ...
    │       └── IInternalLeaderboardHandler
    │               ├── Dummy    → Dummy.Leaderboard.DummyLeaderboardHandler  ← dev / test
    │               ├── EOS      → EOS.Leaderboard.EOSLeaderboardHandler
    │               └── PlayFab  → PlayFab.Leaderboard.LeaderboardHandler
    │
    └── ScreenShot.Use<Steam>() ...
            └── IInternalScreenShot
                    └── Steam    → Steam.ScreenShot.ScreenShot
```

Each facade delegates every call to the active handler.
Your game code only touches the facade — never the platform internals.

各ファサードはアクティブなハンドラーに全操作を委譲します。
呼び出し元はファサードだけを使えばよく、プラットフォーム実装には触れません。

---

## Requirements / 動作要件

| | |
|---|---|
| Unity | 2022.3 or later |
| [UniTask](https://github.com/Cysharp/UniTask) | Required / 必須 |
| Platform SDK | Only for the platform you use / 使用するプラットフォームのSDKのみ |

---

## Installation / インストール

### Via Unity Package Manager (Git URL)

Open **Package Manager → Add package from git URL** and enter:

```
https://github.com/Ruw-Van/CrossPlatformBridge.git?path=Assets/CrossPlatformBridge
```

> **Note:** Install [UniTask](https://github.com/Cysharp/UniTask) first, then install the platform-specific SDK for any platform you intend to use.

---

## Quick Start / クイックスタート

### 1. Initialize with the Dummy handler

```csharp
using CrossPlatformBridge;
using Cysharp.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        // Recommended: instantiate and initialize in one call
        bool initialized = await Network.Instance.Use<Dummy>();
        if (!initialized) return;

        // Connect (userId, displayName)
        bool connected = await Network.Instance.ConnectNetwork("user_001", "Player1");
        Debug.Log($"Connected: {connected}");
    }
}
```

### 2. Create and join a room

```csharp
// Prepare room settings via the factory
IRoomSettings settings = Network.Instance.PrepareRoomSettings();
settings.RoomName   = "MyRoom";
settings.MaxPlayers = 4;
settings.IsVisible  = true;
settings.IsOpen     = true;

bool created = await Network.Instance.CreateRoom(settings);
```

### 3. Send and receive data

```csharp
// Subscribe to events
Network.Instance.OnDataReceived        += (data, senderId) => Debug.Log($"[{senderId}] {data.Length} bytes");
Network.Instance.OnPlayerConnected     += (id, name)       => Debug.Log($"{name} joined");
Network.Instance.OnPlayerDisconnected  += (id, name)       => Debug.Log($"{name} left");

// Send to all players
byte[] payload = System.Text.Encoding.UTF8.GetBytes("hello");
await Network.Instance.SendData(payload);

// Send to a specific player
await Network.Instance.SendData(payload, targetPlayerId);
```

### 4. Switch platforms at runtime

```csharp
// Gracefully shutdown the current handler
await Network.Instance.ShutdownLibrary();

// Initialize with a different handler
await Network.Instance.Use<EOS>();
await Network.Instance.ConnectNetwork(eosAccountId, displayName);
```

### 5. Account authentication

```csharp
using CrossPlatformBridge.Services.Account;

public class AuthManager : MonoBehaviour
{
    async UniTaskVoid Start()
    {
        // Recommended: instantiate and register in one call
        AccountService.Instance.Use<EOS>();
        AccountService.Instance.OnAuthStateChanged += isReady => Debug.Log($"Auth: {isReady}");

        bool ok = await AccountService.Instance.InitializeAsync();
        if (ok)
            Debug.Log($"Logged in as {AccountService.Instance.NickName} ({AccountService.Instance.AccountId})");
    }
}
```

### 6. Cloud storage (save & load)

```csharp
using CrossPlatformBridge.Services.CloudStorage;

// Inject handler once (e.g. at startup)
CloudStorage.Instance.Use<PlayFab>();

// Save
bool saved = await CloudStorage.Instance.SaveData("score", "9999");

// Load (returns null if key does not exist)
string score = await CloudStorage.Instance.LoadData("score");

// Batch save
await CloudStorage.Instance.SaveDataBatch(new Dictionary<string, string>
{
    { "level", "5" },
    { "coins", "120" },
});
```

### 7. Payment (catalog & purchase)

```csharp
using CrossPlatformBridge.Services.Payment;

Payment.Instance.Use<PlayFab>();
Payment.Instance.OnCurrencyUpdated += (code, balance) => Debug.Log($"{code}: {balance}");
Payment.Instance.OnPurchaseError   += err => Debug.LogError(err);

// Fetch catalog and buy an item
var catalog = await Payment.Instance.GetCatalog();
var item = catalog.First();
bool purchased = await Payment.Instance.PurchaseItem(item.ItemId, "GM", item.Price);

// Retrieve inventory
var inventory = await Payment.Instance.GetInventory();
```

### 8. Achievements

```csharp
using CrossPlatformBridge.Services.Achievement;

Achievement.Instance.Use<Dummy>();

// Unlock immediately
bool unlocked = await Achievement.Instance.UnlockAchievement("ACH_FIRST_WIN");

// Update incremental progress (0.0 – 1.0)
await Achievement.Instance.SetProgress("ACH_COLLECTOR", 0.5f);

// Query unlocked list
List<string> unlocked = await Achievement.Instance.GetUnlockedAchievements();
```

### 9. Leaderboard

```csharp
using CrossPlatformBridge.Services.Leaderboard;

Leaderboard.Instance.Use<DummyLeaderboardPlatform>();

// --- Global ranking (cloud-persistent) ---
// Submit score
bool ok = await Leaderboard.Instance.SubmitScore("high_score", 9500L);

// Fetch top 10
List<LeaderboardEntry> top10 = await Leaderboard.Instance.GetTopEntries("high_score", 10);
foreach (var e in top10)
    Debug.Log($"#{e.Rank} {e.PlayerName}: {e.Score}");

// Fetch entries around the current player
var nearby = await Leaderboard.Instance.GetEntriesAroundPlayer("high_score", myPlayerId, range: 2);

// --- In-session ranking (room-scoped, cleared when session ends) ---
await Leaderboard.Instance.UpdateSessionScore(playerId, playerName, 3200L);

List<LeaderboardEntry> board = await Leaderboard.Instance.GetSessionLeaderboard();
int myRank = await Leaderboard.Instance.GetPlayerSessionRank(playerId); // 1-based, -1 if absent

await Leaderboard.Instance.ResetSessionLeaderboard();
```

### 10. Screenshot

```csharp
using CrossPlatformBridge.Services.ScreenShot;

// Register implementation once (or use RuntimeInitializeOnLoadMethod in the platform assembly)
ScreenShot.Use<Steam>();

// Capture (Coroutine)
StartCoroutine(ScreenShot.SaveScreenShot(success => Debug.Log($"Screenshot saved: {success}")));
```

---

## Public API Reference

### Network Lifecycle

| Method | Returns | Description |
|---|---|---|
| `Use<T>()` | `UniTask<bool>` | Instantiate platform `T` and initialize its handler (recommended) |
| `InitializeLibrary(handler)` | `UniTask<bool>` | Attach a pre-created handler and initialize |
| `ShutdownLibrary()` | `UniTask` | Shutdown and detach the handler |
| `ConnectNetwork(userId, userName)` | `UniTask<bool>` | Connect to the network |
| `DisconnectNetwork()` | `UniTask` | Disconnect from the network |
| `SwitchNetwork(newUserId, newUserName)` | `UniTask<bool>` | Disconnect → Connect atomically |

### Lobby & Room

| Method | Returns | Description |
|---|---|---|
| `CreateLobby(settings)` | `UniTask<bool>` | Create a lobby |
| `ConnectLobby(settings)` | `UniTask<bool>` | Join an existing lobby |
| `DisconnectLobby()` | `UniTask` | Leave the lobby |
| `SearchLobby(settings)` | `UniTask<List<object>>` | Search for lobbies |
| `CreateRoom(settings)` | `UniTask<bool>` | Create a room |
| `ConnectRoom(settings)` | `UniTask<bool>` | Join an existing room |
| `DisconnectRoom()` | `UniTask` | Leave the room |
| `SearchRoom(settings)` | `UniTask<List<object>>` | Search for rooms |

### Data & State

| Member | Type | Description |
|---|---|---|
| `SendData(data, targetId?)` | `UniTask` | Send raw bytes (broadcast or unicast) |
| `IsConnected` | `bool` | Whether connected to the network |
| `IsHost` | `bool` | Whether this client is the host |
| `ConnectedList` | `List<PlayerData>` | Currently connected players |
| `DisconnectedList` | `List<PlayerData>` | Recently disconnected players |
| `PrepareRoomSettings()` | `IRoomSettings` | Create a settings object via the factory |

### Events

| Event | Signature | Trigger |
|---|---|---|
| `OnDataReceived` | `(byte[] data, string senderId)` | Data received from any player |
| `OnPlayerConnected` | `(string id, string name)` | A player joined |
| `OnPlayerDisconnected` | `(string id, string name)` | A player left |
| `OnNetworkConnectionStatusChanged` | `(bool isConnected)` | Connection state changed |
| `OnHostStatusChanged` | `(bool isHost)` | Host role changed |
| `OnLobbyOperationCompleted` | `(string op, bool success, string msg)` | Lobby operation result |
| `OnRoomOperationCompleted` | `(string op, bool success, string msg)` | Room operation result |

---

### AccountService

#### Lifecycle

| Method | Returns | Description |
|---|---|---|
| `Use<T>()` | `IInternalAccountHandler` | Instantiate platform `T` and register its handler (recommended) |
| `InitializeHandler(handler)` | `void` | Inject a pre-created handler |
| `InitializeAsync()` | `UniTask<bool>` | Authenticate and initialize |
| `ShutdownAsync()` | `UniTask` | Shutdown and clean up |

#### Properties & Events

| Member | Type | Description |
|---|---|---|
| `IsInitialized` | `bool` | Whether authenticated |
| `AccountId` | `string` | Platform-specific account ID |
| `NickName` | `string` | Display name |
| `OnAuthStateChanged` | `Action<bool>` | Fired when auth state changes |

---

### CloudStorage

| Method | Returns | Description |
|---|---|---|
| `Use<T>()` | `IInternalCloudStorageHandler` | Instantiate platform `T` and register its handler (recommended) |
| `InitializeHandler(handler)` | `void` | Inject a pre-created handler |
| `SaveData(key, value)` | `UniTask<bool>` | Save a single string value |
| `SaveDataBatch(dict)` | `UniTask<bool>` | Save multiple values at once |
| `LoadData(key)` | `UniTask<string>` | Load a value (`null` if not found) |
| `LoadDataBatch(keys)` | `UniTask<Dictionary<string,string>>` | Load multiple values |
| `DeleteData(key)` | `UniTask<bool>` | Delete a value |

---

### Payment

#### Methods

| Method | Returns | Description |
|---|---|---|
| `Use<T>()` | `IInternalPaymentHandler` | Instantiate platform `T` and register its handler (recommended) |
| `InitializeHandler(handler)` | `void` | Inject a pre-created handler |
| `GetVirtualCurrencies()` | `UniTask<Dictionary<string,int>>` | List currency codes and balances |
| `GetCatalog(catalogVersion?)` | `UniTask<List<CatalogItemInfo>>` | Fetch purchasable items |
| `PurchaseItem(itemId, currency, price, version?)` | `UniTask<bool>` | Buy an item with virtual currency |
| `GetInventory()` | `UniTask<List<InventoryItemInfo>>` | List owned items |
| `ConsumeItem(itemInstanceId, count?)` | `UniTask<bool>` | Consume an item from inventory |

#### Events

| Event | Signature | Trigger |
|---|---|---|
| `OnCurrencyUpdated` | `(string code, int balance)` | Currency balance changed |
| `OnPurchaseError` | `(string error)` | Purchase failed |

---

### Achievement

| Method | Returns | Description |
|---|---|---|
| `Use<T>()` | `IInternalAchievementHandler` | Instantiate platform `T` and register its handler (recommended) |
| `InitializeHandler(handler)` | `void` | Inject a pre-created handler |
| `UnlockAchievement(achievementId)` | `UniTask<bool>` | Unlock an achievement |
| `GetUnlockedAchievements()` | `UniTask<List<string>>` | List all unlocked achievement IDs |
| `SetProgress(achievementId, progress)` | `UniTask<bool>` | Update progress (0.0–1.0) |

---

### Leaderboard

#### Global Ranking / グローバルランキング

| Method | Returns | Description |
|---|---|---|
| `Use<T>()` | `IInternalLeaderboardHandler` | Instantiate platform `T` and register its handler (recommended) |
| `InitializeHandler(handler)` | `void` | Inject a pre-created handler |
| `SubmitScore(leaderboardName, score)` | `UniTask<bool>` | Submit the current player's score |
| `GetTopEntries(leaderboardName, count)` | `UniTask<List<LeaderboardEntry>>` | Fetch top N entries sorted by score |
| `GetPlayerEntry(leaderboardName, playerId)` | `UniTask<LeaderboardEntry>` | Get a specific player's entry (`null` if not found) |
| `GetEntriesAroundPlayer(leaderboardName, playerId, range)` | `UniTask<List<LeaderboardEntry>>` | Fetch entries surrounding a player (±range) |

#### In-Session Ranking / セッション内ランキング

| Method | Returns | Description |
|---|---|---|
| `UpdateSessionScore(playerId, playerName, score)` | `UniTask<bool>` | Update a player's in-room score |
| `GetSessionLeaderboard()` | `UniTask<List<LeaderboardEntry>>` | Get all players sorted by score descending |
| `GetPlayerSessionRank(playerId)` | `UniTask<int>` | Get rank (1-based; `-1` if player not found) |
| `ResetSessionLeaderboard()` | `UniTask` | Clear all in-session scores |

#### LeaderboardEntry

| Property | Type | Description |
|---|---|---|
| `PlayerId` | `string` | Platform-specific player ID |
| `PlayerName` | `string` | Display name |
| `Score` | `long` | Score value |
| `Rank` | `int` | Rank position (1-based) |

---

### ScreenShot

| Member | Type | Description |
|---|---|---|
| `Use<T>()` | `IInternalScreenShot` | Instantiate platform `T` and register its implementation (recommended) |
| `SetImplementation(impl)` | `void` | Register a pre-created implementation |
| `SaveScreenShot(onCompleted?)` | `IEnumerator` | Capture and save a screenshot |

---

## Adding a New Handler / ハンドラーの追加

Create a folder under `Assets/CrossPlatformBridge/Platform/<Name>/Network/` and implement these 6 files:

| File | Responsibility |
|---|---|
| `NetworkHandler.Core.cs` | `IInternalNetworkHandler` declaration, lifecycle, properties, events |
| `NetworkHandler.Lobby.cs` | Lobby operations |
| `NetworkHandler.Room.cs` | Room operations |
| `NetworkHandler.Data.cs` | `SendData` implementation |
| `Settings.cs` | `NetworkSettings` (extends `NetworkSettingsScriptableObjectBase`) + `RoomSettings` |
| `SettingsFactory.cs` | `INetworkSettingsFactory` implementation |

Then add the new assembly's GUID to `references` in `CrossPlatformBridge.asmdef`.

The `Dummy` handler under `Platform/Dummy/Network/` serves as a reference implementation — copy it as a starting point.

---

## Testing / テスト

Tests use the **Unity Test Runner** (EditMode + PlayMode). The suite currently contains **265 test methods across 32 test files**.

1. Open **Window → General → Test Runner**
2. Select the **EditMode** tab
3. Click **Run All**

Test files are located in `Assets/CrossPlatformBridge/Platform/<Platform>/Tests/`.

All async tests follow the `[UnityTest] + IEnumerator + UniTask.ToCoroutine()` pattern:

```csharp
[UnityTest]
public IEnumerator Connect_ShouldSucceed() => UniTask.ToCoroutine(async () =>
{
    var handler  = new DummyNetworkHandler();
    var settings = ScriptableObject.CreateInstance<DummyNetworkSettings>();
    bool result  = await handler.Connect(settings);
    Assert.IsTrue(result);
});
```

---

## License

This project is licensed under the [MIT License](LICENSE).

---

## Author

**Ruw** — [@Ruw on X](https://x.com/Ruw)
