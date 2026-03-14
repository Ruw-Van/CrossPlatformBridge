using System.Collections.Generic;
using UnityEngine;
using static CrossPlatformBridge.Services.Network.NetworkSettingsScriptableObjectBase;

namespace CrossPlatformBridge.Services.Network
{
	public abstract class NetworkSettingsScriptableObjectBase : ScriptableObject, INetworkSettings
	{
		public string NickName { get; set; } = "";
	}

	/// <summary>
	/// ネットワーク設定インターフェース。
	/// 各ネットワーク実装はこのインターフェースを継承した具体的な設定クラスを提供します。
	/// </summary>
	public interface INetworkSettings
	{
		string NickName { get; set; }
	}

	/// <summary>
	/// ネットワークルーム（ロビー）作成のための汎用的な設定インターフェース。
	/// 各ネットワーク実装はこのインターフェースを継承した具体的な設定クラスを提供します。
	/// </summary>
	public interface IRoomSettings
	{
		object Id { get; set; }

		/// <summary>ルーム名。</summary>
		string RoomName { get; set; }

		/// <summary>最大プレイヤー数。</summary>
		int MaxPlayers { get; set; }

		/// <summary>ロビー一覧でルームが見えるようにするかどうか。</summary>
		bool IsVisible { get; set; }

		/// <summary>ルームに参加可能かどうか。</summary>
		bool IsOpen { get; set; }

		/// <summary>カスタムルームプロパティの辞書。</summary>
		Dictionary<string, object> CustomProperties { get; set; }

		PlayerData PlayerData { get; set; }
	}

	public class PlayerData
	{
		public string Id;
		public string Name;
		public Dictionary<string, object> PlayerProperties = new Dictionary<string, object>();
	}
}
