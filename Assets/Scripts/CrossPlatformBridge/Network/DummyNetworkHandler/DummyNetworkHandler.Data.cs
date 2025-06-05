// Assets/Scripts/CrossPlatformBridge/Network/DummyNetworkHandler/DummyNetworkHandler.Data.cs
using Cysharp.Threading.Tasks;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Network.DummyNetworkHandler
{
	/// <summary>
	/// IInternalNetworkHandler のダミー実装のデータ送受信部分。
	/// このクラスは partial で分割されています。
	/// </summary>
	public partial class DummyNetworkHandler : IInternalNetworkHandler
	{
		public async UniTask SendData(byte[] data, string targetId = null)
		{
			Debug.Log($"DummyNetworkHandler: データ送信シミュレート。サイズ: {data.Length} bytes, 宛先: {(targetId == null ? "全員" : targetId)}");
			await UniTask.Delay(50); // 送信のシミュレーション
			OnDataReceived?.Invoke(data); // 自分自身が受信したとシミュレート
			Debug.Log("DummyNetworkHandler: データ送信完了。");
		}
	}
}
