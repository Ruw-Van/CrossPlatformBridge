#if USE_CROSSPLATFORMBRIDGE_EOS
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Testing;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Account
{
	/// <summary>
	/// EOS (Epic Online Services) 用のアカウント管理クラス。
	/// AccountPortal 認証 → Connect インターフェースで ProductUserId を取得する。
	/// </summary>
	public class EOSAccount : IInternalAccountHandler, IDisposable, IServiceTestProvider
	{
		private Epic.OnlineServices.Platform.PlatformInterface _platform;
		private EpicAccountId _epicAccountId;
		private ProductUserId _productUserId;
		private EOSManager _ownedEOSManager;

		/// <summary>
		/// 認証が完了しているかどうか。
		/// </summary>
		public bool IsInitialized { get; private set; }

		/// <summary>
		/// ProductUserId の文字列表現。
		/// </summary>
		public string AccountId { get; private set; } = string.Empty;

		/// <summary>
		/// EpicAccountId の文字列表現。
		/// </summary>
		public string NickName { get; private set; } = string.Empty;

		/// <summary>
		/// Auth ログイン後に取得した EpicAccountId。
		/// </summary>
		public EpicAccountId EpicAccountId => _epicAccountId;

		/// <summary>
		/// Connect ログイン後に取得した ProductUserId。
		/// </summary>
		public ProductUserId ProductUserId => _productUserId;

		/// <summary>
		/// 認証状態が変化した際に発生するイベント。
		/// 引数は新しい初期化状態（true = 初期化済み、false = 未初期化）。
		/// </summary>
		public event Action<bool> OnAuthStateChanged;

		/// <summary>
		/// AccountPortal でブラウザ認証を行い、ProductUserId を取得します。
		/// </summary>
		/// <returns>認証に成功した場合は true、失敗した場合は false。</returns>
		public async UniTask<bool> InitializeAsync()
		{
			if (IsInitialized) return true;
			if (!EnsurePlatform()) return false;

			// --- Step 1: Auth (AccountPortal) ---
			var authInterface = _platform.GetAuthInterface();
			var loginOptions = new Epic.OnlineServices.Auth.LoginOptions
			{
				Credentials = new Epic.OnlineServices.Auth.Credentials
				{
					Type = LoginCredentialType.AccountPortal,
					Id = null,
					Token = null,
				},
				ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence,
			};

			var authTcs = new UniTaskCompletionSource<Epic.OnlineServices.Auth.LoginCallbackInfo>();
			authInterface.Login(ref loginOptions, null, (ref Epic.OnlineServices.Auth.LoginCallbackInfo info) =>
			{
				authTcs.TrySetResult(info);
			});

			var authResult = await authTcs.Task;
			if (authResult.ResultCode != Result.Success)
			{
				Debug.LogError($"EOS Account: Auth ログイン失敗 result={authResult.ResultCode}");
				return false;
			}

			_epicAccountId = authResult.LocalUserId;
			Debug.Log($"EOS Account: Auth 完了 EpicAccountId={_epicAccountId}");

			// --- Step 2: Connect (ProductUserId 取得) ---
			var connectInterface = _platform.GetConnectInterface();
			var copyOptions = new CopyUserAuthTokenOptions();
			var copyResult = authInterface.CopyUserAuthToken(ref copyOptions, _epicAccountId, out var authToken);
			if (copyResult != Result.Success || authToken == null)
			{
				Debug.LogError($"EOS Account: AuthToken コピー失敗 result={copyResult}");
				return false;
			}

			var connectLoginOptions = new Epic.OnlineServices.Connect.LoginOptions
			{
				Credentials = new Epic.OnlineServices.Connect.Credentials
				{
					Type = ExternalCredentialType.Epic,
					Token = authToken.Value.AccessToken,
				},
			};

			var connectTcs = new UniTaskCompletionSource<Epic.OnlineServices.Connect.LoginCallbackInfo>();
			connectInterface.Login(ref connectLoginOptions, null, (ref Epic.OnlineServices.Connect.LoginCallbackInfo info) =>
			{
				connectTcs.TrySetResult(info);
			});

			var connectResult = await connectTcs.Task;
			if (connectResult.ResultCode == Result.Success)
			{
				_productUserId = connectResult.LocalUserId;
			}
			else if (connectResult.ResultCode == Result.InvalidUser)
			{
				// ProductUser が存在しないので新規作成
				var createOptions = new CreateUserOptions
				{
					ContinuanceToken = connectResult.ContinuanceToken,
				};
				var createTcs = new UniTaskCompletionSource<CreateUserCallbackInfo>();
				connectInterface.CreateUser(ref createOptions, null, (ref CreateUserCallbackInfo info) =>
				{
					createTcs.TrySetResult(info);
				});
				var createResult = await createTcs.Task;
				if (createResult.ResultCode != Result.Success)
				{
					Debug.LogError($"EOS Account: CreateUser 失敗 result={createResult.ResultCode}");
					return false;
				}
				_productUserId = createResult.LocalUserId;
			}
			else
			{
				Debug.LogError($"EOS Account: Connect ログイン失敗 result={connectResult.ResultCode}");
				return false;
			}

			AccountId = _productUserId.ToString();
			NickName = _epicAccountId.ToString();
			IsInitialized = true;
			OnAuthStateChanged?.Invoke(true);
			Debug.Log($"EOS Account: ログイン完了 ProductUserId={AccountId}");
			return true;
		}

		/// <summary>
		/// EOS Auth からログアウトし、アカウント情報をクリアします。
		/// ログアウトリクエストは送信しますが、コールバック完了は待機しません。
		/// async/await を使わないことで、再生停止時の GC/Dispose 済みオブジェクトへの
		/// アクセスによるクラッシュ（EXC_BAD_ACCESS）を防ぎます。
		/// </summary>
		public UniTask ShutdownAsync()
		{
			if (!IsInitialized) return UniTask.CompletedTask;

			var authInterface = _platform?.GetAuthInterface();
			var epicAccountId = _epicAccountId;

			// コールバック到着前にローカル状態を同期クリアする
			_epicAccountId = null;
			_productUserId = null;
			AccountId = string.Empty;
			NickName = string.Empty;
			IsInitialized = false;
			OnAuthStateChanged?.Invoke(false);

			if (authInterface == null || epicAccountId == null)
			{
				Debug.Log("EOS Account: ログアウト完了（セッションなし）");
				return UniTask.CompletedTask;
			}

			// static ラムダで this を一切キャプチャしない。
			// Release() 後にコールバックが発火しても managed 参照にアクセスしないため安全。
			var logoutOptions = new Epic.OnlineServices.Auth.LogoutOptions { LocalUserId = epicAccountId };
			authInterface.Logout(ref logoutOptions, null, static (ref Epic.OnlineServices.Auth.LogoutCallbackInfo info) =>
			{
				Debug.Log($"EOS Account: ログアウトコールバック result={info.ResultCode}");
			});
			Debug.Log("EOS Account: ログアウトリクエスト送信");
			return UniTask.CompletedTask;
		}

		/// <summary>
		/// 自動生成した EOSManager を破棄します。
		/// </summary>
		public void Dispose()
		{
			if (_ownedEOSManager != null)
			{
				UnityEngine.Object.Destroy(_ownedEOSManager.gameObject);
				_ownedEOSManager = null;
				Debug.Log("EOS Account: 自動生成した EOSManager を破棄しました");
			}
		}

		/// <summary>
		/// EOSManager と PlatformInterface を確保します。
		/// シーンに EOSManager が存在しない場合は自動生成します。
		/// </summary>
		private bool EnsurePlatform()
		{
			if (_platform != null) return true;

			if (UnityEngine.Object.FindFirstObjectByType<EOSManager>() == null)
			{
				var go = new GameObject("EOSManager");
				try
				{
					_ownedEOSManager = go.AddComponent<EOSManager>();
					Debug.Log("EOS Account: EOSManager を自動生成しました");
				}
				catch (Exception e)
				{
					UnityEngine.Object.Destroy(go);
					Debug.LogError($"EOS Account: EOSManager の初期化に失敗しました: {e.Message}");
					return false;
				}
			}

			_platform = EOSManager.Instance.GetEOSPlatformInterface();
			if (_platform == null)
			{
				Debug.LogError("EOS Account: PlatformInterface が取得できません。\"EOS Plugin/EOS Configuration\" の設定を確認してください。");
				return false;
			}

			return true;
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => new TestOperation[]
		{
			new TestOperation { SectionLabel = "アカウント操作" },
			new TestOperation { Label = "Initialize", Action = async ctx => { bool ok = await InitializeAsync(); ctx.ReportResult($"Initialize → {ok}\nAccountId: {AccountId}\nNickName: {NickName}"); ctx.AppendLog($"Initialize → {ok}"); } },
			new TestOperation { Label = "Shutdown", Action = async ctx => { await ShutdownAsync(); ctx.ReportResult($"Shutdown 完了\nIsInitialized: {IsInitialized}"); ctx.AppendLog("Shutdown 完了"); } },
		};

		public TestDefaultData GetDefaultData() => new TestDefaultData();
	}
}

#endif
