#if USE_CROSSPLATFORMBRIDGE_STEAM
#if !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using CrossPlatformBridge.Services.Account;
using CrossPlatformBridge.Testing;
using Steamworks;
using UnityEngine;

namespace CrossPlatformBridge.Platform.Steam.Account
{
	/// <summary>
	/// Steamプラットフォーム向けのアカウントサービス実装。
	/// Steamworks.NET を使用して SteamID と表示名を取得します。
	/// </summary>
	public class SteamAccountService : IInternalAccountHandler, IServiceTestProvider
	{
		public string AccountId { get; private set; }
		public string NickName { get; private set; }
		public bool IsInitialized { get; private set; }

		/// <summary>
		/// 認証状態が変化した際に発生するイベント。
		/// 引数は新しい初期化状態（true = 初期化済み、false = 未初期化）。
		/// </summary>
		public event Action<bool> OnAuthStateChanged;

		/// <summary>
		/// Steam アカウント情報を取得して初期化します。
		/// </summary>
		/// <exception cref="AccountServiceException">
		/// Steam クライアントが起動していない、または SteamAPI の初期化に失敗した場合にスローされます。
		/// </exception>
		public async UniTask<bool> InitializeAsync()
		{
			if (IsInitialized) return true;

			if (!SteamAPI.IsSteamRunning())
			{
				throw new AccountServiceException("Steam クライアントが起動していません。");
			}

			if (!SteamAPI.Init())
			{
				throw new AccountServiceException("SteamAPI の初期化に失敗しました。steam_appid.txt が正しく配置されているか確認してください。");
			}

			try
			{
				var steamId = SteamUser.GetSteamID();
				if (!steamId.IsValid())
				{
					throw new AccountServiceException("有効な SteamID を取得できませんでした。");
				}

				AccountId = steamId.ToString();
				NickName = SteamFriends.GetPersonaName();

				IsInitialized = true;
				OnAuthStateChanged?.Invoke(true);
				Debug.Log($"[SteamAccountService] 初期化完了 AccountId={AccountId} NickName={NickName}");
			}
			catch (AccountServiceException)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new AccountServiceException($"Steam アカウント情報の取得中にエラーが発生しました: {e.Message}", e);
			}

			await UniTask.CompletedTask;
			return true;
		}

		/// <summary>
		/// Steam アカウントサービスをシャットダウンし、SteamAPI をシャットダウンします。
		/// </summary>
		public async UniTask ShutdownAsync()
		{
			if (!IsInitialized) return;

			SteamAPI.Shutdown();

			AccountId = null;
			NickName = null;
			IsInitialized = false;

			OnAuthStateChanged?.Invoke(false);
			Debug.Log("[SteamAccountService] シャットダウン完了");

			await UniTask.CompletedTask;
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

#endif
