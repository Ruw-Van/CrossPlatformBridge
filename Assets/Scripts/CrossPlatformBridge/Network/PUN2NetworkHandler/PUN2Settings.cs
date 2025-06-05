// Assets/Scripts/CrossPlatformBridge/Network/PUN2NetworkHandler/PUN2Settings.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using CrossPlatformBridge.Network; // INetworkSettings を使用するため

namespace CrossPlatformBridge.Network.PUN2NetworkHandler
{
	/// <summary>
	/// PUN2のRoomOptionsをラップし、INetworkSettingsインターフェースを実装するクラス。
	/// </summary>
	[Serializable] // Unityインスペクターで表示できるように
	public class PUN2Settings : INetworkSettings
	{
		[SerializeField] private int _maxPlayers = 4;
		[SerializeField] private bool _isVisible = true;
		[SerializeField] private bool _isOpen = true;
		[SerializeField] private Dictionary<string, object> _customProperties = new Dictionary<string, object>();
		[SerializeField] private string[] _customPropertiesForLobby = { "gameMode" }; // PUN2特有

		public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }
		public bool IsVisible { get => _isVisible; set => _isVisible = value; }
		public bool IsOpen { get => _isOpen; set => _isOpen = value; }
		public Dictionary<string, object> CustomProperties { get => _customProperties; set => _customProperties = value; }

		/// <summary>
		/// PUN2特有のプロパティ。ロビーで表示されるカスタムプロパティのキー。
		/// </summary>
		public string[] CustomPropertiesForLobby { get => _customPropertiesForLobby; set => _customPropertiesForLobby = value; }

		public PUN2Settings()
		{
			// デフォルトのカスタムプロパティ
			if (_customProperties == null)
			{
				_customProperties = new Dictionary<string, object>();
			}
			if (_customProperties.Count == 0)
			{
				_customProperties.Add("gameMode", "Default");
			}
		}

		/// <summary>
		/// INetworkSettings の値から Pun2RoomSettings を構築します。
		/// </summary>
		public PUN2Settings(INetworkSettings baseSettings)
		{
			MaxPlayers = baseSettings.MaxPlayers;
			IsVisible = baseSettings.IsVisible;
			IsOpen = baseSettings.IsOpen;
			CustomProperties = new Dictionary<string, object>(baseSettings.CustomProperties);

			// INetworkSettings に含まれないPun2特有のプロパティはデフォルト値を使用するか、
			// もし baseSettings が Pun2RoomSettings であればキャストして取得
			if (baseSettings is PUN2Settings pun2SpecificSettings)
			{
				CustomPropertiesForLobby = pun2SpecificSettings.CustomPropertiesForLobby;
			}
			else
			{
				// ここではデフォルト値を設定 (例: CustomPropertiesForLobby)
				CustomPropertiesForLobby = new string[] { "gameMode" };
			}
		}

		/// <summary>
		/// この設定オブジェクトから Photon.Realtime.RoomOptions を生成します。
		/// </summary>
		public RoomOptions ToRoomOptions()
		{
			var PhotonProperties = new ExitGames.Client.Photon.Hashtable();
			foreach (var item in CustomProperties)
			{
				PhotonProperties.Add(item.Key, item.Value);
			}
			return new RoomOptions
			{
				MaxPlayers = (byte)MaxPlayers,
				IsVisible = IsVisible,
				IsOpen = IsOpen,
				CustomRoomProperties = PhotonProperties,
				CustomRoomPropertiesForLobby = CustomPropertiesForLobby
			};
		}
	}
}
