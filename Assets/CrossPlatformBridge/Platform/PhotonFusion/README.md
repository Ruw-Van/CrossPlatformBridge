[日本語](#日本語) | [English](#english)

---

<a id="日本語"></a>
# PhotonFusion — CrossPlatformBridge

## 概要
Photon Cloud を使用した Photon Fusion 2 高パフォーマンス マルチプレイヤー実装。

**対応サービス:** Network

## 前提条件
- CrossPlatformBridge パッケージ
- UniTask
- [Photon Fusion 2 SDK](https://www.photonengine.com/fusion)（Photon 公式サイトからダウンロード）
- Fusion 用 Photon App ID（[Photon ダッシュボード](https://dashboard.photonengine.com/) から取得）

## インストール
1. Photon Fusion 2 SDK をインポート。
2. Photon App ID を設定。
3. Scripting Define Symbol を追加: `USE_CROSSPLATFORMBRIDGE_PHOTONFUSION`

## プラットフォーム設定
**Fusion → Fusion Hub** または `PhotonAppSettings` アセットで App ID を設定。

---

## サービス

### Network

#### ハンドラー登録
```csharp
await Network.Instance.Use<Fusion>();
await Network.Instance.ConnectNetwork(userId, userName);
```

#### テスト操作（ServiceTestUI）
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data

---

<a id="english"></a>
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
await Network.Instance.Use<Fusion>();
await Network.Instance.ConnectNetwork(userId, userName);
```

#### Test Operations (ServiceTestUI)
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data
