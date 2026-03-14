using System;
using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Platform.Dummy.Network
{
	[Serializable]
	/// <summary>
	/// ダミーのネットワーク設定クラス。
	/// </summary>
	public class NetworkSettings : NetworkSettingsScriptableObjectBase
	{
		// ダミーのネットワーク設定クラス。特にプロパティはありません。
	}

	/// <summary>
	/// ダミーのネットワークルーム設定クラス。INetworkRoomSettings を実装します。
	/// </summary>
	[Serializable]
	public class RoomSettings : IRoomSettings
	{
		[SerializeField] private int _maxPlayers = 2;
		[SerializeField] private bool _isVisible = true;
		[SerializeField] private bool _isOpen = true;
		[SerializeField] private Dictionary<string, object> _customProperties = new Dictionary<string, object>();

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
		/// ルームID。
		/// </summary>
		public object Id { get; set; } = null;
		/// <summary>
		/// ルーム名。
		/// </summary>
		public string RoomName { get; set; } = "";
		/// <summary>
		/// プレイヤーデータ。
		/// </summary>
		public PlayerData PlayerData { get; set; } = new();

		public RoomSettings()
		{
		}

		/// <summary>
		/// INetworkRoomSettings の値から DummyRoomSettings を構築します。
		/// </summary>
		public RoomSettings(IRoomSettings baseSettings)
		{
			MaxPlayers = baseSettings.MaxPlayers;
			IsVisible = baseSettings.IsVisible;
			IsOpen = baseSettings.IsOpen;
			CustomProperties = new Dictionary<string, object>(baseSettings.CustomProperties);
		}
	}

}
