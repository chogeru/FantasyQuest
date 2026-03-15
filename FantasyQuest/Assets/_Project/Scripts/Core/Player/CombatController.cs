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
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnEnable()
        {
            // InputReaderのアタッチが漏れていた場合、自動的に修復を試みる
            if (_inputReader == null)
            {
                var pc = GetComponent<PlayerController>();
                if (pc != null) _inputReader = pc.GetInputReader();
            }

            if (_inputReader != null) 
            {
                _inputReader.OnAttackEvent += HandleAttackInput;
            }
            else
            {
                Debug.LogError("[CombatController] InputReaderが見つかりません！入力を受け付けられません。");
            }
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

            // 【セーフティネット設定】: AnimationEvent (OnAttackComplete) がクリップに設定されていない場合でも
            // アニメーションがフリーズしてしまうのを防ぐため、一定時間（0.8秒）経過で強制的に状態をリセットする
            if (_stateMachine.CurrentState == PlayerState.Attacking)
            {
                if (Time.time - _lastAttackTime > 0.8f)
                {
                    _stateMachine.ChangeState(PlayerState.Locomotion);
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
                // 【修正】もしすでにAttackトリガーが消費されていない場合はリセットしてから再セットする
                _animator.ResetTrigger("Attack");
                _animator.SetTrigger("Attack");
                _animator.SetInteger("ComboStep", _comboStep);
                Debug.Log($"<color=green>[CombatController]</color> 攻撃を実行しました。コンボ: {_comboStep}");
            }
            else
            {
                Debug.LogWarning("[CombatController] Animatorが見つかりません。攻撃アニメーションが再生されません。");
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
