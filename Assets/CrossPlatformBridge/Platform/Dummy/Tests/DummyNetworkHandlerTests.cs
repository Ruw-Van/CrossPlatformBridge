using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Platform.Dummy.Network;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Tests.EditMode.Dummy
{
    /// <summary>
    /// DummyNetworkHandler の EditMode テスト。
    /// </summary>
    public class DummyNetworkHandlerTests
    {
        private NetworkHandler _handler;
        private RoomSettings _roomSettings;

        [SetUp]
        public void SetUp()
        {
            _handler = new NetworkHandler();
            _roomSettings = new RoomSettings
            {
                RoomName = "TestRoom",
                MaxPlayers = 4,
                IsVisible = true,
                IsOpen = true,
                Id = "test-room-id"
            };
        }

        // ----------------------------------------------------------------
        // Initialize / Shutdown （同期なので [Test] のまま）
        // ----------------------------------------------------------------

        [Test]
        public void Initialize_SetsIsConnectedTrue()
        {
            bool result = _handler.Initialize(new NetworkSettings());

            Assert.IsTrue(result, "Initialize は true を返す必要があります");
            Assert.IsTrue(_handler.IsConnected, "Initialize 後は IsConnected が true になる必要があります");
        }

        [Test]
        public void Initialize_SetsAccountId()
        {
            _handler.Initialize(new NetworkSettings());

            Assert.IsNotNull(_handler.AccountId, "AccountId が設定される必要があります");
            StringAssert.StartsWith("dummyUser_", _handler.AccountId.ToString(), "AccountId は 'dummyUser_' で始まる必要があります");
        }

        [Test]
        public void Initialize_FiresOnNetworkConnectionStatusChanged()
        {
            bool? receivedStatus = null;
            _handler.OnNetworkConnectionStatusChanged += status => receivedStatus = status;

            _handler.Initialize(new NetworkSettings());

            Assert.IsNotNull(receivedStatus, "OnNetworkConnectionStatusChanged が発火する必要があります");
            Assert.IsTrue(receivedStatus.Value, "接続状態は true である必要があります");
        }

        [Test]
        public void Shutdown_ResetsState()
        {
            _handler.Initialize(new NetworkSettings());
            _handler.Shutdown();

            Assert.IsFalse(_handler.IsConnected, "Shutdown 後は IsConnected が false になる必要があります");
            Assert.IsFalse(_handler.IsHost, "Shutdown 後は IsHost が false になる必要があります");
            Assert.IsNull(_handler.AccountId, "Shutdown 後は AccountId が null になる必要があります");
            Assert.IsNull(_handler.NickName, "Shutdown 後は NickName が null になる必要があります");
        }

        [Test]
        public void Shutdown_FiresDisconnectEvents()
        {
            _handler.Initialize(new NetworkSettings());

            bool? connectionStatus = null;
            bool? hostStatus = null;
            _handler.OnNetworkConnectionStatusChanged += s => connectionStatus = s;
            _handler.OnHostStatusChanged += s => hostStatus = s;

            _handler.Shutdown();

            Assert.AreEqual(false, connectionStatus, "OnNetworkConnectionStatusChanged(false) が発火する必要があります");
            Assert.AreEqual(false, hostStatus, "OnHostStatusChanged(false) が発火する必要があります");
        }

        // ----------------------------------------------------------------
        // Connect / Disconnect
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Connect_WhenAlreadyConnected_ReturnsTrueWithoutChangingState() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            string originalAccountId = _handler.AccountId.ToString();

            bool result = await _handler.Connect(new NetworkSettings());

            Assert.IsTrue(result, "既接続時の Connect は true を返す必要があります");
            Assert.AreEqual(originalAccountId, _handler.AccountId.ToString(), "AccountId が変更されてはいけません");
        });

        [UnityTest]
        public IEnumerator Disconnect_SetsIsConnectedFalse() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            await _handler.Disconnect();

            Assert.IsFalse(_handler.IsConnected, "Disconnect 後は IsConnected が false になる必要があります");
        });

        [UnityTest]
        public IEnumerator Disconnect_FiresEvents() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            bool? connectionStatus = null;
            _handler.OnNetworkConnectionStatusChanged += s => connectionStatus = s;

            await _handler.Disconnect();

            Assert.AreEqual(false, connectionStatus, "Disconnect 後に OnNetworkConnectionStatusChanged(false) が発火する必要があります");
        });

        // ----------------------------------------------------------------
        // Lobby
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator CreateLobby_WhenConnected_ReturnsTrue() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            bool result = await _handler.CreateLobby(_roomSettings);

            Assert.IsTrue(result, "接続済みの場合、CreateLobby は true を返す必要があります");
        });

        [UnityTest]
        public IEnumerator CreateLobby_SetsIsHostTrue() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            await _handler.CreateLobby(_roomSettings);

            Assert.IsTrue(_handler.IsHost, "CreateLobby 後は IsHost が true になる必要があります");
        });

        [UnityTest]
        public IEnumerator CreateLobby_WhenNotConnected_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            // 未接続時は Debug.LogError が出ることを想定内として宣言
            LogAssert.Expect(UnityEngine.LogType.Error, "DummyNetworkHandler: 接続されていません。ロビーを作成できません。");

            bool result = await _handler.CreateLobby(_roomSettings);

            Assert.IsFalse(result, "未接続の場合、CreateLobby は false を返す必要があります");
        });

        [UnityTest]
        public IEnumerator CreateLobby_FiresOnLobbyOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            string operationType = null;
            bool? success = null;
            string lobbyId = null;
            _handler.OnLobbyOperationCompleted += (op, s, id) => { operationType = op; success = s; lobbyId = id; };

            await _handler.CreateLobby(_roomSettings);

            Assert.AreEqual("CreateLobby", operationType);
            Assert.IsTrue(success.Value);
            Assert.IsNotNull(lobbyId);
        });

        [UnityTest]
        public IEnumerator CreateLobby_AddsHostToConnectedList() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            string connectedId = null;
            _handler.OnPlayerConnected += (id, name) => connectedId = id;

            await _handler.CreateLobby(_roomSettings);

            Assert.IsNotNull(connectedId, "ホスト自身が OnPlayerConnected で通知される必要があります");
            Assert.AreEqual(_handler.AccountId.ToString(), connectedId);
        });

        [UnityTest]
        public IEnumerator ConnectLobby_WhenConnected_ReturnsTrue() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            bool result = await _handler.ConnectLobby(_roomSettings);

            Assert.IsTrue(result, "接続済みの場合、ConnectLobby は true を返す必要があります");
        });

        [UnityTest]
        public IEnumerator ConnectLobby_SetsIsHostFalse() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            await _handler.ConnectLobby(_roomSettings);

            Assert.IsFalse(_handler.IsHost, "ConnectLobby 後は IsHost が false になる必要があります");
        });

        [UnityTest]
        public IEnumerator DisconnectLobby_ResetsLobbyState() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            await _handler.CreateLobby(_roomSettings);

            await _handler.DisconnectLobby();

            Assert.IsFalse(_handler.IsHost, "DisconnectLobby 後は IsHost が false になる必要があります");
            Assert.IsNull(_handler.StationId, "DisconnectLobby 後は StationId が null になる必要があります");
        });

        [UnityTest]
        public IEnumerator DisconnectLobby_FiresOnLobbyOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            await _handler.CreateLobby(_roomSettings);

            string operationType = null;
            bool? success = null;
            _handler.OnLobbyOperationCompleted += (op, s, id) => { operationType = op; success = s; };

            await _handler.DisconnectLobby();

            Assert.AreEqual("DisconnectLobby", operationType);
            Assert.IsTrue(success.Value);
        });

        [UnityTest]
        public IEnumerator SearchLobby_ReturnsResults() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            _roomSettings.RoomName = "Dummy";

            List<object> results = await _handler.SearchLobby(_roomSettings);

            Assert.IsNotNull(results);
            Assert.Greater(results.Count, 0, "「Dummy」を含むロビーが検索結果に含まれる必要があります");
        });

        [UnityTest]
        public IEnumerator SearchLobby_WithEmptyQuery_ReturnsAll() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            _roomSettings.RoomName = "";

            List<object> results = await _handler.SearchLobby(_roomSettings);

            Assert.AreEqual(3, results.Count, "空クエリでは全ロビーが返される必要があります");
        });

        [UnityTest]
        public IEnumerator SearchLobby_WithNonMatchingQuery_ReturnsEmpty() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            _roomSettings.RoomName = "NonExistentLobby_XYZ";

            List<object> results = await _handler.SearchLobby(_roomSettings);

            Assert.AreEqual(0, results.Count, "マッチしないクエリでは空リストが返される必要があります");
        });

        // ----------------------------------------------------------------
        // Room
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator CreateRoom_WhenConnected_ReturnsTrue() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            bool result = await _handler.CreateRoom(_roomSettings);

            Assert.IsTrue(result);
        });

        [UnityTest]
        public IEnumerator CreateRoom_FiresOnRoomOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            string operationType = null;
            bool? success = null;
            _handler.OnRoomOperationCompleted += (op, s, id) => { operationType = op; success = s; };

            await _handler.CreateRoom(_roomSettings);

            Assert.AreEqual("CreateRoom", operationType);
            Assert.IsTrue(success.Value);
        });

        [UnityTest]
        public IEnumerator ConnectRoom_WhenConnected_ReturnsTrue() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            bool result = await _handler.ConnectRoom(_roomSettings);

            Assert.IsTrue(result);
        });

        [UnityTest]
        public IEnumerator DisconnectRoom_FiresOnRoomOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            await _handler.CreateRoom(_roomSettings);

            string operationType = null;
            bool? success = null;
            _handler.OnRoomOperationCompleted += (op, s, id) => { operationType = op; success = s; };

            await _handler.DisconnectRoom();

            Assert.AreEqual("DisconnectRoom", operationType);
            Assert.IsTrue(success.Value);
        });

        [UnityTest]
        public IEnumerator SearchRoom_DelegatesToSearchLobby() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            _roomSettings.RoomName = "";

            List<object> results = await _handler.SearchRoom(_roomSettings);

            Assert.AreEqual(3, results.Count, "SearchRoom は SearchLobby に委譲し、同じ結果を返す必要があります");
        });

        [UnityTest]
        public IEnumerator CreateRoom_WhenNotConnected_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(UnityEngine.LogType.Error, "DummyNetworkHandler: 接続されていません。ルームを作成できません。");

            bool result = await _handler.CreateRoom(_roomSettings);

            Assert.IsFalse(result, "未接続の場合、CreateRoom は false を返す必要があります");
        });

        [UnityTest]
        public IEnumerator CreateRoom_DoesNotFireOnLobbyOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());

            bool lobbyEventFired = false;
            _handler.OnLobbyOperationCompleted += (op, s, id) => lobbyEventFired = true;

            await _handler.CreateRoom(_roomSettings);

            Assert.IsFalse(lobbyEventFired, "CreateRoom は OnLobbyOperationCompleted を発火させてはいけません");
        });

        [UnityTest]
        public IEnumerator ConnectRoom_WhenNotConnected_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(UnityEngine.LogType.Error, "DummyNetworkHandler: 接続されていません。ルームに接続できません。");

            bool result = await _handler.ConnectRoom(_roomSettings);

            Assert.IsFalse(result, "未接続の場合、ConnectRoom は false を返す必要があります");
        });

        [UnityTest]
        public IEnumerator DisconnectRoom_DoesNotFireOnLobbyOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            await _handler.CreateRoom(_roomSettings);

            bool lobbyEventFired = false;
            _handler.OnLobbyOperationCompleted += (op, s, id) => lobbyEventFired = true;

            await _handler.DisconnectRoom();

            Assert.IsFalse(lobbyEventFired, "DisconnectRoom は OnLobbyOperationCompleted を発火させてはいけません");
        });

        // ----------------------------------------------------------------
        // SendData
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator SendData_FiresOnDataReceived_WithSameData() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            byte[] sentData = new byte[] { 1, 2, 3, 4, 5 };
            byte[] receivedData = null;
            _handler.OnDataReceived += (data, _) => receivedData = data;

            await _handler.SendData(sentData);

            Assert.IsNotNull(receivedData, "OnDataReceived が発火する必要があります");
            Assert.AreEqual(sentData, receivedData, "送信したデータと受信したデータが一致する必要があります");
        });

        // ----------------------------------------------------------------
        // SettingsFactory
        // ----------------------------------------------------------------

        [Test]
        public void SettingsFactory_IsNotNull()
        {
            Assert.IsNotNull(_handler.SettingsFactory, "SettingsFactory は null であってはいけません");
        }

        [Test]
        public void SettingsFactory_CreateRoomSettings_ReturnsNewInstance()
        {
            IRoomSettings settings1 = _handler.SettingsFactory.CreateRoomSettings();
            IRoomSettings settings2 = _handler.SettingsFactory.CreateRoomSettings();

            Assert.IsNotNull(settings1);
            Assert.IsNotNull(settings2);
            Assert.AreNotSame(settings1, settings2, "毎回新しいインスタンスが返される必要があります");
        }

        // ----------------------------------------------------------------
        // キャンセル機能
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator CreateLobby_CancelBeforeStart_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            await _handler.Connect(new NetworkSettings());

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // 事前にキャンセル済み

            bool result = await _handler.CreateLobby(_roomSettings, cts.Token);

            Assert.IsFalse(result, "キャンセル済みトークンでは false が返る必要があります");
        });

        [UnityTest]
        public IEnumerator CreateLobby_CancelDuringOperation_ReturnsFalseAndFiresEvent() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            await _handler.Connect(new NetworkSettings());

            using var cts = new CancellationTokenSource();
            string receivedOperation = null;
            bool receivedSuccess = true;
            string receivedMessage = null;
            _handler.OnLobbyOperationCompleted += (op, success, msg) =>
            {
                receivedOperation = op;
                receivedSuccess = success;
                receivedMessage = msg;
            };

            // 操作を開始してすぐにキャンセル
            var task = _handler.CreateLobby(_roomSettings, cts.Token);
            cts.Cancel();
            bool result = await task;

            Assert.IsFalse(result, "キャンセル時は false が返る必要があります");
            Assert.AreEqual("CreateLobby", receivedOperation, "操作名が正しい必要があります");
            Assert.IsFalse(receivedSuccess, "キャンセル時はイベントの success が false である必要があります");
            Assert.AreEqual("Cancelled", receivedMessage, "メッセージが 'Cancelled' である必要があります");
        });

        [UnityTest]
        public IEnumerator ConnectLobby_CancelDuringOperation_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            await _handler.Connect(new NetworkSettings());

            using var cts = new CancellationTokenSource();
            var task = _handler.ConnectLobby(_roomSettings, cts.Token);
            cts.Cancel();
            bool result = await task;

            Assert.IsFalse(result, "キャンセル時は false が返る必要があります");
        });

        [UnityTest]
        public IEnumerator CreateRoom_CancelDuringOperation_ReturnsFalseAndFiresEvent() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            await _handler.Connect(new NetworkSettings());

            using var cts = new CancellationTokenSource();
            string receivedOperation = null;
            bool receivedSuccess = true;
            string receivedMessage = null;
            _handler.OnRoomOperationCompleted += (op, success, msg) =>
            {
                receivedOperation = op;
                receivedSuccess = success;
                receivedMessage = msg;
            };

            var task = _handler.CreateRoom(_roomSettings, cts.Token);
            cts.Cancel();
            bool result = await task;

            Assert.IsFalse(result, "キャンセル時は false が返る必要があります");
            Assert.AreEqual("CreateRoom", receivedOperation, "操作名が正しい必要があります");
            Assert.IsFalse(receivedSuccess, "キャンセル時はイベントの success が false である必要があります");
            Assert.AreEqual("Cancelled", receivedMessage, "メッセージが 'Cancelled' である必要があります");
        });

        [UnityTest]
        public IEnumerator ConnectRoom_CancelDuringOperation_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            _handler.Initialize(new NetworkSettings());
            await _handler.Connect(new NetworkSettings());

            using var cts = new CancellationTokenSource();
            var task = _handler.ConnectRoom(_roomSettings, cts.Token);
            cts.Cancel();
            bool result = await task;

            Assert.IsFalse(result, "キャンセル時は false が返る必要があります");
        });
    }
}
