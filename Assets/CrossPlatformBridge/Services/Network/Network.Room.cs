using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Services.Network
{
	/// <summary>
	/// ルーム関連のネットワーク操作を提供するNetworkクラスの部分クラス。
	/// </summary>
	public partial class Network
	{
		// --------------------------------------------------------------------------------
		// ルーム機能
		// --------------------------------------------------------------------------------

		/// <summary>
		/// ルームを作成します。
		/// </summary>
		/// <param name="baseSettings">ルーム設定</param>
		/// <param name="cancellationToken">操作をキャンセルするためのトークン（省略可）</param>
		/// <returns>作成に成功した場合はtrue、失敗またはキャンセル時はfalse</returns>
		public async UniTask<bool> CreateRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return false;
			}
			_currentOperationStatus = NetworkOperationStatus.CreatingRoom;
			_operationCts = new CancellationTokenSource();
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_operationCts.Token, cancellationToken);

			try
			{
				if (_internalNetworkHandler == null) return false;
				return await _internalNetworkHandler.CreateRoom(baseSettings, linkedCts.Token);
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("Network: ルーム作成がキャンセルされました。");
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
		/// ルームに接続します。
		/// </summary>
		/// <param name="baseSettings">ルーム設定</param>
		/// <param name="cancellationToken">操作をキャンセルするためのトークン（省略可）</param>
		/// <returns>接続に成功した場合はtrue、失敗またはキャンセル時はfalse</returns>
		public async UniTask<bool> ConnectRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return false;
			}
			_currentOperationStatus = NetworkOperationStatus.ConnectingRoom;
			_operationCts = new CancellationTokenSource();
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_operationCts.Token, cancellationToken);

			try
			{
				Debug.Log($"Network: ルーム '{baseSettings.RoomName}' に非同期で接続中...");
				if (_internalNetworkHandler == null) return false;

				bool success = await _internalNetworkHandler.ConnectRoom(baseSettings, linkedCts.Token);
				if (success)
				{
					// IsHost は IInternalNetworkHandler のイベントで更新される
				}
				return success;
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("Network: ルーム接続がキャンセルされました。");
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
		/// ルームから切断します。
		/// </summary>
		public async UniTask DisconnectRoom()
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return;
			}
			_currentOperationStatus = NetworkOperationStatus.DisconnectingRoom;

			try
			{
				Debug.Log("Network: ルームから非同期で切断中...");
				if (_internalNetworkHandler == null) return;
				await _internalNetworkHandler.DisconnectRoom();
				//ConnectedList.Clear();
			}
			finally
			{
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
		}

		/// <summary>
		/// ルームを検索します。
		/// </summary>
		/// <param name="baseSettings">検索条件</param>
		/// <returns>ルーム情報リスト</returns>
		public async UniTask<List<object>> SearchRoom(IRoomSettings baseSettings)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return null;
			}
			_currentOperationStatus = NetworkOperationStatus.SearchingRoom;

			try
			{
				Debug.Log($"Network: ルームを非同期で検索中... クエリ: '{baseSettings.RoomName}'");
				if (_internalNetworkHandler == null) return new List<object>();
				return await _internalNetworkHandler.SearchRoom(baseSettings);
			}
			finally
			{
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
		}
	}
}
