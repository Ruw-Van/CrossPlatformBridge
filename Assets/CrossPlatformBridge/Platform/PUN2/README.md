[日本語](#日本語) | [English](#english)

---

<a id="日本語"></a>
# PUN2 — CrossPlatformBridge

## 概要
Photon Cloud を使用した Photon PUN2 ネットワーク実装。

**対応サービス:** Network

## 前提条件
- CrossPlatformBridge パッケージ
- UniTask
- [Photon PUN2](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922)（Unity Asset Store）
- Photon App ID（[Photon ダッシュボード](https://dashboard.photonengine.com/) から取得）

## インストール
1. Unity Asset Store から PUN2 をインポート。
2. **Window → Photon Unity Networking → Highlight Server Settings** で Photon App ID を設定。
3. Scripting Define Symbol を追加: `USE_CROSSPLATFORMBRIDGE_PUN2`

## プラットフォーム設定
`PhotonServerSettings` アセット（PUN2 ウィザードで自動生成）に Photon App ID を設定。

---

## サービス

### Network

#### ハンドラー登録
```csharp
await Network.Instance.Use<PUN2>();
await Network.Instance.ConnectNetwork(userId, userName);
```

#### テスト操作（ServiceTestUI）
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data

---

<a id="english"></a>
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
await Network.Instance.Use<PUN2>();
await Network.Instance.ConnectNetwork(userId, userName);
```

#### Test Operations (ServiceTestUI)
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data
