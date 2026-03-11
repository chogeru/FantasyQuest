using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using Project.Systems.Audio;
using Project.UI.Utility;

namespace Project.UI.Title
{
    public class TitleUIController : SerializedMonoBehaviour
    {
        [TitleGroup("Panel Management (Hierarchy)")]
        [SerializeField] private UIMenuPanel _pressAnyButtonPanel;
        [SerializeField] private UIMenuPanel _mainMenuPanel;
        [SerializeField] private UIMenuPanel _optionsPanel; // 将来用の拡張パネル

        private UIMenuPanel _currentActivePanel;

        [TitleGroup("Main Menu Buttons")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _quitButton;

        [TitleGroup("Cursor & Animation Settings")]
        [SerializeField] private RectTransform _cursorRect;
        [Tooltip("カーソル移動のスムーズさ。低いほど速い")]
        [SerializeField] private float _cursorMoveTime = 0.1f;
        [SerializeField] private Vector2 _cursorOffset = new Vector2(-150f, 0f); // ボタンの左側に配置
        
        [TitleGroup("Audio Settings")]
        [SerializeField] private string _hoverSEId = "CursorMove";

        private TitleSceneManager _manager;
        private GameObject _lastSelectedObject;
        private Vector3 _cursorTargetPos;
        private Vector3 _cursorVelocity;

        private void Awake()
        {
            _manager = GetComponent<TitleSceneManager>();
            
            // UIの初期設定
            _newGameButton.onClick.AddListener(_manager.OnClickNewGame);
            _continueButton.onClick.AddListener(_manager.OnClickContinue);
            _optionsButton.onClick.AddListener(_manager.OnClickOptions);
            _quitButton.onClick.AddListener(_manager.OnClickQuit);

            // 全パネルを一旦非表示に
            _pressAnyButtonPanel.Hide();
            _mainMenuPanel.Hide();
            if (_optionsPanel != null) _optionsPanel.Hide();

            if (_cursorRect != null)
            {
                _cursorTargetPos = _cursorRect.position;
            }
        }

        private void Update()
        {
            HandleCursorMovement();
        }

        // --- Panel Navigation ---

        private void SwitchPanel(UIMenuPanel newPanel)
        {
            if (_currentActivePanel != null)
            {
                _currentActivePanel.Hide();
            }

            _currentActivePanel = newPanel;

            if (_currentActivePanel != null)
            {
                _currentActivePanel.Show();
                UpdateCursorTargetInstantly();
            }
        }

        public void ShowPressAnyButton()
        {
            SwitchPanel(_pressAnyButtonPanel);
            if (_cursorRect != null) _cursorRect.gameObject.SetActive(false);
        }

        public void ShowMainMenu()
        {
            SwitchPanel(_mainMenuPanel);
            if (_cursorRect != null) _cursorRect.gameObject.SetActive(true);
        }

        public void ShowOptionsPanel()
        {
            if (_optionsPanel != null)
            {
                SwitchPanel(_optionsPanel);
            }
            else
            {
                Debug.Log("Options Panel is not assigned yet.");
            }
        }

        // --- Cursor Animation & Hybrid Input ---

        private void HandleCursorMovement()
        {
            if (_cursorRect == null || !gameObject.activeInHierarchy || _currentActivePanel != _mainMenuPanel) return;

            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            
            // もし何も選択されていない（マウスクリック等でフォーカス外れ）場合は最後に選択されていた対象に戻す
            // これにより、マウスとコントローラーのハイブリッド対応が可能になる
            if (currentSelected == null && _lastSelectedObject != null && _lastSelectedObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(_lastSelectedObject);
                currentSelected = _lastSelectedObject;
            }

            if (currentSelected != null && currentSelected != _lastSelectedObject)
            {
                // 新しい項目が選択された
                RectTransform targetRect = currentSelected.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    _cursorTargetPos = targetRect.position + (Vector3)_cursorOffset;
                }

                if (_lastSelectedObject != null)
                {
                    AudioManager.Instance.PlaySE(_hoverSEId);
                }

                _lastSelectedObject = currentSelected;
            }

            // SmoothDampでカーソルを滑らかに移動させる
            _cursorRect.position = Vector3.SmoothDamp(_cursorRect.position, _cursorTargetPos, ref _cursorVelocity, _cursorMoveTime);
        }

        private void UpdateCursorTargetInstantly()
        {
            if (_cursorRect == null) return;
            
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected != null)
            {
                RectTransform targetRect = currentSelected.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    _cursorTargetPos = targetRect.position + (Vector3)_cursorOffset;
                    _cursorRect.position = _cursorTargetPos; // 即座に反映
                    _lastSelectedObject = currentSelected;
                }
            }
        }
    }
}
