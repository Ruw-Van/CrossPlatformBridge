using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Platform.Dummy.Network;
using NUnit.Framework;

namespace CrossPlatformBridge.Tests.EditMode.Dummy
{
    /// <summary>
    /// RoomSettings および DummySettingsFactory の EditMode テスト。
    /// </summary>
    public class RoomSettingsTests
    {
        // ----------------------------------------------------------------
        // RoomSettings デフォルト値
        // ----------------------------------------------------------------

        [Test]
        public void RoomSettings_DefaultValues_AreCorrect()
        {
            var settings = new RoomSettings();

            Assert.AreEqual(2, settings.MaxPlayers, "デフォルトの MaxPlayers は 2 である必要があります");
            Assert.IsTrue(settings.IsVisible, "デフォルトの IsVisible は true である必要があります");
            Assert.IsTrue(settings.IsOpen, "デフォルトの IsOpen は true である必要があります");
            Assert.AreEqual("", settings.RoomName, "デフォルトの RoomName は空文字列である必要があります");
            Assert.IsNull(settings.Id, "デフォルトの Id は null である必要があります");
        }

        [Test]
        public void RoomSettings_DefaultCustomProperties_ContainsDummyMode()
        {
            var settings = new RoomSettings();

            Assert.IsTrue(settings.CustomProperties.ContainsKey("dummyMode"), "デフォルトのカスタムプロパティに 'dummyMode' が含まれる必要があります");
            Assert.AreEqual("DefaultDummyGame", settings.CustomProperties["dummyMode"]);
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
                RoomName = "OriginalRoom"
            };
            original.CustomProperties["key"] = "value";

            var copy = new RoomSettings(original);

            Assert.AreEqual(6, copy.MaxPlayers);
            Assert.IsFalse(copy.IsVisible);
            Assert.IsFalse(copy.IsOpen);
            // RoomName と Id はコピーコンストラクタの対象外（IRoomSettings 由来のプロパティのみ）
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
        // DummySettingsFactory
        // ----------------------------------------------------------------

        [Test]
        public void DummySettingsFactory_CreateRoomSettings_ReturnsRoomSettings()
        {
            var factory = new DummySettingsFactory();

            IRoomSettings settings = factory.CreateRoomSettings();

            Assert.IsNotNull(settings);
            Assert.IsInstanceOf<RoomSettings>(settings);
        }

        [Test]
        public void DummySettingsFactory_CreateRoomSettings_ReturnsUniqueInstances()
        {
            var factory = new DummySettingsFactory();

            IRoomSettings s1 = factory.CreateRoomSettings();
            IRoomSettings s2 = factory.CreateRoomSettings();

            Assert.AreNotSame(s1, s2, "CreateRoomSettings は毎回異なるインスタンスを返す必要があります");
        }

        [Test]
        public void DummySettingsFactory_CreateNetworkSettings_WithExisting_ReturnsSameInstance()
        {
            var factory = new DummySettingsFactory();
            var existing = new NetworkSettings();

            var result = factory.CreateNetworkSettings(existing);

            Assert.AreSame(existing, result, "既存の設定を渡した場合、同じインスタンスが返される必要があります");
        }

        // ----------------------------------------------------------------
        // PlayerData
        // ----------------------------------------------------------------

        [Test]
        public void PlayerData_CanSetIdAndName()
        {
            var player = new PlayerData
            {
                Id = "player-001",
                Name = "TestPlayer"
            };

            Assert.AreEqual("player-001", player.Id);
            Assert.AreEqual("TestPlayer", player.Name);
        }

        [Test]
        public void PlayerData_PlayerProperties_DefaultIsEmpty()
        {
            var player = new PlayerData();

            Assert.IsNotNull(player.PlayerProperties, "PlayerProperties はデフォルトで初期化されている必要があります");
            Assert.AreEqual(0, player.PlayerProperties.Count, "PlayerProperties はデフォルトで空の辞書である必要があります");
        }
    }
}
