using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Project.Systems.Input;
using Project.Systems.Audio;
using Project.UI.Utility;

namespace Project.UI.Title
{
    public class TitleSceneManager : MonoBehaviour
    {
        public enum TitleState
        {
            PressAnyButton,
            MainMenu,
            Loading
        }

        [Header("References")]
        [SerializeField] private TitleUIController _titleUIController;
        [SerializeField] private InputReader _inputReader;

        [Header("Audio IDs")]
        [SerializeField] private string _titleBGMId = "TitleBGM";
        [SerializeField] private string _submitSEId = "SubmitSE";
        [SerializeField] private string _cancelSEId = "CancelSE";

        [Header("Loading UI")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private Slider _loadingProgressBar;
        [SerializeField] private Text _loadingText;

        [Header("Settings")]
        [SerializeField] private string _newGameSceneName = "MainLevelScene";

        private TitleState _currentState = TitleState.PressAnyButton;
        private bool _isTransitioning = false;

        private void Start()
        {
            _currentState = TitleState.PressAnyButton;
            _titleUIController.ShowPressAnyButton();
            
            if (_loadingPanel != null) _loadingPanel.SetActive(false);

            if (AudioManager.Instance != null && !string.IsNullOrEmpty(_titleBGMId))
            {
                AudioManager.Instance.PlayBGM(_titleBGMId);
            }

            // [フェードイン] シーン開始時
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FadeIn(1.0f);
            }
        }

        private void OnEnable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnJumpEvent += HandleAnyButton;
                _inputReader.OnAttackEvent += HandleAnyButton;
            }
        }

        private void OnDisable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnJumpEvent -= HandleAnyButton;
                _inputReader.OnAttackEvent -= HandleAnyButton;
            }
        }

        private void HandleAnyButton()
        {
            if (_currentState == TitleState.PressAnyButton && !_isTransitioning)
            {
                TransitionToMainMenu();
            }
        }

        public void TransitionToMainMenu()
        {
            if (_currentState == TitleState.MainMenu || _isTransitioning) return;

            PlaySubmitSE();
            _currentState = TitleState.MainMenu;
            _titleUIController.ShowMainMenu();
        }

        public void TransitionToPressAnyButton()
        {
            if (_currentState == TitleState.PressAnyButton || _isTransitioning) return;

            PlayCancelSE();
            _currentState = TitleState.PressAnyButton;
            _titleUIController.ShowPressAnyButton();
        }

        // --- Menu Actions ---

        public void OnClickNewGame()
        {
            if (_isTransitioning) return;
            PlaySubmitSE();
            StartGameWithFadeAndLoad(_newGameSceneName);
        }

        public void OnClickContinue()
        {
            if (_isTransitioning) return;
            PlaySubmitSE();
            Debug.Log("Continue Game... (Checking save data...)");
            // TODO: セーブデータが存在するか確認し、そのシーン名を渡す
        }

        public void OnClickOptions()
        {
            if (_isTransitioning) return;
            PlaySubmitSE();
            _titleUIController.ShowOptionsPanel();
        }

        public void OnClickQuit()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            PlayCancelSE();
            
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FadeOut(0.5f, () => 
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        // --- Loading Sequences ---

        private void StartGameWithFadeAndLoad(string sceneName)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            _currentState = TitleState.Loading;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM(1.5f);
            }

            if (ScreenFader.Instance != null)
            {
                // 暗転してからロード画面を表示
                ScreenFader.Instance.FadeOut(1.0f, () => 
                {
                    StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
                });
            }
            else
            {
                StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
            }
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
        {
            // ローディング画面を表示し、他のUIを隠す
            if (_loadingPanel != null) _loadingPanel.SetActive(true);
            _titleUIController.gameObject.SetActive(false);

            // フェードを戻して(明転して)ローディング画面を見せる (お好みで暗転させたまま裏でロードさせてもOK)
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.FadeIn(0.5f);
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false; // 90%で止める

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                
                if (_loadingProgressBar != null) _loadingProgressBar.value = progress;
                if (_loadingText != null) _loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";

                if (asyncLoad.progress >= 0.9f)
                {
                    // ロード完了したら少し待って自動で次へ行くか、「Push to Start」を出すことができる
                    
                    if (ScreenFader.Instance != null)
                    {
                        // 完全にロード完了したら再度暗転し、次シーンへ
                        ScreenFader.Instance.FadeOut(0.5f, () => 
                        {
                            asyncLoad.allowSceneActivation = true;
                        });
                    }
                    else
                    {
                        asyncLoad.allowSceneActivation = true;
                    }

                    yield break;
                }
                yield return null;
            }
        }

        // --- Audio Helpers ---
        private void PlaySubmitSE()
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(_submitSEId))
            {
                AudioManager.Instance.PlaySE(_submitSEId);
            }
        }

        private void PlayCancelSE()
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(_cancelSEId))
            {
                AudioManager.Instance.PlaySE(_cancelSEId);
            }
        }
    }
}
