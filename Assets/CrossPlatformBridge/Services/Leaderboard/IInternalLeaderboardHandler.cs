using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CrossPlatformBridge.Services.Leaderboard
{
	/// <summary>
	/// プラットフォーム固有のリーダーボード処理を抽象化するインターフェース。
	/// グローバルランキング（クラウド永続）とセッション内ランキング（ルーム内一時）の両方を提供します。
	/// </summary>
	public interface IInternalLeaderboardHandler
	{
		// ── グローバルランキング ──────────────────────────────────

		/// <summary>
		/// 現在のプレイヤーのスコアをリーダーボードに送信します。
		/// </summary>
		/// <param name="leaderboardName">リーダーボード名（プラットフォームの設定IDと一致させる）</param>
		/// <param name="score">送信するスコア</param>
		UniTask<bool> SubmitScore(string leaderboardName, long score);

		/// <summary>
		/// リーダーボードの上位エントリを取得します。
		/// </summary>
		/// <param name="leaderboardName">リーダーボード名</param>
		/// <param name="count">取得するエントリ数</param>
		UniTask<List<LeaderboardEntry>> GetTopEntries(string leaderboardName, int count);

		/// <summary>
		/// 指定プレイヤーのリーダーボードエントリを取得します。
		/// </summary>
		/// <param name="leaderboardName">リーダーボード名</param>
		/// <param name="playerId">対象プレイヤーのID</param>
		UniTask<LeaderboardEntry> GetPlayerEntry(string leaderboardName, string playerId);

		/// <summary>
		/// 指定プレイヤーを中心とした前後のエントリを取得します。
		/// </summary>
		/// <param name="leaderboardName">リーダーボード名</param>
		/// <param name="playerId">中心とするプレイヤーのID</param>
		/// <param name="range">上下に取得する件数（range=2なら合計最大5件）</param>
		UniTask<List<LeaderboardEntry>> GetEntriesAroundPlayer(string leaderboardName, string playerId, int range);

		// ── セッション内ランキング ────────────────────────────────

		/// <summary>
		/// セッション（ルーム）内でのプレイヤーのスコアを更新します。
		/// セッション終了後はデータが消えます。
		/// </summary>
		/// <param name="playerId">プレイヤーID</param>
		/// <param name="playerName">プレイヤー表示名</param>
		/// <param name="score">スコア</param>
		UniTask<bool> UpdateSessionScore(string playerId, string playerName, long score);

		/// <summary>
		/// セッション内の全プレイヤーのランキングをスコア降順で返します。
		/// </summary>
		UniTask<List<LeaderboardEntry>> GetSessionLeaderboard();

		/// <summary>
		/// セッション内での指定プレイヤーの順位を返します（1始まり）。
		/// 存在しない場合は -1 を返します。
		/// </summary>
		/// <param name="playerId">プレイヤーID</param>
		UniTask<int> GetPlayerSessionRank(string playerId);

		/// <summary>
		/// セッション内のスコアをすべてリセットします。
		/// </summary>
		UniTask ResetSessionLeaderboard();
	}
}
