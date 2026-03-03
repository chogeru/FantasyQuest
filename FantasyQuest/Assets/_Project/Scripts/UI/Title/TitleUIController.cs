using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Title
{
    public class TitleUIController : MonoBehaviour
    {
        [Header("State Containers")]
        [SerializeField] private GameObject _pressAnyButtonContainer;
        [SerializeField] private GameObject _mainMenuContainer;

        [Header("Buttons")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _quitButton;

        private TitleSceneManager _manager;

        private void Awake()
        {
            _manager = GetComponent<TitleSceneManager>();
            if (_manager == null)
            {
                Debug.LogError("TitleUIController requires TitleSceneManager on the same GameObject!");
                return;
            }

            // UIの初期設定
            _newGameButton.onClick.AddListener(_manager.OnClickNewGame);
            _continueButton.onClick.AddListener(_manager.OnClickContinue);
            _optionsButton.onClick.AddListener(_manager.OnClickOptions);
            _quitButton.onClick.AddListener(_manager.OnClickQuit);
        }

        public void ShowPressAnyButton()
        {
            _pressAnyButtonContainer.SetActive(true);
            _mainMenuContainer.SetActive(false);
        }

        public void ShowMainMenu()
        {
            _pressAnyButtonContainer.SetActive(false);
            _mainMenuContainer.SetActive(true);
            
            // コントローラー操作用にフォーカスを初期化する
            if (_newGameButton != null)
            {
                _newGameButton.Select();
            }
        }
    }
}
