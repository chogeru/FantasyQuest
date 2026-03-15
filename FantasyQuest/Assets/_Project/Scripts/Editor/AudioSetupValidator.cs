using UnityEngine;
using UnityEditor;
using Project.Systems.Audio;
using Project.UI.Title;

namespace Project.Editor
{
    public class AudioSetupValidator : EditorWindow
    {
        [MenuItem("Project/Validate/Title Audio Setup")]
        public static void ShowWindow()
        {
            GetWindow<AudioSetupValidator>("Audio Validator");
        }

        private void OnGUI()
        {
            GUILayout.Label("タイトルBGM 診断ツール (Title BGM Validator)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("診断を実行する (Run Diagnosis)", GUILayout.Height(40)))
            {
                RunDiagnosis();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("診断結果はConsole(コンソール)ウィンドウに表示・色分けされます。\n赤色(エラー)の項目を確認して修正してください。", MessageType.Info);
        }

        private void RunDiagnosis()
        {
            Debug.Log("<b><color=cyan>=== タイトルBGM 診断開始 ===</color></b>");

            bool allPassed = true;

            // 1. Check AudioManager
            var audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogError("<color=red>[失敗: Step 1]</color> シーン内に <b>AudioManager</b> が見つかりません。Setup Title Scene を実行しましたか？");
                allPassed = false;
            }
            else
            {
                Debug.Log("<color=green>[OK]</color> AudioManager がシーンに存在します。");
                
                // Check Common Config
                var serializedManager = new SerializedObject(audioManager);
                var commonConfigProp = serializedManager.FindProperty("_commonConfig");
                if (commonConfigProp.objectReferenceValue == null)
                {
                    Debug.LogError("<color=red>[失敗: Step 1]</color> AudioManager の <b>Common Config</b> が設定されていません。");
                    allPassed = false;
                }
            }

            // 2. Check Scene Audio Config Loader
            var configLoader = FindObjectOfType<AudioConfigLoader>();
            if (configLoader == null)
            {
                Debug.LogError("<color=red>[失敗: Step 2]</color> シーン内に <b>SceneAudioConfigLoader</b> が見つかりません。");
                allPassed = false;
            }
            else
            {
                Debug.Log("<color=green>[OK]</color> SceneAudioConfigLoader がシーンに存在します。");
                
                // Check Title Audio Config
                var serializedLoader = new SerializedObject(configLoader);
                var sceneConfigProp = serializedLoader.FindProperty("_sceneAudioConfig");
                var titleConfig = sceneConfigProp.objectReferenceValue as AudioDataConfig;

                if (titleConfig == null)
                {
                    Debug.LogError("<color=red>[失敗: Step 2]</color> SceneAudioConfigLoader に <b>TitleAudioConfig</b> がセットされていません。");
                    allPassed = false;
                }
                else
                {
                    Debug.Log("<color=green>[OK]</color> TitleAudioConfig が Loader にセットされています。");

                    // 3. Check inside TitleAudioConfig for "TitleBGM"
                    var bgmEntry = titleConfig.GetBGMEntry("TitleBGM");
                    if (bgmEntry == null)
                    {
                        Debug.LogError("<color=red>[失敗: Step 3]</color> <b>TitleAudioConfig</b> の Bgm Dictionary に <b>'TitleBGM'</b> という名前(ID)の項目がありません。文字の打ち間違い(大文字やスペース)がないか確認してください！");
                        allPassed = false;
                    }
                    else
                    {
                        Debug.Log("<color=green>[OK]</color> 'TitleBGM' のIDが登録されています。");
                        
                        // 4. Check if AudioClip is assigned
                        if (bgmEntry.mainLoopClip == null)
                        {
                            Debug.LogError("<color=red>[失敗: Step 4]</color> 'TitleBGM' の <b>Main Loop Clip</b> にオーディオデータ(音楽ファイル)がセットされていません。");
                            allPassed = false;
                        }
                        else
                        {
                            Debug.Log("<color=green>[OK]</color> 音楽ファイルがセットされています: " + bgmEntry.mainLoopClip.name);
                        }
                    }
                }
            }

            // 5. Check TitleSceneManager BGM ID settings
            var titleManager = FindObjectOfType<TitleSceneManager>();
            if (titleManager == null)
            {
                Debug.LogError("<color=red>[失敗: Step 5]</color> シーン内に <b>TitleManager</b> が見つかりません。");
                allPassed = false;
            }
            else
            {
                var serializedTitle = new SerializedObject(titleManager);
                var bgmIdProp = serializedTitle.FindProperty("_titleBGMId");
                
                if (bgmIdProp.stringValue != "TitleBGM")
                {
                    Debug.LogWarning($"<color=yellow>[警告: Step 5]</color> TitleSceneManager が呼び出す BGM ID が '{bgmIdProp.stringValue}' になっています。TitleAudioConfig の名前と一致していないと鳴りません。");
                }
                else
                {
                    Debug.Log("<color=green>[OK]</color> TitleSceneManager は 'TitleBGM' を呼び出す設定になっています。");
                }
            }

            if (allPassed)
            {
                Debug.Log("<b><color=yellow>=== 全ての診断をクリアしました！ ===</color></b>\nこれで音が鳴らない場合、Unityエディタの「Mute Audio」ボタンがオンになっていないか確認してください！");
            }
            else
            {
                Debug.Log("<b><color=red>=== エラーが見つかりました！上の赤文字を確認してください ===</color></b>");
            }
        }
    }
}
