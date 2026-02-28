using UnityEngine;
using UnityEngine.UI;
using Project.Core.Stats;

// ※ 本来はText等のUI制御に際して TMPro を使用しますが、
// ここではエラーを防ぐためUnity標準のUIコンポーネントのみで基盤を作成します。

namespace Project.UI
{
    /// <summary>
    /// HUD (Head-Up Display) を管理するクラス。
    /// プレイヤーの画面上の体力・スタミナゲージを司ります。
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("情報を表示する対象（通常はプレイヤー）のStatsコンポーネント")]
        [SerializeField] private CharacterStats _targetStats;

        [Header("UI Elements")]
        [Tooltip("HPゲージのImage(FillType=Filled推奨)")]
        [SerializeField] private Image _healthFillImage;
        [Tooltip("スタミナゲージのImage(FillType=Filled推奨)")]
        [SerializeField] private Image _staminaFillImage;
        
        [Tooltip("ダメージポップアップのプレハブ")]
        [SerializeField] private GameObject _damagePopupPrefab;
        
        // --- 演出用パラメータ ---
        [Header("Settings")]
        [SerializeField] private float _barSmoothSpeed = 5f;

        private float _targetHealthFill = 1f;
        private float _targetStaminaFill = 1f;

        private void OnEnable()
        {
            if (_targetStats != null)
            {
                // Statsのイベントを講読し、ダメージ・回復時にUIを更新するように紐付け
                _targetStats.OnHealthChanged += HandleHealthChanged;
                _targetStats.OnStaminaChanged += HandleStaminaChanged;
            }
        }

        private void OnDisable()
        {
            if (_targetStats != null)
            {
                _targetStats.OnHealthChanged -= HandleHealthChanged;
                _targetStats.OnStaminaChanged -= HandleStaminaChanged;
            }
        }

        // === Event Handlers ===
        private void HandleHealthChanged(float currentVal, float maxVal)
        {
            _targetHealthFill = currentVal / maxVal;
            // 致命傷で赤く点滅させるなどの拡張もここで行います
        }

        private void HandleStaminaChanged(float currentVal, float maxVal)
        {
            _targetStaminaFill = currentVal / maxVal;
        }

        // Statsでダメージを受けた(TakeDamage)際に呼ばれるようにするため、OnEnableで追加フックアップが必要ですが、
        // 今はStats側でPopUpを生成するのではなく、HUD経由で呼べる口を用意しています。
        // もしStats側から直接呼びたいなら、StatsにEventを追加しHandleDamageTakenを実装します。

        // === Visual Update ===
        private void Update()
        {
            // UI上のゲージの見た目を、徐々に（滑らかに）目標値まで動かす（ゲーム特有の気持ちいい演出）
            if (_healthFillImage != null)
            {
                _healthFillImage.fillAmount = Mathf.Lerp(_healthFillImage.fillAmount, _targetHealthFill, Time.deltaTime * _barSmoothSpeed);
            }

            if (_staminaFillImage != null)
            {
                _staminaFillImage.fillAmount = Mathf.Lerp(_staminaFillImage.fillAmount, _targetStaminaFill, Time.deltaTime * _barSmoothSpeed);
            }
        }

        /// <summary>
        /// （拡張用）敵などにダメージを与えた際の「ダメージポップアップ数字(UI)」を生成する関数
        /// </summary>
        public void SpawnDamagePopup(Vector3 worldPosition, float damage)
        {
            if (_damagePopupPrefab == null) return;

            // ワールド空間上にPopUpを生成
            GameObject popupObj = Instantiate(_damagePopupPrefab, worldPosition + Vector3.up * 1.5f, Quaternion.identity);
            if (popupObj.TryGetComponent(out DamagePopup popup))
            {
                popup.Setup(damage);
            }
        }
    }
}
