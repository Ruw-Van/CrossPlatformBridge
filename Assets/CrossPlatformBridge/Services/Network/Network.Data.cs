using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using System.Linq; // Action のために必要

namespace CrossPlatformBridge.Services.Network
{
	/// <summary>
	/// データ送受信および内部イベントハンドラを提供するNetworkクラスの部分クラス。
	/// </summary>
	public partial class Network // ★ クラス名変更
	{
		// --------------------------------------------------------------------------------
		// データ送受信
		// --------------------------------------------------------------------------------

		/// <summary>
		/// データを送信します。
		/// </summary>
		/// <param name="data">送信データ</param>
		/// <param name="targetId">宛先ID（省略時は全員）</param>
		public async UniTask SendData(byte[] data, string targetId = null)
		{
			if (data == null)
			{
				Debug.LogWarning("Network: null のデータは送信できません。");
				return;
			}

			Debug.Log($"Network: データ送信中... サイズ: {data.Length} bytes, 宛先: {(targetId == null ? "全員" : targetId)}"); // ★ Debug.Log のメッセージ変更
			if (_internalNetworkHandler == null) return;
			await _internalNetworkHandler.SendData(data, targetId);
		}

		// --------------------------------------------------------------------------------
		// 内部イベントハンドラ (IInternalNetworkHandlerからの通知を受け取る)
		// --------------------------------------------------------------------------------

		/// <summary>
		/// データ受信時の内部処理。
		/// </summary>
		/// <param name="data">受信データ</param>
		private void HandleReceivedData(byte[] data, string senderId)
		{
			if (data == null)
			{
				Debug.LogWarning($"Network: null データを受信しました。送信者: {senderId}");
				return;
			}

			Debug.Log($"Network: 内部からデータを受信しました。サイズ: {data.Length} bytes, 送信者: {senderId}");
			OnDataReceived?.Invoke(data, senderId); // 公開イベントを発行
		}

		/// <summary>
		/// プレイヤー接続時の内部処理。
		/// </summary>
		/// <param name="playerId">プレイヤーID</param>
		/// <param name="playerName">プレイヤー名</param>
		private void HandlePlayerConnected(string playerId, string playerName)
		{
			Debug.Log($"Network: プレイヤーが接続しました。ID: {playerId}, 名前: {playerName}"); // ★ Debug.Log のメッセージ変更
			if (ConnectedList != null && !ConnectedList.Any(x => x.Id == playerId))
			{
				ConnectedList.Add(new PlayerData() { Id = playerId, Name = playerName, PlayerProperties = new()});
			}
			DisconnectedList?.RemoveAll(x => x.Id == playerId); // 切断リストから削除
			OnPlayerConnected?.Invoke(playerId, playerName); // 公開イベントを発行
		}

		/// <summary>
		/// プレイヤー切断時の内部処理。
		/// </summary>
		/// <param name="playerId">プレイヤーID</param>
		/// <param name="playerName">プレイヤー名</param>
		private void HandlePlayerDisconnected(string playerId, string playerName)
		{
			Debug.Log($"Network: プレイヤーが切断しました。ID: {playerId}, 名前: {playerName}"); // ★ Debug.Log のメッセージ変更
			ConnectedList?.RemoveAll(x => x.Id == playerId);
			if (DisconnectedList != null && !DisconnectedList.Any(x => x.Id == playerId))
			{
				DisconnectedList.Add(new PlayerData() { Id = playerId, Name = playerName, PlayerProperties = new() });
			}
			OnPlayerDisconnected?.Invoke(playerId, playerName); // 公開イベントを発行
		}
	}
}
