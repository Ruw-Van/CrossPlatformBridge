// Assets/Scripts/CrossPlatformBridge/Network/IInternalNetworkHandler.cs
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks; // UniTask を使用するために必要

namespace CrossPlatformBridge.Network
{
	/// <summary>
	/// 内部のプラットフォーム固有のネットワーク処理を抽象化するためのインターフェース。
	/// NetworkManagerはこのインターフェースを通じて、具体的なネットワーク実装と通信します。
	/// すべてのネットワーク操作は非同期 (UniTask) で実行されます。
	/// </summary>
	public interface IInternalNetworkHandler
	{
		// --------------------------------------------------------------------------------
		// イベント (内部からNetworkへ通知するため)
		// --------------------------------------------------------------------------------

		/// <summary>データを受信した際に発生するイベント。</summary>
		event Action<byte[]> OnDataReceived;

		/// <summary>新しいプレイヤーが接続した際に発生するイベント。引数はプレイヤーIDとプレイヤー名。</summary>
		event Action<string, string> OnPlayerConnected;

		/// <summary>プレイヤーが切断した際に発生するイベント。引数はプレイヤーIDとプレイヤー名。</summary>
		event Action<string, string> OnPlayerDisconnected;

		/// <summary>ネットワーク接続状態が変更された際に発生するイベント。引数は新しい接続状態。</summary>
		event Action<bool> OnNetworkConnectionStatusChanged;

		/// <summary>ホスト状態が変更された際に発生するイベント。引数は新しいホスト状態。</summary>
		event Action<bool> OnHostStatusChanged;

		/// <summary>ロビーの作成、接続、切断、検索などの結果が返ってきた際に発生するイベント。</summary>
		event Action<string, bool, string> OnLobbyOperationCompleted;

		/// <summary>ルームの作成、接続、切断、検索などの結果が返ってきた際に発生するイベント。</summary>
		event Action<string, bool, string> OnRoomOperationCompleted;

		// --------------------------------------------------------------------------------
		// 関数 (Networkから内部へ呼び出すため - 非同期 UniTask)
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 内部ネットワークライブラリを非同期で初期化します。
		/// </summary>
		/// <returns>初期化が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> Initialize();

		/// <summary>
		/// 内部ネットワークライブラリを非同期で終了します。
		/// </summary>
		UniTask Shutdown();

		/// <summary>
		/// 内部ネットワークサービスに非同期で接続します。
		/// </summary>
		/// <param name="userId">接続に使用するユーザーID。</param>
		/// <param name="userName">接続に使用するユーザー名。</param>
		/// <returns>接続が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> Connect(string userId, string userName);

		/// <summary>
		/// 内部ネットワークサービスから非同期で切断します。
		/// </summary>
		UniTask Disconnect();

		/// <summary>
		/// 新しいロビーを内部で非同期で作成します。
		/// </summary>
		/// <param name="lobbyName">作成するロビーの名前。</param>
		/// <returns>ロビー作成が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> CreateLobby(string lobbyName, INetworkSettings settings);

		/// <summary>
		/// 既存のロビーに内部で非同期で接続します。
		/// </summary>
		/// <param name="lobbyId">接続するロビーのID。</param>
		/// <returns>ロビー接続が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> ConnectLobby(string lobbyId);

		/// <summary>
		/// 内部でロビーから非同期で切断します。
		/// </summary>
		UniTask DisconnectLobby();

		/// <summary>
		/// 利用可能なロビーを内部で非同期で検索します。
		/// </summary>
		/// <param name="query">検索クエリ（オプション）。</param>
		/// <returns>検索結果のロビーIDのリスト。</returns>
		UniTask<List<string>> SearchLobby(string query = "");

		/// <summary>
		/// 新しいルームを内部で非同期で作成します。
		/// </summary>
		/// <param name="roomName">作成するルームの名前。</param>
		/// <param name="maxPlayers">最大プレイヤー数。</param>
		/// <returns>ルーム作成が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> CreateRoom(string roomName, INetworkSettings baseSettings);

		/// <summary>
		/// 既存のルームに内部で非同期で接続します。
		/// </summary>
		/// <param name="roomId">接続するルームのID。</param>
		/// <returns>ルーム接続が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> ConnectRoom(string roomId);

		/// <summary>
		/// 内部でルームから非同期で切断します。
		/// </summary>
		UniTask DisconnectRoom();

		/// <summary>
		/// 利用可能なルームを内部で非同期で検索します。
		/// </summary>
		/// <param name="query">検索クエリ（オプション）。</param>
		/// <returns>検索結果のルームIDのリスト。</returns>
		UniTask<List<string>> SearchRoom(string query = "");

		/// <summary>
		/// ネットワークを通じてデータを非同期で送信します。
		/// </summary>
		/// <param name="data">送信するバイト配列データ。</param>
		/// <param name="targetId">送信先のID（オプション、指定しない場合は全員に送信）。</param>
		UniTask SendData(byte[] data, string targetId = null);

		/// <summary>
		/// 内部のネットワーク状態を更新します。
		/// (例: ポーリングベースの場合に定期的に呼び出す)
		/// </summary>
		void UpdateState(); // UniTask にはしない。定期的に呼び出される同期的処理のため

		// ★ 追加: AccountId, NickName, StationId のプロパティ
		string AccountId { get; }
		string NickName { get; }
		string StationId { get; }

		/// <summary>
		/// このハンドラが提供するINetworkRoomSettingsのファクトリーを取得します。
		/// </summary>
		INetworkSettingsFactory SettingsFactory { get; } // ★ 追加
	}
}
