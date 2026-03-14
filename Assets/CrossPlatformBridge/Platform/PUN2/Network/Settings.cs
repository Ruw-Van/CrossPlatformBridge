#if USE_CROSSPLATFORMBRIDGE_PUN2
using System;
using System.Collections.Generic;
using CrossPlatformBridge.Services.Network;
using UnityEngine;
using Photon.Realtime;
using Cysharp.Threading.Tasks; // INetworkSettings を使用するため

namespace CrossPlatformBridge.Platform.PUN2.Network
{
	[Serializable]
	/// <summary>
	/// PUN2用ネットワーク設定を保持するクラス。
	/// </summary>
	public class NetworkSettings : NetworkSettingsScriptableObjectBase
	{
		// PUN2の特定の設定はここでは必要ないため、空のクラスとして定義
	}

	public partial class NetworkHandler
	{
		TimeoutController timeoutController = new TimeoutController();
		private RoomList roomList = new();
	}

	/// <summary>
	/// PUN2のRoomOptionsをラップし、INetworkSettingsインターフェースを実装するクラス。
	/// </summary>
	[Serializable]
	public class RoomSettings : IRoomSettings
	{
		[SerializeField] private int _maxPlayers = 4;
		[SerializeField] private bool _isVisible = true;
		[SerializeField] private bool _isOpen = true;
		[SerializeField] private Dictionary<string, object> _customProperties = new Dictionary<string, object>();
		[SerializeField] private string[] _customPropertiesForLobby = { "gameMode" }; // PUN2特有

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
		public PlayerData PlayerData { get; set; } = new();

		/// <summary>
		/// PUN2特有のプロパティ。ロビーで表示されるカスタムプロパティのキー。
		/// </summary>
		public string[] CustomPropertiesForLobby { get => _customPropertiesForLobby; set => _customPropertiesForLobby = value; }

		public TypedLobby ToTypedLobby(LobbyType lobbyType)
		{
			return lobbyType switch {
				LobbyType.Default => new TypedLobby(RoomName, LobbyType.Default),
				LobbyType.SqlLobby => new TypedLobby(RoomName, LobbyType.SqlLobby),
				LobbyType.AsyncRandomLobby => new TypedLobby(RoomName, LobbyType.AsyncRandomLobby),
				_ => new TypedLobby(RoomName, LobbyType.Default),
			};
		}

		public RoomSettings()
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
		public RoomSettings(IRoomSettings baseSettings)
		{
			MaxPlayers = baseSettings.MaxPlayers;
			IsVisible = baseSettings.IsVisible;
			IsOpen = baseSettings.IsOpen;
			CustomProperties = new Dictionary<string, object>(baseSettings.CustomProperties);

			// INetworkSettings に含まれないPun2特有のプロパティはデフォルト値を使用するか、
			// もし baseSettings が Pun2RoomSettings であればキャストして取得
			if (baseSettings is RoomSettings pun2SpecificSettings)
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

#endif
