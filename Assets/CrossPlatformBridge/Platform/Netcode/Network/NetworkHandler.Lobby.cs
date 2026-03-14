#if USE_CROSSPLATFORMBRIDGE_NETCODE
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;

namespace CrossPlatformBridge.Platform.Netcode.Network
{
	/// <summary>
	/// Netcode for GameObjects ではロビー概念を持たないため、即時成功扱いで完了イベントを発火します。
	/// マルチプレイヤーセッション管理は Room メソッド（CreateRoom/ConnectRoom 等）を使用してください。
	/// </summary>
	public partial class NetworkHandler : IInternalNetworkHandler
	{
		public UniTask<bool> CreateLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			OnLobbyOperationCompleted?.Invoke("CreateLobby", true, "Netcode does not use Lobby. Use CreateRoom instead.");
			return UniTask.FromResult(true);
		}

		public UniTask<bool> ConnectLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			OnLobbyOperationCompleted?.Invoke("ConnectLobby", true, "Netcode does not use Lobby. Use ConnectRoom instead.");
			return UniTask.FromResult(true);
		}

		public UniTask DisconnectLobby()
		{
			OnLobbyOperationCompleted?.Invoke("DisconnectLobby", true, "Netcode does not use Lobby.");
			return UniTask.CompletedTask;
		}

		public UniTask<List<object>> SearchLobby(IRoomSettings baseSettings)
		{
			OnLobbyOperationCompleted?.Invoke("SearchLobby", true, "Netcode does not use Lobby. Use SearchRoom instead.");
			return UniTask.FromResult(new List<object>());
		}
	}
}

#endif
