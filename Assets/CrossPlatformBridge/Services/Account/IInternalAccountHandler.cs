using System;
using Cysharp.Threading.Tasks;

namespace CrossPlatformBridge.Services.Account
{
	/// <summary>
	/// プラットフォーム固有のアカウント機能へのインターフェース。
	/// </summary>
	public interface IInternalAccountHandler
	{
		/// <summary>初期化が完了しているかどうかを取得します。</summary>
		bool IsInitialized { get; }

		/// <summary>アカウントのプラットフォーム固有IDを取得します。</summary>
		string AccountId { get; }

		/// <summary>アカウントの表示名を取得します。</summary>
		string NickName { get; }

		/// <summary>
		/// 認証状態が変化した際に発生するイベント。
		/// 引数は新しい初期化状態（true = 初期化済み、false = 未初期化）。
		/// </summary>
		event Action<bool> OnAuthStateChanged;

		/// <summary>
		/// アカウントを非同期で初期化し、アカウント情報を取得します。
		/// </summary>
		/// <returns>初期化に成功した場合は true、失敗した場合は false。</returns>
		UniTask<bool> InitializeAsync();

		/// <summary>
		/// アカウントサービスを非同期でシャットダウンし、リソースを解放します。
		/// </summary>
		UniTask ShutdownAsync();
	}

	/// <summary>
	/// アカウントサービスの操作が失敗した場合にスローされる例外。
	/// </summary>
	public class AccountServiceException : Exception
	{
		public AccountServiceException(string message) : base(message) { }
		public AccountServiceException(string message, Exception innerException) : base(message, innerException) { }
	}
}
