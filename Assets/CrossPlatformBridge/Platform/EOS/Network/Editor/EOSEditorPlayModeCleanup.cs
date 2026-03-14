#if USE_CROSSPLATFORMBRIDGE_EOS
#if UNITY_EDITOR
using Epic.OnlineServices.Platform;
using PlayEveryWare.EpicOnlineServices;
using UnityEditor;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Network.Editor
{
	/// <summary>
	/// Unity Editor でプレイモードを停止した際に EOS を適切にシャットダウンするユーティリティ。
	///
	/// 問題: Unity Editor では Application.quitting が発火しないため、
	/// EOSManager が EOS_Platform_Release / EOS_Shutdown を呼ばない。
	/// EOS のネイティブスレッドが動き続けた状態で2回目のプレイに入ると、
	/// Init() 内の ForceUnloadEOSLibrary (FreeLibrary) がスレッド終了待ちでハングする。
	///
	/// 対策:
	///   ExitingPlayMode で EOSManager を無効化し、Release するプラットフォームを退避。
	///   EnteredEditMode で Release（および Windows は Shutdown）を実行する。
	///
	/// Release を ExitingPlayMode で即座に呼ばない理由:
	///   ExitingPlayMode コールバックは Unity の TickTimer 内から発火する場合がある。
	///   同フレームの BehaviourManager.Update() はすでにキューに積まれており、
	///   enabled = false が間に合わず EOSManager.Update() → Tick() が
	///   解放済みネイティブオブジェクトにアクセスして SIGSEGV になる。
	///   EnteredEditMode では全 MonoBehaviour が破棄済みで Update() は絶対に呼ばれない。
	/// </summary>
	[InitializeOnLoad]
	public static class EOSEditorPlayModeCleanup
	{
		private static PlatformInterface _platformToRelease = null;

		static EOSEditorPlayModeCleanup()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				// EOSManager.Instance は EOSManager.EOSSingleton 型を返す（MonoBehaviour ではない）。
				// MonoBehaviour.enabled を操作するには FindFirstObjectByType で実体を取得する。
				var eosManagerComponent = UnityEngine.Object.FindFirstObjectByType<EOSManager>();
				var platform = EOSManager.Instance?.GetEOSPlatformInterface();
				if (platform == null) return;

				// EOSManager.Update() → Tick() をなるべく早く止める。
				// enabled = false は次フレーム以降に確実に効くが、
				// 同フレーム内の Update キューには間に合わない場合があるため
				// Release() はこのタイミングでは呼ばず EnteredEditMode まで遅延する。
				if (eosManagerComponent != null)
					eosManagerComponent.enabled = false;

				_platformToRelease = platform;
				Debug.Log("EOS: ExitingPlayMode — EOSManager を無効化。Release は EnteredEditMode で実行します");
			}
			else if (state == PlayModeStateChange.EnteredEditMode)
			{
				if (_platformToRelease == null) return;

				var platform = _platformToRelease;
				_platformToRelease = null;

				// EnteredEditMode では全 MonoBehaviour が破棄済みのため
				// EOSManager.Update() → Tick() は絶対に呼ばれない。安全に Release できる。
				Debug.Log("EOS: EnteredEditMode — PlatformInterface.Release() を呼び出します");
				platform.Release();

#if UNITY_EDITOR_WIN
				// Windows: EOS ネイティブスレッドが動いたまま FreeLibrary を呼ぶとフリーズするため
				// 事前に Shutdown() でスレッドを終了させる。
				// (Shutdown 後は EOS.dll が再ロードされるため次回プレイでも再初期化できる)
				PlatformInterface.Shutdown();
				Debug.Log("EOS: Shutdown 完了 (Windows)");
#else
				// macOS 等: PlatformInterface.Shutdown() を呼ぶと同一プロセス内で
				// dylib を再ロードできず、次の再生時に「failed to create PlatformInterface」
				// エラーになる。Release のみで留め、EOS SDK の初期化状態を維持する。
				Debug.Log("EOS: Release 完了 (macOS: Shutdown はスキップ — 次回プレイで再利用)");
#endif
			}
		}
	}
}
#endif

#endif
