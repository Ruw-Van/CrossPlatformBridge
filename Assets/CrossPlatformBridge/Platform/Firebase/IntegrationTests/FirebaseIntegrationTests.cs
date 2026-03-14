#if USE_CROSSPLATFORMBRIDGE_FIREBASE
using System.Collections;
using CrossPlatformBridge.Platform.Firebase;
using CrossPlatformBridge.Services.Account;
using CloudStorageService = CrossPlatformBridge.Services.CloudStorage.CloudStorage;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Firebase.IntegrationTests
{
	/// <summary>
	/// Firebase アカウント・クラウドストレージ機能の PlayMode 統合テスト。
	///
	/// 実行前提条件:
	/// - Assets/StreamingAssets/google-services.json（Android）または
	///   GoogleService-Info.plist（iOS/Editor）が配置済みであること
	/// - Firebase Console でプロジェクトが設定済みであること
	///   （Authentication: 匿名ログイン有効、Firestore: 有効）
	/// </summary>
	public class FirebaseIntegrationTests
	{
		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
		{
			// Firebase プラットフォームのアカウントハンドラを設定して初期化
			AccountService.Instance.Use<Firebase>();

			bool initialized = await AccountService.Instance.InitializeAsync();
			Assume.That(initialized, Is.True,
				"Firebase の初期化に失敗しました。google-services.json / Firebase Console の設定を確認してください。");

			// サインイン完了後にCloudStorageハンドラを設定
			// (awaitの後に設定することで、直前のTearDownの遅延Destroyが完了してから新インスタンスを取得できる)
			CloudStorageService.Instance.Use<Firebase>();
		});

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			if (AccountService.Instance != null)
				await AccountService.Instance.ShutdownAsync();

			if (CloudStorageService.Instance != null)
				Object.Destroy(CloudStorageService.Instance.gameObject);
		});

		// ----------------------------------------------------------------
		// FirebaseAccountHandler — 認証済み状態の確認
		// ----------------------------------------------------------------

		[UnityTest]
		public IEnumerator InitializeAsync_WithFirebase_SetsIsInitializedTrue() => UniTask.ToCoroutine(() =>
		{
			// SetUp で既に初期化済み
			Assert.IsTrue(AccountService.Instance.IsInitialized,
				"InitializeAsync() 後は IsInitialized が true のはずです。");
			Assert.IsNotEmpty(AccountService.Instance.AccountId,
				"InitializeAsync() 後は AccountId が設定されているはずです。");
			return UniTask.CompletedTask;
		});

		// ----------------------------------------------------------------
		// FirebaseCloudStorageHandler — 保存・読み込み・削除
		// ----------------------------------------------------------------

		[UnityTest]
		public IEnumerator SaveAndLoad_WithFirebase_ReturnsExpectedValue() => UniTask.ToCoroutine(async () =>
		{
			const string key = "integration_test_key";
			const string value = "integration_test_value";

			bool saved = await CloudStorageService.Instance.SaveData(key, value);
			Assert.IsTrue(saved, "SaveData() が false を返しました。");

			string loaded = await CloudStorageService.Instance.LoadData(key);
			Assert.AreEqual(value, loaded, "LoadData() が保存した値と異なる値を返しました。");

			// クリーンアップ
			await CloudStorageService.Instance.DeleteData(key);
		});
	}
}
#endif
