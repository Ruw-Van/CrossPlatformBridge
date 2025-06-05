using UnityEditor; // Editor スクリプトなので必要です
using UnityEngine; // Debug.Log などで使いますが、必須ではありません

// Unity Editor の起動時やスクリプトのコンパイル時にこのクラスを初期化する
[InitializeOnLoad]
public class PlayModeRefresher
{
	// 静的コンストラクタは、InitializeOnLoad が付与されたクラスがロードされる際に自動的に実行されます
	static PlayModeRefresher()
	{
		// playModeStateChanged イベントに OnPlayModeStateChanged メソッドを登録します
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	// Play Mode の状態が変化したときに呼び出されるメソッド
	private static void OnPlayModeStateChanged(PlayModeStateChange state)
	{
		// Play Mode に入る直前の状態である ExitingEditMode の場合のみ処理を実行します
		if (state == PlayModeStateChange.ExitingEditMode)
		{
			Debug.Log("Play Mode に入ります。アセットデータベースをリフレッシュします。");
			AssetDatabase.Refresh(); // ここでアセットデータベースを更新します
		}
	}
}
