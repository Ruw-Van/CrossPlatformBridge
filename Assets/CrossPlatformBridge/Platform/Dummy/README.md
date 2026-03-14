# Dummy — CrossPlatformBridge

## Overview
Pure C# in-memory simulation. No external SDK required. Use for development, testing, and CI/CD.

**Supported services:** Achievement, Network

## Prerequisites
- CrossPlatformBridge package
- UniTask

## Installation
1. Install CrossPlatformBridge via UPM:
   ```
   https://github.com/Ruw-Van/CrossPlatformBridge.git?path=Assets/CrossPlatformBridge
   ```
2. No Scripting Define Symbol required — Dummy is always compiled.

---

## Services

### Achievement
In-memory achievement simulation.

#### Configuration
No configuration needed.

#### Handler Registration
```csharp
var achievement = new CrossPlatformBridge.Platform.Dummy.Achievement.DummyAchievementHandler();
bool ok = await achievement.UnlockAchievement("achievement_001");
```

#### Test Operations (ServiceTestUI)
- Unlock Achievement
- Get Unlocked Achievements
- Set Progress (50%)

---

### Network
In-memory network simulation.

#### Configuration
No configuration needed.

#### Handler Registration
```csharp
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.Dummy.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### Test Operations (ServiceTestUI)
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby
- Create / Join / Leave Room
- Search Lobbies / Search Rooms
- Send Data
