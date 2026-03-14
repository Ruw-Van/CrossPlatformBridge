#if USE_CROSSPLATFORMBRIDGE_PLAYFAB && !DISABLE_PLAYFABCLIENT_API
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Services.Account;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using PlayFab;
using CrossPlatformBridge.Platform.PlayFab;
using UnityEngine;
using UnityEngine.TestTools;
using CloudStorageService = CrossPlatformBridge.Services.CloudStorage.CloudStorage;

namespace CrossPlatformBridge.Platform.PlayFab.IntegrationTests
{
	/// <summary>
	/// PlayFab クラウドストレージの PlayMode 統合テスト。
	/// CrossPlatformBridge ファサード（CloudStorage）経由で PlayFab PlayerData の
	/// 保存・読み込み・削除を検証する。
	///
	/// 実行前提条件: PlayFab TitleId が設定済みであること。
	/// </summary>
	public class PlayFabCloudStorageIntegrationTests
	{
		private const string TestKey1 = "it_playfab_key1";
		private const string TestKey2 = "it_playfab_key2";

		private GameObject _accountServiceGo;

		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
		{
			Assume.That(
				!string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId),
				"PlayFab の TitleId が設定されていません。");

			_accountServiceGo = new GameObject("AccountService");
			_accountServiceGo.AddComponent<AccountService>();
			AccountService.Instance.Use<PlayFab>();

			bool initialized = await AccountService.Instance.InitializeAsync();
			Assume.That(initialized, Is.True,
				"PlayFab の初期化に失敗しました。TitleId と PlayFab Console の設定を確認してください。");

			CloudStorageService.Instance.Use<PlayFab>();
		});

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			// テスト用キーのクリーンアップ
			if (CloudStorageService.Instance != null && CloudStorageService.Instance.IsInitialized)
			{
				await CloudStorageService.Instance.DeleteData(TestKey1);
				await CloudStorageService.Instance.DeleteData(TestKey2);
				Object.Destroy(CloudStorageService.Instance.gameObject);
			}

			if (AccountService.Instance != null)
				await AccountService.Instance.ShutdownAsync();

			if (_accountServiceGo != null)
				Object.Destroy(_accountServiceGo);
		});

		// -----------------------------------------------------------------------
		// SaveData / LoadData
		// -----------------------------------------------------------------------

		/// <summary>
		/// SaveData() で保存した値が LoadData() で正しく取得できることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator SaveAndLoad_WithPlayFab_ReturnsExpectedValue() => UniTask.ToCoroutine(async () =>
		{
			const string value = "integration_test_value";

			bool saved = await CloudStorageService.Instance.SaveData(TestKey1, value);
			Assert.IsTrue(saved, "SaveData() が false を返しました。");

			string loaded = await CloudStorageService.Instance.LoadData(TestKey1);
			Assert.AreEqual(value, loaded, "LoadData() が保存した値と異なる値を返しました。");

			await CloudStorageService.Instance.DeleteData(TestKey1);
		});

		// -----------------------------------------------------------------------
		// DeleteData
		// -----------------------------------------------------------------------

		/// <summary>
		/// DeleteData() 後に LoadData() が null または空を返すことを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator DeleteData_AfterSave_RemovesKey() => UniTask.ToCoroutine(async () =>
		{
			bool saved = await CloudStorageService.Instance.SaveData(TestKey1, "to_delete");
			Assert.IsTrue(saved, "SaveData() が false を返しました。");

			bool deleted = await CloudStorageService.Instance.DeleteData(TestKey1);
			Assert.IsTrue(deleted, "DeleteData() が false を返しました。");

			string loaded = await CloudStorageService.Instance.LoadData(TestKey1);
			Assert.IsTrue(string.IsNullOrEmpty(loaded),
				"DeleteData() 後は LoadData() が null または空を返すはずです。");
		});

		// -----------------------------------------------------------------------
		// SaveDataBatch / LoadDataBatch
		// -----------------------------------------------------------------------

		/// <summary>
		/// SaveDataBatch() で保存した複数の値が LoadDataBatch() で正しく取得できることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator SaveAndLoadBatch_WithMultipleKeys_RoundTrips() => UniTask.ToCoroutine(async () =>
		{
			var data = new Dictionary<string, string>
			{
				{ TestKey1, "batch_value_1" },
				{ TestKey2, "batch_value_2" },
			};

			bool saved = await CloudStorageService.Instance.SaveDataBatch(data);
			Assert.IsTrue(saved, "SaveDataBatch() が false を返しました。");

			var loaded = await CloudStorageService.Instance.LoadDataBatch(new List<string> { TestKey1, TestKey2 });
			Assert.IsNotNull(loaded, "LoadDataBatch() が null を返しました。");
			Assert.AreEqual("batch_value_1", loaded[TestKey1], $"{TestKey1} の値が一致しません。");
			Assert.AreEqual("batch_value_2", loaded[TestKey2], $"{TestKey2} の値が一致しません。");

			await CloudStorageService.Instance.DeleteData(TestKey1);
			await CloudStorageService.Instance.DeleteData(TestKey2);
		});
	}
}
#endif
