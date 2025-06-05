// Assets/Scripts/CrossPlatformBridge/Services/NetcodeNetworkHandler/NetcodeNetworkHandler.cs
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;

namespace CrossPlatformBridge.Network.NetcodeNetworkHandler
{
	/// <summary>
	/// Unity Netcode for GameObjects を使用した IInternalNetworkHandler の実装。
	/// Unity Gaming Services (Lobby, Relay, Authentication) と連携します。
	/// </summary>
	public partial class NetcodeNetworkHandler : IInternalNetworkHandler
	{
		public async UniTask SendData(byte[] data, string targetId = null)
		{
			// Netcode for GameObjects では、RPC (Remote Procedure Call) または NetworkVariable を使ってデータを同期します。
			// ここでは簡単な例として、すべてのクライアント/サーバーにRPCを送信するダミーを示します。
			// 実際には、NetworkObjectを持つ特定のオブジェクトに対してRpcMessageを送信することになります。

			if (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsServer)
			{
				Debug.LogWarning("NetcodeNetworkHandler: 接続されていません。データ送信できません。");
				return;
			}

			// TODO: 汎用的なデータ送信レイヤーが必要な場合は、NetworkBehaviourを継承した独自のメッセージングシステムを構築します。
			// 例: 特定の NetworkObject にアタッチされたコンポーネント経由でRPCを呼び出す
			// NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<MyNetworkComponent>().MyRpcMethodClientRpc(data);
			Debug.Log($"NetcodeNetworkHandler: データ送信シミュレート (Netcode RPC経由)。サイズ: {data.Length} bytes, 宛先: {(targetId == null ? "全員" : targetId)}");

			// ダミーとして、送信されたデータを内部で受信イベントとして発生させる（自己送信のシミュレーション）
			OnDataReceived?.Invoke(data);

			await UniTask.Yield(); // 非同期メソッドなのでUniTaskを返す
		}
	}
}
