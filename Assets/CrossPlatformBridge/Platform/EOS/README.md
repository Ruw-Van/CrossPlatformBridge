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
var account = new CrossPlatformBridge.Platform.EOS.Account.EOSAccount();
bool ok = await account.InitializeAsync(); // Opens browser for AccountPortal login
string userId = account.AccountId;
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
var achievement = new CrossPlatformBridge.Platform.EOS.Achievement.EOSAchievementHandler();
bool ok = await achievement.UnlockAchievement("eos_achievement_001");
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
var account = new EOSAccount();
await account.InitializeAsync();
// Then use cloud storage
var storage = new CrossPlatformBridge.Platform.EOS.CloudStorage.CloudStorageHandler();
await storage.SaveData("myKey", "myValue");
```

#### Test Operations (ServiceTestUI)
- Save Data / Load Data / Load All Data / Delete Data

---

### Network
EOS Lobby and Relay services.

#### Handler Registration
```csharp
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.EOS.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### Test Operations (ServiceTestUI)
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby
- Create / Join / Leave Room
- Search Lobbies / Search Rooms
- Send Data
