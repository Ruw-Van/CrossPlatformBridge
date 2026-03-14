#if USE_CROSSPLATFORMBRIDGE_FIREBASE
using System;
using System.Collections.Generic;
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

namespace CrossPlatformBridge.Platform.Firebase.Account
{
	/// <summary>
	/// Firebase Authentication を利用したアカウントハンドラ。
	/// 匿名認証 (Anonymous Sign-in) でログインし、UID をアカウントIDとして提供します。
	/// </summary>
	public class FirebaseAccountHandler : IInternalAccountHandler, IServiceTestProvider
	{
		private FirebaseAuth _auth;
		private FirebaseUser _user;

		public bool IsInitialized { get; private set; }

		public string AccountId => _user?.UserId ?? string.Empty;

		public string NickName => _user?.DisplayName ?? "AnonymousUser";

		public event Action<bool> OnAuthStateChanged;

		public async UniTask<bool> InitializeAsync()
		{
			if (IsInitialized) return true;

			try
			{
				// Firebaseの依存関係をチェック＆解決
				var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
				if (dependencyStatus != DependencyStatus.Available)
				{
					Debug.LogError($"[FirebaseAccountHandler] Could not resolve all Firebase dependencies: {dependencyStatus}");
					return false;
				}

				_auth = FirebaseAuth.DefaultInstance;

				// 既にサインイン済みの場合はそのユーザーを利用
				if (_auth.CurrentUser != null)
				{
					_user = _auth.CurrentUser;
					MarkAsInitialized();
					return true;
				}

				// 未サインインの場合は匿名ログインを実行
				var authResult = await _auth.SignInAnonymouslyAsync();
				_user = authResult.User;

				MarkAsInitialized();
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[FirebaseAccountHandler] Failed to initialize: {ex}");
				throw new AccountServiceException("Firebase Authentication failed.", ex);
			}
		}

		public UniTask ShutdownAsync()
		{
			if (!IsInitialized) return UniTask.CompletedTask;

			try
			{
				// サインアウト
				_auth?.SignOut();
				_user = null;
				_auth = null;

				IsInitialized = false;
				OnAuthStateChanged?.Invoke(false);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[FirebaseAccountHandler] Failed to shutdown: {ex}");
			}

			return UniTask.CompletedTask;
		}

		private void MarkAsInitialized()
		{
			IsInitialized = true;
			OnAuthStateChanged?.Invoke(true);
			Debug.Log($"[FirebaseAccountHandler] Successfully signed in. UID: {AccountId}");
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

#endif
