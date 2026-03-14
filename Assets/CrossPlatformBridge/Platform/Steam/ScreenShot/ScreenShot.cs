#if USE_CROSSPLATFORMBRIDGE_STEAM
#if !DISABLESTEAMWORKS
using Cysharp.Threading.Tasks;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CrossPlatformBridge.Services.ScreenShot;
using CrossPlatformBridge.Testing;
using UnityEngine;

namespace CrossPlatformBridge.Platform.Steam.ScreenShot
{
	public partial class ScreenShot : IInternalScreenShot, IDisposable, IServiceTestProvider
	{
		string tempFileName = string.Empty;
		private Callback<ScreenshotReady_t> m_ScreenshotReady = null;

		// コンストラクタでコールバックを登録
		public ScreenShot()
		{
			if (m_ScreenshotReady == null)
			{
				m_ScreenshotReady = Callback<ScreenshotReady_t>.Create(OnScreenshotReady);
			}
		}

		// クラスが不要になったら必ず呼ばれるようにする
		public void Dispose()
		{
			if (m_ScreenshotReady != null)
			{
				m_ScreenshotReady.Dispose();
				m_ScreenshotReady = null;
				Debug.Log("[Screenshot] Callback Disposed.");
			}
		}

		// ScreenshotReady_t の情報を受け取る関数
		private void OnScreenshotReady(ScreenshotReady_t pCallback)
		{
			// ----------------------------------------------------
			// 呼び出しが成功したかを確認
			// ----------------------------------------------------
			Debug.Log($"[ScreenshotReady] Result: {pCallback.m_eResult}.");
			if (pCallback.m_eResult == EResult.k_EResultOK)
			{
				// 成功
				Debug.Log($"[ScreenshotReady] ✅ SUCCESS: Screenshot written to disk.");
			}
			else
			{
				// 失敗
				Debug.LogError($"[ScreenshotReady] ❌ FAILED: Screenshot writing failed. Result: {pCallback.m_eResult}");
			}
			DeleteFileWithRetry(tempFileName).Forget();
		}

		public IEnumerator SaveScreenShot(Action<bool> onCompleted = null)
		{
			tempFileName = Path.GetTempFileName();
			File.Delete(tempFileName);
			tempFileName += ".jpg";
			yield return new WaitForEndOfFrame();
			ScreenCapture.CaptureScreenshot(tempFileName);
			yield return new WaitUntil(() => File.Exists(tempFileName));

			try
			{
				var handle = SteamScreenshots.AddScreenshotToLibrary(tempFileName, null, Screen.width, Screen.height);
				Debug.Log($"tempFileName => {tempFileName}({Screen.width}x{Screen.height})");

				if (handle != ScreenshotHandle.Invalid)
				{
					Debug.Log("Screenshot successfully registered to Steam!");
					onCompleted?.Invoke(true);
				}
				else
				{
					Debug.LogError("Failed to register screenshot to Steam.");
					onCompleted?.Invoke(false);
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				onCompleted?.Invoke(false);
			}
		}

		private static async UniTask DeleteFileWithRetry(string filePath, int maxRetryCount = 5, float retryIntervalSeconds = 1.0f)
		{
			int retryCount = 0;
			while (retryCount < maxRetryCount)
			{
				try
				{
					File.Delete(filePath);
					Debug.Log($"ファイル '{filePath}' を削除しました。");
					return; // 削除成功
				}
				catch (IOException ex)
				{
					// IOExceptionが発生した場合、ファイルがロックされている可能性がある
					if (IsFileLocked(ex))
					{
						retryCount++;
						Debug.LogWarning($"ファイル '{filePath}' はロックされています。{retryCount}/{maxRetryCount} 回目の再試行を行います。");
						await UniTask.WaitForSeconds(retryIntervalSeconds);
					}
					else
					{
						// その他のIOExceptionの場合は再試行しない
						Debug.LogError($"ファイルの削除中に予期しないエラーが発生しました: {ex.Message}");
						throw;
					}
				}
				catch (Exception ex)
				{
					// その他の例外はそのままthrow
					Debug.LogError($"ファイルの削除中にエラーが発生しました: {ex.Message}");
					throw;
				}
			}

			// 指定回数リトライしても削除できなかった場合
			Debug.LogError($"ファイル '{filePath}' の削除に失敗しました。ロックが解除されませんでした。");
			throw new IOException($"ファイル '{filePath}' の削除に失敗しました。");
		}

		private static bool IsFileLocked(IOException exception)
		{
			int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 0xFFFF;
			// 共有違反 (The process cannot access the file because it is being used by another process.)
			return errorCode == 32 || errorCode == 33;
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => null;

		public TestDefaultData GetDefaultData() => new TestDefaultData();
	}
}
#endif

#endif
