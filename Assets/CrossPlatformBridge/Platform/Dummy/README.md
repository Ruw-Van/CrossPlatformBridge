[日本語](#日本語) | [English](#english)

---

<a id="日本語"></a>
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
Achievement.Instance.Use<Dummy>();
bool ok = await Achievement.Instance.UnlockAchievement("achievement_001");
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
await Network.Instance.Use<Dummy>();
await Network.Instance.ConnectNetwork(userId, userName);
```

#### テスト操作（ServiceTestUI）
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby
- Create / Join / Leave Room
- Search Lobbies / Search Rooms
- Send Data

---

<a id="english"></a>
# Dummy — CrossPlatformBridge

## Overview
Pure C# in-memory simulation. No external SDK required. Use for development, testing, and CI/CD.

**Supported services:** Achievement, Network

## Prerequisites
- CrossPlatformBridge package
- UniTask

## Installation
1. Install CrossPlatformBridge via UPM:
   ```
   https://github.com/Ruw-Van/CrossPlatformBridge.git?path=Assets/CrossPlatformBridge
   ```
2. No Scripting Define Symbol required — Dummy is always compiled.

---

## Services

### Achievement
In-memory achievement simulation.

#### Configuration
No configuration needed.

#### Handler Registration
```csharp
Achievement.Instance.Use<Dummy>();
bool ok = await Achievement.Instance.UnlockAchievement("achievement_001");
```

#### Test Operations (ServiceTestUI)
- Unlock Achievement
- Get Unlocked Achievements
- Set Progress (50%)

---

### Network
In-memory network simulation.

#### Configuration
No configuration needed.

#### Handler Registration
```csharp
await Network.Instance.Use<Dummy>();
await Network.Instance.ConnectNetwork(userId, userName);
```

#### Test Operations (ServiceTestUI)
- Initialize Network / Connect / Disconnect / Shutdown
- Create / Join / Leave Lobby
- Create / Join / Leave Room
- Search Lobbies / Search Rooms
- Send Data
