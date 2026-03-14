#if USE_CROSSPLATFORMBRIDGE_NETCODE
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Platform.Netcode.Network;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.Netcode.Tests
{
    /// <summary>
    /// Unity Netcode for GameObjects の Settings / SettingsFactory / NetworkHandler に関する EditMode テスト。
    /// 実際のネットワーク操作テスト（Connect / CreateLobby 等）は
    /// Unity Gaming Services (Lobby, Relay, Authentication) への接続が必要なため、
    /// 統合テスト環境で行うこと。
    /// </summary>
    public class NetcodeNetworkHandlerTests
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
        }

        [Test]
        public void NetworkHandler_CanBeInstantiated()
        {
            Assert.IsNotNull(_handler, "NetworkHandler は new でインスタンス化できる必要があります");
        }

        // ----------------------------------------------------------------
        // CreateLobby / ConnectLobby — Netcode は Lobby 概念なし → 常に true
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator CreateLobby_Netcode_AlwaysReturnsTrue() => UniTask.ToCoroutine(async () =>
        {
            // Netcode は Lobby 概念を持たないため、CreateLobby は常に即時成功する
            bool result = await _handler.CreateLobby(_roomSettings);

            Assert.IsTrue(result, "Netcode の CreateLobby は常に true を返す必要があります");
        });

        [UnityTest]
        public IEnumerator CreateLobby_Netcode_FiresOnLobbyOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            string receivedOperation = null;
            bool? receivedSuccess = null;
            _handler.OnLobbyOperationCompleted += (op, s, _) => { receivedOperation = op; receivedSuccess = s; };

            await _handler.CreateLobby(_roomSettings);

            Assert.AreEqual("CreateLobby", receivedOperation);
            Assert.IsTrue(receivedSuccess.Value, "Netcode の CreateLobby イベントは success=true で発火する必要があります");
        });

        [UnityTest]
        public IEnumerator ConnectLobby_Netcode_AlwaysReturnsTrue() => UniTask.ToCoroutine(async () =>
        {
            // Netcode は Lobby 概念を持たないため、ConnectLobby は常に即時成功する
            bool result = await _handler.ConnectLobby(_roomSettings);

            Assert.IsTrue(result, "Netcode の ConnectLobby は常に true を返す必要があります");
        });

        [UnityTest]
        public IEnumerator ConnectLobby_Netcode_FiresOnLobbyOperationCompleted() => UniTask.ToCoroutine(async () =>
        {
            string receivedOperation = null;
            bool? receivedSuccess = null;
            _handler.OnLobbyOperationCompleted += (op, s, _) => { receivedOperation = op; receivedSuccess = s; };

            await _handler.ConnectLobby(_roomSettings);

            Assert.AreEqual("ConnectLobby", receivedOperation);
            Assert.IsTrue(receivedSuccess.Value, "Netcode の ConnectLobby イベントは success=true で発火する必要があります");
        });

        // ----------------------------------------------------------------
        // RoomSettings デフォルト値
        // ----------------------------------------------------------------

        [Test]
        public void RoomSettings_DefaultValues_AreCorrect()
        {
            var settings = new RoomSettings();

            Assert.AreEqual(4, settings.MaxPlayers, "デフォルトの MaxPlayers は 4 である必要があります");
            Assert.IsTrue(settings.IsVisible, "デフォルトの IsVisible は true である必要があります");
            Assert.IsTrue(settings.IsOpen, "デフォルトの IsOpen は true である必要があります");
            Assert.AreEqual("", settings.RoomName, "デフォルトの RoomName は空文字列である必要があります");
            Assert.IsNull(settings.Id, "デフォルトの Id は null である必要があります");
        }

        [Test]
        public void RoomSettings_DefaultCustomProperties_ContainsGameMode()
        {
            var settings = new RoomSettings();

            Assert.IsTrue(settings.CustomProperties.ContainsKey("gameMode"), "デフォルトのカスタムプロパティに 'gameMode' が含まれる必要があります");
            Assert.AreEqual("Default", settings.CustomProperties["gameMode"]);
        }

        [Test]
        public void RoomSettings_PlayerData_IsInitialized()
        {
            var settings = new RoomSettings();

            Assert.IsNotNull(settings.PlayerData, "PlayerData は null であってはいけません");
        }

        // ----------------------------------------------------------------
        // RoomSettings プロパティ設定
        // ----------------------------------------------------------------

        [Test]
        public void RoomSettings_CanSetMaxPlayers()
        {
            var settings = new RoomSettings();
            settings.MaxPlayers = 16;

            Assert.AreEqual(16, settings.MaxPlayers);
        }

        [Test]
        public void RoomSettings_CanSetRoomName()
        {
            var settings = new RoomSettings();
            settings.RoomName = "NetcodeRoom";

            Assert.AreEqual("NetcodeRoom", settings.RoomName);
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
            var props = new Dictionary<string, object> { { "scene", "lobby" } };
            settings.CustomProperties = props;

            Assert.AreEqual(1, settings.CustomProperties.Count);
            Assert.AreEqual("lobby", settings.CustomProperties["scene"]);
        }

        // ----------------------------------------------------------------
        // RoomSettings コピーコンストラクタ
        // ----------------------------------------------------------------

        [Test]
        public void RoomSettings_CopyConstructor_CopiesValues()
        {
            var original = new RoomSettings
            {
                MaxPlayers = 10,
                IsVisible = false,
                IsOpen = false,
            };
            original.CustomProperties["key"] = "value";

            var copy = new RoomSettings(original);

            Assert.AreEqual(10, copy.MaxPlayers);
            Assert.IsFalse(copy.IsVisible);
            Assert.IsFalse(copy.IsOpen);
        }

        [Test]
        public void RoomSettings_CopyConstructor_DeepCopiesCustomProperties()
        {
            var original = new RoomSettings();
            original.CustomProperties["key"] = "value";

            var copy = new RoomSettings(original);
            copy.CustomProperties["key"] = "modified";

            Assert.AreEqual("value", original.CustomProperties["key"], "コピー元のカスタムプロパティが変更されてはいけません");
        }

        // ----------------------------------------------------------------
        // NetworkSettings
        // ----------------------------------------------------------------

        [Test]
        public void NetworkSettings_CanBeCreated()
        {
            var settings = ScriptableObject.CreateInstance<NetworkSettings>();

            Assert.IsNotNull(settings, "NetworkSettings は ScriptableObject.CreateInstance で生成できる必要があります");

            Object.DestroyImmediate(settings);
        }

        // ----------------------------------------------------------------
        // NetcodeSettingsFactory
        // ----------------------------------------------------------------

        [Test]
        public void NetcodeSettingsFactory_CreateRoomSettings_ReturnsRoomSettings()
        {
            var factory = new NetcodeSettingsFactory();

            IRoomSettings settings = factory.CreateRoomSettings();

            Assert.IsNotNull(settings);
            Assert.IsInstanceOf<RoomSettings>(settings);
        }

        [Test]
        public void NetcodeSettingsFactory_CreateRoomSettings_ReturnsUniqueInstances()
        {
            var factory = new NetcodeSettingsFactory();

            IRoomSettings s1 = factory.CreateRoomSettings();
            IRoomSettings s2 = factory.CreateRoomSettings();

            Assert.AreNotSame(s1, s2, "CreateRoomSettings は毎回異なるインスタンスを返す必要があります");
        }

        [Test]
        public void NetcodeSettingsFactory_CreateNetworkSettings_WithExisting_ReturnsSameInstance()
        {
            var factory = new NetcodeSettingsFactory();
            var existing = ScriptableObject.CreateInstance<NetworkSettings>();

            var result = factory.CreateNetworkSettings(existing);

            Assert.AreSame(existing, result, "既存の設定を渡した場合、同じインスタンスが返される必要があります");

            Object.DestroyImmediate(existing);
        }

        // ----------------------------------------------------------------
        // TODO: 統合テスト（Unity Gaming Services / 実ネットワーク必須）
        // ----------------------------------------------------------------
        // - NetworkHandler.Initialize() / Shutdown()
        // - NetworkHandler.Connect() (UGS Authentication が必要)
        // - NetworkHandler.CreateLobby() / ConnectLobby() (UGS Lobby が必要)
        // - NetworkHandler.CreateRoom() / ConnectRoom() (UGS Relay + Netcode が必要)
        // - NetworkHandler.SendData()
    }
}

#endif
