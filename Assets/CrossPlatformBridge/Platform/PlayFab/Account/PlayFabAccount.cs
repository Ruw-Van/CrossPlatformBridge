#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
#if !DISABLE_PLAYFABCLIENT_API

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Testing;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace CrossPlatformBridge.Platform.PlayFab.Account
{
	/// <summary>
	/// PlayFab を使用したアカウント管理クラス。
	/// IInternalAccountHandler を実装し、端末固有 ID による匿名ログインを提供します。
	/// 追加の認証方式 (メール・カスタム ID) は本クラスのメソッドを直接呼び出してください。
	/// </summary>
	public class PlayFabAccount : IInternalAccountHandler, IServiceTestProvider
	{
		/// <summary>PlayFab ログインが完了しているかどうか。</summary>
		public bool IsInitialized { get; private set; }

		/// <summary>PlayFab PlayerId。</summary>
		public string AccountId { get; private set; } = string.Empty;

		/// <summary>プレイヤーの表示名。</summary>
		public string NickName { get; private set; } = string.Empty;

		/// <summary>現在のセッションチケット。</summary>
		public string SessionTicket { get; private set; } = string.Empty;

		/// <summary>
		/// 認証状態が変化した際に発生するイベント。
		/// 引数は新しい初期化状態（true = 初期化済み、false = 未初期化）。
		/// </summary>
		public event Action<bool> OnAuthStateChanged;

		// --------------------------------------------------------------------------------
		// IInternalAccountHandler 実装
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 端末固有 ID で匿名ログインします (IInternalAccountHandler.InitializeAsync の実装)。
		/// </summary>
		public async UniTask<bool> InitializeAsync()
		{
			return await LoginAnonymous();
		}

		/// <summary>
		/// PlayFab からログアウトし、アカウント情報をクリアします。
		/// </summary>
		public UniTask ShutdownAsync()
		{
			PlayFabClientAPI.ForgetAllCredentials();
			AccountId     = string.Empty;
			NickName      = string.Empty;
			SessionTicket = string.Empty;
			IsInitialized = false;
			OnAuthStateChanged?.Invoke(false);
			Debug.Log("[PlayFabAccount] ログアウト完了");
			return UniTask.CompletedTask;
		}

		// --------------------------------------------------------------------------------
		// 追加認証メソッド
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 端末固有 ID で匿名ログインします。
		/// </summary>
		public async UniTask<bool> LoginAnonymous()
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var request = new LoginWithCustomIDRequest
			{
				TitleId       = PlayFabSettings.staticSettings.TitleId,
				CustomId      = SystemInfo.deviceUniqueIdentifier,
				CreateAccount = true,
			};

			PlayFabClientAPI.LoginWithCustomID(request,
				result =>
				{
					ApplyLoginResult(result.PlayFabId, result.SessionTicket, result.InfoResultPayload?.PlayerProfile?.DisplayName);
					tcs.TrySetResult(true);
				},
				error =>
				{
					Debug.LogError($"[PlayFabAccount] 匿名ログイン失敗: {error.GenerateErrorReport()}");
					tcs.TrySetResult(false);
				});

			return await tcs.Task;
		}

		/// <summary>
		/// カスタム ID でログインします。
		/// </summary>
		/// <param name="customId">ゲーム側が管理する一意な ID。</param>
		/// <param name="createAccount">アカウントが存在しない場合に新規作成するか。</param>
		public async UniTask<bool> LoginWithCustomId(string customId, bool createAccount = true)
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var request = new LoginWithCustomIDRequest
			{
				TitleId       = PlayFabSettings.staticSettings.TitleId,
				CustomId      = customId,
				CreateAccount = createAccount,
			};

			PlayFabClientAPI.LoginWithCustomID(request,
				result =>
				{
					ApplyLoginResult(result.PlayFabId, result.SessionTicket, result.InfoResultPayload?.PlayerProfile?.DisplayName);
					tcs.TrySetResult(true);
				},
				error =>
				{
					Debug.LogError($"[PlayFabAccount] CustomId ログイン失敗: {error.GenerateErrorReport()}");
					tcs.TrySetResult(false);
				});

			return await tcs.Task;
		}

		/// <summary>
		/// メールアドレスとパスワードでログインします。
		/// </summary>
		public async UniTask<bool> LoginWithEmail(string email, string password)
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var request = new LoginWithEmailAddressRequest
			{
				TitleId  = PlayFabSettings.staticSettings.TitleId,
				Email    = email,
				Password = password,
			};

			PlayFabClientAPI.LoginWithEmailAddress(request,
				result =>
				{
					ApplyLoginResult(result.PlayFabId, result.SessionTicket, result.InfoResultPayload?.PlayerProfile?.DisplayName);
					tcs.TrySetResult(true);
				},
				error =>
				{
					Debug.LogError($"[PlayFabAccount] メールログイン失敗: {error.GenerateErrorReport()}");
					tcs.TrySetResult(false);
				});

			return await tcs.Task;
		}

		/// <summary>
		/// メールアドレスとパスワードで新規アカウントを作成します。
		/// </summary>
		public async UniTask<bool> RegisterWithEmail(string email, string password, string username)
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var request = new RegisterPlayFabUserRequest
			{
				TitleId     = PlayFabSettings.staticSettings.TitleId,
				Email       = email,
				Password    = password,
				Username    = username,
				DisplayName = username,
			};

			PlayFabClientAPI.RegisterPlayFabUser(request,
				result =>
				{
					ApplyLoginResult(result.PlayFabId, result.SessionTicket, username);
					tcs.TrySetResult(true);
				},
				error =>
				{
					Debug.LogError($"[PlayFabAccount] 新規登録失敗: {error.GenerateErrorReport()}");
					tcs.TrySetResult(false);
				});

			return await tcs.Task;
		}

		/// <summary>
		/// 表示名を更新します。
		/// </summary>
		public async UniTask<bool> UpdateDisplayName(string displayName)
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var request = new UpdateUserTitleDisplayNameRequest
			{
				DisplayName = displayName,
			};

			PlayFabClientAPI.UpdateUserTitleDisplayName(request,
				result =>
				{
					NickName = result.DisplayName;
					tcs.TrySetResult(true);
				},
				error =>
				{
					Debug.LogError($"[PlayFabAccount] 表示名更新失敗: {error.GenerateErrorReport()}");
					tcs.TrySetResult(false);
				});

			return await tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// プライベート
		// --------------------------------------------------------------------------------

		private void ApplyLoginResult(string playerId, string sessionTicket, string displayName)
		{
			AccountId     = playerId ?? string.Empty;
			SessionTicket = sessionTicket ?? string.Empty;
			NickName      = displayName ?? string.Empty;
			IsInitialized = true;
			OnAuthStateChanged?.Invoke(true);
			Debug.Log($"[PlayFabAccount] ログイン完了 PlayerId={AccountId} DisplayName={NickName}");
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => new TestOperation[]
		{
			new TestOperation { SectionLabel = "アカウント操作" },
			new TestOperation { Label = "Initialize", Action = async ctx => { bool ok = await InitializeAsync(); ctx.ReportResult($"Initialize → {ok}\nAccountId: {AccountId}\nNickName: {NickName}"); ctx.AppendLog($"Initialize → {ok}"); } },
			new TestOperation { Label = "Shutdown", Action = async ctx => { await ShutdownAsync(); ctx.ReportResult($"Shutdown 完了\nIsInitialized: {IsInitialized}"); ctx.AppendLog("Shutdown 完了"); } },
		};

		public TestDefaultData GetDefaultData() => new TestDefaultData();
	}
}

#endif // !DISABLE_PLAYFABCLIENT_API

#endif
