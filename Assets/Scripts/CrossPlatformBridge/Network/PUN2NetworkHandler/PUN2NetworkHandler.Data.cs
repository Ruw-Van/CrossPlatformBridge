// Assets/Scripts/CrossPlatformBridge/Network/PUN2NetworkHandler/PUN2NetworkHandler.Data.cs
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon; // For RaiseEventOptions, EventData

namespace CrossPlatformBridge.Network.PUN2NetworkHandler
{
	/// <summary>
	/// Photon Unity Networking 2 (PUN2) を使用した IInternalNetworkHandler の実装のデータ送受信部分。
	/// IOnEventCallback を実装し、カスタムイベントを処理します。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class PUN2NetworkHandler : MonoBehaviourPunCallbacks, IInternalNetworkHandler, IOnEventCallback
	{
		// --------------------------------------------------------------------------------
		// 内部定数
		// --------------------------------------------------------------------------------
		private const byte DATA_EVENT_CODE = 1; // カスタムデータ送信用のイベントコード

		// --------------------------------------------------------------------------------
		// IInternalNetworkHandler インターフェース実装 - データ送受信
		// --------------------------------------------------------------------------------

		/// <summary>
		/// ネットワークを通じてデータを送信します。
		/// </summary>
		/// <param name="data">送信するバイト配列データ。</param>
		/// <param name="targetId">送信先のID（オプション、指定しない場合は全員に送信）。</param>
		public async UniTask SendData(byte[] data, string targetId = null)
		{
			if (!PhotonNetwork.IsConnectedAndReady)
			{
				Debug.LogWarning("PUN2NetworkHandler: Photon に接続されていません。データ送信できません。");
				return;
			}

			if (!PhotonNetwork.InRoom)
			{
				Debug.LogWarning("PUN2NetworkHandler: ルームに参加していません。データ送信できません。");
				return;
			}

			RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
			if (targetId == null)
			{
				raiseEventOptions.Receivers = ReceiverGroup.Others; // 自分以外に送信
			}
			else
			{
				// 特定のプレイヤーに送信する場合
				// Player オブジェクトを UserId で見つける必要がある
				Player targetPlayer = null;
				foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
				{
					if (player.UserId == targetId)
					{
						targetPlayer = player;
						break;
					}
				}

				if (targetPlayer != null)
				{
					raiseEventOptions.TargetActors = new int[] { targetPlayer.ActorNumber };
				}
				else
				{
					Debug.LogWarning($"PUN2NetworkHandler: ターゲットプレイヤー (ID: {targetId}) が見つかりませんでした。データは送信されません。");
					return;
				}
			}

			SendOptions sendOptions = new SendOptions { Reliability = true }; // 信頼性のある送信

			PhotonNetwork.RaiseEvent(DATA_EVENT_CODE, data, raiseEventOptions, sendOptions);

			Debug.Log($"PUN2NetworkHandler: データ送信 (Photon RaiseEvent経由)。サイズ: {data.Length} bytes, 宛先: {(targetId == null ? "全員" : targetId)}");
			await UniTask.Yield(); // 非同期メソッドなのでUniTaskを返す
		}

		// --------------------------------------------------------------------------------
		// IOnEventCallback (カスタムイベント受信ハンドラ)
		// --------------------------------------------------------------------------------

		/// <summary>
		/// カスタムイベントを受信した際に呼び出されます。
		/// </summary>
		/// <param name="photonEvent">受信したイベントデータ。</param>
		public void OnEvent(ExitGames.Client.Photon.EventData photonEvent) // ExitGames.Client.Photon.EventData を明示
		{
			if (photonEvent.Code == DATA_EVENT_CODE)
			{
				byte[] data = photonEvent.CustomData as byte[];
				if (data != null)
				{
					Debug.Log($"PUN2NetworkHandler.OnEvent: カスタムデータ ({DATA_EVENT_CODE}) を受信しました。サイズ: {data.Length} bytes");
					OnDataReceived?.Invoke(data); // 受信イベントを発火
				}
			}
		}
	}
}
