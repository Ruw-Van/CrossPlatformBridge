#if USE_CROSSPLATFORMBRIDGE_EOS
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Network;
using CrossPlatformBridge.Testing;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace CrossPlatformBridge.Platform.EOS.Network
{
	/// <summary>
	/// EOS (Epic Online Services) 用のネットワークコア機能を実装するクラス。
	/// AccountPortal 認証 → Connect インターフェースで ProductUserId を取得する。
	///
	/// EOSAccount などで既にログイン済みの場合は既存セッションを再利用し、
	/// 二重ログインを回避する。
	/// </summary>
	public partial class NetworkHandler : IInternalNetworkHandler, IServiceTestProvider
	{
		private Epic.OnlineServices.Platform.PlatformInterface _platform;
		private EpicAccountId _epicAccountId;
		private ProductUserId _productUserId;
		private EOSManager _ownedEOSManager = null; // 自動生成した場合のみ非null

		/// <summary>
		/// Connect() で Auth ログインを自前で行った場合 true。
		/// 既存セッションを再利用した場合は false で、Disconnect() 時にログアウトしない。
		/// </summary>
		private bool _ownedAuthSession = false;

		public object AccountId { get; private set; }
		public string NickName { get; private set; }
		public object StationId { get; private set; }
		public bool IsConnected { get; private set; } = false;
		public bool IsHost { get; private set; } = false;
		public List<PlayerData> ConnectedList { get; } = new List<PlayerData>();
		public List<PlayerData> DisconnectedList { get; } = new List<PlayerData>();

		public INetworkSettingsFactory SettingsFactory { get; } = new EOSSettingsFactory();

		public event Action<byte[], string> OnDataReceived;
#pragma warning disable CS0067
		public event Action<string, string> OnPlayerConnected;
		public event Action<string, string> OnPlayerDisconnected;
#pragma warning restore CS0067
		public event Action<bool> OnNetworkConnectionStatusChanged;
		public event Action<bool> OnHostStatusChanged;
		public event Action<string, bool, string> OnLobbyOperationCompleted;
		public event Action<string, bool, string> OnRoomOperationCompleted;

		/// <summary>
		/// EOSManager を確保して PlatformInterface を取得する。
		/// シーンに EOSManager が存在しない場合は自動生成する。
		/// 認証情報は "EOS Plugin/EOS Configuration" で設定済みであること。
		/// </summary>
		public bool Initialize(INetworkSettings baseSettings)
		{
			// EOSManager MonoBehaviour が存在しない場合は生成する。
			// Awake() が即座に呼ばれ、Init() → DontDestroyOnLoad が実行される。
			if (UnityEngine.Object.FindFirstObjectByType<EOSManager>() == null)
			{
				var go = new GameObject("EOSManager");
				try
				{
					_ownedEOSManager = go.AddComponent<EOSManager>();
					Debug.Log("EOS: EOSManager を自動生成しました");
				}
				catch (Exception e)
				{
					UnityEngine.Object.Destroy(go);
					Debug.LogError($"EOS: EOSManager の初期化に失敗しました: {e.Message}");
					return false;
				}
			}

			_platform = EOSManager.Instance.GetEOSPlatformInterface();
			if (_platform == null)
			{
				Debug.LogError("EOS: PlatformInterface が取得できません。\"EOS Plugin/EOS Configuration\" の設定を確認してください。");
				return false;
			}

			Debug.Log("EOS: Platform 取得完了");
			return true;
		}

		public void Shutdown()
		{
			_epicAccountId = null;
			_productUserId = null;
			AccountId = null;
			NickName = null;
			StationId = null;
			IsConnected = false;
			IsHost = false;
			_ownedAuthSession = false;
			ConnectedList.Clear();
			DisconnectedList.Clear();

			// PlatformInterface の Release は EOSManager が管理するため、ここでは行わない
			_platform = null;

			// 自動生成した EOSManager のみ破棄する（既存のものは残す）
			if (_ownedEOSManager != null)
			{
				UnityEngine.Object.Destroy(_ownedEOSManager.gameObject);
				_ownedEOSManager = null;
				Debug.Log("EOS: 自動生成した EOSManager を破棄しました");
			}

			Debug.Log("EOS: Shutdown 完了");
		}

		/// <summary>
		/// EOS Auth + Connect ログインを行い、ProductUserId を取得する。
		/// EOSAccount などで既にログイン済みの場合は既存セッションを再利用し、
		/// 二重ログイン（ブラウザ再起動）を回避する。
		/// </summary>
		public async UniTask<bool> Connect(INetworkSettings baseSettings)
		{
			if (_platform == null)
			{
				Debug.LogError("EOS: Platform が初期化されていません");
				OnNetworkConnectionStatusChanged?.Invoke(false);
				return false;
			}

			var authInterface = _platform.GetAuthInterface();
			var connectInterface = _platform.GetConnectInterface();

			// --- 既存セッション確認 ---
			// EOSAccount など他コンポーネントが既に Auth + Connect 済みの場合は再利用する。
			// 再利用した場合は _ownedAuthSession = false とし、Disconnect() で Auth ログアウトしない。
			if (authInterface.GetLoggedInAccountsCount() > 0)
			{
				_epicAccountId = authInterface.GetLoggedInAccountByIndex(0);
				Debug.Log($"EOS: 既存の Auth セッションを再利用 EpicAccountId={_epicAccountId}");

				if (connectInterface.GetLoggedInUsersCount() > 0)
				{
					_productUserId = connectInterface.GetLoggedInUserByIndex(0);
					AccountId = _productUserId.ToString();
					NickName = _epicAccountId.ToString();
					IsConnected = true;
					_ownedAuthSession = false;
					Debug.Log($"EOS: 既存セッションで接続完了 ProductUserId={AccountId}");
					OnNetworkConnectionStatusChanged?.Invoke(true);
					return true;
				}

				// Auth は済んでいるが Connect が未完了の場合は Step 2 のみ実行
				Debug.Log("EOS: Auth は済み、Connect のみ実行します");
			}

			// --- Step 1: Auth (AccountPortal) ---
			// まだ Auth ログインしていない場合のみ実行
			if (_epicAccountId == null)
			{
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
					Debug.LogError($"EOS: Auth ログイン失敗 result={authResult.ResultCode}");
					OnNetworkConnectionStatusChanged?.Invoke(false);
					return false;
				}

				_epicAccountId = authResult.LocalUserId;
				_ownedAuthSession = true;
				Debug.Log($"EOS: Auth ログイン完了 EpicAccountId={_epicAccountId}");
			}

			// --- Step 2: Connect (ProductUserId 取得) ---
			var copyOptions = new Epic.OnlineServices.Auth.CopyUserAuthTokenOptions();
			var copyResult = authInterface.CopyUserAuthToken(ref copyOptions, _epicAccountId, out var authToken);
			if (copyResult != Result.Success || authToken == null)
			{
				Debug.LogError($"EOS: AuthToken コピー失敗 result={copyResult}");
				OnNetworkConnectionStatusChanged?.Invoke(false);
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
					Debug.LogError($"EOS: CreateUser 失敗 result={createResult.ResultCode}");
					OnNetworkConnectionStatusChanged?.Invoke(false);
					return false;
				}
				_productUserId = createResult.LocalUserId;
			}
			else
			{
				Debug.LogError($"EOS: Connect ログイン失敗 result={connectResult.ResultCode}");
				OnNetworkConnectionStatusChanged?.Invoke(false);
				return false;
			}

			AccountId = _productUserId.ToString();
			NickName = _epicAccountId.ToString();
			IsConnected = true;
			Debug.Log($"EOS: 接続完了 ProductUserId={AccountId}");
			OnNetworkConnectionStatusChanged?.Invoke(true);
			return true;
		}

		public async UniTask Disconnect()
		{
			// 自前で Auth ログインした場合のみログアウトする。
			// EOSAccount の既存セッションを再利用した場合はログアウトしない
			// （EOSAccount 側が引き続きセッションを使用している可能性があるため）。
			if (_ownedAuthSession)
			{
				var authInterface = _platform?.GetAuthInterface();
				if (authInterface != null && _epicAccountId != null)
				{
					var logoutOptions = new Epic.OnlineServices.Auth.LogoutOptions { LocalUserId = _epicAccountId };
					var tcs = new UniTaskCompletionSource<Epic.OnlineServices.Auth.LogoutCallbackInfo>();
					authInterface.Logout(ref logoutOptions, null, (ref Epic.OnlineServices.Auth.LogoutCallbackInfo info) =>
					{
						tcs.TrySetResult(info);
					});
					await tcs.Task;
					Debug.Log("EOS: Auth ログアウト完了");
				}
			}
			else
			{
				Debug.Log("EOS: 既存セッション再利用のため Auth ログアウトをスキップ");
			}

			_epicAccountId = null;
			_productUserId = null;
			AccountId = null;
			NickName = null;
			StationId = null;
			IsConnected = false;
			IsHost = false;
			_ownedAuthSession = false;
			ConnectedList.Clear();
			OnNetworkConnectionStatusChanged?.Invoke(false);
			OnHostStatusChanged?.Invoke(false);
			Debug.Log("EOS: 切断完了");
		}

		/// <summary>
		/// P2P パケット受信ポーリングを行う。
		/// Tick は EOSManager が内部で処理するため、ここでは呼ばない。
		/// Network.cs の Update() から毎フレーム呼ばれる想定。
		/// </summary>
		public void UpdateState()
		{
			PollIncomingPackets();
		}

		// --------------------------------------------------------------------------------
		// IServiceTestProvider
		// --------------------------------------------------------------------------------

		public IReadOnlyList<TestOperation> GetTestOperations() => null;

		public TestDefaultData GetDefaultData() => new TestDefaultData { UserName = "EOSPlayer", LobbyRoomName = "EOSLobby", SendData = "Hello from EOS!" };
	}
}

#endif
