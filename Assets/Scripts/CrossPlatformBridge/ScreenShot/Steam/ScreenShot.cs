#if !DISABLESTEAMWORKS
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace CrossPlatformBridge.ScreenShot.Steam
{
	public partial class ScreenShot : IInternalScreenShot
	{
		public IEnumerator SaveScreenShot()
		{
			string tempFileName = Path.GetTempFileName();
			yield return new WaitForEndOfFrame();
			ScreenCapture.CaptureScreenshot(tempFileName);
			yield return new WaitUntil(() => File.Exists(tempFileName));
			try
			{
				var handle = Steamworks.SteamScreenshots.AddScreenshotToLibrary(tempFileName, null, Screen.width, Screen.height);
				Debug.Log($"tempFileName => {tempFileName}({Screen.width}x{Screen.height})");

				if (handle != Steamworks.ScreenshotHandle.Invalid)
				{
					Debug.Log("Screenshot successfully registered to Steam!");
				}
				else
				{
					Debug.LogError("Failed to register screenshot to Steam.");
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
			finally
			{
				DeleteFileWithRetry(tempFileName).Forget();
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
						Debug.Log($"ファイル '{filePath}' はロックされています。{retryCount}/{maxRetryCount} 回目の再試行を行います。");
						await UniTask.WaitForSeconds(retryIntervalSeconds);
					}
					else
					{
						// その他のIOExceptionの場合は再試行しない
						Debug.Log($"ファイルの削除中に予期しないエラーが発生しました: {ex.Message}");
						throw;
					}
				}
				catch (Exception ex)
				{
					// その他の例外はそのままthrow
					Debug.Log($"ファイルの削除中にエラーが発生しました: {ex.Message}");
					throw;
				}
			}

			// 指定回数リトライしても削除できなかった場合
			Debug.Log($"ファイル '{filePath}' の削除に失敗しました。ロックが解除されませんでした。");
			throw new IOException($"ファイル '{filePath}' の削除に失敗しました。");
		}

		private static bool IsFileLocked(IOException exception)
		{
			int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & 0xFFFF;
			// 共有違反 (The process cannot access the file because it is being used by another process.)
			return errorCode == 32 || errorCode == 33;
		}
	}
}
#endif
