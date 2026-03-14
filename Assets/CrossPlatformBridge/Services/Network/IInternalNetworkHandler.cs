using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks; // UniTask を使用するために必要

namespace CrossPlatformBridge.Services.Network
{
	/// <summary>
	/// 内部のプラットフォーム固有のネットワーク処理を抽象化するためのインターフェース。
	/// NetworkManagerはこのインターフェースを通じて、具体的なネットワーク実装と通信します。
	/// 非同期操作は UniTask を使用します。Initialize / Shutdown / UpdateState は同期で実行されます。
	/// </summary>
	public interface IInternalNetworkHandler
	{
		// --------------------------------------------------------------------------------
		// イベント (内部からNetworkへ通知するため)
		// --------------------------------------------------------------------------------

		/// <summary>データを受信した際に発生するイベント。第1引数はデータ、第2引数は送信者ID。</summary>
		event Action<byte[], string> OnDataReceived;

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
		/// 内部ネットワークライブラリを初期化します。
		/// </summary>
		/// <returns>初期化が成功した場合は true、それ以外は false。</returns>
		bool Initialize(INetworkSettings baseSettings);

		/// <summary>
		/// 内部ネットワークライブラリを非同期で終了します。
		/// </summary>
		void Shutdown();

		/// <summary>
		/// 内部ネットワークサービスに非同期で接続します。
		/// </summary>
		/// <param name="baseSettings"></param>
		///
		/// <returns>接続が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> Connect(INetworkSettings baseSettings);

		/// <summary>
		/// 内部ネットワークサービスから非同期で切断します。
		/// </summary>
		UniTask Disconnect();

		/// <summary>
		/// 新しいロビーを内部で非同期で作成します。
		/// </summary>
		/// <param name="lobbyName">作成するロビーの名前。</param>
		/// <returns>ロビー作成が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> CreateLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default);

		/// <summary>
		/// 既存のロビーに内部で非同期で接続します。
		/// </summary>
		/// <param name="baseSettings"></param>
		/// <returns>ロビー接続が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> ConnectLobby(IRoomSettings baseSettings, CancellationToken cancellationToken = default);

		/// <summary>
		/// 内部でロビーから非同期で切断します。
		/// </summary>
		UniTask DisconnectLobby();

		/// <summary>
		/// 利用可能なロビーを内部で非同期で検索します。
		/// </summary>
		/// <param name="baseSettings"></param>
		/// <returns>検索結果のロビーIDのリスト。</returns>
		UniTask<List<object>> SearchLobby(IRoomSettings baseSettings);

		/// <summary>
		/// 新しいルームを内部で非同期で作成します。
		/// </summary>
		/// <param name="baseSettings">作成するルームの設定。</param>
		/// <returns>ルーム作成が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> CreateRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default);

		/// <summary>
		/// 既存のルームに内部で非同期で接続します。
		/// </summary>
		/// <param name="baseSettings">接続するルームの設定。</param>
		/// <returns>ルーム接続が成功した場合は true、それ以外は false。</returns>
		UniTask<bool> ConnectRoom(IRoomSettings baseSettings, CancellationToken cancellationToken = default);

		/// <summary>
		/// 内部でルームから非同期で切断します。
		/// </summary>
		UniTask DisconnectRoom();

		/// <summary>
		/// 利用可能なルームを内部で非同期で検索します。
		/// </summary>
		/// <param name="baseSettings">接続するルームの設定</param>
		/// <returns>検索結果のルームIDのリスト。</returns>
		UniTask<List<object>> SearchRoom(IRoomSettings baseSettings);

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
		object AccountId { get; }
		string NickName { get; }
		object StationId { get; }
		public bool IsConnected { get; }
		public bool IsHost { get; }
		public List<PlayerData> ConnectedList { get; }
		public List<PlayerData> DisconnectedList { get; }

		/// <summary>
		/// このハンドラが提供するINetworkRoomSettingsのファクトリーを取得します。
		/// </summary>
		INetworkSettingsFactory SettingsFactory { get; } // ★ 追加
	}
}
