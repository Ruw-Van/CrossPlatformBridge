#if USE_CROSSPLATFORMBRIDGE_EOS
using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.PlayerDataStorage;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;
using CrossPlatformBridge.Services.CloudStorage;
using CrossPlatformBridge.Testing;

namespace CrossPlatformBridge.Platform.EOS.CloudStorage
{
	/// <summary>
	/// EOS Player Data Storage を使用したクラウドストレージの実装。
	/// キー = ファイル名、値 = UTF-8 エンコードされたファイル内容として管理します。
	/// 使用前に EOSManager が初期化済みで ProductUserId が取得済みであることが必要です。
	/// </summary>
	public class CloudStorageHandler : IInternalCloudStorageHandler, IServiceTestProvider
	{
		private const uint ReadChunkSize = 4096;

		private PlayerDataStorageInterface Storage =>
			EOSManager.Instance.GetEOSPlatformInterface().GetPlayerDataStorageInterface();

		private ProductUserId LocalUserId =>
			EOSManager.Instance.GetProductUserId();

		// --------------------------------------------------------------------------------
		// 保存
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public UniTask<bool> SaveData(string key, string value)
		{
			return WriteFile(key, Encoding.UTF8.GetBytes(value ?? string.Empty));
		}

		/// <inheritdoc/>
		public async UniTask<bool> SaveDataBatch(Dictionary<string, string> data)
		{
			if (data == null || data.Count == 0)
				return true;

			// EOS のレート制限を考慮して順次処理する
			foreach (var kv in data)
			{
				if (!await WriteFile(kv.Key, Encoding.UTF8.GetBytes(kv.Value ?? string.Empty)))
					return false;
			}
			return true;
		}

		// --------------------------------------------------------------------------------
		// 読み込み
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public async UniTask<string> LoadData(string key)
		{
			var bytes = await ReadFile(key);
			return bytes != null ? Encoding.UTF8.GetString(bytes) : null;
		}

		/// <inheritdoc/>
		public async UniTask<Dictionary<string, string>> LoadDataBatch(List<string> keys)
		{
			if (keys == null || keys.Count == 0)
				keys = await QueryAllFileNames();

			var result = new Dictionary<string, string>();
			foreach (var key in keys)
			{
				var value = await LoadData(key);
				if (value != null)
					result[key] = value;
			}
			return result;
		}

		// --------------------------------------------------------------------------------
		// 削除
		// --------------------------------------------------------------------------------

		/// <inheritdoc/>
		public UniTask<bool> DeleteData(string key)
		{
			var tcs     = new UniTaskCompletionSource<bool>();
			var options = new DeleteFileOptions
			{
				LocalUserId = LocalUserId,
				Filename    = key,
			};

			Storage.DeleteFile(ref options, null, (ref DeleteFileCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
					tcs.TrySetResult(true);
				else
				{
					Debug.LogError($"[EOS CloudStorage] DeleteData 失敗: key={key} result={info.ResultCode}");
					tcs.TrySetResult(false);
				}
			});

			return tcs.Task;
		}

		// --------------------------------------------------------------------------------
		// プライベート
		// --------------------------------------------------------------------------------

		private UniTask<bool> WriteFile(string filename, byte[] data)
		{
			var tcs    = new UniTaskCompletionSource<bool>();
			var offset = 0;

			var options = new WriteFileOptions
			{
				LocalUserId  = LocalUserId,
				Filename     = filename,
				// ChunkLengthBytes はファイル全体のサイズを指定することを推奨（EOS が分割を制御する）
				ChunkLengthBytes      = (uint)Math.Max(data.Length, 1),
				WriteFileDataCallback = (ref WriteFileDataCallbackInfo info, out ArraySegment<byte> outDataBuffer) =>
				{
					var remaining = data.Length - offset;
					if (remaining <= 0)
					{
						outDataBuffer = ArraySegment<byte>.Empty;
						return WriteResult.CompleteRequest;
					}

					var chunkSize = Math.Min(remaining, (int)info.DataBufferLengthBytes);
					outDataBuffer = new ArraySegment<byte>(data, offset, chunkSize);
					offset += chunkSize;

					return offset >= data.Length ? WriteResult.CompleteRequest : WriteResult.ContinueWriting;
				},
			};

			Storage.WriteFile(ref options, null, (ref WriteFileCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
					tcs.TrySetResult(true);
				else
				{
					Debug.LogError($"[EOS CloudStorage] WriteFile 失敗: filename={filename} result={info.ResultCode}");
					tcs.TrySetResult(false);
				}
			});

			return tcs.Task;
		}

		private UniTask<byte[]> ReadFile(string filename)
		{
			var tcs    = new UniTaskCompletionSource<byte[]>();
			var buffer = new List<byte>();

			var options = new ReadFileOptions
			{
				LocalUserId          = LocalUserId,
				Filename             = filename,
				ReadChunkLengthBytes = ReadChunkSize,
				ReadFileDataCallback = (ref ReadFileDataCallbackInfo info) =>
				{
					buffer.AddRange(info.DataChunk);
					return ReadResult.ContinueReading;
				},
			};

			Storage.ReadFile(ref options, null, (ref ReadFileCallbackInfo info) =>
			{
				if (info.ResultCode == Result.Success)
					tcs.TrySetResult(buffer.ToArray());
				else
				{
					// NotFound は警告レベル（キーが存在しない = null を返す）
					if (info.ResultCode != Result.NotFound)
						Debug.LogError($"[EOS CloudStorage] ReadFile 失敗: filename={filename} result={info.ResultCode}");
					tcs.TrySetResult(null);
				}
			});

			return tcs.Task;
		}

		private UniTask<List<string>> QueryAllFileNames()
		{
			var tcs     = new UniTaskCompletionSource<List<string>>();
			var userId  = LocalUserId;
			var options = new QueryFileListOptions { LocalUserId = userId };

			Storage.QueryFileList(ref options, null, (ref QueryFileListCallbackInfo info) =>
			{
				var names = new List<string>();
				if (info.ResultCode == Result.Success)
				{
					var countOptions = new GetFileMetadataCountOptions { LocalUserId = userId };
					Storage.GetFileMetadataCount(ref countOptions, out var count);

					for (var i = 0; i < count; i++)
					{
						var metaOptions = new CopyFileMetadataAtIndexOptions
						{
							LocalUserId = userId,
							Index       = (uint)i,
						};
						if (Storage.CopyFileMetadataAtIndex(ref metaOptions, out var metadata) == Result.Success
							&& metadata.HasValue)
						{
							names.Add(metadata.Value.Filename);
						}
					}
				}
				else
				{
					Debug.LogError($"[EOS CloudStorage] QueryFileList 失敗: result={info.ResultCode}");
				}
				tcs.TrySetResult(names);
			});

			return tcs.Task;
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

		public TestDefaultData GetDefaultData() => new TestDefaultData { CloudStorageKey = "eos_save", CloudStorageValue = "eos_value" };
	}
}

#endif
