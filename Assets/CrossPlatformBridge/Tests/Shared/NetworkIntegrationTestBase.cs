using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using NetworkService = CrossPlatformBridge.Services.Network.Network;
using CrossPlatformBridge.Services.Network;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Tests.Shared
{
	/// <summary>
	/// ネットワーク統合テストの共通基底クラス。
	/// CrossPlatformBridge ファサード（NetworkService.Instance）を通じた
	/// Connect / Disconnect / Lobby / Room 操作を各プラットフォームで共通テストします。
	/// </summary>
	public abstract class NetworkIntegrationTestBase
	{
		protected bool _testCreatedLobby;
		protected bool _testCreatedRoom;

		// -----------------------------------------------------------------------
		// 派生クラスで実装: プラットフォーム固有の初期化
		// Use<T>() + ConnectNetwork() を行う
		// -----------------------------------------------------------------------
		protected abstract UniTask SetUpPlatform();

		// MonoBehaviour ハンドラ（PUN2 等）用の追加後処理（デフォルト: 何もしない）
		protected virtual UniTask TearDownPlatform() => UniTask.CompletedTask;

		// ルーム名の長さ制限対応（PUN2/PhotonFusion は 20 文字制限）
		protected virtual string TrimRoomName(string name) => name;

		// -----------------------------------------------------------------------
		// SetUp / TearDown
		// -----------------------------------------------------------------------

		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
		{
			await SetUpPlatform();
			_testCreatedLobby = false;
			_testCreatedRoom = false;
		});

		[UnityTearDown]
		public IEnumerator TearDown() => UniTask.ToCoroutine(async () =>
		{
			if (_testCreatedRoom)
				await NetworkService.Instance.DisconnectRoom();
			else if (_testCreatedLobby)
				await NetworkService.Instance.DisconnectLobby();

			await NetworkService.Instance.DisconnectNetwork();
			await NetworkService.Instance.ShutdownLibrary();
			await TearDownPlatform();

			if (NetworkService.Instance != null)
			{
				Object.Destroy(NetworkService.Instance.gameObject);
				await UniTask.NextFrame();
			}
		});

		// -----------------------------------------------------------------------
		// 共通テスト: Connect / Disconnect
		// -----------------------------------------------------------------------

		/// <summary>
		/// SetUp で ConnectNetwork() が成功し、IsConnected が true になることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator ConnectNetwork_SetsIsConnectedTrue() => UniTask.ToCoroutine(async () =>
		{
			Assert.IsTrue(NetworkService.Instance.IsConnected, "ConnectNetwork() 後は IsConnected が true のはずです。");
			await UniTask.CompletedTask;
		});

		/// <summary>
		/// DisconnectNetwork() 後に IsConnected が false になることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator DisconnectNetwork_SetsIsConnectedFalse() => UniTask.ToCoroutine(async () =>
		{
			await NetworkService.Instance.DisconnectNetwork();

			Assert.IsFalse(NetworkService.Instance.IsConnected, "DisconnectNetwork() 後は IsConnected が false のはずです。");
		});

		// -----------------------------------------------------------------------
		// 共通テスト: Lobby
		// -----------------------------------------------------------------------

		/// <summary>
		/// CreateLobby() が成功し、StationId が設定されることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator CreateLobby_ReturnsTrueAndSetsStationId() => UniTask.ToCoroutine(async () =>
		{
			IRoomSettings settings = NetworkService.Instance.PrepareRoomSettings();
			settings.RoomName = TrimRoomName("IntegrationTestLobby");
			settings.MaxPlayers = 4;

			bool result = await NetworkService.Instance.CreateLobby(settings);
			_testCreatedLobby = result;

			Assert.IsTrue(result, "Network.CreateLobby() が false を返しました。");
			Assert.IsNotNull(NetworkService.Instance.StationId, "CreateLobby() 後は StationId が設定されているはずです。");
		});

		/// <summary>
		/// DisconnectLobby() 後に StationId が null になることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator DisconnectLobby_ClearsStationId() => UniTask.ToCoroutine(async () =>
		{
			IRoomSettings settings = NetworkService.Instance.PrepareRoomSettings();
			settings.RoomName = TrimRoomName("DisconnectTestLobby");
			settings.MaxPlayers = 4;

			bool created = await NetworkService.Instance.CreateLobby(settings);
			Assert.IsTrue(created, "ロビーの作成に失敗しました。");

			await NetworkService.Instance.DisconnectLobby();
			_testCreatedLobby = false;

			Assert.IsNull(NetworkService.Instance.StationId, "DisconnectLobby() 後は StationId が null のはずです。");
		});

		// -----------------------------------------------------------------------
		// 共通テスト: Room
		// -----------------------------------------------------------------------

		/// <summary>
		/// CreateRoom() が成功し、IsHost が true になることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator CreateRoom_ReturnsTrueAndSetsIsHost() => UniTask.ToCoroutine(async () =>
		{
			IRoomSettings settings = NetworkService.Instance.PrepareRoomSettings();
			settings.RoomName = TrimRoomName($"IntegTestRoom_{System.Guid.NewGuid():N}");
			settings.MaxPlayers = 4;

			bool result = await NetworkService.Instance.CreateRoom(settings);
			_testCreatedRoom = result;

			Assert.IsTrue(result, "Network.CreateRoom() が false を返しました。");
			Assert.IsTrue(NetworkService.Instance.IsHost, "CreateRoom() 後は IsHost が true のはずです。");
		});

		/// <summary>
		/// DisconnectRoom() 後に IsHost が false になることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator DisconnectRoom_ClearsIsHost() => UniTask.ToCoroutine(async () =>
		{
			IRoomSettings settings = NetworkService.Instance.PrepareRoomSettings();
			settings.RoomName = TrimRoomName($"DisconTestRoom_{System.Guid.NewGuid():N}");
			settings.MaxPlayers = 4;

			bool created = await NetworkService.Instance.CreateRoom(settings);
			Assert.IsTrue(created, "ルームの作成に失敗しました。");

			await NetworkService.Instance.DisconnectRoom();
			_testCreatedRoom = false;

			Assert.IsFalse(NetworkService.Instance.IsHost, "DisconnectRoom() 後は IsHost が false のはずです。");
		});
	}
}
