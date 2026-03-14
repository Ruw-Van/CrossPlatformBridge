# PUN2 — CrossPlatformBridge

## Overview
Photon PUN2 (Photon Unity Networking 2) network implementation using Photon Cloud.

**Supported services:** Network

## Prerequisites
- CrossPlatformBridge package
- UniTask
- [Photon PUN2](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922) (Unity Asset Store)
- Photon App ID (from [Photon Dashboard](https://dashboard.photonengine.com/))

## Installation
1. Import PUN2 from the Unity Asset Store.
2. Set your Photon App ID in **Window → Photon Unity Networking → Highlight Server Settings**.
3. Add Scripting Define Symbol: `USE_CROSSPLATFORMBRIDGE_PUN2`

## Platform Configuration
Configure Photon App ID in `PhotonServerSettings` asset (auto-created by PUN2 wizard).

---

## Services

### Network

#### Handler Registration
```csharp
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.PUN2.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### Test Operations (ServiceTestUI)
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data
