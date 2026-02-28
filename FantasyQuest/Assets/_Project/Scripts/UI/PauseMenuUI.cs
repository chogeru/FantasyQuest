using UnityEngine;
using Project.Systems.Save;

namespace Project.UI
{
    /// <summary>
    /// ESCキーなどで開かれるシステムメニュー。
    /// 時間の停止（Time.timeScale）、ポーズ解除、およびセーブ・ロードUIの基盤。
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("システムメニューのパネル全体")]
        [SerializeField] private GameObject _pauseMenuPanel;
        [Tooltip("呼び出す対象のSaveSystem")]
        [SerializeField] private SaveSystem _saveSystem;

        private bool _isPaused = false;

        private void Start()
        {
            if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(false);
        }

        private void Update()
        {
            // 仮入力: ESCキーでメニューを開閉
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }

        private void PauseGame()
        {
            if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(true);
            
            // ゲーム内の時間を極限まで遅くする（物理演算やUpdateを停止・スロー化するため）
            // 0 にするとアクションゲームの完全停止が可能
            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void ResumeGame()
        {
            if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(false);
            
            // 時間の流れを元に戻す
            Time.timeScale = 1f;
            _isPaused = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ==========================================
        // UIボタンから呼び出されるメソッド群
        // ==========================================

        /// <summary>
        /// Save Button の OnClick で発火
        /// </summary>
        public void OnSaveButtonClick()
        {
            if (_saveSystem != null)
            {
                _saveSystem.SaveGame();
                Debug.Log("セーブボタンが押されました。");
            }
        }

        /// <summary>
        /// Load Button の OnClick で発火
        /// </summary>
        public void OnLoadButtonClick()
        {
            if (_saveSystem != null)
            {
                _saveSystem.LoadGame();
                Debug.Log("ロードボタンが押されました。");
                
                // ロード直後にゲームへ戻る
                ResumeGame(); 
            }
        }

        /// <summary>
        /// Quit Button の OnClick で発火
        /// </summary>
        public void OnQuitButtonClick()
        {
            // 時間を戻してから終了
            Time.timeScale = 1f;
            Debug.Log("ゲームを終了します...");
            Application.Quit();
        }
    }
}
