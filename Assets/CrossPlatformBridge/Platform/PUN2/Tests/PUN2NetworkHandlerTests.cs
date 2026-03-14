#if USE_CROSSPLATFORMBRIDGE_PUN2
using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Platform.PUN2.Network;
using NUnit.Framework;
using UnityEngine;

namespace CrossPlatformBridge.Tests.EditMode.PUN2
{
    /// <summary>
    /// PUN2 の Settings / SettingsFactory に関する EditMode テスト。
    /// NetworkHandler は MonoBehaviourPunCallbacks (MonoBehaviour) を継承するため、
    /// 実際のネットワーク操作テストは統合テスト環境で行うこと。
    /// </summary>
    public class PUN2NetworkHandlerTests
    {
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
            settings.MaxPlayers = 8;

            Assert.AreEqual(8, settings.MaxPlayers);
        }

        [Test]
        public void RoomSettings_CanSetRoomName()
        {
            var settings = new RoomSettings();
            settings.RoomName = "MyRoom";

            Assert.AreEqual("MyRoom", settings.RoomName);
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
            var props = new Dictionary<string, object> { { "level", 5 } };
            settings.CustomProperties = props;

            Assert.AreEqual(1, settings.CustomProperties.Count);
            Assert.AreEqual(5, settings.CustomProperties["level"]);
        }

        // ----------------------------------------------------------------
        // RoomSettings コピーコンストラクタ
        // ----------------------------------------------------------------

        [Test]
        public void RoomSettings_CopyConstructor_CopiesValues()
        {
            var original = new RoomSettings
            {
                MaxPlayers = 6,
                IsVisible = false,
                IsOpen = false,
            };
            original.CustomProperties["key"] = "value";

            var copy = new RoomSettings(original);

            Assert.AreEqual(6, copy.MaxPlayers);
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
        // PUN2SettingsFactory
        // ----------------------------------------------------------------

        [Test]
        public void PUN2SettingsFactory_CreateRoomSettings_ReturnsRoomSettings()
        {
            var factory = new PUN2SettingsFactory();

            IRoomSettings settings = factory.CreateRoomSettings();

            Assert.IsNotNull(settings);
            Assert.IsInstanceOf<RoomSettings>(settings);
        }

        [Test]
        public void PUN2SettingsFactory_CreateRoomSettings_ReturnsUniqueInstances()
        {
            var factory = new PUN2SettingsFactory();

            IRoomSettings s1 = factory.CreateRoomSettings();
            IRoomSettings s2 = factory.CreateRoomSettings();

            Assert.AreNotSame(s1, s2, "CreateRoomSettings は毎回異なるインスタンスを返す必要があります");
        }

        [Test]
        public void PUN2SettingsFactory_CreateNetworkSettings_WithExisting_ReturnsSameInstance()
        {
            var factory = new PUN2SettingsFactory();
            var existing = ScriptableObject.CreateInstance<NetworkSettings>();

            var result = factory.CreateNetworkSettings(existing);

            Assert.AreSame(existing, result, "既存の設定を渡した場合、同じインスタンスが返される必要があります");

            Object.DestroyImmediate(existing);
        }

        // ----------------------------------------------------------------
        // TODO: 統合テスト（実 PUN2 SDK / Photon サーバー必須）
        // ----------------------------------------------------------------
        // - NetworkHandler.Initialize() / Shutdown()
        // - NetworkHandler.Connect() / Disconnect()
        // - NetworkHandler.CreateLobby() / ConnectLobby()
        // - NetworkHandler.CreateRoom() / ConnectRoom()
        // - NetworkHandler.SendData()
    }
}

#endif
