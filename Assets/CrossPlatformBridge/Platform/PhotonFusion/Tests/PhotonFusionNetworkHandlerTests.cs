#if USE_CROSSPLATFORMBRIDGE_PHOTONFUSION
using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Platform.PhotonFusion.Network;
using NUnit.Framework;
using UnityEngine;

namespace CrossPlatformBridge.Tests.EditMode.PhotonFusion
{
    /// <summary>
    /// PhotonFusion の Settings / SettingsFactory / NetworkHandler に関する EditMode テスト。
    /// 実際のネットワーク操作テスト（Connect / CreateLobby 等）は
    /// Photon Fusion サーバーへの接続が必要なため、統合テスト環境で行うこと。
    /// </summary>
    public class PhotonFusionNetworkHandlerTests
    {
        // ----------------------------------------------------------------
        // RoomSettings デフォルト値
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
            settings.RoomName = "FusionRoom";

            Assert.AreEqual("FusionRoom", settings.RoomName);
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
            var props = new Dictionary<string, object> { { "map", "arena" } };
            settings.CustomProperties = props;

            Assert.AreEqual(1, settings.CustomProperties.Count);
            Assert.AreEqual("arena", settings.CustomProperties["map"]);
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
        // PhotonFusionSettingsFactory
        // ----------------------------------------------------------------

        [Test]
        public void PhotonFusionSettingsFactory_CreateRoomSettings_ReturnsRoomSettings()
        {
            var factory = new PhotonFusionSettingsFactory();

            IRoomSettings settings = factory.CreateRoomSettings();

            Assert.IsNotNull(settings);
            Assert.IsInstanceOf<RoomSettings>(settings);
        }

        [Test]
        public void PhotonFusionSettingsFactory_CreateRoomSettings_ReturnsUniqueInstances()
        {
            var factory = new PhotonFusionSettingsFactory();

            IRoomSettings s1 = factory.CreateRoomSettings();
            IRoomSettings s2 = factory.CreateRoomSettings();

            Assert.AreNotSame(s1, s2, "CreateRoomSettings は毎回異なるインスタンスを返す必要があります");
        }

        [Test]
        public void PhotonFusionSettingsFactory_CreateNetworkSettings_WithExisting_ReturnsSameInstance()
        {
            var factory = new PhotonFusionSettingsFactory();
            var existing = ScriptableObject.CreateInstance<NetworkSettings>();

            var result = factory.CreateNetworkSettings(existing);

            Assert.AreSame(existing, result, "既存の設定を渡した場合、同じインスタンスが返される必要があります");

            Object.DestroyImmediate(existing);
        }

        // ----------------------------------------------------------------
        // NetworkHandler インスタンス化
        // ----------------------------------------------------------------

        [Test]
        public void NetworkHandler_CanBeInstantiated()
        {
            var handler = new NetworkHandler();

            Assert.IsNotNull(handler, "NetworkHandler は new でインスタンス化できる必要があります");
        }

        [Test]
        public void NetworkHandler_SettingsFactory_IsNotNull()
        {
            var handler = new NetworkHandler();

            Assert.IsNotNull(handler.SettingsFactory, "SettingsFactory は null であってはいけません");
        }

        [Test]
        public void NetworkHandler_InitialState_IsNotConnected()
        {
            var handler = new NetworkHandler();

            Assert.IsFalse(handler.IsConnected, "初期状態では IsConnected は false である必要があります");
            Assert.IsFalse(handler.IsHost, "初期状態では IsHost は false である必要があります");
        }

        // ----------------------------------------------------------------
        // TODO: 統合テスト（実 Photon Fusion SDK / サーバー必須）
        // ----------------------------------------------------------------
        // - NetworkHandler.Initialize() / Shutdown()
        // - NetworkHandler.Connect() / Disconnect()
        // - NetworkHandler.CreateLobby() / ConnectLobby()
        // - NetworkHandler.CreateRoom() / ConnectRoom()
        // - NetworkHandler.SendData()
    }
}

#endif
