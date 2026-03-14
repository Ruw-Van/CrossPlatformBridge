#if USE_CROSSPLATFORMBRIDGE_EOS
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Platform.EOS.Network;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.EOS.Tests
{
    /// <summary>
    /// EOS NetworkHandler の EditMode テスト。
    ///
    /// テストの目的は、EOS NetworkHandler が IInternalNetworkHandler 契約を
    /// 正しく実装していることを CrossPlatformBridge の視点から確認すること。
    ///
    /// 実際の EOS サーバー接続（Connect / CreateLobby 等）は
    /// IntegrationTests フォルダの PlayMode テストで検証する。
    /// </summary>
    public class EOSNetworkHandlerTests
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
            };
        }

        // ----------------------------------------------------------------
        // 初期状態（IInternalNetworkHandler 契約）
        // ----------------------------------------------------------------

        [Test]
        public void NetworkHandler_InitialState_IsNotConnected()
        {
            Assert.IsFalse(_handler.IsConnected, "初期状態では IsConnected は false である必要があります");
            Assert.IsFalse(_handler.IsHost, "初期状態では IsHost は false である必要があります");
            Assert.IsNull(_handler.AccountId, "初期状態では AccountId は null である必要があります");
            Assert.IsNull(_handler.NickName, "初期状態では NickName は null である必要があります");
            Assert.IsNull(_handler.StationId, "初期状態では StationId は null である必要があります");
        }

        [Test]
        public void NetworkHandler_InitialState_ConnectedListIsEmpty()
        {
            Assert.IsNotNull(_handler.ConnectedList, "ConnectedList は null であってはいけません");
            Assert.AreEqual(0, _handler.ConnectedList.Count, "初期状態では ConnectedList は空である必要があります");
        }

        [Test]
        public void NetworkHandler_SettingsFactory_IsNotNull()
        {
            Assert.IsNotNull(_handler.SettingsFactory, "SettingsFactory は null であってはいけません");
        }

        // ----------------------------------------------------------------
        // Shutdown — 未初期化ハンドラへの呼び出しが冪等であること
        // ----------------------------------------------------------------

        [Test]
        public void Shutdown_OnFreshHandler_StateRemainsClean()
        {
            // Initialize 未実施のハンドラへの Shutdown は例外を出さず、状態が clean のままであること
            Assert.DoesNotThrow(() => _handler.Shutdown());

            Assert.IsFalse(_handler.IsConnected);
            Assert.IsFalse(_handler.IsHost);
            Assert.IsNull(_handler.AccountId);
            Assert.IsNull(_handler.StationId);
        }

        // ----------------------------------------------------------------
        // Connect — Platform 未初期化時のエラーパス
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Connect_WithoutPlatform_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            // Initialize() を呼ばずに Connect() → _platform == null のためエラーになる
            LogAssert.Expect(LogType.Error, "EOS: Platform が初期化されていません");

            bool result = await _handler.Connect(ScriptableObject.CreateInstance<NetworkSettings>());

            Assert.IsFalse(result, "Platform 未初期化時は Connect が false を返す必要があります");
            Assert.IsFalse(_handler.IsConnected);
        });

        [UnityTest]
        public IEnumerator Connect_WithoutPlatform_FiresOnNetworkConnectionStatusChanged() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform が初期化されていません");

            bool? receivedStatus = null;
            _handler.OnNetworkConnectionStatusChanged += status => receivedStatus = status;

            await _handler.Connect(ScriptableObject.CreateInstance<NetworkSettings>());

            Assert.IsNotNull(receivedStatus, "OnNetworkConnectionStatusChanged が発火する必要があります");
            Assert.IsFalse(receivedStatus.Value, "失敗時は false が通知される必要があります");
        });

        // ----------------------------------------------------------------
        // CreateLobby — 未接続時のエラーパス
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator CreateLobby_WhenNotInitialized_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform または ProductUserId が未初期化です");

            bool result = await _handler.CreateLobby(_roomSettings);

            Assert.IsFalse(result, "未初期化時は CreateLobby が false を返す必要があります");
        });

        [UnityTest]
        public IEnumerator CreateLobby_WhenNotInitialized_FiresOnLobbyOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform または ProductUserId が未初期化です");

            string receivedOperation = null;
            bool? receivedSuccess = null;
            string receivedMessage = null;
            _handler.OnLobbyOperationCompleted += (op, s, msg) =>
            {
                receivedOperation = op;
                receivedSuccess = s;
                receivedMessage = msg;
            };

            await _handler.CreateLobby(_roomSettings);

            Assert.AreEqual("CreateLobby", receivedOperation, "操作名が正しい必要があります");
            Assert.IsFalse(receivedSuccess.Value, "失敗時は success=false が通知される必要があります");
            Assert.AreEqual("Not initialized", receivedMessage);
        });

        // ----------------------------------------------------------------
        // ConnectLobby — 未接続時のエラーパス
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator ConnectLobby_WhenNotInitialized_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform または ProductUserId が未初期化です");

            bool result = await _handler.ConnectLobby(_roomSettings);

            Assert.IsFalse(result, "未初期化時は ConnectLobby が false を返す必要があります");
        });

        [UnityTest]
        public IEnumerator ConnectLobby_WhenNotInitialized_FiresOnLobbyOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform または ProductUserId が未初期化です");

            string receivedOperation = null;
            bool? receivedSuccess = null;
            _handler.OnLobbyOperationCompleted += (op, s, _) => { receivedOperation = op; receivedSuccess = s; };

            await _handler.ConnectLobby(_roomSettings);

            Assert.AreEqual("ConnectLobby", receivedOperation);
            Assert.IsFalse(receivedSuccess.Value);
        });

        // ----------------------------------------------------------------
        // SearchLobby — 未接続時のエラーパス
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator SearchLobby_WhenNotInitialized_ReturnsEmptyList() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform または ProductUserId が未初期化です");

            List<object> results = await _handler.SearchLobby(_roomSettings);

            Assert.IsNotNull(results, "SearchLobby は null ではなく空リストを返す必要があります");
            Assert.AreEqual(0, results.Count, "未初期化時は空リストが返される必要があります");
        });

        // ----------------------------------------------------------------
        // CreateRoom — 未接続時のエラーパス
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator CreateRoom_WhenNotInitialized_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform または ProductUserId が未初期化です");

            bool result = await _handler.CreateRoom(_roomSettings);

            Assert.IsFalse(result, "未初期化時は CreateRoom が false を返す必要があります");
        });

        [UnityTest]
        public IEnumerator CreateRoom_WhenNotInitialized_FiresOnRoomOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform または ProductUserId が未初期化です");

            string receivedOperation = null;
            bool? receivedSuccess = null;
            string receivedMessage = null;
            _handler.OnRoomOperationCompleted += (op, s, msg) =>
            {
                receivedOperation = op;
                receivedSuccess = s;
                receivedMessage = msg;
            };

            await _handler.CreateRoom(_roomSettings);

            Assert.AreEqual("CreateRoom", receivedOperation, "操作名が正しい必要があります");
            Assert.IsFalse(receivedSuccess.Value, "失敗時は success=false が通知される必要があります");
            Assert.AreEqual("Not initialized", receivedMessage);
        });

        // ----------------------------------------------------------------
        // ConnectRoom — 未接続時のエラーパス
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator ConnectRoom_WhenNotInitialized_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform または ProductUserId が未初期化です");

            bool result = await _handler.ConnectRoom(_roomSettings);

            Assert.IsFalse(result, "未初期化時は ConnectRoom が false を返す必要があります");
        });

        [UnityTest]
        public IEnumerator ConnectRoom_WhenNotInitialized_FiresOnRoomOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, "EOS: Platform または ProductUserId が未初期化です");

            string receivedOperation = null;
            bool? receivedSuccess = null;
            _handler.OnRoomOperationCompleted += (op, s, _) => { receivedOperation = op; receivedSuccess = s; };

            await _handler.ConnectRoom(_roomSettings);

            Assert.AreEqual("ConnectRoom", receivedOperation);
            Assert.IsFalse(receivedSuccess.Value);
        });

        // ----------------------------------------------------------------
        // RoomSettings — IRoomSettings 契約をEOS実装が満たすことの確認
        // ----------------------------------------------------------------

        [Test]
        public void RoomSettings_DefaultValues_AreCorrect()
        {
            var settings = new RoomSettings();

            Assert.AreEqual(8, settings.MaxPlayers, "デフォルトの MaxPlayers は 8 である必要があります");
            Assert.IsTrue(settings.IsVisible, "デフォルトの IsVisible は true である必要があります");
            Assert.IsTrue(settings.IsOpen, "デフォルトの IsOpen は true である必要があります");
            Assert.AreEqual("", settings.RoomName, "デフォルトの RoomName は空文字列である必要があります");
            Assert.IsNull(settings.Id, "デフォルトの Id は null である必要があります");
            Assert.AreEqual("", settings.LobbyId, "デフォルトの LobbyId は空文字列である必要があります");
        }

        [Test]
        public void RoomSettings_PlayerData_IsInitialized()
        {
            var settings = new RoomSettings();

            Assert.IsNotNull(settings.PlayerData, "PlayerData は null であってはいけません");
        }

        [Test]
        public void RoomSettings_CanSetMaxPlayers()
        {
            var settings = new RoomSettings();
            settings.MaxPlayers = 32;

            Assert.AreEqual(32, settings.MaxPlayers);
        }

        [Test]
        public void RoomSettings_CanSetRoomName()
        {
            var settings = new RoomSettings();
            settings.RoomName = "EOSRoom";

            Assert.AreEqual("EOSRoom", settings.RoomName);
        }

        [Test]
        public void RoomSettings_CanSetLobbyId()
        {
            var settings = new RoomSettings();
            settings.LobbyId = "eos-lobby-abc123";

            Assert.AreEqual("eos-lobby-abc123", settings.LobbyId);
        }

        [Test]
        public void RoomSettings_CanSetVisibility()
        {
            var settings = new RoomSettings();
            settings.IsVisible = false;

            Assert.IsFalse(settings.IsVisible);
        }

        [Test]
        public void RoomSettings_CanSetIsOpen()
        {
            var settings = new RoomSettings();
            settings.IsOpen = false;

            Assert.IsFalse(settings.IsOpen);
        }

        [Test]
        public void RoomSettings_CanSetCustomProperties()
        {
            var settings = new RoomSettings();
            var props = new Dictionary<string, object> { { "region", "Asia" } };
            settings.CustomProperties = props;

            Assert.AreEqual(1, settings.CustomProperties.Count);
            Assert.AreEqual("Asia", settings.CustomProperties["region"]);
        }

        // ----------------------------------------------------------------
        // NetworkSettings — ScriptableObject として生成できること
        // ----------------------------------------------------------------

        [Test]
        public void NetworkSettings_CanBeCreated()
        {
            var settings = ScriptableObject.CreateInstance<NetworkSettings>();

            Assert.IsNotNull(settings, "NetworkSettings は ScriptableObject.CreateInstance で生成できる必要があります");

            Object.DestroyImmediate(settings);
        }

        // ----------------------------------------------------------------
        // EOSSettingsFactory — INetworkSettingsFactory 契約の確認
        // ----------------------------------------------------------------

        [Test]
        public void EOSSettingsFactory_CreateRoomSettings_ReturnsRoomSettings()
        {
            var factory = new EOSSettingsFactory();

            IRoomSettings settings = factory.CreateRoomSettings();

            Assert.IsNotNull(settings);
            Assert.IsInstanceOf<RoomSettings>(settings);
        }

        [Test]
        public void EOSSettingsFactory_CreateRoomSettings_ReturnsUniqueInstances()
        {
            var factory = new EOSSettingsFactory();

            IRoomSettings s1 = factory.CreateRoomSettings();
            IRoomSettings s2 = factory.CreateRoomSettings();

            Assert.AreNotSame(s1, s2, "CreateRoomSettings は毎回異なるインスタンスを返す必要があります");
        }

        [Test]
        public void EOSSettingsFactory_CreateNetworkSettings_WithExisting_ReturnsSameInstance()
        {
            var factory = new EOSSettingsFactory();
            var existing = ScriptableObject.CreateInstance<NetworkSettings>();

            var result = factory.CreateNetworkSettings(existing);

            Assert.AreSame(existing, result, "既存の設定を渡した場合、同じインスタンスが返される必要があります");

            Object.DestroyImmediate(existing);
        }
    }
}

#endif
