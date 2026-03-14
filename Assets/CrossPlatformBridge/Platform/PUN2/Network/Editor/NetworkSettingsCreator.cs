#if USE_CROSSPLATFORMBRIDGE_PUN2
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrossPlatformBridge.Platform.PUN2.Network.Editor
{
	/// <summary>
	/// NetworkSettingsScriptableObject を特定のフォルダに生成するための専用エディタスクリプト。
	/// </summary>
	public class NetworkSettingsCreator : IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		private const string TARGET_NAME = "PUN2";

		private const string MENU_ITEM_PATH = "Assets/Create/CrossPlatformBridgeSettings/Network/" + TARGET_NAME + "/SetupManager";

		private const string ConfigDataPath = "Assets/Scripts/CrossPlatformBridgeSettings/Network/PUN2/";

		private const string PhotonServerSettingDataFolder = "Assets/Photon/PhotonUnityNetworking/Resources/";
		private static readonly string DestinationPath = PhotonServerSettingDataFolder + "PhotonServerSettings.asset";
		private static readonly string EditorPath = ConfigDataPath + "Editor/PhotonServerSettings.asset";
		private static readonly string DevPath = ConfigDataPath + "Development/PhotonServerSettings.asset";
		private static readonly string ReleasePath = ConfigDataPath + "Release/PhotonServerSettings.asset";

		private static List<string> BuildMode = new List<string>()
		{
			"Editor",
			"Development",
			"Release"
		};

		[MenuItem(MENU_ITEM_PATH, false, 0)]
		public static void SetupManager()
		{
			if (!Directory.Exists(ConfigDataPath))
			{
				Debug.Log($"Directory.CreateDirectory({ConfigDataPath});");
				Directory.CreateDirectory(ConfigDataPath);
			}

			foreach (var mode in BuildMode)
			{
				string FolderPath = Path.Combine(ConfigDataPath, mode);
				if (!Directory.Exists(FolderPath))
				{
					Debug.Log($"Directory.CreateDirectory({FolderPath});");
					Directory.CreateDirectory(FolderPath);
				}
				string FileName = Path.GetFileName(DestinationPath);
				string FilePath = Path.Combine(FolderPath, FileName);
				if (!File.Exists(FilePath))
				{
					Debug.Log($"AssetDatabase.CopyAsset({DestinationPath}, {FilePath});");
					AssetDatabase.CopyAsset(DestinationPath, FilePath);
				}
			}
		}

		/// <summary>
		/// ファイルコピーの共通メソッド
		/// </summary>
		private static void CopyConfigFile(string sourcePath)
		{
			Debug.Log($"Copying PhotonServerSettings from: {sourcePath} to: {DestinationPath}");
			AssetDatabase.CopyAsset(sourcePath, DestinationPath);
			AssetDatabase.Refresh();
		}

		[MenuItem("Window/Photon Unity Networking/ConfigCopyFromEditor")]
		private static void ConfigCopyFromEditor() => CopyConfigFile(EditorPath);

		[MenuItem("Window/Photon Unity Networking/ConfigCopyFromDevelop")]
		private static void CopyFromDevelop() => CopyConfigFile(DevPath);

		[MenuItem("Window/Photon Unity Networking/ConfigCopyFromRelease")]
		private static void CopyFromRelease() => CopyConfigFile(ReleasePath);

		public void OnPreprocessBuild(BuildReport buildReport)
		{
			// 開発ビルドの場合はDevPathを使用し、それ以外はReleasePathを使用
			string sourcePath = (buildReport.summary.options & BuildOptions.Development) == BuildOptions.Development
				? DevPath
				: ReleasePath;

			CopyConfigFile(sourcePath);
		}

		public void OnPostprocessBuild(BuildReport buildReport)
		{
			// ビルド後はEditorPathを使用
			CopyConfigFile(EditorPath);
		}

		/// <summary>
		/// 実行順を指定（0がデフォルト、低いほど先に実行される）
		/// </summary>
		public int callbackOrder => 0;
	}
}
#endif

#endif
