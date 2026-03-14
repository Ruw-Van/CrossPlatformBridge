#if USE_CROSSPLATFORMBRIDGE_PLAYFAB && !DISABLE_PLAYFABCLIENT_API
using System.Collections;
using CrossPlatformBridge.Platform.PlayFab.Account;
using CrossPlatformBridge.Services.Account;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using PlayFab;
using CrossPlatformBridge.Platform.PlayFab;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.PlayFab.IntegrationTests
{
	/// <summary>
	/// PlayFab アカウントサービスの PlayMode 統合テスト。
	/// CrossPlatformBridge ファサード（AccountService）経由で PlayFab に接続して動作を検証する。
	///
	/// 実行前に PlayFab の TitleId を設定してください。
	/// Settings: Edit > Project Settings > Player > Scripting Define Symbols に
	/// USE_CROSSPLATFORMBRIDGE_PLAYFAB を追加し、PlayFabSharedSettings の TitleId を設定してください。
	/// </summary>
	public class PlayFabAccountIntegrationTests
	{
		private GameObject _accountServiceGo;

		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
		{
			Assume.That(
				!string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId),
				"PlayFab の TitleId が設定されていません。PlayFabSharedSettings で TitleId を設定してください。");

			_accountServiceGo = new GameObject("AccountService");
			_accountServiceGo.AddComponent<AccountService>();

			AccountService.Instance.Use<PlayFab>();

			bool initialized = await AccountService.Instance.InitializeAsync();
			Assume.That(initialized, Is.True,
				"PlayFab の初期化に失敗しました。レート制限・TitleId・PlayFab Console の設定を確認してください。\n" +
				"レート制限の場合は数分待ってから再実行してください。");
		});

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			if (AccountService.Instance != null)
				await AccountService.Instance.ShutdownAsync();

			if (_accountServiceGo != null)
				Object.Destroy(_accountServiceGo);
		});

		// -----------------------------------------------------------------------
		// InitializeAsync
		// -----------------------------------------------------------------------

		/// <summary>
		/// 匿名ログイン後に IsInitialized が true になり、AccountId が設定されることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator InitializeAsync_AnonymousLogin_SetsIsInitializedAndAccountId() => UniTask.ToCoroutine(() =>
		{
			// SetUp で既に InitializeAsync 済み
			Assert.IsTrue(AccountService.Instance.IsInitialized,
				"InitializeAsync() 後は IsInitialized が true のはずです。");
			Assert.IsNotEmpty(AccountService.Instance.AccountId,
				"InitializeAsync() 後は AccountId が設定されているはずです。");
			return UniTask.CompletedTask;
		});

		// -----------------------------------------------------------------------
		// ShutdownAsync
		// -----------------------------------------------------------------------

		/// <summary>
		/// ShutdownAsync() 後に IsInitialized が false になり、AccountId がクリアされることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator Shutdown_AfterLogin_ClearsState() => UniTask.ToCoroutine(async () =>
		{
			await AccountService.Instance.ShutdownAsync();

			Assert.IsFalse(AccountService.Instance.IsInitialized,
				"ShutdownAsync() 後は IsInitialized が false のはずです。");
			Assert.IsEmpty(AccountService.Instance.AccountId,
				"ShutdownAsync() 後は AccountId が空のはずです。");
		});

		// -----------------------------------------------------------------------
		// LoginWithCustomId
		// -----------------------------------------------------------------------

		/// <summary>
		/// カスタム ID でログインし、AccountId が設定されることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator LoginWithCustomId_WithUniqueId_SetsAccountId() => UniTask.ToCoroutine(async () =>
		{
			var handler = AccountService.Instance.Use<PlayFab>() as PlayFabAccount;
			Assert.IsNotNull(handler, "Use<PlayFab>() が PlayFabAccount を返しませんでした。");

			string customId = $"IntegTest_{System.Guid.NewGuid():N}"[..32];
			bool result = await handler.LoginWithCustomId(customId, createAccount: true);

			Assert.IsTrue(result, $"LoginWithCustomId({customId}) が false を返しました。");
			Assert.IsNotEmpty(AccountService.Instance.AccountId,
				"LoginWithCustomId() 後は AccountId が設定されているはずです。");
		});

		// -----------------------------------------------------------------------
		// UpdateDisplayName
		// -----------------------------------------------------------------------

		/// <summary>
		/// UpdateDisplayName() 後に NickName が更新されることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator UpdateDisplayName_AfterLogin_UpdatesNickName() => UniTask.ToCoroutine(async () =>
		{
			const string displayName = "PlayFabTestPlayer";
			var handler = AccountService.Instance.Use<PlayFab>() as PlayFabAccount;
			Assert.IsNotNull(handler, "Use<PlayFab>() が PlayFabAccount を返しませんでした。");

			// 再ログインして新しいハンドラで操作
			bool initialized = await AccountService.Instance.InitializeAsync();
			Assert.IsTrue(initialized, "InitializeAsync() が false を返しました。");

			bool result = await handler.UpdateDisplayName(displayName);

			Assert.IsTrue(result, "UpdateDisplayName() が false を返しました。");
			Assert.AreEqual(displayName, AccountService.Instance.NickName,
				"UpdateDisplayName() 後は NickName が更新されているはずです。");
		});
	}
}
#endif
