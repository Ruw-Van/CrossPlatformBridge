#if USE_CROSSPLATFORMBRIDGE_PHOTONFUSION
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using Fusion;
using UnityEngine;
using System;

namespace CrossPlatformBridge.Platform.PhotonFusion.Network
{
	/// <summary>
	/// PhotonFusion用のデータ送受信・内部イベントハンドラを実装するクラス。
	/// </summary>
	public partial class NetworkHandler : IInternalNetworkHandler
	{
		public UniTask SendData(byte[] data, string targetId = null)
		{
			if (_runner == null)
			{
				Debug.LogError("PhotonFusion: Runnerが初期化されていません");
				return UniTask.CompletedTask;
			}
			PlayerRef target = PlayerRef.None;
			if (!string.IsNullOrEmpty(targetId) && int.TryParse(targetId, out var id))
			{
				target = PlayerRef.FromIndex(id);
			}
			// RPCでデータ送信
			RpcSendData(data);
			Debug.Log($"PhotonFusion: SendData called, size={data.Length}, target={target}");
			return UniTask.CompletedTask;
		}

		// [Rpc]属性でデータ受信RPCを定義
		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RpcSendData(byte[] data, RpcInfo info = default)
		{
			Debug.Log($"PhotonFusion: RpcSendData received, size={data.Length}, sender={info.Source}");
			HandleReceivedData(data, info.Source.RawEncoded.ToString());
		}

		private void HandleReceivedData(byte[] data, string senderId = "")
		{
			Debug.Log($"PhotonFusion: HandleReceivedData called, size={data.Length}");
			// 必要に応じて受信データの処理を実装
			OnDataReceived?.Invoke(data, senderId);
		}

		private void HandlePlayerConnected(string playerId, string playerName)
		{
			Debug.Log($"PhotonFusion: HandlePlayerConnected called, id={playerId}, name={playerName}");
			OnPlayerConnected?.Invoke(playerId, playerName);
		}

		private void HandlePlayerDisconnected(string playerId, string playerName)
		{
			Debug.Log($"PhotonFusion: HandlePlayerDisconnected called, id={playerId}, name={playerName}");
			OnPlayerDisconnected?.Invoke(playerId, playerName);
		}
	}
}

#endif
