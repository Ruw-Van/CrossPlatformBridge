#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrossPlatformBridge.Services.Network.Editor
{
	public class NetworkSettingsCreatorBase
	{
		protected const string DEFAULT_PATH = "Assets/Scripts/CrossPlatformBridgeSettings/Network/";
		protected const string ASSET_NAME = "NetworkSettings.asset";

		public static void CreateNetworkSettingsAsset<T>(string targetName) where T : ScriptableObject
		{
			ScriptableObjectCreationUtility.CreateAndSaveAsset<T>(DEFAULT_PATH, targetName + ASSET_NAME);
		}
	}

	/// <summary>
	/// 任意の ScriptableObject を指定されたパスに生成するための汎用ユーティリティ。
	/// </summary>
	public static class ScriptableObjectCreationUtility
	{
		/// <summary>
		/// 指定された型の ScriptableObject インスタンスを生成し、指定されたパスに保存します。
		/// </summary>
		/// <typeparam name="T">生成する ScriptableObject の型。</typeparam>
		/// <param name="path">アセットを生成するデフォルトのフォルダパス (例: "Assets/MyFolder/").</param>
		/// <param name="assetName">生成するアセットのファイル名 (例: "NewAsset.asset").</param>
		public static void CreateAndSaveAsset<T>(string path, string assetName) where T : ScriptableObject
		{
			// デフォルトパスが存在しない場合は作成します
			if (!AssetDatabase.IsValidFolder(path))
			{
				// 親フォルダから順に作成
				string[] pathParts = path.Split('/');
				string currentPath = "Assets";
				for (int i = 1; i < pathParts.Length; i++)
				{
					if (!string.IsNullOrEmpty(pathParts[i]))
					{
						string nextPath = currentPath + "/" + pathParts[i];
						if (!AssetDatabase.IsValidFolder(nextPath))
						{
							AssetDatabase.CreateFolder(currentPath, pathParts[i]);
						}
						currentPath = nextPath;
					}
				}
			}

			// 作成するアセットのフルパスを構築します
			string fullPath = Path.Combine(path, assetName);

			//セットが既に存在するか確認し、存在する場合は終了します。
			if (AssetDatabase.LoadAssetAtPath<T>(fullPath) != null)
			{
				Debug.LogWarning($"Asset {assetName} already exists at {path}. No new asset created.");
				EditorUtility.FocusProjectWindow();
				Selection.activeObject = AssetDatabase.LoadAssetAtPath<T>(fullPath);
				return;
			}

			// 新しい ScriptableObject のインスタンスを作成します
			T asset = ScriptableObject.CreateInstance<T>();

			// アセットを保存します
			AssetDatabase.CreateAsset(asset, fullPath);

			// アセットの保存を確定し、Unity エディタを更新します
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			// 新しく作成されたアセットを Project ウィンドウで選択状態にします
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;

			Debug.Log($"ScriptableObject of type {typeof(T).Name} was created at: {fullPath}");
		}
	}
}
#endif
