// Assets/Scripts/CrossPlatformBridge/Network/DummyNetworkHandler/DummySettings.cs
using System;
using System.Collections.Generic;
using UnityEngine; // For Debug.Log

namespace CrossPlatformBridge.Network.DummyNetworkHandler
{
	/// <summary>
	/// ダミーのネットワークルーム設定クラス。INetworkRoomSettings を実装します。
	/// </summary>
	[Serializable]
	public class DummySettings : INetworkSettings
	{
		[SerializeField] private int _maxPlayers = 2;
		[SerializeField] private bool _isVisible = true;
		[SerializeField] private bool _isOpen = true;
		[SerializeField] private Dictionary<string, object> _customProperties = new Dictionary<string, object>();

		public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }
		public bool IsVisible { get => _isVisible; set => _isVisible = value; }
		public bool IsOpen { get => _isOpen; set => _isOpen = value; }
		public Dictionary<string, object> CustomProperties { get => _customProperties; set => _customProperties = value; }

		public DummySettings()
		{
			if (_customProperties.Count == 0)
			{
				_customProperties.Add("dummyMode", "DefaultDummyGame");
			}
		}

		/// <summary>
		/// INetworkRoomSettings の値から DummyRoomSettings を構築します。
		/// </summary>
		public DummySettings(INetworkSettings baseSettings)
		{
			MaxPlayers = baseSettings.MaxPlayers;
			IsVisible = baseSettings.IsVisible;
			IsOpen = baseSettings.IsOpen;
			CustomProperties = new Dictionary<string, object>(baseSettings.CustomProperties);
		}
	}

}
