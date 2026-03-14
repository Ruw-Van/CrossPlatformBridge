#if USE_CROSSPLATFORMBRIDGE_FIREBASE
using UnityEngine;

namespace CrossPlatformBridge.Platform.Firebase.IntegrationTests
{
	/// <summary>
	/// Firebase 統合テスト用の設定ファイル。
	/// CloudStorage テストで使用するキーと値を保持する。
	///
	/// 設定アセットの作成は
	/// Tools > CrossPlatformBridge > Firebase > Create Integration Test Settings
	/// を使用してください。
	/// .gitignore で除外されているため認証情報が誤ってコミットされません。
	/// </summary>
	[CreateAssetMenu(
		fileName = "FirebaseIntegrationTestSettings",
		menuName = "CrossPlatformBridge/Firebase/Integration Test Settings")]
	public class FirebaseIntegrationTestSettings : ScriptableObject
	{
		[Header("CloudStorage テスト設定")]
		[Tooltip("テストで保存・読み込みに使用するキー名")]
		[SerializeField] private string _testKey = "firebase_integration_test_key";

		[Tooltip("テストで保存・読み込みに使用する値")]
		[SerializeField] private string _testValue = "firebase_integration_test_value";

		public string TestKey   => _testKey;
		public string TestValue => _testValue;
	}
}
#endif
