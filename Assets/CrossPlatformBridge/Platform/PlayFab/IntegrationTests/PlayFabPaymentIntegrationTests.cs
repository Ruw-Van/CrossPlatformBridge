#if USE_CROSSPLATFORMBRIDGE_PLAYFAB && !DISABLE_PLAYFABCLIENT_API
using System.Collections;
using CrossPlatformBridge.Services.Account;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using PlayFab;
using CrossPlatformBridge.Platform.PlayFab;
using UnityEngine;
using UnityEngine.TestTools;
using PaymentService = CrossPlatformBridge.Services.Payment.Payment;

namespace CrossPlatformBridge.Platform.PlayFab.IntegrationTests
{
	/// <summary>
	/// PlayFab 決済サービスの PlayMode 統合テスト。
	/// CrossPlatformBridge ファサード（Payment）経由で PlayFab の
	/// 仮想通貨・カタログ・インベントリ取得を検証する。
	///
	/// 実行前提条件: PlayFab TitleId が設定済みであること。
	/// PurchaseItem / ConsumeItem はゲーム固有の設定（アイテム・通貨の存在）に依存するため対象外。
	/// </summary>
	public class PlayFabPaymentIntegrationTests
	{
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

			PaymentService.Instance.Use<PlayFab>();
		});

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			if (PaymentService.Instance != null)
				Object.Destroy(PaymentService.Instance.gameObject);

			if (AccountService.Instance != null)
				await AccountService.Instance.ShutdownAsync();

			if (_accountServiceGo != null)
				Object.Destroy(_accountServiceGo);
		});

		// -----------------------------------------------------------------------
		// GetVirtualCurrencies
		// -----------------------------------------------------------------------

		/// <summary>
		/// GetVirtualCurrencies() がログイン後に null でない結果を返すことを確認する（空辞書は可）。
		/// </summary>
		[UnityTest]
		public IEnumerator GetVirtualCurrencies_AfterLogin_ReturnsNotNull() => UniTask.ToCoroutine(async () =>
		{
			var currencies = await PaymentService.Instance.GetVirtualCurrencies();

			Assert.IsNotNull(currencies,
				"GetVirtualCurrencies() が null を返しました。");
		});

		// -----------------------------------------------------------------------
		// GetCatalog
		// -----------------------------------------------------------------------

		/// <summary>
		/// GetCatalog() がログイン後に null でない結果を返すことを確認する（空リストは可）。
		/// </summary>
		[UnityTest]
		public IEnumerator GetCatalog_AfterLogin_ReturnsNotNull() => UniTask.ToCoroutine(async () =>
		{
			var catalog = await PaymentService.Instance.GetCatalog();

			Assert.IsNotNull(catalog,
				"GetCatalog() が null を返しました。");
		});

		// -----------------------------------------------------------------------
		// GetInventory
		// -----------------------------------------------------------------------

		/// <summary>
		/// GetInventory() がログイン後に null でない結果を返すことを確認する（空リストは可）。
		/// </summary>
		[UnityTest]
		public IEnumerator GetInventory_AfterLogin_ReturnsNotNull() => UniTask.ToCoroutine(async () =>
		{
			var inventory = await PaymentService.Instance.GetInventory();

			Assert.IsNotNull(inventory,
				"GetInventory() が null を返しました。");
		});
	}
}
#endif
