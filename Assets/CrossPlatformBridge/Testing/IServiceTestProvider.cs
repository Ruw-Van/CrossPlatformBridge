using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CrossPlatformBridge.Testing
{
	/// <summary>
	/// ServiceTestUI がプラットフォームハンドラからテスト操作とデフォルトデータを取得するためのインターフェース。
	/// 各プラットフォームのサービスハンドラに実装し、テスト内容をプラットフォーム側で管理します。
	/// Network・ScreenShot サービスのハンドラは GetTestOperations() で null を返し、
	/// ServiceTestUI 側の従来実装にフォールバックさせることができます。
	/// </summary>
	public interface IServiceTestProvider
	{
		/// <summary>
		/// テスト操作の一覧を返します。
		/// null または空リストを返すと ServiceTestUI の従来実装にフォールバックします。
		/// </summary>
		IReadOnlyList<TestOperation> GetTestOperations();

		/// <summary>
		/// テスト用デフォルト入力値を返します。
		/// ServiceTestUI はプラットフォームが選択されたときにこの値で入力フィールドを初期化します。
		/// </summary>
		TestDefaultData GetDefaultData();
	}

	/// <summary>
	/// テスト操作の定義。SectionLabel が設定されている場合はセクションヘッダとして描画されます。
	/// </summary>
	public class TestOperation
	{
		/// <summary>セクションヘッダ表示用ラベル（null の場合はボタン）</summary>
		public string SectionLabel;

		/// <summary>ボタンラベル（SectionLabel が null の場合に使用）</summary>
		public string Label;

		/// <summary>ボタン押下時に実行される非同期アクション</summary>
		public Func<TestOperationContext, UniTask> Action;
	}

	/// <summary>
	/// テスト用デフォルト入力値。プラットフォームごとに固有の値を設定できます。
	/// null の場合は ServiceTestUI のビルトイン初期値が使用されます。
	/// </summary>
	public class TestDefaultData
	{
		// Network / 共通
		public string UserName = "Player1";
		public string LobbyRoomName = "MyLobby";
		public string SendData = "Hello World!";

		// CloudStorage
		public string CloudStorageKey = "myKey";
		public string CloudStorageValue = "myValue";

		// Payment
		public string PaymentCurrencyCode = "GD";
		public string PaymentPrice = "0";

		// Achievement
		public string AchievementId = "achievement_001";

		// Leaderboard
		public string LeaderboardName = "leaderboard_001";
		public string LeaderboardScore = "1000";
	}

	/// <summary>
	/// テスト操作実行時に渡されるコンテキスト。UI の入力値と結果報告コールバックを含みます。
	/// </summary>
	public class TestOperationContext
	{
		// 入力値（UI フィールドの現在値）
		public string UserName;
		public string LobbyRoomName;
		public string SendData;
		public string CloudStorageKey;
		public string CloudStorageValue;
		public string PaymentItemId;
		public string PaymentCurrencyCode;
		public string PaymentPrice;
		public string PaymentItemInstanceId;
		public string AchievementId;

		// Leaderboard
		public string LeaderboardName;
		public string LeaderboardScore;

		// 出力コールバック
		/// <summary>操作結果を UI に表示するコールバック</summary>
		public Action<string> ReportResult;
		/// <summary>操作ログを追記するコールバック</summary>
		public Action<string> AppendLog;
	}
}
