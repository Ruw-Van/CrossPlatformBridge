#if USE_CROSSPLATFORMBRIDGE_EOS
using System;
using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Network
{
	/// <summary>
	/// EOS用ネットワーク設定。
	/// 認証情報は Unity メニュー "EOS Plugin/EOS Configuration" で管理されるため、
	/// このクラスには EOS 固有の追加設定のみを記述してください。
	/// </summary>
	[Serializable]
	public class NetworkSettings : NetworkSettingsScriptableObjectBase
	{
		// 認証情報 (ProductId / SandboxId / DeploymentId / ClientId / ClientSecret) は
		// "EOS Plugin/EOS Configuration" で設定してください。
	}

	public partial class NetworkHandler
	{
		// 内部状態・ヘルパーフィールドはここに追加する
	}

	/// <summary>
	/// EOSのルーム（ロビー）設定。
	/// </summary>
	[Serializable]
	public class RoomSettings : IRoomSettings
	{
		[SerializeField] private int _maxPlayers = 8;
		[SerializeField] private bool _isVisible = true;
		[SerializeField] private bool _isOpen = true;
		[SerializeField] private Dictionary<string, object> _customProperties = new Dictionary<string, object>();

		public object Id { get; set; } = null;
		public string RoomName { get; set; } = "";
		public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }
		public bool IsVisible { get => _isVisible; set => _isVisible = value; }
		public bool IsOpen { get => _isOpen; set => _isOpen = value; }
		public Dictionary<string, object> CustomProperties { get => _customProperties; set => _customProperties = value; }
		public PlayerData PlayerData { get; set; } = new();

		/// <summary>EOS ロビーID（JoinLobbyById に使用）</summary>
		public string LobbyId { get; set; } = "";
	}
}

#endif
