using UnityEngine;
using Project.Core.Player;
using Project.Core.Stats;
using Project.Core.AI;

namespace Project.UI
{
    /// <summary>
    /// 開発用のインゲーム・チートコンソール。
    /// F1キーやバッククォートキーでトグルでき、ワンクリックでバランス調整や検証を行えます。
    /// </summary>
    public class DebugConsole : MonoBehaviour
    {
        [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
        
        private bool _showConsole = false;
        private Rect _windowRect = new Rect(20, 20, 250, 300);

        private float _timeScale = 1.0f;
        private bool _isGodMode = false;
        private bool _isInfiniteStamina = false;

        private CharacterStats _playerStats;

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerStats = player.GetComponent<CharacterStats>();
            }
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(_toggleKey))
            {
                _showConsole = !_showConsole;
            }

            // 無敵化ループ
            if (_isGodMode && _playerStats != null)
            {
                if (!_playerStats.IsDead) _playerStats.Heal(9999f);
            }

            // スタミナ無限ループ
            if (_isInfiniteStamina && _playerStats != null)
            {
                _playerStats.RestoreStamina(9999f);
            }
        }

        private void OnGUI()
        {
            if (!_showConsole) return;

            // 古いGUIシステムですが、開発用エディタツールとしては最も手軽です
            _windowRect = GUI.Window(0, _windowRect, DrawConsoleWindow, "チート / 開発デバッグ");
        }

        private void DrawConsoleWindow(int windowID)
        {
            GUILayout.Space(10);
            
            // 1. Time Scale Slider
            GUILayout.Label($"ゲーム速度 (TimeScale): {_timeScale:F1}");
            _timeScale = GUILayout.HorizontalSlider(_timeScale, 0.1f, 3.0f);
            if (GUILayout.Button("適用 & リセット"))
            {
                if (Mathf.Approximately(_timeScale, 1.0f)) _timeScale = 1.0f;
                Time.timeScale = _timeScale;
            }

            GUILayout.Space(20);

            // 2. God Mode Toggle
            bool prevGodMode = _isGodMode;
            _isGodMode = GUILayout.Toggle(_isGodMode, " 無敵モード (God Mode)");
            
            // 3. Infinite Stamina Toggle
            _isInfiniteStamina = GUILayout.Toggle(_isInfiniteStamina, " スタミナ無限 (Infinite SP)");

            GUILayout.Space(20);

            // 4. Kill All Enemies Button
            if (GUILayout.Button("画面内の敵を全滅させる (Kill All)"))
            {
                var enemies = FindObjectsOfType<EnemyAIController>();
                foreach (var e in enemies)
                {
                    if (e.TryGetComponent(out CharacterStats enemyStats))
                    {
                        enemyStats.TakeDamage(99999f);
                    }
                }
            }

            // 5. Restore Health/Stamina Button
            if (GUILayout.Button("プレイヤー全回復 (Full Heal)"))
            {
                if (_playerStats != null)
                {
                    _playerStats.Heal(9999f);
                    _playerStats.RestoreStamina(9999f);
                }
            }

            // Windowをドラッグ可能にする
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}
