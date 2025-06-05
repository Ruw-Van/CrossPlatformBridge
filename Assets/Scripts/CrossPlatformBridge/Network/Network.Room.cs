// Assets/Scripts/CrossPlatformBridge/Network/Network.Room.cs
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Network
{
	// Network クラスのルーム関連部分 // ★ コメント更新
	public partial class Network // ★ クラス名変更
	{
		// --------------------------------------------------------------------------------
		// ルーム機能
		// --------------------------------------------------------------------------------

		public async UniTask<bool> CreateRoom(string roomName)
		{
			if (_internalNetworkHandler == null)
			{
				Debug.LogError("Network: 内部ネットワークハンドラが設定されていません。");
				return false;
			}

			// 準備された設定を使用するか、デフォルト設定を生成
			INetworkSettings settingsToUse = _preparedSettings ?? _internalNetworkHandler.SettingsFactory.CreateSettings();
			return await _internalNetworkHandler.CreateRoom(roomName, settingsToUse); // ★settingsToUseを渡す
		}

		public async UniTask<bool> ConnectRoom(string roomId)
		{
			Debug.Log($"Network: ルーム '{roomId}' に非同期で接続中..."); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return false;

			bool success = await _internalNetworkHandler.ConnectRoom(roomId);
			if (success)
			{
				// IsHost は IInternalNetworkHandler のイベントで更新される
			}
			return success;
		}

		public async UniTask DisconnectRoom()
		{
			Debug.Log("Network: ルームから非同期で切断中..."); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return;
			await _internalNetworkHandler.DisconnectRoom();
			IsHost = false;
			ConnectedList.Clear();
		}

		public async UniTask<List<string>> SearchRoom(string query = "")
		{
			Debug.Log($"Network: ルームを非同期で検索中... クエリ: '{query}'"); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return new List<string>();
			return await _internalNetworkHandler.SearchRoom(query);
		}
	}
}