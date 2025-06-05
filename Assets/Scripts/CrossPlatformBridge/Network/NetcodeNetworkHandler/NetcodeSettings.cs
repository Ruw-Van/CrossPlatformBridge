// Assets/Scripts/CrossPlatformBridge/Network/NetcodeNetworkHandler/NetcodeSettings.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using CrossPlatformBridge.Network;

namespace CrossPlatformBridge.Network.NetcodeNetworkHandler
{
	/// <summary>
	/// Netcode for GameObjects のLobbyオプションをラップし、INetworkSettingsインターフェースを実装するクラス。
	/// </summary>
	[Serializable] // Unityインスペクターで表示できるように
	public class NetcodeSettings : INetworkSettings
	{
		[SerializeField] private int _maxPlayers = 4;
		[SerializeField] private bool _isVisible = true; // LobbyOptions.IsPrivateの逆
		[SerializeField] private bool _isOpen = true;     // LobbyOptions.IsLockedの逆
		[SerializeField] private Dictionary<string, object> _customProperties = new Dictionary<string, object>();
		[SerializeField] private Dictionary<string, PlayerDataObject> _playerData = new Dictionary<string, PlayerDataObject>(); // Lobby特有

		public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }
		public bool IsVisible { get => _isVisible; set => _isVisible = value; }
		public bool IsOpen { get => _isOpen; set => _isOpen = value; }
		public Dictionary<string, object> CustomProperties { get => _customProperties; set => _customProperties = value; }

		/// <summary>
		/// Lobbyサービス特有のプレイヤーデータ。
		/// </summary>
		public Dictionary<string, PlayerDataObject> PlayerData { get => _playerData; set => _playerData = value; }

		public NetcodeSettings()
		{
			// デフォルトのカスタムプロパティ
			if (_customProperties.Count == 0)
			{
				_customProperties.Add("gameMode", "Default");
			}
		}

		/// <summary>
		/// INetworkSettings の値から NetcodeSettings を構築します。
		/// </summary>
		public NetcodeSettings(INetworkSettings baseSettings)
		{
			MaxPlayers = baseSettings.MaxPlayers;
			IsVisible = baseSettings.IsVisible;
			IsOpen = baseSettings.IsOpen;
			CustomProperties = new Dictionary<string, object>(baseSettings.CustomProperties);

			// INetworkSettings に含まれないNetcode特有のプロパティはデフォルト値を使用するか、
			// もし baseSettings が NetcodeSettings であればキャストして取得
			if (baseSettings is NetcodeSettings netcodeSpecificSettings)
			{
				PlayerData = netcodeSpecificSettings.PlayerData;
			}
			else
			{
				PlayerData = new Dictionary<string, PlayerDataObject>();
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
				// その他のオプションもここに追加可能
			};
		}

		/// <summary>
		/// この設定オブジェクトから Unity.Services.Lobbies.Models.JoinLobbyByIdOptions を生成します。
		/// (JoinLobbyByIdOptionsはルーム作成オプションではないが、プレイヤーデータなどを渡す場合に利用)
		/// </summary>
		public JoinLobbyByIdOptions ToJoinLobbyByIdOptions()
		{
			return new JoinLobbyByIdOptions
			{
				// その他のオプションもここに追加可能
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
