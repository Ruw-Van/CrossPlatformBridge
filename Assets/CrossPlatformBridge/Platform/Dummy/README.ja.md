# Dummy — CrossPlatformBridge

## 概要
外部 SDK 不要の純粋 C# インメモリ実装。開発・テスト・CI/CD に使用します。

**対応サービス:** Achievement, Network

## 前提条件
- CrossPlatformBridge パッケージ
- UniTask

## インストール
1. UPM で CrossPlatformBridge をインポート:
   ```
   https://github.com/Ruw-Van/CrossPlatformBridge.git?path=Assets/CrossPlatformBridge
   ```
2. Scripting Define Symbol の追加は不要（Dummy は常にコンパイル対象）。

---

## サービス

### Achievement
インメモリ実績シミュレーション。

#### 設定
設定不要。

#### ハンドラー登録
```csharp
var achievement = new CrossPlatformBridge.Platform.Dummy.Achievement.DummyAchievementHandler();
bool ok = await achievement.UnlockAchievement("achievement_001");
```

#### テスト操作（ServiceTestUI）
- Unlock Achievement
- Get Unlocked Achievements
- Set Progress (50%)

---

### Network
インメモリネットワークシミュレーション。

#### 設定
設定不要。

#### ハンドラー登録
```csharp
var network = GetComponent<Network>();
await network.InitializeLibrary(new CrossPlatformBridge.Platform.Dummy.Network.NetworkHandler());
await network.ConnectNetwork(userId, userName);
```

#### テスト操作（ServiceTestUI）
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby
- Create / Join / Leave Room
- Search Lobbies / Search Rooms
- Send Data
