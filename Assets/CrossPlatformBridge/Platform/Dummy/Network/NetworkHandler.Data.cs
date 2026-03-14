using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Platform.Dummy.Network
{
	/// <summary>
	/// IInternalNetworkHandler のダミー実装のデータ送受信部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class NetworkHandler : IInternalNetworkHandler
	{
		public async UniTask SendData(byte[] data, string targetId = null)
		{
			Debug.Log($"DummyNetworkHandler: データ送信シミュレート。サイズ: {data.Length} bytes, 宛先: {(targetId == null ? "全員" : targetId)}");
			await UniTask.Delay(50); // 送信のシミュレーション
			OnDataReceived?.Invoke(data, AccountId?.ToString() ?? ""); // 自分自身が受信したとシミュレート
			Debug.Log("DummyNetworkHandler: データ送信完了。");
		}
	}
}
