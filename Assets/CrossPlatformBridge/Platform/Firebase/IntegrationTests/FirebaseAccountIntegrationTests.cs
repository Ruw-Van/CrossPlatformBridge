#if USE_CROSSPLATFORMBRIDGE_FIREBASE
using System.Collections;
using CrossPlatformBridge.Platform.Firebase;
using CrossPlatformBridge.Services.Account;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Firebase.IntegrationTests
{
	/// <summary>
	/// Firebase アカウントハンドラの PlayMode 統合テスト。
	/// Firebase 匿名認証で実際のサービスに接続して動作を検証する。
	///
	/// 実行前に以下を確認してください:
	/// - google-services.json（Android）または GoogleService-Info.plist（iOS/macOS）を
	///   Assets/ 直下に配置し、Firebase プロジェクトが構成済みであること
	/// - Tools > CrossPlatformBridge > Firebase > Create Integration Test Settings
	///   で設定アセットを作成すること（設定値はデフォルトのままで動作します）
	/// </summary>
	public class FirebaseAccountIntegrationTests
	{
		private const string SettingsPath =
			"Assets/CrossPlatformBridgeSettings/Editor/FirebaseIntegrationTestSettings.asset";

		private static FirebaseIntegrationTestSettings _settings;
		private static bool _sessionInitialized;

		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
		{
			if (_sessionInitialized) return;

#if UNITY_EDITOR
			_settings = UnityEditor.AssetDatabase.LoadAssetAtPath<FirebaseIntegrationTestSettings>(SettingsPath);
#endif
			Assume.That(
				_settings != null,
				"FirebaseIntegrationTestSettings が見つかりません。" +
				"Tools > CrossPlatformBridge > Firebase > Create Integration Test Settings で作成してください。");

			AccountService.Instance.Use<Firebase>();
			bool initialized = await AccountService.Instance.InitializeAsync();

			Assume.That(
				initialized,
				"Firebase 初期化に失敗しました。google-services.json / GoogleService-Info.plist の設定を確認してください。");

			_sessionInitialized = true;
		});

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			await AccountService.Instance.ShutdownAsync();
			_sessionInitialized = false;
		});

		// ----------------------------------------------------------------
		// Account
		// ----------------------------------------------------------------

		/// <summary>
		/// InitializeAsync() 後に IsInitialized が true であることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator IsInitialized_AfterSetUp_ShouldBeTrue() => UniTask.ToCoroutine(async () =>
		{
			Assert.IsTrue(AccountService.Instance.IsInitialized,
				"Firebase 認証後は IsInitialized が true のはずです。");
			await UniTask.CompletedTask;
		});

		/// <summary>
		/// 匿名ログイン後に AccountId が空でないことを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator AccountId_AfterSignIn_ShouldNotBeEmpty() => UniTask.ToCoroutine(async () =>
		{
			Assert.IsFalse(
				string.IsNullOrEmpty(AccountService.Instance.AccountId),
				"Firebase 匿名ログイン後は AccountId が空でないはずです。");
			await UniTask.CompletedTask;
		});

		/// <summary>
		/// ShutdownAsync() 後に IsInitialized が false になることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator ShutdownAsync_ShouldClearIsInitialized() => UniTask.ToCoroutine(async () =>
		{
			await AccountService.Instance.ShutdownAsync();
			_sessionInitialized = false;

			Assert.IsFalse(AccountService.Instance.IsInitialized,
				"ShutdownAsync() 後は IsInitialized が false のはずです。");
		});
	}
}
#endif
