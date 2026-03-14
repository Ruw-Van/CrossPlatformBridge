#if USE_CROSSPLATFORMBRIDGE_PLAYFAB
#if !DISABLE_PLAYFABCLIENT_API

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using CrossPlatformBridge.Services.CloudStorage;
using CrossPlatformBridge.Testing;

namespace CrossPlatformBridge.Platform.PlayFab.CloudStorage
{
	/// <summary>
	/// PlayFab PlayerData を使用したクラウドストレージの実装。
	/// IInternalCloudStorageHandler を実装し、PlayFab SDK のコールバック API を UniTask でラップします。
	/// </summary>
	public class CloudStorageHandler : IInternalCloudStorageHandler, IServiceTestProvider
	{
		// --------------------------------------------------------------------------------
		// 保存
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<bool> SaveData(string key, string value)
		{
			return await SaveDataBatch(new Dictionary<string, string> { { key, value } });
		}

		/// <inheritdoc/>
		public async UniTask<bool> SaveDataBatch(Dictionary<string, string> data)
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var request = new UpdateUserDataRequest { Data = data };

			PlayFabClientAPI.UpdateUserData(request,
				_ => tcs.TrySetResult(true),
				error =>
				{
					Debug.LogError($"[CloudStorageHandler] SaveDataBatch 失敗: {error.GenerateErrorReport()}");
					tcs.TrySetResult(false);
				});

			return await tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// 読み込み
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<string> LoadData(string key)
		{
			var result = await LoadDataBatch(new List<string> { key });
			result.TryGetValue(key, out var value);
			return value;
		}

		/// <inheritdoc/>
		public async UniTask<Dictionary<string, string>> LoadDataBatch(List<string> keys)
		{
			var tcs     = new UniTaskCompletionSource<Dictionary<string, string>>();
			var request = new GetUserDataRequest
			{
				// null を渡すと全キーを取得する
				Keys = (keys != null && keys.Count > 0) ? keys : null,
			};

			PlayFabClientAPI.GetUserData(request,
				result =>
				{
					var data = new Dictionary<string, string>();
					if (result.Data != null)
					{
						foreach (var kv in result.Data)
							data[kv.Key] = kv.Value.Value;
					}
					tcs.TrySetResult(data);
				},
				error =>
				{
					Debug.LogError($"[CloudStorageHandler] LoadDataBatch 失敗: {error.GenerateErrorReport()}");
					tcs.TrySetResult(new Dictionary<string, string>());
				});

			return await tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// 削除
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<bool> DeleteData(string key)
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var request = new UpdateUserDataRequest
			{
				KeysToRemove = new List<string> { key },
			};

			PlayFabClientAPI.UpdateUserData(request,
				_ => tcs.TrySetResult(true),
				error =>
				{
					Debug.LogError($"[CloudStorageHandler] DeleteData 失敗: {error.GenerateErrorReport()}");
					tcs.TrySetResult(false);
				});

			return await tcs.Task;
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

		public TestDefaultData GetDefaultData() => new TestDefaultData { CloudStorageKey = "playfab_save", CloudStorageValue = "playfab_value" };
	}
}

#endif // !DISABLE_PLAYFABCLIENT_API

#endif
