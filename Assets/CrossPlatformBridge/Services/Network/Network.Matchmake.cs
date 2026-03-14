using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Services.Network
{
	/// <summary>
	/// マッチメイキング関連のネットワーク操作を提供するNetworkクラスの部分クラス。
	/// </summary>
	public partial class Network
	{
		// --------------------------------------------------------------------------------
		// マッチメイキング機能
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 条件に合うロビーを検索して接続します（マッチメイキング）。
		/// </summary>
		/// <param name="conditions">検索条件（RoomName・CustomProperties など）</param>
		/// <param name="cancellationToken">操作をキャンセルするためのトークン（省略可）</param>
		/// <returns>接続に成功した場合は true、失敗またはキャンセル時は false</returns>
		public async UniTask<bool> MatchmakeLobby(IRoomSettings conditions, CancellationToken cancellationToken = default)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return false;
			}
			_currentOperationStatus = NetworkOperationStatus.Matchmaking;
			_operationCts = new CancellationTokenSource();
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_operationCts.Token, cancellationToken);

			try
			{
				Debug.Log($"Network: ロビーをマッチメイキング中... クエリ: '{conditions.RoomName}'");
				if (_internalNetworkHandler == null) return false;
				return await _internalNetworkHandler.MatchmakeLobby(conditions, linkedCts.Token);
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("Network: ロビーマッチメイキングがキャンセルされました。");
				return false;
			}
			finally
			{
				_operationCts?.Dispose();
				_operationCts = null;
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
		}

		/// <summary>
		/// 条件に合うルームを検索して接続します。
		/// createIfNotFound が true の場合、見つからなければ conditions でルームを作成します。
		/// </summary>
		/// <param name="conditions">検索条件（RoomName・MaxPlayers・CustomProperties など）</param>
		/// <param name="createIfNotFound">true の場合、マッチするルームがなければ新規作成する</param>
		/// <param name="cancellationToken">操作をキャンセルするためのトークン（省略可）</param>
		/// <returns>接続または作成に成功した場合は true、失敗またはキャンセル時は false</returns>
		public async UniTask<bool> MatchmakeRoom(IRoomSettings conditions, bool createIfNotFound = false, CancellationToken cancellationToken = default)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return false;
			}
			_currentOperationStatus = NetworkOperationStatus.Matchmaking;
			_operationCts = new CancellationTokenSource();
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_operationCts.Token, cancellationToken);

			try
			{
				Debug.Log($"Network: ルームをマッチメイキング中... クエリ: '{conditions.RoomName}'");
				if (_internalNetworkHandler == null) return false;
				return await _internalNetworkHandler.MatchmakeRoom(conditions, createIfNotFound, linkedCts.Token);
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("Network: ルームマッチメイキングがキャンセルされました。");
				return false;
			}
			finally
			{
				_operationCts?.Dispose();
				_operationCts = null;
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
		}
	}
}
