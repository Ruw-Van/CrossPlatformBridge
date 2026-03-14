#if USE_CROSSPLATFORMBRIDGE_PHOTONFUSION
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using UnityEngine;

namespace CrossPlatformBridge.Platform.PhotonFusion.Network
{
	/// <summary>
	/// PhotonFusion用ネットワーク設定を保持するクラス。
	/// </summary>
	[Serializable]
	public class NetworkSettings : NetworkSettingsScriptableObjectBase
	{
		// 必要に応じてPhotonFusion特有の設定プロパティを追加してください。
	}

	public partial class NetworkHandler
	{
		TimeoutController timeoutController = new TimeoutController();
		private RoomList roomList = new();
	}

	/// <summary>
	/// PhotonFusionのルーム設定をラップし、IRoomSettingsインターフェースを実装するクラス。
	/// </summary>
	[Serializable]
	public class RoomSettings : IRoomSettings
	{
		[SerializeField] private int _maxPlayers = 8;
		[SerializeField] private bool _isVisible = true;
		[SerializeField] private bool _isOpen = true;
		[SerializeField] private Dictionary<string, object> _customProperties = new Dictionary<string, object>();

		public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }
		public bool IsVisible { get => _isVisible; set => _isVisible = value; }
		public bool IsOpen { get => _isOpen; set => _isOpen = value; }
		public Dictionary<string, object> CustomProperties { get => _customProperties; set => _customProperties = value; }

		public object Id { get; set; } = null;
		public string RoomName { get; set; } = "";
		public PlayerData PlayerData { get; set; } = new();

		// 必要に応じてPhotonFusion特有のプロパティを追加してください。
	}
}

#endif
