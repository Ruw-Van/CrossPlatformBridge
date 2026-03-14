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
var account = new CrossPlatformBridge.Platform.Steam.SteamAccountService();
bool ok = await account.InitializeAsync(); // Reads SteamID and display name
string steamId = account.AccountId;
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
var achievement = new CrossPlatformBridge.Platform.Steam.Achievement.SteamAchievementHandler();
bool ok = await achievement.UnlockAchievement("STEAM_ACHIEVEMENT_1");
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
var screenshot = new CrossPlatformBridge.Platform.Steam.ScreenShot.ScreenShot();
yield return screenshot.SaveScreenShot(success => Debug.Log($"Screenshot: {success}"));
```

#### Test Operations (ServiceTestUI)
- Save Screenshot
