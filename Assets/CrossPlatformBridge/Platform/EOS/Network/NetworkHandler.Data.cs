#if USE_CROSSPLATFORMBRIDGE_EOS
using System;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Network
{
	/// <summary>
	/// EOS P2PInterface を使ったデータ送受信を実装するクラス。
	/// 受信は UpdateState() 内でポーリングする。
	/// </summary>
	public partial class NetworkHandler
	{
		private const byte ChannelId = 0;
		private const string SocketName = "CrossPlatformBridge";

		public UniTask SendData(byte[] data, string targetId = null)
		{
			if (_platform == null || _productUserId == null)
			{
				Debug.LogError("EOS: Platform または ProductUserId が未初期化です");
				return UniTask.CompletedTask;
			}

			var p2p = _platform.GetP2PInterface();
			var socketId = new SocketId { SocketName = SocketName };

			if (!string.IsNullOrEmpty(targetId))
			{
				// 特定の相手に送信
				var target = ProductUserId.FromString(targetId);
				if (target == null || !target.IsValid())
				{
					Debug.LogWarning($"EOS: 無効な targetId={targetId}");
					return UniTask.CompletedTask;
				}

				var options = new SendPacketOptions
				{
					LocalUserId = _productUserId,
					RemoteUserId = target,
					SocketId = socketId,
					Channel = ChannelId,
					Data = new System.ArraySegment<byte>(data),
					AllowDelayedDelivery = true,
					Reliability = PacketReliability.ReliableOrdered,
				};
				var result = p2p.SendPacket(ref options);
				Debug.Log($"EOS: SendPacket to={targetId} size={data.Length} result={result}");
			}
			else
			{
				// 接続中の全員に送信
				foreach (var player in ConnectedList)
				{
					var target = ProductUserId.FromString(player.Id);
					if (target == null || !target.IsValid()) continue;

					var options = new SendPacketOptions
					{
						LocalUserId = _productUserId,
						RemoteUserId = target,
						SocketId = socketId,
						Channel = ChannelId,
						Data = new System.ArraySegment<byte>(data),
						AllowDelayedDelivery = true,
						Reliability = PacketReliability.ReliableOrdered,
					};
					p2p.SendPacket(ref options);
				}
				Debug.Log($"EOS: SendPacket broadcast size={data.Length} to {ConnectedList.Count} players");
			}

			return UniTask.CompletedTask;
		}

		/// <summary>
		/// UpdateState() から毎フレーム呼ばれる受信ポーリング。
		/// </summary>
		private void PollIncomingPackets()
		{
			if (_platform == null || _productUserId == null) return;

			var p2p = _platform.GetP2PInterface();

			while (true)
			{
				var sizeOptions = new GetNextReceivedPacketSizeOptions
				{
					LocalUserId = _productUserId,
					RequestedChannel = ChannelId,
				};
				if (p2p.GetNextReceivedPacketSize(ref sizeOptions, out uint packetSize) != Result.Success) break;

				var buffer = new byte[packetSize];
				var outData = new ArraySegment<byte>(buffer);
				ProductUserId outPeerId = null;
				var outSocketId = new SocketId();
				var receiveOptions = new ReceivePacketOptions
				{
					LocalUserId = _productUserId,
					MaxDataSizeBytes = packetSize,
					RequestedChannel = ChannelId,
				};
				var receiveResult = p2p.ReceivePacket(ref receiveOptions, ref outPeerId, ref outSocketId, out byte _, outData, out uint outBytesWritten);
				if (receiveResult == Result.Success)
				{
					Debug.Log($"EOS: ReceivePacket size={outBytesWritten}");
					OnDataReceived?.Invoke(buffer[..(int)outBytesWritten], outPeerId?.ToString() ?? "");
				}
				else
				{
					break;
				}
			}
		}
	}
}

#endif
