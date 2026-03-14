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
		private static AccountService _instance;
		public static AccountService Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindFirstObjectByType<AccountService>();
					if (_instance == null)
					{
						var go = new GameObject(nameof(AccountService));
						_instance = go.AddComponent<AccountService>();
					}
					DontDestroyOnLoad(_instance.gameObject);
				}
				return _instance;
			}
		}

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

		private void OnDestroy()
		{
			if (_instance == this)
				_instance = null;
		}

		/// <summary>
		/// プラットフォーム固有のハンドラーを設定します。
		/// </summary>
		[System.Obsolete("Use Use<T>() instead for parameterless handlers. " +
		                 "InitializeHandler remains available when constructor arguments are required.")]
		public void InitializeHandler(IInternalAccountHandler handler)
		{
			if (_handler != null)
				_handler.OnAuthStateChanged -= OnHandlerAuthStateChanged;

			_handler = handler;

			if (_handler != null)
				_handler.OnAuthStateChanged += OnHandlerAuthStateChanged;
		}

		/// <summary>
		/// 指定したプラットフォームのハンドラーを生成して設定し、返します。
		/// </summary>
		/// <typeparam name="T"><see cref="IAccountPlatform"/> を実装し、パラメーターなしコンストラクターを持つプラットフォーム型。</typeparam>
		/// <returns>生成されたハンドラー。</returns>
		public IInternalAccountHandler Use<T>() where T : IAccountPlatform, new()
		{
			var handler = new T().CreateAccountHandler();
#pragma warning disable CS0618
			InitializeHandler(handler);
#pragma warning restore CS0618
			return handler;
		}

		private void OnHandlerAuthStateChanged(bool isInitialized) =>
			OnAuthStateChanged?.Invoke(isInitialized);

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
