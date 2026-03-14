namespace CrossPlatformBridge.Services.Leaderboard
{
	/// <summary>
	/// リーダーボードの1エントリを表すデータクラス。
	/// </summary>
	public class LeaderboardEntry
	{
		/// <summary>プレイヤーのプラットフォーム固有ID</summary>
		public string PlayerId { get; set; }

		/// <summary>プレイヤーの表示名</summary>
		public string PlayerName { get; set; }

		/// <summary>スコア（大きいほど上位）</summary>
		public long Score { get; set; }

		/// <summary>順位（1始まり）</summary>
		public int Rank { get; set; }
	}
}
