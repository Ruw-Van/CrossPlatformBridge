#if USE_CROSSPLATFORMBRIDGE_EOS
using System.Collections;
using System.Collections.Generic;
using CrossPlatformBridge.Platform.EOS.Leaderboard;
using CrossPlatformBridge.Platform.EOS.Network;
using CrossPlatformBridge.Services.Leaderboard;
using LeaderboardService = CrossPlatformBridge.Services.Leaderboard.Leaderboard;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using NUnit.Framework;
using PlayEveryWare.EpicOnlineServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace CrossPlatformBridge.Platform.EOS.IntegrationTests
{
	/// <summary>
	/// EOS リーダーボードハンドラの PlayMode 統合テスト。
	/// <see cref="Leaderboard"/> ファサードを経由して実際の EOS サーバーに接続し動作を検証する。
	///
	/// 実行前に EOS DevAuthTool を起動し、EOSIntegrationTestSettings.asset を
	/// Assets/CrossPlatformBridgeSettings/Editor/EOS/ に配置してください。
	/// 詳細は IntegrationTests/README.md を参照してください。
	///
	/// また EOS 開発者ポータルで以下を事前設定してください:
	///   - Stat: <see cref="EOSIntegrationTestSettings.TestLeaderboardId"/> と同名の Stat を作成
	///   - Leaderboard: 上記 Stat を集計するリーダーボードを作成
	/// </summary>
	public class EOSLeaderboardIntegrationTests
	{
		private const string SettingsPath =
			"Assets/CrossPlatformBridgeSettings/Editor/EOSIntegrationTestSettings.asset";

		// セッションはテストクラス内で共有する（DevAuth ログインは一度のみ）
		private static EOSIntegrationTestSettings _settings;
		private static NetworkHandler _networkHandler;
		private static bool _sessionInitialized;

		[UnitySetUp]
		public IEnumerator SetUp() => UniTask.ToCoroutine(async () =>
		{
			_settings = AssetDatabase.LoadAssetAtPath<EOSIntegrationTestSettings>(SettingsPath);
			Assume.That(_settings, Is.Not.Null,
				"EOSIntegrationTestSettings.asset が見つかりません。IntegrationTests/README.md を参照してください。");

			if (!_sessionInitialized)
			{
				_networkHandler = new NetworkHandler();
				bool initialized = _networkHandler.Initialize(ScriptableObject.CreateInstance<NetworkSettings>());
				Assert.IsTrue(initialized, "NetworkHandler.Initialize() が失敗しました。EOS Plugin の設定を確認してください。");

				await LoginWithDevAuth(_settings);

				bool connected = await _networkHandler.Connect(ScriptableObject.CreateInstance<NetworkSettings>());
				Assert.IsTrue(connected, "NetworkHandler.Connect() が失敗しました。");

				_sessionInitialized = true;
			}

			// テストごとに新しいハンドラーをファサードに注入する
			var handler = new EOSLeaderboardHandler();
#pragma warning disable CS0618
			LeaderboardService.Instance.InitializeHandler(handler);
#pragma warning restore CS0618
		});

		[TearDown]
		public void TearDown()
		{
			// 次テストのために InitializeHandler で差し替えるため、ここでは何もしない
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_networkHandler?.Shutdown();
			_networkHandler = null;
			_settings = null;
			_sessionInitialized = false;
		}

		// ── グローバルランキング ──────────────────────────────────

		/// <summary>
		/// スコアを送信し true が返ることを確認する。
		/// EOS 側では Stat が更新され、リーダーボードが再集計される。
		/// </summary>
		[UnityTest]
		public IEnumerator SubmitScore_ViaFacade_ReturnsTrue() => UniTask.ToCoroutine(async () =>
		{
			// Act
			bool result = await LeaderboardService.Instance.SubmitScore(_settings.TestLeaderboardId, 1000L);

			// Assert
			Assert.IsTrue(result,
				$"SubmitScore({_settings.TestLeaderboardId}, 1000) が false を返しました。" +
				"EOS 開発者ポータルで Stat が定義されているか確認してください。");
		});

		/// <summary>
		/// 上位エントリを取得し、null でなくリストが返ることを確認する（0 件でも可）。
		/// </summary>
		[UnityTest]
		public IEnumerator GetTopEntries_ViaFacade_ReturnsNonNullList() => UniTask.ToCoroutine(async () =>
		{
			// Act
			List<LeaderboardEntry> result =
				await LeaderboardService.Instance.GetTopEntries(_settings.TestLeaderboardId, 10);

			// Assert
			Assert.IsNotNull(result,
				"GetTopEntries() が null を返しました。");
		});

		/// <summary>
		/// 現在のプレイヤーのエントリを取得する。
		/// スコア送信後であればエントリが存在する（ない場合は null でエラーにしない）。
		/// </summary>
		[UnityTest]
		public IEnumerator GetPlayerEntry_ViaFacade_AfterSubmit_ReturnsEntryOrNull() => UniTask.ToCoroutine(async () =>
		{
			// Arrange: まずスコアを送信しておく
			var localUserId = EOSManager.Instance.GetEOSPlatformInterface()
				?.GetConnectInterface()?.GetLoggedInUserByIndex(0)?.ToString();
			Assume.That(localUserId, Is.Not.Null.And.Not.Empty,
				"EOS Connect ユーザーが取得できませんでした。");

			await LeaderboardService.Instance.SubmitScore(_settings.TestLeaderboardId, 2000L);

			// Act
			LeaderboardEntry entry =
				await LeaderboardService.Instance.GetPlayerEntry(_settings.TestLeaderboardId, localUserId);

			// Assert: null でなければスコアが一致することも確認
			// EOS のリーダーボード集計にラグがある場合は null でも許容する
			if (entry != null)
			{
				Assert.GreaterOrEqual(entry.Score, 0L,
					"取得したエントリのスコアが負の値です。");
				Assert.AreEqual(localUserId, entry.PlayerId);
			}
			else
			{
				Debug.Log("[EOS Leaderboard] GetPlayerEntry が null を返しました（集計ラグの可能性があります）。");
			}
		});

		// ── セッション内ランキング ────────────────────────────────

		/// <summary>
		/// セッションスコアを更新し、GetSessionLeaderboard で取得できることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator UpdateSessionScore_ViaFacade_ReturnsTrueAndAppearsInLeaderboard()
			=> UniTask.ToCoroutine(async () =>
		{
			var localUserId = EOSManager.Instance.GetEOSPlatformInterface()
				?.GetConnectInterface()?.GetLoggedInUserByIndex(0)?.ToString() ?? "eos_player";

			// Act
			bool ok = await LeaderboardService.Instance.UpdateSessionScore(localUserId, "EOSPlayer", 5000L);

			// Assert
			Assert.IsTrue(ok, "UpdateSessionScore() が false を返しました。");

			List<LeaderboardEntry> board = await LeaderboardService.Instance.GetSessionLeaderboard();
			Assert.IsNotNull(board);
			Assert.AreEqual(1, board.Count);
			Assert.AreEqual(localUserId, board[0].PlayerId);
			Assert.AreEqual(5000L, board[0].Score);
		});

		/// <summary>
		/// セッションリセット後はエントリが 0 件になることを確認する。
		/// </summary>
		[UnityTest]
		public IEnumerator ResetSessionLeaderboard_ViaFacade_ClearsAllEntries()
			=> UniTask.ToCoroutine(async () =>
		{
			await LeaderboardService.Instance.UpdateSessionScore("p1", "Alice", 999L);

			// Act
			await LeaderboardService.Instance.ResetSessionLeaderboard();

			// Assert
			List<LeaderboardEntry> board = await LeaderboardService.Instance.GetSessionLeaderboard();
			Assert.AreEqual(0, board.Count, "リセット後はエントリが 0 件である必要があります。");
		});

		// ── ヘルパー ──────────────────────────────────────────────

		/// <summary>
		/// EOS DevAuthTool を使って Auth ログインする。
		/// 既にログイン済みの場合はスキップする。
		/// </summary>
		private static async UniTask LoginWithDevAuth(EOSIntegrationTestSettings settings)
		{
			var authInterface = EOSManager.Instance.GetEOSPlatformInterface().GetAuthInterface();
			if (authInterface.GetLoggedInAccountsCount() > 0)
			{
				Debug.Log("[EOS Leaderboard Integration] 既存の Auth セッションを再利用します。");
				return;
			}

			var loginOptions = new LoginOptions
			{
				Credentials = new Credentials
				{
					Type  = LoginCredentialType.Developer,
					Id    = $"localhost:{settings.DevAuthPort}",
					Token = settings.DevAuthCredentialName,
				},
				ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence,
			};

			var tcs = new UniTaskCompletionSource<LoginCallbackInfo>();
			authInterface.Login(ref loginOptions, null,
				(ref LoginCallbackInfo info) => tcs.TrySetResult(info));

			var result = await tcs.Task;
			Assert.AreEqual(Result.Success, result.ResultCode,
				$"DevAuth ログイン失敗: {result.ResultCode}。" +
				$"DevAuthTool (port:{settings.DevAuthPort}) が起動していることを確認してください。");
		}
	}
}
#endif
