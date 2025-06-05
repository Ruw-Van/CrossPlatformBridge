// Assets/Scripts/CrossPlatformBridge/Network/DummyNetworkHandler/DummyNetworkHandler.Room.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Network.DummyNetworkHandler
{
	/// <summary>
	/// IInternalNetworkHandler のダミー実装のルーム操作部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class DummyNetworkHandler : IInternalNetworkHandler
	{
		public async UniTask<bool> CreateRoom(string roomName, INetworkSettings settings)
		{
			Debug.Log($"DummyNetworkHandler: ルーム '{roomName}' を作成中 (MaxPlayers: {settings.MaxPlayers})...");
			// DummyNetworkHandlerではLobbyとRoomを同じものとして扱う
			// DefaultRoomSettingsのMaxPlayersを上書きする
			bool success = await CreateLobby(roomName, settings); // ロビー作成を呼び出す
			if (success)
			{
				OnRoomOperationCompleted?.Invoke("CreateRoom", true, StationId);
			}
			else
			{
				OnRoomOperationCompleted?.Invoke("CreateRoom", false, "Room creation failed.");
			}
			return success;
		}

		public async UniTask<bool> ConnectRoom(string roomId)
		{
			Debug.Log($"DummyNetworkHandler: ルーム '{roomId}' に接続中...");
			// DummyNetworkHandlerではLobbyとRoomを同じものとして扱う
			bool success = await ConnectLobby(roomId); // ロビー接続を呼び出す
			if (success)
			{
				OnRoomOperationCompleted?.Invoke("ConnectRoom", true, StationId);
			}
			else
			{
				OnRoomOperationCompleted?.Invoke("ConnectRoom", false, "Room connection failed.");
			}
			return success;
		}

		public async UniTask DisconnectRoom()
		{
			Debug.Log("DummyNetworkHandler: ルームから切断中...");
			// DummyNetworkHandlerではLobbyとRoomを同じものとして扱う
			await DisconnectLobby(); // ロビー切断を呼び出す
			OnRoomOperationCompleted?.Invoke("DisconnectRoom", true, "");
			Debug.Log("DummyNetworkHandler: ルーム切断完了。");
		}

		public async UniTask<List<string>> SearchRoom(string query = "")
		{
			Debug.Log($"DummyNetworkHandler: ルームを検索中... クエリ: '{query}'");
			// DummyNetworkHandlerではLobbyとRoomを同じものとして扱う
			return await SearchLobby(query); // ロビー検索を呼び出す
		}
	}
}
