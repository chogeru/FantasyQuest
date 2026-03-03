using UnityEngine;
using UnityEngine.SceneManagement;
using Project.Systems.Input;
using Project.Systems.Audio;

namespace Project.UI.Title
{
    public class TitleSceneManager : MonoBehaviour
    {
        public enum TitleState
        {
            PressAnyButton,
            MainMenu
        }

        [Header("References")]
        [SerializeField] private TitleUIController _titleUIController;
        [SerializeField] private InputReader _inputReader;

        [Header("Audio IDs")]
        [SerializeField] private string _titleBGMId = "TitleBGM";
        [SerializeField] private string _submitSEId = "SubmitSE";
        [SerializeField] private string _cancelSEId = "CancelSE";

        [Header("Settings")]
        [SerializeField] private string _newGameSceneName = "MainLevelScene";

        private TitleState _currentState = TitleState.PressAnyButton;
        private bool _isTransitioning = false;

        private void Start()
        {
            _currentState = TitleState.PressAnyButton;
            _titleUIController.ShowPressAnyButton();
            
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(_titleBGMId))
            {
                AudioManager.Instance.PlayBGM(_titleBGMId);
            }
        }

        private void OnEnable()
        {
            if (_inputReader != null)
            {
                // Push Button画面からの遷移用
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
            if (_currentState == TitleState.PressAnyButton)
            {
                TransitionToMainMenu();
            }
        }

        public void TransitionToMainMenu()
        {
            if (_currentState == TitleState.MainMenu) return;

            PlaySubmitSE();
            _currentState = TitleState.MainMenu;
            _titleUIController.ShowMainMenu();
        }

        public void TransitionToPressAnyButton()
        {
            if (_currentState == TitleState.PressAnyButton) return;

            PlayCancelSE();
            _currentState = TitleState.PressAnyButton;
            _titleUIController.ShowPressAnyButton();
        }

        // --- Menu Actions ---

        public void OnClickNewGame()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            PlaySubmitSE();
            Debug.Log("Starting New Game...");
            
            // AudioManager.Instance?.StopBGM();
            // SceneManager.LoadSceneAsync(_newGameSceneName);
        }

        public void OnClickContinue()
        {
            if (_isTransitioning) return;
            PlaySubmitSE();
            Debug.Log("Continue Game... (Load save data if implemented)");
            // TODO: セーブデータ読み込み
        }

        public void OnClickOptions()
        {
            if (_isTransitioning) return;
            PlaySubmitSE();
            Debug.Log("Opening Options...");
            // TODO: オプション画面の表示
        }

        public void OnClickQuit()
        {
            if (_isTransitioning) return;
            PlayCancelSE();
            Debug.Log("Quitting Game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
