using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Services.Network
{
	/// <summary>
	/// ロビー関連のネットワーク操作を提供するNetworkクラスの部分クラス。
	/// </summary>
	public partial class Network
	{
		// --------------------------------------------------------------------------------
		// ロビー機能
		// --------------------------------------------------------------------------------

		/// <summary>
		/// ロビーを作成します。
		/// </summary>
		/// <param name="baseSettings">ロビー設定</param>
		/// <param name="cancellationToken">操作をキャンセルするためのトークン（省略可）</param>
		/// <returns>作成に成功した場合はtrue、失敗またはキャンセル時はfalse</returns>
		public async UniTask<bool> CreateLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return false;
			}
			_currentOperationStatus = NetworkOperationStatus.CreatingLobby;
			_operationCts = new CancellationTokenSource();
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_operationCts.Token, cancellationToken);

			try
			{
				Debug.Log($"Network: ロビー '{baseSettings.RoomName}' を非同期で作成中...");
				if (_internalNetworkHandler == null) return false;
				return await _internalNetworkHandler.CreateLobby(baseSettings, linkedCts.Token);
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("Network: ロビー作成がキャンセルされました。");
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
		/// ロビーに接続します。
		/// </summary>
		/// <param name="baseSettings">ロビー設定</param>
		/// <param name="cancellationToken">操作をキャンセルするためのトークン（省略可）</param>
		/// <returns>接続に成功した場合はtrue、失敗またはキャンセル時はfalse</returns>
		public async UniTask<bool> ConnectLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return false;
			}
			_currentOperationStatus = NetworkOperationStatus.ConnectingLobby;
			_operationCts = new CancellationTokenSource();
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_operationCts.Token, cancellationToken);

			try
			{
				Debug.Log($"Network: ロビー '{baseSettings.RoomName}' に非同期で接続中...");
				if (_internalNetworkHandler == null) return false;
				return await _internalNetworkHandler.ConnectLobby(baseSettings, linkedCts.Token);
			}
			catch (System.OperationCanceledException)
			{
				Debug.Log("Network: ロビー接続がキャンセルされました。");
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
		/// ロビーから切断します。
		/// </summary>
		public async UniTask DisconnectLobby()
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return;
			}
			_currentOperationStatus = NetworkOperationStatus.DisconnectingLobby;
			try
			{
				Debug.Log("Network: ロビーから非同期で切断中...");
				if (_internalNetworkHandler == null) return;
				await _internalNetworkHandler.DisconnectLobby();

			}
			finally
			{
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
		}

		/// <summary>
		/// ロビーを検索します。
		/// </summary>
		/// <param name="baseSettings">検索条件</param>
		/// <returns>ロビー情報リスト</returns>
		public async UniTask<List<object>> SearchLobby(IRoomSettings baseSettings)
		{
			if (_currentOperationStatus != NetworkOperationStatus.Idle && _currentOperationStatus != NetworkOperationStatus.ShuttingDown)
			{
				Debug.LogWarning($"Network: 現在 '{_currentOperationStatus}' のため、開始できません。");
				return null;
			}
			_currentOperationStatus = NetworkOperationStatus.SearchingLobby;

			try
			{
				Debug.Log($"Network: ロビーを非同期で検索中... クエリ: '{baseSettings.RoomName}'");
				if (_internalNetworkHandler == null) return new List<object>();
				return await _internalNetworkHandler.SearchLobby(baseSettings);
			}
			finally
			{
				_currentOperationStatus = NetworkOperationStatus.Idle;
			}
		}
	}
}
