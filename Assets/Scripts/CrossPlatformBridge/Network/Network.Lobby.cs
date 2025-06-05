// Assets/Scripts/CrossPlatformBridge/Network/Network.Lobby.cs
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Network
{
	// Network クラスのロビー関連部分 // ★ コメント更新
	public partial class Network // ★ クラス名変更
	{
		// --------------------------------------------------------------------------------
		// ロビー機能
		// --------------------------------------------------------------------------------

		public async UniTask<bool> CreateLobby(string lobbyName)
		{
			Debug.Log($"Network: ロビー '{lobbyName}' を非同期で作成中..."); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return false;

			// 準備された設定を使用するか、デフォルト設定を生成
			INetworkSettings settingsToUse = _preparedSettings ?? _internalNetworkHandler.SettingsFactory.CreateSettings();

			return await _internalNetworkHandler.CreateLobby(lobbyName, settingsToUse);
		}

		public async UniTask<bool> ConnectLobby(string lobbyId)
		{
			Debug.Log($"Network: ロビー '{lobbyId}' に非同期で接続中..."); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return false;
			return await _internalNetworkHandler.ConnectLobby(lobbyId);
		}

		public async UniTask DisconnectLobby()
		{
			Debug.Log("Network: ロビーから非同期で切断中..."); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return;
			await _internalNetworkHandler.DisconnectLobby();
		}

		public async UniTask<List<string>> SearchLobby(string query = "")
		{
			Debug.Log($"Network: ロビーを非同期で検索中... クエリ: '{query}'"); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return new List<string>();
			return await _internalNetworkHandler.SearchLobby(query);
		}
	}
}