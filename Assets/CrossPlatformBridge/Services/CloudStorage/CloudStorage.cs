using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CrossPlatformBridge.Services.CloudStorage
{
	/// <summary>
	/// クラウドストレージ機能の公開ファサード。
	/// IInternalCloudStorageHandler の実装を差し替えることで、バックエンドを切り替えられます。
	/// 使用前に InitializeHandler() で実装を注入してください。
	/// </summary>
	public class CloudStorage : MonoBehaviour
	{
		private static CloudStorage _instance;
		public static CloudStorage Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindFirstObjectByType<CloudStorage>();
					if (_instance == null)
					{
						var go = new GameObject(typeof(CloudStorage).Name);
						_instance = go.AddComponent<CloudStorage>();
					}
					DontDestroyOnLoad(_instance.gameObject);
				}
				return _instance;
			}
		}

		// --------------------------------------------------------------------------------
		// プロパティ
		// --------------------------------------------------------------------------------

		/// <summary>ハンドラが初期化済みかどうか。</summary>
		public bool IsInitialized => _handler != null;

		// --------------------------------------------------------------------------------
		// フィールド
		// --------------------------------------------------------------------------------

		private IInternalCloudStorageHandler _handler;

		// --------------------------------------------------------------------------------
		// Unity ライフサイクル
		// --------------------------------------------------------------------------------

		private void Awake()
		{
			if (_instance == null)
			{
				_instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else if (_instance != this)
			{
				Destroy(gameObject);
			}
		}

		// --------------------------------------------------------------------------------
		// 初期化
		// --------------------------------------------------------------------------------

		/// <summary>
		/// 使用するクラウドストレージ実装を注入します。
		/// </summary>
		/// <param name="handler">クラウドストレージハンドラーの実装。</param>
		public void InitializeHandler(IInternalCloudStorageHandler handler)
		{
			_handler = handler;
		}

		// --------------------------------------------------------------------------------
		// 保存
		// --------------------------------------------------------------------------------

		/// <summary>指定キーに文字列データを保存します。</summary>
		public async UniTask<bool> SaveData(string key, string value)
		{
			AssertInitialized();
			return await _handler.SaveData(key, value);
		}

		/// <summary>複数のキーと値を一括で保存します。</summary>
		public async UniTask<bool> SaveDataBatch(Dictionary<string, string> data)
		{
			AssertInitialized();
			return await _handler.SaveDataBatch(data);
		}

		// --------------------------------------------------------------------------------
		// 読み込み
		// --------------------------------------------------------------------------------

		/// <summary>指定キーのデータを読み込みます。キーが存在しない場合は null を返します。</summary>
		public async UniTask<string> LoadData(string key)
		{
			AssertInitialized();
			return await _handler.LoadData(key);
		}

		/// <summary>複数のキーのデータを一括で読み込みます。</summary>
		/// <param name="keys">読み込むキーの一覧。null または空の場合は全データを取得します。</param>
		public async UniTask<Dictionary<string, string>> LoadDataBatch(List<string> keys)
		{
			AssertInitialized();
			return await _handler.LoadDataBatch(keys);
		}

		// --------------------------------------------------------------------------------
		// 削除
		// --------------------------------------------------------------------------------

		/// <summary>指定キーのデータを削除します。</summary>
		public async UniTask<bool> DeleteData(string key)
		{
			AssertInitialized();
			return await _handler.DeleteData(key);
		}

		// --------------------------------------------------------------------------------
		// プライベート
		// --------------------------------------------------------------------------------

		private void AssertInitialized()
		{
			if (_handler == null)
				throw new InvalidOperationException("[CloudStorage] InitializeHandler() を先に呼び出してください。");
		}
	}
}
