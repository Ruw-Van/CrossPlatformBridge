#if USE_CROSSPLATFORMBRIDGE_FIREBASE
using System;
using System.Collections.Generic;
using System.Linq;
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Services.CloudStorage;
using CrossPlatformBridge.Testing;
using Cysharp.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

namespace CrossPlatformBridge.Platform.Firebase.CloudStorage
{
	/// <summary>
	/// Firebase Firestore を利用したクラウドストレージのハンドラ。
	/// 認証済みのユーザーの専用ドキュメント（users/{uid}/data/{key} など）へアクセスします。
	/// </summary>
	public class FirebaseCloudStorageHandler : IInternalCloudStorageHandler, IServiceTestProvider
	{
		private FirebaseFirestore _db;

		// データを保存するコレクションのベースパス
		private const string UsersCollection = "users";
		private const string DataCollection = "data";

		private void InitializeIfNeeded()
		{
			if (_db == null)
			{
				_db = FirebaseFirestore.DefaultInstance;
			}
		}

		private string GetCurrentUserId()
		{
			if (AccountService.Instance == null || !AccountService.Instance.IsInitialized)
			{
				throw new InvalidOperationException("[FirebaseCloudStorageHandler] AccountService is not initialized. Please sign in first.");
			}
			return AccountService.Instance.AccountId;
		}

		/// <summary>
		/// 指定したキーに対するドキュメントリファレンスを取得します。
		/// パス例: users/{uid}/data/{key}
		/// </summary>
		private DocumentReference GetDocumentRef(string key)
		{
			InitializeIfNeeded();
			string uid = GetCurrentUserId();
			return _db.Collection(UsersCollection).Document(uid).Collection(DataCollection).Document(key);
		}

		public async UniTask<bool> SaveData(string key, string value)
		{
			try
			{
				var docRef = GetDocumentRef(key);
				var dataMap = new Dictionary<string, object>
				{
					{ "Value", value },
					{ "UpdatedAt", FieldValue.ServerTimestamp }
				};

				await docRef.SetAsync(dataMap, SetOptions.MergeAll);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[FirebaseCloudStorageHandler] Failed to save data for key '{key}': {ex}");
				return false;
			}
		}

		public async UniTask<bool> SaveDataBatch(Dictionary<string, string> data)
		{
			if (data == null || data.Count == 0) return true;

			try
			{
				InitializeIfNeeded();
				WriteBatch batch = _db.StartBatch();

				foreach (var kvp in data)
				{
					var docRef = GetDocumentRef(kvp.Key);
					var dataMap = new Dictionary<string, object>
					{
						{ "Value", kvp.Value },
						{ "UpdatedAt", FieldValue.ServerTimestamp }
					};
					batch.Set(docRef, dataMap, SetOptions.MergeAll);
				}

				await batch.CommitAsync();
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[FirebaseCloudStorageHandler] Failed to save batch data: {ex}");
				return false;
			}
		}

		public async UniTask<string> LoadData(string key)
		{
			try
			{
				var docRef = GetDocumentRef(key);
				DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

				if (snapshot.Exists && snapshot.TryGetValue("Value", out string storedValue))
				{
					return storedValue;
				}
				return null;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[FirebaseCloudStorageHandler] Failed to load data for key '{key}': {ex}");
				return null;
			}
		}

		public async UniTask<Dictionary<string, string>> LoadDataBatch(List<string> keys)
		{
			var result = new Dictionary<string, string>();
			
			// キーリストがnullまたは空の場合は、ユーザーのすべてのデータを取得する
			if (keys == null || keys.Count == 0)
			{
				try
				{
					InitializeIfNeeded();
					string uid = GetCurrentUserId();
					var querySnapshot = await _db.Collection(UsersCollection).Document(uid).Collection(DataCollection).GetSnapshotAsync();
					
					foreach (var document in querySnapshot.Documents)
					{
						if (document.Exists && document.TryGetValue("Value", out string storedValue))
						{
							result[document.Id] = storedValue;
						}
					}
					return result;
				}
				catch (Exception ex)
				{
					Debug.LogError($"[FirebaseCloudStorageHandler] Failed to load all data: {ex}");
					return result;
				}
			}

			// 指定されたキーのみ取得する (単純に順次/並行リクエスト)
			var tasks = keys.Select(async key => 
			{
				string value = await LoadData(key);
				return new KeyValuePair<string, string>(key, value);
			});

			var results = await UniTask.WhenAll(tasks);
			foreach (var kvp in results)
			{
				if (kvp.Value != null)
				{
					result[kvp.Key] = kvp.Value;
				}
			}

			return result;
		}

		public async UniTask<bool> DeleteData(string key)
		{
			try
			{
				var docRef = GetDocumentRef(key);
				await docRef.DeleteAsync();
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[FirebaseCloudStorageHandler] Failed to delete data for key '{key}': {ex}");
				return false;
			}
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => new TestOperation[]
		{
			new TestOperation { SectionLabel = "保存" },
			new TestOperation { Label = "Save Data", Action = async ctx => { bool ok = await SaveData(ctx.CloudStorageKey, ctx.CloudStorageValue); ctx.ReportResult(ok ? $"保存完了: [{ctx.CloudStorageKey}]" : "保存失敗"); ctx.AppendLog($"SaveData({ctx.CloudStorageKey}) → {ok}"); } },
			new TestOperation { SectionLabel = "読み込み" },
			new TestOperation { Label = "Load Data", Action = async ctx => { string val = await LoadData(ctx.CloudStorageKey); ctx.ReportResult(val != null ? $"[{ctx.CloudStorageKey}] = {val}" : $"[{ctx.CloudStorageKey}] が見つかりません"); ctx.AppendLog($"LoadData({ctx.CloudStorageKey}) → {val ?? "null"}"); } },
			new TestOperation { Label = "Load All Data", Action = async ctx => { var data = await LoadDataBatch(null); if (data == null || data.Count == 0) ctx.ReportResult("データが存在しません"); else { var sb = new System.Text.StringBuilder(); foreach (var kv in data) sb.AppendLine($"{kv.Key}: {kv.Value}"); ctx.ReportResult(sb.ToString()); } ctx.AppendLog($"LoadDataBatch → {data?.Count ?? 0} 件"); } },
			new TestOperation { SectionLabel = "削除" },
			new TestOperation { Label = "Delete Data", Action = async ctx => { bool ok = await DeleteData(ctx.CloudStorageKey); ctx.ReportResult(ok ? $"削除完了: [{ctx.CloudStorageKey}]" : "削除失敗"); ctx.AppendLog($"DeleteData({ctx.CloudStorageKey}) → {ok}"); } },
		};

		public TestDefaultData GetDefaultData() => new TestDefaultData { CloudStorageKey = "firebase_save", CloudStorageValue = "firebase_value" };
	}
}

#endif
