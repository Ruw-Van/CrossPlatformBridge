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
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.PhotonFusion.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### テスト操作（ServiceTestUI）
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data
