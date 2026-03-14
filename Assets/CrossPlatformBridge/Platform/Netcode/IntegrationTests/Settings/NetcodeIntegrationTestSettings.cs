#if USE_CROSSPLATFORMBRIDGE_NETCODE
using UnityEngine;

namespace CrossPlatformBridge.Platform.Netcode.IntegrationTests
{
	/// <summary>
	/// Netcode 統合テスト用の設定ファイル。
	/// Unity Gaming Services (UGS) が設定済みの場合のみテストを有効化する。
	///
	/// 設定アセットの作成は
	/// Tools > CrossPlatformBridge > Netcode > Create Integration Test Settings
	/// を使用してください。
	///
	/// 前提条件:
	/// - Unity プロジェクトが UGS プロジェクトにリンク済みであること
	///   (Edit > Project Settings > Services > General Settings)
	/// - 設定アセットの TestEnabled を true に設定すること
	/// </summary>
	[CreateAssetMenu(
		fileName = "NetcodeIntegrationTestSettings",
		menuName = "CrossPlatformBridge/Netcode/Integration Test Settings")]
	public class NetcodeIntegrationTestSettings : ScriptableObject
	{
		[Header("テスト有効化")]
		[Tooltip("UGS が設定済みの場合のみ true にしてください")]
		[SerializeField] private bool _testEnabled = false;

		[Header("テスト設定")]
		[Tooltip("テスト用ロビー名プレフィックス")]
		[SerializeField] private string _testLobbyNamePrefix = "IntegrationTest";

		public bool   TestEnabled          => _testEnabled;
		public string TestLobbyNamePrefix  => _testLobbyNamePrefix;
	}
}
#endif
