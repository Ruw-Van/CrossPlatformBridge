#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
using UnityEngine;

namespace CrossPlatformBridge.Platform.PlayFab.IntegrationTests
{
	/// <summary>
	/// PlayFab 統合テスト用の設定ファイル。
	/// TitleId と CloudStorage テスト用のキー・値を保持する。
	///
	/// 設定アセットの作成は
	/// Tools > CrossPlatformBridge > PlayFab > Create Integration Test Settings
	/// を使用してください。
	/// .gitignore で除外されているため認証情報が誤ってコミットされません。
	/// </summary>
	[CreateAssetMenu(
		fileName = "PlayFabIntegrationTestSettings",
		menuName = "CrossPlatformBridge/PlayFab/Integration Test Settings")]
	public class PlayFabIntegrationTestSettings : ScriptableObject
	{
		[Header("PlayFab 接続設定")]
		[Tooltip("PlayFab ゲームマネージャーの Title ID")]
		[SerializeField] private string _titleId = "";

		[Header("CloudStorage テスト設定")]
		[Tooltip("テストで保存・読み込みに使用するキー名")]
		[SerializeField] private string _testKey = "playfab_integration_test_key";

		[Tooltip("テストで保存・読み込みに使用する値")]
		[SerializeField] private string _testValue = "playfab_integration_test_value";

		public string TitleId   => _titleId;
		public string TestKey   => _testKey;
		public string TestValue => _testValue;
	}
}
#endif
