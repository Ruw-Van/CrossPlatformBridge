using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Platform.Dummy.Network
{
	/// <summary>
	/// IInternalNetworkHandler のダミー実装のルーム操作部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class NetworkHandler : IInternalNetworkHandler
	{
		// DummyNetworkHandlerではLobbyとRoomを同じものとして扱う。
		// CreateSessionCoreAsync / ConnectSessionCoreAsync / DisconnectSessionCoreAsync（NetworkHandler.Lobby.cs定義）を利用し、
		// OnLobbyOperationCompleted を発火させずに OnRoomOperationCompleted のみ発火する。

		public async UniTask<bool> CreateRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			Debug.Log($"DummyNetworkHandler: ルーム '{baseSettings.RoomName}' を作成中 (MaxPlayers: {baseSettings.MaxPlayers})...");
			if (!_isConnected)
			{
				Debug.LogError("DummyNetworkHandler: 接続されていません。ルームを作成できません。");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Not connected.");
				return false;
			}
			try
			{
				string sessionId = await CreateSessionCoreAsync(baseSettings, cancellationToken);
				OnRoomOperationCompleted?.Invoke("CreateRoom", true, sessionId);
				return true;
			}
			catch (OperationCanceledException)
			{
				Debug.Log("DummyNetworkHandler: ルーム作成がキャンセルされました。");
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Cancelled");
				return false;
			}
		}

		public async UniTask<bool> ConnectRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			Debug.Log($"DummyNetworkHandler: ルーム '{baseSettings.RoomName}' に接続中...");
			if (!_isConnected)
			{
				Debug.LogError("DummyNetworkHandler: 接続されていません。ルームに接続できません。");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Not connected.");
				return false;
			}
			try
			{
				string sessionId = await ConnectSessionCoreAsync(baseSettings, cancellationToken);
				OnRoomOperationCompleted?.Invoke("ConnectRoom", true, sessionId);
				return true;
			}
			catch (OperationCanceledException)
			{
				Debug.Log("DummyNetworkHandler: ルーム接続がキャンセルされました。");
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Cancelled");
				return false;
			}
		}

		public async UniTask DisconnectRoom()
		{
			Debug.Log("DummyNetworkHandler: ルームから切断中...");
			await DisconnectSessionCoreAsync();
			OnRoomOperationCompleted?.Invoke("DisconnectRoom", true, "");
			Debug.Log("DummyNetworkHandler: ルーム切断完了。");
		}

		public async UniTask<List<object>> SearchRoom(IRoomSettings baseSettings)
		{
			Debug.Log($"DummyNetworkHandler: ルームを検索中... クエリ: '{baseSettings.RoomName}'");
			return await SearchLobby(baseSettings); // ロビー検索に委譲（イベントなし）
		}
	}
}
