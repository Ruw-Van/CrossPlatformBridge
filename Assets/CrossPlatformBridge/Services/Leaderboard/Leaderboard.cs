using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Services.Leaderboard
{
	/// <summary>
	/// リーダーボード・ランキング機能の公開ファサード。
	/// IInternalLeaderboardHandler の実装を差し替えることで、バックエンドを切り替えられます。
	/// 使用前に Use&lt;T&gt;() または InitializeHandler() で実装を注入してください。
	/// </summary>
	public class Leaderboard : MonoBehaviour
	{
		private static Leaderboard _instance;
		public static Leaderboard Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindFirstObjectByType<Leaderboard>();
					if (_instance == null)
					{
						var go = new GameObject(typeof(Leaderboard).Name);
						_instance = go.AddComponent<Leaderboard>();
					}
					if (Application.isPlaying)
						DontDestroyOnLoad(_instance.gameObject);
				}
				return _instance;
			}
		}

		// --------------------------------------------------------------------------------
		// プロパティ
		// --------------------------------------------------------------------------------

		/// <summary>ハンドラが初期化済みかどうか。</summary>
		public bool IsInitialized => _handler != null;

		// --------------------------------------------------------------------------------
		// フィールド
		// --------------------------------------------------------------------------------

		private IInternalLeaderboardHandler _handler;

		// --------------------------------------------------------------------------------
		// Unity ライフサイクル
		// --------------------------------------------------------------------------------

		private void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
				if (Application.isPlaying)
					DontDestroyOnLoad(gameObject);
			}
			else if (_instance != this)
			{
				Destroy(gameObject);
			}
		}

		// --------------------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 使用するリーダーボードハンドラー実装を注入します。
		/// </summary>
		/// <param name="handler">リーダーボードハンドラーの実装。</param>
		[Obsolete("Use Use<T>() instead for parameterless handlers. " +
		          "InitializeHandler remains available when constructor arguments are required.")]
		public void InitializeHandler(IInternalLeaderboardHandler handler)
		{
			_handler = handler;
		}

		/// <summary>
		/// 指定したプラットフォームのハンドラーを生成して設定し、返します。
		/// </summary>
		/// <typeparam name="T"><see cref="ILeaderboardPlatform"/> を実装し、パラメーターなしコンストラクターを持つプラットフォーム型。</typeparam>
		/// <returns>生成されたハンドラー。</returns>
		public IInternalLeaderboardHandler Use<T>() where T : ILeaderboardPlatform, new()
		{
			var handler = new T().CreateLeaderboardHandler();
#pragma warning disable CS0618
			InitializeHandler(handler);
#pragma warning restore CS0618
			return handler;
		}

		// --------------------------------------------------------------------------------
		// グローバルランキング
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 現在のプレイヤーのスコアをリーダーボードに送信します。
		/// </summary>
		public async UniTask<bool> SubmitScore(string leaderboardName, long score)
		{
			AssertInitialized();
			return await _handler.SubmitScore(leaderboardName, score);
		}

		/// <summary>
		/// リーダーボードの上位エントリを取得します。
		/// </summary>
		public async UniTask<List<LeaderboardEntry>> GetTopEntries(string leaderboardName, int count)
		{
			AssertInitialized();
			return await _handler.GetTopEntries(leaderboardName, count);
		}

		/// <summary>
		/// 指定プレイヤーのリーダーボードエントリを取得します。
		/// </summary>
		public async UniTask<LeaderboardEntry> GetPlayerEntry(string leaderboardName, string playerId)
		{
			AssertInitialized();
			return await _handler.GetPlayerEntry(leaderboardName, playerId);
		}

		/// <summary>
		/// 指定プレイヤーを中心とした前後のエントリを取得します。
		/// </summary>
		public async UniTask<List<LeaderboardEntry>> GetEntriesAroundPlayer(string leaderboardName, string playerId, int range)
		{
			AssertInitialized();
			return await _handler.GetEntriesAroundPlayer(leaderboardName, playerId, range);
		}

		// --------------------------------------------------------------------------------
		// セッション内ランキング
		// --------------------------------------------------------------------------------

		/// <summary>
		/// セッション（ルーム）内でのプレイヤーのスコアを更新します。
		/// </summary>
		public async UniTask<bool> UpdateSessionScore(string playerId, string playerName, long score)
		{
			AssertInitialized();
			return await _handler.UpdateSessionScore(playerId, playerName, score);
		}

		/// <summary>
		/// セッション内の全プレイヤーのランキングをスコア降順で返します。
		/// </summary>
		public async UniTask<List<LeaderboardEntry>> GetSessionLeaderboard()
		{
			AssertInitialized();
			return await _handler.GetSessionLeaderboard();
		}

		/// <summary>
		/// セッション内での指定プレイヤーの順位を返します（1始まり）。
		/// 存在しない場合は -1 を返します。
		/// </summary>
		public async UniTask<int> GetPlayerSessionRank(string playerId)
		{
			AssertInitialized();
			return await _handler.GetPlayerSessionRank(playerId);
		}

		/// <summary>
		/// セッション内のスコアをすべてリセットします。
		/// </summary>
		public async UniTask ResetSessionLeaderboard()
		{
			AssertInitialized();
			await _handler.ResetSessionLeaderboard();
		}

		// --------------------------------------------------------------------------------
		// プライベート
		// --------------------------------------------------------------------------------

		private void AssertInitialized()
		{
			if (_handler == null)
				throw new InvalidOperationException("[Leaderboard] InitializeHandler() を先に呼び出してください。");
		}
	}
}
