// Assets/Scripts/CrossPlatformBridge/Network/INetworkRoomSettings.cs
using System.Collections.Generic;

namespace CrossPlatformBridge.Network
{
	/// <summary>
	/// ネットワークルーム（ロビー）作成のための汎用的な設定インターフェース。
	/// 各ネットワーク実装はこのインターフェースを継承した具体的な設定クラスを提供します。
	/// </summary>
	public interface INetworkSettings
	{
		/// <summary>最大プレイヤー数。</summary>
		int MaxPlayers { get; set; }

		/// <summary>ロビー一覧でルームが見えるようにするかどうか。</summary>
		bool IsVisible { get; set; }

		/// <summary>ルームに参加可能かどうか。</summary>
		bool IsOpen { get; set; }

		/// <summary>カスタムルームプロパティの辞書。</summary>
		Dictionary<string, object> CustomProperties { get; set; }
	}
}
