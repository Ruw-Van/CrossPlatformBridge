# Netcode — CrossPlatformBridge

## 概要
Unity Gaming Services (UGS) の Lobby・Relay を使用した Unity Netcode for GameObjects 実装。

**対応サービス:** Network

## 前提条件
- CrossPlatformBridge パッケージ
- UniTask
- Unity Netcode for GameObjects (`com.unity.netcode.gameobjects`)
- Unity Gaming Services パッケージ:
  - `com.unity.services.lobby`
  - `com.unity.services.relay`
  - `com.unity.services.authentication`
- [Unity ダッシュボード](https://cloud.unity.com/) で UGS プロジェクトを設定済み

## インストール
1. Unity Package Manager で各パッケージをインストール。
2. Scripting Define Symbol を追加: `USE_CROSSPLATFORMBRIDGE_NETCODE`
3. **Edit → Project Settings → Services** で Unity プロジェクトと UGS プロジェクトをリンク。

## プラットフォーム設定
ネットワーク初期化時に UGS 認証が自動実行されます。Lobby・Relay サービスが有効な UGS プロジェクトにリンクされていることを確認してください。

---

## サービス

### Network

#### ハンドラー登録
```csharp
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.Netcode.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### テスト操作（ServiceTestUI）
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby & Room
- Search Lobbies / Search Rooms
- Send Data
