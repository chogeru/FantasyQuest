using System;
using UnityEngine;

namespace Project.Core.Stats
{
    /// <summary>
    /// プレイヤー、敵、NPCなどで共通して利用する能力値管理クラス。
    /// （ブラッシュアップ版：防御力によるダメージ軽減と、スタミナの自動回復機能付き）
    /// </summary>
    public class CharacterStats : MonoBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxStamina = 50f;
        [SerializeField] private float _baseAttackPower = 10f;
        
        [Tooltip("ダメージ減算値（例: 5 なら 10ダメージを食らっても 5ダメージになる）")]
        [SerializeField] private float _armor = 5f;

        [Header("Stamina Regen System")]
        [Tooltip("1秒間に回復するスタミナの量")]
        [SerializeField] private float _staminaRegenRate = 10f;
        [Tooltip("スタミナを最後に消費してから、自動回復が始まるまでのディレイ(秒)")]
        [SerializeField] private float _staminaRegenDelay = 2f;

        // Current Stats
        private float _currentHealth;
        private float _currentStamina;
        private float _lastStaminaConsumeTime;

        // Events
        public event Action<float, float> OnHealthChanged = delegate { };
        public event Action<float, float> OnStaminaChanged = delegate { };
        public event Action<float> OnDamageTaken = delegate { }; // ひるみ判定等に使うイベント
        public event Action OnDeath = delegate { };

        public bool IsDead => _currentHealth <= 0f;

        private void Awake()
        {
            _currentHealth = _maxHealth;
            _currentStamina = _maxStamina;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
        }

        private void Update()
        {
            if (IsDead) return;

            // スタミナの自動回復ロジック
            if (_currentStamina < _maxStamina)
            {
                if (Time.time - _lastStaminaConsumeTime > _staminaRegenDelay)
                {
                    _currentStamina += _staminaRegenRate * Time.deltaTime;
                    _currentStamina = Mathf.Clamp(_currentStamina, 0, _maxStamina);
                    OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
                }
            }
        }

        /// <summary>
        /// Hurtboxからのダメージを受け取る
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            // 防御力(Armor)による攻撃の減算（最低でも1ダメージは食らうようにする）
            float actualDamage = Mathf.Max(1f, amount - _armor);
            
            _currentHealth -= actualDamage;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);

            OnDamageTaken?.Invoke(actualDamage);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// アクション（攻撃、ダッシュ、回避）でスタミナを消費する
        /// </summary>
        public bool ConsumeStamina(float amount)
        {
            if (_currentStamina >= amount)
            {
                _currentStamina -= amount;
                _lastStaminaConsumeTime = Time.time; // 回復ディレイタイマーをリセット
                OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
                return true;
            }
            return false;
        }

        public void RestoreStamina(float amount)
        {
            if (IsDead) return;
            _currentStamina += amount;
            _currentStamina = Mathf.Clamp(_currentStamina, 0, _maxStamina);
            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            _currentHealth += amount;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Die()
        {
            Debug.Log($"[CharacterStats] {gameObject.name} は力尽きた！");
            OnDeath?.Invoke();
        }

        public float AttackPower => _baseAttackPower;
    }
}
