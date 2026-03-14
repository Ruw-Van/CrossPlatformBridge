using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Services.Achievement
{
	/// <summary>
	/// 実績・トロフィー機能の公開ファサード。
	/// IInternalAchievementHandler の実装を差し替えることで、バックエンドを切り替えられます。
	/// 使用前に InitializeHandler() で実装を注入してください。
	/// </summary>
	public class Achievement : MonoBehaviour
	{
		private static Achievement _instance;
		public static Achievement Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindFirstObjectByType<Achievement>();
					if (_instance == null)
					{
						var go = new GameObject(typeof(Achievement).Name);
						_instance = go.AddComponent<Achievement>();
					}
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

		private IInternalAchievementHandler _handler;

		// --------------------------------------------------------------------------------
		// Unity ライフサイクル
		// --------------------------------------------------------------------------------

		private void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
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
		/// 使用する実績ハンドラー実装を注入します。
		/// </summary>
		/// <param name="handler">実績ハンドラーの実装。</param>
		[System.Obsolete("Use Use<T>() instead for parameterless handlers. " +
		                 "InitializeHandler remains available when constructor arguments are required.")]
		public void InitializeHandler(IInternalAchievementHandler handler)
		{
			_handler = handler;
		}

		/// <summary>
		/// 指定したプラットフォームのハンドラーを生成して設定し、返します。
		/// </summary>
		/// <typeparam name="T"><see cref="IAchievementPlatform"/> を実装し、パラメーターなしコンストラクターを持つプラットフォーム型。</typeparam>
		/// <returns>生成されたハンドラー。</returns>
		public IInternalAchievementHandler Use<T>() where T : IAchievementPlatform, new()
		{
			var handler = new T().CreateAchievementHandler();
#pragma warning disable CS0618
			InitializeHandler(handler);
#pragma warning restore CS0618
			return handler;
		}

		// --------------------------------------------------------------------------------
		// メソッド
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 指定したIDの実績を解除します。
		/// </summary>
		public async UniTask<bool> UnlockAchievement(string achievementId)
		{
			AssertInitialized();
			return await _handler.UnlockAchievement(achievementId);
		}

		/// <summary>
		/// 現在解除済みの実績ID一覧を取得します。
		/// </summary>
		public async UniTask<List<string>> GetUnlockedAchievements()
		{
			AssertInitialized();
			return await _handler.GetUnlockedAchievements();
		}

		/// <summary>
		/// 実績の進行度を更新します。
		/// </summary>
		public async UniTask<bool> SetProgress(string achievementId, float progress)
		{
			AssertInitialized();
			return await _handler.SetProgress(achievementId, progress);
		}

		// --------------------------------------------------------------------------------
		// プライベート
		// --------------------------------------------------------------------------------

		private void AssertInitialized()
		{
			if (_handler == null)
				throw new InvalidOperationException("[Achievement] InitializeHandler() を先に呼び出してください。");
		}
	}
}
