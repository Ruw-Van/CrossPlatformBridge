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
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.PUN2.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### テスト操作（ServiceTestUI）
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data
