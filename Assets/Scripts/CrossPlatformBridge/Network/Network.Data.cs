// Assets/Scripts/CrossPlatformBridge/Network/Network.Data.cs
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System; // Action のために必要

namespace CrossPlatformBridge.Network
{
	// Network クラスのデータ送受信および内部イベントハンドラ部分 // ★ コメント更新
	public partial class Network // ★ クラス名変更
	{
		// --------------------------------------------------------------------------------
		// データ送受信
		// --------------------------------------------------------------------------------

		public async UniTask SendData(byte[] data, string targetId = null)
		{
			Debug.Log($"Network: データ送信中... サイズ: {data.Length} bytes, 宛先: {(targetId == null ? "全員" : targetId)}"); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return;
			await _internalNetworkHandler.SendData(data, targetId);
		}

		// --------------------------------------------------------------------------------
		// 内部イベントハンドラ (IInternalNetworkHandlerからの通知を受け取る)
		// --------------------------------------------------------------------------------

		private void HandleReceivedData(byte[] data)
		{
			Debug.Log($"Network: 内部からデータを受信しました。サイズ: {data.Length} bytes"); // ★ Debug.Log のメッセージ変更
																			// ここで受信データをアプリケーション層に渡すための処理を実装します。
																			// 例: イベント発行、メッセージキューへの追加など
																			// 例えば、GameManager.Instance.ProcessNetworkMessage(data); のように呼び出す
			OnDataReceived?.Invoke(data); // 公開イベントを発行
		}

		private void HandlePlayerConnected(string playerId, string playerName)
		{
			Debug.Log($"Network: プレイヤーが接続しました。ID: {playerId}, 名前: {playerName}"); // ★ Debug.Log のメッセージ変更
			if (!ConnectedList.Contains(playerName))
			{
				ConnectedList.Add(playerName);
			}
			DisconnectedList.Remove(playerName); // 切断リストから削除
			OnPlayerConnected?.Invoke(playerId, playerName); // 公開イベントを発行
		}

		private void HandlePlayerDisconnected(string playerId, string playerName)
		{
			Debug.Log($"Network: プレイヤーが切断しました。ID: {playerId}, 名前: {playerName}"); // ★ Debug.Log のメッセージ変更
			ConnectedList.Remove(playerName);
			if (!DisconnectedList.Contains(playerName))
			{
				DisconnectedList.Add(playerName);
			}
			OnPlayerDisconnected?.Invoke(playerId, playerName); // 公開イベントを発行
		}
	}
}