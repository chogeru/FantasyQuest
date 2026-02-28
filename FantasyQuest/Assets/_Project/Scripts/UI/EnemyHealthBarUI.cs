using UnityEngine;
using UnityEngine.UI;
using Project.Core.Stats;

namespace Project.UI
{
    /// <summary>
    /// 敵の頭上に追従してHPゲージを表示するUI機能。
    /// （※CanvasのRender Modeは「World Space」を想定。EnemyのPrefab内の子要素として配置）
    /// </summary>
    public class EnemyHealthBarUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("このHPバーが監視する対象のCharacterStats")]
        [SerializeField] private CharacterStats _targetStats;
        
        [Tooltip("HPの残量を表現するSlider（またはImageのFillAmount）")]
        [SerializeField] private Slider _healthSlider;
        
        [Tooltip("Canvasごと消すための大元パネル")]
        [SerializeField] private GameObject _uiPanel;

        [Header("Settings")]
        [Tooltip("ダメージを受けた後、何秒間HPバーを表示し続けるか")]
        [SerializeField] private float _displayDuration = 3f;
        
        [Tooltip("常にカメラの方向を向く（ビルボード）機能")]
        [SerializeField] private bool _faceCamera = true;
        private Camera _mainCamera;

        private float _displayTimer;

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (_targetStats == null)
            {
                // アタッチ漏れ対策で親やすぐ上の階層から探す
                _targetStats = GetComponentInParent<CharacterStats>();
            }

            if (_uiPanel != null) _uiPanel.SetActive(false); // 初期表示は消しておく
        }

        private void OnEnable()
        {
            if (_targetStats != null)
            {
                _targetStats.OnHealthChanged += UpdateHealthBar;
                _targetStats.OnDeath += HideBarForever;
            }
        }

        private void OnDisable()
        {
            if (_targetStats != null)
            {
                _targetStats.OnHealthChanged -= UpdateHealthBar;
                _targetStats.OnDeath -= HideBarForever;
            }
        }

        private void Update()
        {
            // カメラを常に向く（ビルボード処理）
            if (_faceCamera && _uiPanel != null && _uiPanel.activeSelf && _mainCamera != null)
            {
                transform.rotation = _mainCamera.transform.rotation;
            }

            // タイマー経過で非表示にする
            if (_displayTimer > 0f)
            {
                _displayTimer -= Time.deltaTime;
                if (_displayTimer <= 0f)
                {
                    if (_uiPanel != null) _uiPanel.SetActive(false);
                }
            }
        }

        /// <summary>
        /// HPが変動した際に呼ばれ、バーの長さを更新して一定時間表示する
        /// </summary>
        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (_uiPanel != null) _uiPanel.SetActive(true);

            if (_healthSlider != null)
            {
                _healthSlider.maxValue = maxHealth;
                _healthSlider.value = currentHealth;
            }

            // 表示タイマーをリセット
            _displayTimer = _displayDuration;
        }

        private void HideBarForever()
        {
            // 敵が死んだら消す
            if (_uiPanel != null) _uiPanel.SetActive(false);
            _displayTimer = 0f;
            enabled = false;
        }
    }
}
