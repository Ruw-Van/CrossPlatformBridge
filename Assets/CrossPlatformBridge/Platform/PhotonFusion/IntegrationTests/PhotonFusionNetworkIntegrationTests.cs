#if USE_CROSSPLATFORMBRIDGE_PHOTONFUSION
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.PhotonFusion;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Tests.Shared;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Fusion.Photon.Realtime;
using UnityEngine.TestTools;
using NetworkService = CrossPlatformBridge.Services.Network.Network;

namespace CrossPlatformBridge.Platform.PhotonFusion.IntegrationTests
{
	/// <summary>
	/// PhotonFusion ネットワーク機能の PlayMode 統合テスト。
	///
	/// CrossPlatformBridge の Network ファサードを通じて PhotonFusion が正しく動作することを確認する。
	/// - NetworkService.Instance.Use&lt;PhotonFusion&gt;() でハンドラを登録し、
	/// - NetworkService.Instance.ConnectNetwork() / CreateLobby() などのファサード API を呼ぶことで
	///   Bridge 全体のスタックを検証する。
	///
	/// 実行前に Photon Fusion の AppId を設定してください。
	/// Settings: Edit > Project Settings > Photon > Fusion App Settings
	/// </summary>
	public class PhotonFusionNetworkIntegrationTests : NetworkIntegrationTestBase
	{
		// -----------------------------------------------------------------------
		// NetworkIntegrationTestBase 実装
		// -----------------------------------------------------------------------

		protected override async UniTask SetUpPlatform()
		{
			Assume.That(
				!string.IsNullOrEmpty(PhotonAppSettings.Global.AppSettings.AppIdFusion),
				"PhotonAppSettings の AppIdFusion が設定されていません。" +
				"Edit > Project Settings > Photon > Fusion App Settings で設定してください。");

			bool initialized = await NetworkService.Instance.Use<Fusion>();
			Assert.IsTrue(initialized, "Network.Use<Fusion>() が失敗しました。AppSettings を確認してください。");
			Assert.IsTrue(NetworkService.Instance.IsInitialized, "Use<Fusion>() 後は IsInitialized が true のはずです。");

			bool connected = await NetworkService.Instance.ConnectNetwork("", "");
			Assert.IsTrue(connected, "Network.ConnectNetwork() が失敗しました。");
		}

		/// <summary>PhotonFusion はルーム名が 20 文字制限。</summary>
		protected override string TrimRoomName(string name) =>
			name.Length > 20 ? name[..20] : name;

		// -----------------------------------------------------------------------
		// PhotonFusion 固有テスト
		// -----------------------------------------------------------------------

		/// <summary>
		/// Connect 後に AccountId が設定されることを確認する（PhotonFusion 固有）。
		/// </summary>
		[UnityTest]
		public IEnumerator Connect_ToPhotonFusionServer_SetsAccountId() => UniTask.ToCoroutine(async () =>
		{
			Assert.IsTrue(NetworkService.Instance.IsConnected, "ConnectNetwork() 後は IsConnected が true のはずです。");
			Assert.IsNotNull(NetworkService.Instance.AccountId, "ConnectNetwork() 後は AccountId が設定されているはずです。");
			await UniTask.CompletedTask;
		});

		/// <summary>
		/// SearchLobby() がリストを返すことを確認する（Fusion はセッションリスト取得をサポート）。
		/// </summary>
		[UnityTest]
		public IEnumerator SearchLobby_AfterCreateLobby_ReturnsResults() => UniTask.ToCoroutine(async () =>
		{
			IRoomSettings roomSettings = NetworkService.Instance.PrepareRoomSettings();
			roomSettings.RoomName = "SearchTestLobby";
			roomSettings.MaxPlayers = 4;

			bool created = await NetworkService.Instance.CreateLobby(roomSettings);
			_testCreatedLobby = created;
			Assert.IsTrue(created, "ロビーの作成に失敗しました。");

			// セッションリストが更新されるまで少し待機
			await UniTask.Delay(2000);

			IRoomSettings searchSettings = NetworkService.Instance.PrepareRoomSettings();
			List<object> results = await NetworkService.Instance.SearchLobby(searchSettings);

			Assert.IsNotNull(results, "SearchLobby() が null を返しました。");
		});
	}
}
#endif
