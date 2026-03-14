using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CrossPlatformBridge.Services.CloudStorage
{
	/// <summary>
	/// プラットフォーム固有のクラウドストレージ処理を抽象化するインターフェース。
	/// </summary>
	public interface IInternalCloudStorageHandler
	{
		// --------------------------------------------------------------------------------
		// 保存
		// --------------------------------------------------------------------------------

		/// <summary>指定キーに文字列データを保存します。</summary>
		/// <param name="key">データキー。</param>
		/// <param name="value">保存する値。</param>
		UniTask<bool> SaveData(string key, string value);

		/// <summary>複数のキーと値を一括で保存します。</summary>
		/// <param name="data">保存するキー・値のペア。</param>
		UniTask<bool> SaveDataBatch(Dictionary<string, string> data);

		// --------------------------------------------------------------------------------
		// 読み込み
		// --------------------------------------------------------------------------------

		/// <summary>指定キーのデータを読み込みます。キーが存在しない場合は null を返します。</summary>
		UniTask<string> LoadData(string key);

		/// <summary>複数のキーのデータを一括で読み込みます。</summary>
		/// <param name="keys">読み込むキーの一覧。null または空の場合は全データを取得します。</param>
		UniTask<Dictionary<string, string>> LoadDataBatch(List<string> keys);

		// --------------------------------------------------------------------------------
		// 削除
		// --------------------------------------------------------------------------------

		/// <summary>指定キーのデータを削除します。</summary>
		UniTask<bool> DeleteData(string key);
	}
}
