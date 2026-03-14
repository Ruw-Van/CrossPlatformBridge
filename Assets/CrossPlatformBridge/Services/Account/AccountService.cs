using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Services.Account
{
	/// <summary>
	/// アカウントサービスのファサード。MonoBehaviour シングルトン。
	/// </summary>
	public class AccountService : MonoBehaviour
	{
		public static AccountService Instance { get; private set; }

		private IInternalAccountHandler _handler;

		/// <summary>初期化が完了しているかどうかを取得します。</summary>
		public bool IsInitialized => _handler?.IsInitialized ?? false;

		/// <summary>アカウントのプラットフォーム固有IDを取得します。</summary>
		public string AccountId => _handler?.AccountId ?? string.Empty;

		/// <summary>アカウントの表示名を取得します。</summary>
		public string NickName => _handler?.NickName ?? string.Empty;

		/// <summary>
		/// 認証状態が変化した際に発生するイベント。
		/// 引数は新しい初期化状態（true = 初期化済み、false = 未初期化）。
		/// </summary>
		public event Action<bool> OnAuthStateChanged;

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}

		/// <summary>
		/// プラットフォーム固有のハンドラーを設定します。
		/// </summary>
		public void InitializeHandler(IInternalAccountHandler handler)
		{
			_handler = handler;
			_handler.OnAuthStateChanged += isInitialized => OnAuthStateChanged?.Invoke(isInitialized);
		}

		/// <summary>
		/// アカウントを非同期で初期化します。
		/// </summary>
		public UniTask<bool> InitializeAsync()
		{
			if (_handler == null)
				throw new InvalidOperationException("AccountService: ハンドラが設定されていません。InitializeHandler を先に呼び出してください。");
			return _handler.InitializeAsync();
		}

		/// <summary>
		/// アカウントサービスを非同期でシャットダウンします。
		/// </summary>
		public UniTask ShutdownAsync()
		{
			if (_handler == null)
				throw new InvalidOperationException("AccountService: ハンドラが設定されていません。InitializeHandler を先に呼び出してください。");
			return _handler.ShutdownAsync();
		}
	}
}
