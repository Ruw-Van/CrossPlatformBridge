using System;
using System.Collections;
using UnityEngine;

namespace CrossPlatformBridge.Tests.Integration
{
	/// <summary>
	/// PlayMode 統合テストの共通基底クラス。
	/// タイムアウト付き WaitForCondition と共通クリーンアップを提供します。
	/// </summary>
	public abstract class IntegrationTestBase
	{
		/// <summary>
		/// 条件が満たされるまで待機します。タイムアウト時間を超えた場合は例外をスローします。
		/// </summary>
		/// <param name="condition">待機を終了する条件</param>
		/// <param name="timeoutSeconds">タイムアウト時間（秒）。デフォルト: 10秒</param>
		/// <param name="label">タイムアウト時のエラーメッセージに含めるラベル</param>
		protected static IEnumerator WaitForCondition(
			Func<bool> condition,
			float timeoutSeconds = 10f,
			string label = "条件")
		{
			float elapsed = 0f;
			while (!condition())
			{
				if (elapsed >= timeoutSeconds)
					throw new TimeoutException($"WaitForCondition: {label} が {timeoutSeconds} 秒以内に満たされませんでした。");
				yield return null;
				elapsed += Time.deltaTime;
			}
		}

		/// <summary>
		/// 指定秒数待機します。
		/// </summary>
		protected static IEnumerator Wait(float seconds)
		{
			yield return new WaitForSeconds(seconds);
		}

		/// <summary>
		/// シーン内の全 GameObject をリセットします（テスト後クリーンアップ用）。
		/// DontDestroyOnLoad のオブジェクトには影響しません。
		/// </summary>
		protected static void DestroyGameObject(GameObject go)
		{
			if (go != null)
				UnityEngine.Object.Destroy(go);
		}
	}
}
