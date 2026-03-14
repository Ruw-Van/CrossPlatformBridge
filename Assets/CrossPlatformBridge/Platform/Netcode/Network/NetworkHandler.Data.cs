#if USE_CROSSPLATFORMBRIDGE_NETCODE
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using CrossPlatformBridge.Services.Network;

namespace CrossPlatformBridge.Platform.Netcode.Network
{
	public partial class NetworkHandler : IInternalNetworkHandler
	{
		private const string MessageIdentifier = "CPB_Data";

		/// <summary>
		/// データを送信します。
		/// ホスト: 自分以外の全クライアントへ直接送信し、自分自身はローカル発火。
		/// クライアント: サーバーへ送信し、サーバー側でリレーします。
		/// プロトコル: [senderId: string] [targetId: string (空 = ブロードキャスト)] [dataLength: int] [data: bytes]
		/// </summary>
		public UniTask SendData(byte[] data, string targetId = null)
		{
			if (NetworkManager.Singleton == null
				|| (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsServer))
			{
				Debug.LogWarning("NetcodeNetworkHandler: 接続されていません。データ送信できません。");
				return UniTask.CompletedTask;
			}

			string senderIdStr = AccountId?.ToString() ?? "";
			string targetIdStr = targetId ?? "";
			// senderId + targetId それぞれ文字数×2 + ヘッダー4バイト、data長、data本体、余白
			int bufferSize = 4 + senderIdStr.Length * 2 + 4 + targetIdStr.Length * 2 + sizeof(int) + data.Length + 64;

			using var writer = new FastBufferWriter(bufferSize, Allocator.Temp);
			writer.WriteValueSafe(senderIdStr);
			writer.WriteValueSafe(targetIdStr);
			writer.WriteValueSafe(data.Length);
			writer.WriteBytesSafe(data, data.Length);

			if (NetworkManager.Singleton.IsServer)
			{
				if (string.IsNullOrEmpty(targetId))
				{
					// ホスト: 自分以外の全クライアントへ送信
					// SendNamedMessageToAll を使うとホスト自身も宛先に含まれ
					// OnDataMessageReceived が再トリガーされてリレーループが発生するため、
					// 明示的にホストの clientId を除いたループで送信する
					foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
					{
						if (clientId != NetworkManager.Singleton.LocalClientId)
						{
							NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
								MessageIdentifier, clientId, writer, NetworkDelivery.ReliableSequenced);
						}
					}
					// ホスト自身はローカル発火
					OnDataReceived?.Invoke(data, senderIdStr);
				}
				else
				{
					// ホスト: 特定クライアントへ送信
					if (ulong.TryParse(targetId, out ulong targetClientId))
					{
						// 送信先がホスト自身の場合はローカル発火
						if (targetClientId == NetworkManager.Singleton.LocalClientId)
						{
							OnDataReceived?.Invoke(data, senderIdStr);
						}
						else
						{
							NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
								MessageIdentifier, targetClientId, writer, NetworkDelivery.ReliableSequenced);
						}
					}
					else
					{
						Debug.LogError($"NetcodeNetworkHandler: 無効な targetId '{targetId}'。送信をスキップします。");
					}
				}
			}
			else
			{
				// クライアント: サーバーへ転送依頼（サーバー側でリレー）
				NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
					MessageIdentifier, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
				// クライアント自身もローカル発火（送信者が自分のデータも受け取れるようにする）
				OnDataReceived?.Invoke(data, senderIdStr);
			}

			return UniTask.CompletedTask;
		}

		// --------------------------------------------------------------------------------
		// CustomMessagingManager ハンドラー登録 / 解除
		// --------------------------------------------------------------------------------

		private void RegisterDataMessageHandler()
		{
			if (NetworkManager.Singleton?.CustomMessagingManager == null) return;
			NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
				MessageIdentifier, OnDataMessageReceived);
			Debug.Log("NetcodeNetworkHandler: データメッセージハンドラーを登録しました。");
		}

		private void UnregisterDataMessageHandler()
		{
			if (NetworkManager.Singleton?.CustomMessagingManager == null) return;
			NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(MessageIdentifier);
			Debug.Log("NetcodeNetworkHandler: データメッセージハンドラーを解除しました。");
		}

		/// <summary>
		/// ネットワークメッセージ受信ハンドラー。
		/// サーバー側では、クライアントからのメッセージを他のクライアントへリレーします。
		/// クライアント側では、受信データを OnDataReceived で通知します。
		/// </summary>
		private void OnDataMessageReceived(ulong senderClientId, FastBufferReader reader)
		{
			reader.ReadValueSafe(out string senderIdStr);
			reader.ReadValueSafe(out string targetIdStr);
			reader.ReadValueSafe(out int length);
			byte[] data = new byte[length];
			reader.ReadBytesSafe(ref data, length);

			if (NetworkManager.Singleton.IsServer)
			{
				// サーバー側リレー処理
				if (string.IsNullOrEmpty(targetIdStr))
				{
					// ブロードキャスト: 送信元とホスト自身を除いた全クライアントへ転送
					// ホスト自身への SendNamedMessage はハンドラーを再トリガーするため
					// LocalClientId を除外してホスト受信はローカル発火で処理する
					int bufferSize = 4 + senderIdStr.Length * 2 + 4 + sizeof(int) + data.Length + 64;
					using var writer = new FastBufferWriter(bufferSize, Allocator.Temp);
					writer.WriteValueSafe(senderIdStr);
					writer.WriteValueSafe("");
					writer.WriteValueSafe(data.Length);
					writer.WriteBytesSafe(data, data.Length);

					foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
					{
						if (clientId != senderClientId && clientId != NetworkManager.Singleton.LocalClientId)
						{
							NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
								MessageIdentifier, clientId, writer, NetworkDelivery.ReliableSequenced);
						}
					}
					// ホスト自身も受信者（ローカル発火でリレーループを防ぐ）
					if (NetworkManager.Singleton.IsHost)
					{
						OnDataReceived?.Invoke(data, senderIdStr);
					}
				}
				else if (ulong.TryParse(targetIdStr, out ulong targetClientId))
				{
					// ユニキャスト: 特定クライアントへ転送
					if (targetClientId == NetworkManager.Singleton.LocalClientId)
					{
						// ホスト宛はローカル発火
						OnDataReceived?.Invoke(data, senderIdStr);
					}
					else
					{
						int bufferSize = 4 + senderIdStr.Length * 2 + 4 + targetIdStr.Length * 2 + sizeof(int) + data.Length + 64;
						using var writer = new FastBufferWriter(bufferSize, Allocator.Temp);
						writer.WriteValueSafe(senderIdStr);
						writer.WriteValueSafe("");
						writer.WriteValueSafe(data.Length);
						writer.WriteBytesSafe(data, data.Length);
						NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
							MessageIdentifier, targetClientId, writer, NetworkDelivery.ReliableSequenced);
					}
				}
			}
			else
			{
				// クライアント側: 受信データを通知
				OnDataReceived?.Invoke(data, senderIdStr);
			}
		}
	}
}

#endif
