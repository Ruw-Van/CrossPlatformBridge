# PhotonFusion — CrossPlatformBridge

## Overview
Photon Fusion 2 network implementation for high-performance multiplayer using Photon Cloud.

**Supported services:** Network

## Prerequisites
- CrossPlatformBridge package
- UniTask
- [Photon Fusion 2 SDK](https://www.photonengine.com/fusion) (download from Photon website)
- Photon App ID for Fusion (from [Photon Dashboard](https://dashboard.photonengine.com/))

## Installation
1. Import Photon Fusion 2 SDK.
2. Set your Photon App ID in the Fusion settings.
3. Add Scripting Define Symbol: `USE_CROSSPLATFORMBRIDGE_PHOTONFUSION`

## Platform Configuration
Configure App ID via **Fusion → Fusion Hub** or the `PhotonAppSettings` asset.

---

## Services

### Network

#### Handler Registration
```csharp
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.PhotonFusion.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### Test Operations (ServiceTestUI)
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data
