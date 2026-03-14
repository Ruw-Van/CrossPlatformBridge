#if USE_CROSSPLATFORMBRIDGE_FIREBASE
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.Firebase;
using CrossPlatformBridge.Services.Account;
using CloudStorageService = CrossPlatformBridge.Services.CloudStorage.CloudStorage;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Firebase.IntegrationTests
{
	/// <summary>
	/// Firebase CloudStorage ハンドラの PlayMode 統合テスト。
	/// Firestore を使用した保存・読み込み・削除操作を検証する。
	///
	/// 実行前に FirebaseAccountIntegrationTests と同様の Firebase 設定が必要です。
	/// </summary>
	public class FirebaseCloudStorageIntegrationTests
	{
		private const string SettingsPath =
			"Assets/CrossPlatformBridgeSettings/Editor/FirebaseIntegrationTestSettings.asset";

		private FirebaseIntegrationTestSettings _settings;

		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
		{
#if UNITY_EDITOR
			_settings = UnityEditor.AssetDatabase.LoadAssetAtPath<FirebaseIntegrationTestSettings>(SettingsPath);
#endif
			Assume.That(
				_settings != null,
				"FirebaseIntegrationTestSettings が見つかりません。" +
				"Tools > CrossPlatformBridge > Firebase > Create Integration Test Settings で作成してください。");

			// Account を初期化（CloudStorage は AccountService.AccountId に依存する）
			AccountService.Instance.Use<Firebase>();
			bool initialized = await AccountService.Instance.InitializeAsync();
			Assume.That(initialized, "Firebase 認証に失敗しました。");

			CloudStorageService.Instance.Use<Firebase>();
		});

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			// テストデータを削除してクリーンアップ
			await CloudStorageService.Instance.DeleteData(_settings.TestKey);
			await AccountService.Instance.ShutdownAsync();
		});

		// ----------------------------------------------------------------
		// Save & Load
		// ----------------------------------------------------------------

		/// <summary>
		/// SaveData() した値を LoadData() で取得できることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator SaveData_ThenLoadData_ShouldReturnSameValue() => UniTask.ToCoroutine(async () =>
		{
			bool saved = await CloudStorageService.Instance.SaveData(_settings.TestKey, _settings.TestValue);
			Assert.IsTrue(saved, "SaveData() が失敗しました。");

			string loaded = await CloudStorageService.Instance.LoadData(_settings.TestKey);
			Assert.AreEqual(_settings.TestValue, loaded,
				"LoadData() で保存した値と同じ値が返るはずです。");
		});

		/// <summary>
		/// SaveDataBatch() した値を LoadDataBatch() でまとめて取得できることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator SaveDataBatch_ThenLoadDataBatch_ShouldReturnAllValues() => UniTask.ToCoroutine(async () =>
		{
			var batchKey1 = _settings.TestKey + "_batch1";
			var batchKey2 = _settings.TestKey + "_batch2";
			var batchData = new Dictionary<string, string>
			{
				{ batchKey1, "value1" },
				{ batchKey2, "value2" },
			};

			bool saved = await CloudStorageService.Instance.SaveDataBatch(batchData);
			Assert.IsTrue(saved, "SaveDataBatch() が失敗しました。");

			var loaded = await CloudStorageService.Instance.LoadDataBatch(new List<string> { batchKey1, batchKey2 });
			Assert.AreEqual(2, loaded.Count, "LoadDataBatch() は 2 件を返すはずです。");
			Assert.AreEqual("value1", loaded[batchKey1]);
			Assert.AreEqual("value2", loaded[batchKey2]);

			// クリーンアップ
			await CloudStorageService.Instance.DeleteData(batchKey1);
			await CloudStorageService.Instance.DeleteData(batchKey2);
		});

		// ----------------------------------------------------------------
		// Delete
		// ----------------------------------------------------------------

		/// <summary>
		/// DeleteData() 後に LoadData() が null を返すことを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator DeleteData_ShouldRemoveKey() => UniTask.ToCoroutine(async () =>
		{
			await CloudStorageService.Instance.SaveData(_settings.TestKey, _settings.TestValue);

			bool deleted = await CloudStorageService.Instance.DeleteData(_settings.TestKey);
			Assert.IsTrue(deleted, "DeleteData() が失敗しました。");

			string loaded = await CloudStorageService.Instance.LoadData(_settings.TestKey);
			Assert.IsNull(loaded, "DeleteData() 後は LoadData() が null を返すはずです。");
		});

		/// <summary>
		/// 存在しないキーの LoadData() が null を返すことを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator LoadData_NonExistentKey_ShouldReturnNull() => UniTask.ToCoroutine(async () =>
		{
			string result = await CloudStorageService.Instance.LoadData("non_existent_key_xyz_12345");
			Assert.IsNull(result, "存在しないキーの LoadData() は null を返すはずです。");
		});
	}
}
#endif
