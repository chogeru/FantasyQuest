using UnityEngine;

namespace Project.Core.Managers
{
    /// <summary>
    /// ScriptableObjectをベースにしたシングルトンクラス。
    /// （アイディア3番の具現化）
    /// </summary>
    public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Resourcesフォルダ内にある同型のアセットを読み込む
                    T[] assets = Resources.LoadAll<T>("");
                    if (assets == null || assets.Length == 0)
                    {
                        Debug.LogError($"[ScriptableSingleton] {typeof(T).Name} のインスタンスが見つかりません。Resourcesフォルダの中に作成してください。");
                    }
                    else if (assets.Length > 1)
                    {
                        Debug.LogWarning($"[ScriptableSingleton] {typeof(T).Name} のインスタンスが複数見つかりました。最初に見つかったものを利用します。");
                        _instance = assets[0];
                    }
                    else
                    {
                        _instance = assets[0];
                    }
                }
                return _instance;
            }
        }
    }
}
