#if USE_CROSSPLATFORMBRIDGE_NETCODE
using System.Collections;
using NetworkService = CrossPlatformBridge.Services.Network.Network;
using CrossPlatformBridge.Platform.Netcode;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Netcode.IntegrationTests
{
	/// <summary>
	/// Netcode ネットワーク機能の PlayMode 統合テスト。
	///
	/// CrossPlatformBridge の Network ファサードを通じて Netcode の認証エラーパスを検証する。
	/// - SetUp で Use&lt;Netcode&gt;() + ConnectNetwork() により匿名サインインして認証済み状態にする。
	/// - 各テストで DisconnectNetwork() を呼んで未認証状態に戻してから
	///   CreateRoom / ConnectRoom を呼び、認証エラーが正しく発生することを確認する。
	/// </summary>
	public class NetcodeNetworkIntegrationTests
	{
		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
		{
			bool initialized = await NetworkService.Instance.Use<Netcode>();
			Assert.IsTrue(initialized, "Network.Use<Netcode>() が失敗しました。");

			bool connected = await NetworkService.Instance.ConnectNetwork("", "");
			Assert.IsTrue(connected, "Network.ConnectNetwork() が失敗しました。");
		});

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			await NetworkService.Instance.DisconnectNetwork();
			await NetworkService.Instance.ShutdownLibrary();
		});

		// ----------------------------------------------------------------
		// CreateRoom — 未認証時のエラーパス
		// ----------------------------------------------------------------

		[UnityTest]
		public IEnumerator CreateRoom_WhenNotAuthenticated_ReturnsFalse() => UniTask.ToCoroutine(async () =>
		{
			await NetworkService.Instance.DisconnectNetwork();

			LogAssert.Expect(LogType.Error, "NetcodeNetworkHandler: 認証されていません。ルームを作成できません。");
			var settings = NetworkService.Instance.PrepareRoomSettings();
			settings.RoomName = "TestRoom";

			bool result = await NetworkService.Instance.CreateRoom(settings);

			Assert.IsFalse(result, "未認証時は CreateRoom が false を返す必要があります");
		});

		[UnityTest]
		public IEnumerator CreateRoom_WhenNotAuthenticated_FiresOnRoomOperationCompleted() => UniTask.ToCoroutine(async () =>
		{
			await NetworkService.Instance.DisconnectNetwork();

			LogAssert.Expect(LogType.Error, "NetcodeNetworkHandler: 認証されていません。ルームを作成できません。");

			string receivedOperation = null;
			bool? receivedSuccess = null;
			string receivedMessage = null;
			NetworkService.Instance.OnRoomOperationCompleted += (op, s, msg) =>
			{
				receivedOperation = op;
				receivedSuccess = s;
				receivedMessage = msg;
			};

			var settings = NetworkService.Instance.PrepareRoomSettings();
			settings.RoomName = "TestRoom";
			await NetworkService.Instance.CreateRoom(settings);

			Assert.AreEqual("CreateRoom", receivedOperation, "操作名が正しい必要があります");
			Assert.IsFalse(receivedSuccess.Value, "失敗時は success=false が通知される必要があります");
			Assert.AreEqual("Not authenticated.", receivedMessage);
		});

		// ----------------------------------------------------------------
		// ConnectRoom — 未認証時のエラーパス
		// ----------------------------------------------------------------

		[UnityTest]
		public IEnumerator ConnectRoom_WhenNotAuthenticated_ReturnsFalse() => UniTask.ToCoroutine(async () =>
		{
			await NetworkService.Instance.DisconnectNetwork();

			LogAssert.Expect(LogType.Error, "NetcodeNetworkHandler: 認証されていません。ルームに接続できません。");
			var settings = NetworkService.Instance.PrepareRoomSettings();
			settings.RoomName = "TestRoom";

			bool result = await NetworkService.Instance.ConnectRoom(settings);

			Assert.IsFalse(result, "未認証時は ConnectRoom が false を返す必要があります");
		});

		[UnityTest]
		public IEnumerator ConnectRoom_WhenNotAuthenticated_FiresOnRoomOperationCompleted() => UniTask.ToCoroutine(async () =>
		{
			await NetworkService.Instance.DisconnectNetwork();

			LogAssert.Expect(LogType.Error, "NetcodeNetworkHandler: 認証されていません。ルームに接続できません。");

			string receivedOperation = null;
			bool? receivedSuccess = null;
			string receivedMessage = null;
			NetworkService.Instance.OnRoomOperationCompleted += (op, s, msg) =>
			{
				receivedOperation = op;
				receivedSuccess = s;
				receivedMessage = msg;
			};

			var settings = NetworkService.Instance.PrepareRoomSettings();
			settings.RoomName = "TestRoom";
			await NetworkService.Instance.ConnectRoom(settings);

			Assert.AreEqual("ConnectRoom", receivedOperation, "操作名が正しい必要があります");
			Assert.IsFalse(receivedSuccess.Value, "失敗時は success=false が通知される必要があります");
			Assert.AreEqual("Not authenticated.", receivedMessage);
		});
	}
}
#endif
