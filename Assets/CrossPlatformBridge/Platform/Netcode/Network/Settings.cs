#if USE_CROSSPLATFORMBRIDGE_NETCODE
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using CrossPlatformBridge.Services.Network;

namespace CrossPlatformBridge.Platform.Netcode.Network
{
	/// <summary>
	/// Netcode for GameObjects用ネットワーク設定を保持するクラス。
	/// </summary>
	[Serializable]
	public class NetworkSettings : NetworkSettingsScriptableObjectBase
	{
		// Netcode for GameObjects の設定は特に必要ないため、空のクラスとして定義
	}

	/// <summary>
	/// Netcode for GameObjects のLobbyオプションをラップし、IRoomSettingsインターフェースを実装するクラス。
	/// </summary>
	[Serializable]
	public class RoomSettings : IRoomSettings
	{
		[SerializeField] private int _maxPlayers = 4;
		[SerializeField] private bool _isVisible = true; // LobbyOptions.IsPrivateの逆
		[SerializeField] private bool _isOpen = true;     // LobbyOptions.IsLockedの逆
		[SerializeField] private Dictionary<string, object> _customProperties = new Dictionary<string, object>();
		[SerializeField] private Dictionary<string, object> _playerProperties = new Dictionary<string, object>();
		[SerializeField] private Dictionary<string, PlayerDataObject> _playerData = new Dictionary<string, PlayerDataObject>(); // Lobby特有

		/// <summary>
		/// ルームID。
		/// </summary>
		public object Id { get; set; } = null;
		/// <summary>
		/// ルーム名。
		/// </summary>
		public string RoomName { get; set; } = "";
		/// <summary>
		/// 最大プレイヤー数。
		/// </summary>
		public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }
		/// <summary>
		/// ルームが公開かどうか。
		/// </summary>
		public bool IsVisible { get => _isVisible; set => _isVisible = value; }
		/// <summary>
		/// ルームがオープンかどうか。
		/// </summary>
		public bool IsOpen { get => _isOpen; set => _isOpen = value; }
		/// <summary>
		/// カスタムプロパティ。
		/// </summary>
		public Dictionary<string, object> CustomProperties { get => _customProperties; set => _customProperties = value; }
		/// <summary>
		/// プレイヤーデータ。
		/// </summary>
		public PlayerData PlayerData { get; set; } = new();
		/// <summary>
		/// Lobbyサービス特有のプレイヤーデータ。
		/// </summary>
		public Dictionary<string, PlayerDataObject> PlayerDataDict { get => _playerData; set => _playerData = value; }

		public RoomSettings()
		{
			// デフォルトのカスタムプロパティ
			if (_customProperties.Count == 0)
			{
				_customProperties.Add("gameMode", "Default");
			}
		}

		/// <summary>
		/// IRoomSettings の値から RoomSettings を構築します。
		/// </summary>
		public RoomSettings(IRoomSettings baseSettings)
		{
			MaxPlayers = baseSettings.MaxPlayers;
			IsVisible = baseSettings.IsVisible;
			IsOpen = baseSettings.IsOpen;
			CustomProperties = new Dictionary<string, object>(baseSettings.CustomProperties);

			if (baseSettings is RoomSettings netcodeSpecificSettings)
			{
				PlayerData = netcodeSpecificSettings.PlayerData;
			}
			else
			{
				PlayerDataDict = new Dictionary<string, PlayerDataObject>();
			}
		}

		/// <summary>
		/// この設定オブジェクトから Unity.Services.Lobbies.Models.CreateLobbyOptions を生成します。
		/// </summary>
		public CreateLobbyOptions ToCreateLobbyOptions()
		{
			// カスタムプロパティをDictionary<string, DataObject>に変換
			var dataObjects = new Dictionary<string, DataObject>();
			foreach (var prop in CustomProperties)
			{
				dataObjects.Add(prop.Key, new DataObject(visibility: DataObject.VisibilityOptions.Public, value: prop.Value.ToString()));
			}

			return new CreateLobbyOptions
			{
				IsPrivate = !IsVisible, // IsVisible の逆
				IsLocked = !IsOpen,     // IsOpen の逆
				Data = dataObjects,
				Player = new()
				{
					Data = new Dictionary<string, PlayerDataObject>()
					{
						{ "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerData.Name) }
					}
				}
			};
		}

		/// <summary>
		/// この設定オブジェクトから Unity.Services.Lobbies.Models.JoinLobbyByIdOptions を生成します。
		/// </summary>
		public JoinLobbyByIdOptions ToJoinLobbyByIdOptions()
		{
			return new JoinLobbyByIdOptions
			{
				Password = null,
				Player = new Player()
				{
					Data = new Dictionary<string, PlayerDataObject>()
					{
						{ "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerData.Name) }
					}
				}
			};
		}

		/// <summary>
		/// この設定オブジェクトから Unity.Services.Lobbies.Models.JoinLobbyByCodeOptions を生成します。
		/// </summary>
		public JoinLobbyByCodeOptions ToJoinLobbyByCodeOptions()
		{
			return new JoinLobbyByCodeOptions
			{
				// その他のオプションもここに追加可能
			};
		}
	}
}

#endif
