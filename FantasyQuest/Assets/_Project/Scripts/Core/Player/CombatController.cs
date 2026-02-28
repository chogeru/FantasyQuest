using System.Collections;
using UnityEngine;
using Project.Systems.Input;

namespace Project.Core.Player
{
    /// <summary>
    /// アクションゲーム用のコンボコントローラー。
    /// （ブラッシュアップ版：コンボを滑らかに繋ぐ『入力バッファリング』機能搭載）
    /// </summary>
    [RequireComponent(typeof(PlayerStateMachine))]
    public class CombatController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Animator _animator;
        
        private PlayerStateMachine _stateMachine;

        [Header("Combo Settings")]
        [Tooltip("攻撃をしないままこの秒数経過するとコンボがリセットされる")]
        [SerializeField] private float _comboResetTime = 1.2f;
        
        [Header("Input Buffer Settings")]
        [Tooltip("攻撃ボタンを早押ししても、この時間(秒)だけ記憶して次の攻撃最速フレームで発動する")]
        [SerializeField] private float _inputBufferTime = 0.4f;
        
        private int _comboStep;
        private float _lastAttackTime;
        private float _attackBufferTimer; // バッファ残り時間

        private void Awake()
        {
            _stateMachine = GetComponent<PlayerStateMachine>();
        }

        private void OnEnable()
        {
            if (_inputReader != null) _inputReader.OnAttackEvent += HandleAttackInput;
        }

        private void OnDisable()
        {
            if (_inputReader != null) _inputReader.OnAttackEvent -= HandleAttackInput;
        }

        private void Update()
        {
            // 時間経過によるコンボ回数のリセット
            if (_comboStep > 0 && Time.time - _lastAttackTime > _comboResetTime)
            {
                ResetCombo();
            }

            // 入力バッファの消化処理
            if (_attackBufferTimer > 0f)
            {
                _attackBufferTimer -= Time.deltaTime;
                
                // バッファ時間中に攻撃可能な状態（Locomotion）に戻るか、すでにAttacking中でAnimationEventが許可を出せば攻撃を実行する
                if (_stateMachine.CanAttack || _stateMachine.CurrentState == PlayerState.Attacking)
                {
                    TryExecuteAttack();
                }
            }
        }

        /// <summary>
        /// InputSystemからのイベントコールバック。即座に攻撃せず「バッファ(予約)」に入れる。
        /// </summary>
        private void HandleAttackInput()
        {
            // ボタンが押されたらバッファタイマーをセット
            _attackBufferTimer = _inputBufferTime;
            TryExecuteAttack();
        }

        /// <summary>
        /// 実際に攻撃ステートへ遷移できるか判定し、実行する
        /// </summary>
        private void TryExecuteAttack()
        {
            // バッファが切れている場合は何もしない
            if (_attackBufferTimer <= 0f) return;

            // 状態が攻撃可能ではない、かつ既に攻撃中(コンボ受付中)でもない場合はキャンセル
            if (!_stateMachine.CanAttack && _stateMachine.CurrentState != PlayerState.Attacking) return;
            
            // あまりにも短すぎる同じフレーム等での連打防止
            if (Time.time - _lastAttackTime < 0.2f) return; 

            // 攻撃実行決定。バッファを消費してゼロにする
            _attackBufferTimer = 0f; 

            _stateMachine.ChangeState(PlayerState.Attacking);

            _comboStep++;
            if (_comboStep > 3) _comboStep = 1; // 3段コンボでループ（仮）

            _lastAttackTime = Time.time;
            
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
                _animator.SetInteger("ComboStep", _comboStep);
            }
        }

        // ==========================================
        // Animation Events（AnimationClipから呼ばれる）
        // ==========================================
        public void OnAttackComplete()
        {
            _stateMachine.ChangeState(PlayerState.Locomotion);
        }

        private void ResetCombo()
        {
            _comboStep = 0;
            if (_animator != null) _animator.SetInteger("ComboStep", 0);
            
            if (_stateMachine.CurrentState == PlayerState.Attacking)
            {
                _stateMachine.ChangeState(PlayerState.Locomotion);
            }
        }
    }
}
