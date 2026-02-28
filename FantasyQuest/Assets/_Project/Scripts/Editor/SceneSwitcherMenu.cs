using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>
    /// シーンをワンクリックで切り替えるエディタ拡張メニュー。
    /// （アイディア7番の具現化）
    /// </summary>
    public static class SceneSwitcherMenu
    {
        private const string SCENE_DIR = "Assets/_Project/Scenes/";
        private const string PLAYGROUND_DIR = "Assets/_Project/Tests/Playground/";

        [MenuItem("Scenes/Quick Load/Title Scene", false, 1)]
        public static void OpenTitleScene()
        {
            OpenScene(SCENE_DIR + "Title.unity");
        }

        [MenuItem("Scenes/Quick Load/Game Scene", false, 2)]
        public static void OpenGameScene()
        {
            OpenScene(SCENE_DIR + "Game.unity");
        }

        [MenuItem("Scenes/Quick Load/Playground (Test)", false, 20)]
        public static void OpenPlaygroundScene()
        {
            OpenScene(PLAYGROUND_DIR + "Playground.unity"); // ※事前に作成しておく必要があります
        }

        private static void OpenScene(string path)
        {
            // シーンを開く前に変更があれば保存するか確認する
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                try
                {
                    EditorSceneManager.OpenScene(path);
                    Debug.Log($"[SceneSwitcher] シーンをロードしました: {path}");
                }
                catch
                {
                    Debug.LogWarning($"[SceneSwitcher] シーンが見つかりません: {path} \n先にシーンファイルを作成してください。");
                }
            }
        }
    }
}
