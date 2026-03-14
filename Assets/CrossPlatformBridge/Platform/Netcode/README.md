# Netcode — CrossPlatformBridge

## Overview
Unity Netcode for GameObjects with Unity Gaming Services (UGS) Lobby and Relay.

**Supported services:** Network

## Prerequisites
- CrossPlatformBridge package
- UniTask
- Unity Netcode for GameObjects (`com.unity.netcode.gameobjects`)
- Unity Gaming Services packages:
  - `com.unity.services.lobby`
  - `com.unity.services.relay`
  - `com.unity.services.authentication`
- UGS project configured in [Unity Dashboard](https://cloud.unity.com/)

## Installation
1. Install packages via Unity Package Manager.
2. Add Scripting Define Symbol: `USE_CROSSPLATFORMBRIDGE_NETCODE`
3. Link your UGS project in **Edit → Project Settings → Services**.

## Platform Configuration
UGS authentication runs automatically during network initialization. Ensure your Unity project is linked to a UGS project with Lobby and Relay services enabled.

---

## Services

### Network

#### Handler Registration
```csharp
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.Netcode.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### Test Operations (ServiceTestUI)
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data
