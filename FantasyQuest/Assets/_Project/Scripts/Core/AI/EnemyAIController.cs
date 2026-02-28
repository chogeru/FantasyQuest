using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Project.Core.Stats;

namespace Project.Core.AI
{
    /// <summary>
    /// NavMeshAgentとEnemyStateMachineを利用した高度な敵AI。
    /// （ブラッシュアップ第2弾：ひるみ、ノックバック、攻撃の予兆・硬直対応）
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(EnemyStateMachine))]
    public class EnemyAIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        private NavMeshAgent _agent;
        private CharacterStats _stats;
        private EnemyStateMachine _stateMachine;
        private Transform _targetPlayer;

        [Header("AI Settings")]
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _attackRange = 2.5f;
        [SerializeField] private float _patrolRadius = 10f;
        [SerializeField] private float _fieldOfViewAngle = 120f;
        [SerializeField] private LayerMask _obstacleLayer;

        [Header("Stagger & Knockback (ひるみ)")]
        [Tooltip("このダメージ以上を一度に受けた場合のみ怯む")]
        [SerializeField] private float _staggerThreshold = 5f;
        [Tooltip("ひるみで操作不能になる時間")]
        [SerializeField] private float _staggerDuration = 0.5f;
        [Tooltip("ひるみ時のノックバック力")]
        [SerializeField] private float _knockbackForce = 2f;

        [Header("Attack Phases (攻撃フェーズ)")]
        [Tooltip("攻撃を行う前の「溜め(予兆)」の時間")]
        [SerializeField] private float _attackTelegraphTime = 0.5f;
        [Tooltip("攻撃を振り抜いた後の「硬直(隙)」の時間")]
        [SerializeField] private float _attackRecoveryTime = 1.0f;

        private Vector3 _patrolTarget;
        private float _aiTickTimer;
        private const float AI_TICK_RATE = 0.2f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _stats = GetComponent<CharacterStats>();
            _stateMachine = GetComponent<EnemyStateMachine>();

            // 死亡と被ダメージイベントを購読
            _stats.OnDeath += HandleDeath;
            _stats.OnDamageTaken += HandleDamageTaken;
        }

        private void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _targetPlayer = playerObj.transform;

            _stateMachine.ChangeState(EnemyState.Patrol);
        }

        private void OnDestroy()
        {
            if (_stats != null)
            {
                _stats.OnDeath -= HandleDeath;
                _stats.OnDamageTaken -= HandleDamageTaken;
            }
        }

        private void Update()
        {
            if (_stateMachine.CurrentState == EnemyState.Dead) return;

            // 移動速度をアニメーターへ反映
            if (_animator != null && _agent.enabled)
            {
                _animator.SetFloat("Speed", _agent.velocity.magnitude);
            }

            // ひるみ中や攻撃中（移動不可）の場合はAIの思考プロセスを一時停止する
            if (!_stateMachine.CanMove) 
            {
                // エージェントを止める
                if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;
                return;
            }

            // 移動可能な状態ならエージェントを動かす
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;

            // AIの思考ポーリング（軽い処理）
            _aiTickTimer -= Time.deltaTime;
            if (_aiTickTimer <= 0f)
            {
                _aiTickTimer = AI_TICK_RATE;
                ExecuteAIState();
            }
        }

        private void ExecuteAIState()
        {
            if (_targetPlayer == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _targetPlayer.position);

            if (_stateMachine.CurrentState == EnemyState.Patrol)
            {
                PatrolBehavior(distanceToPlayer);
            }
            else if (_stateMachine.CurrentState == EnemyState.Chase)
            {
                ChaseBehavior(distanceToPlayer);
            }
        }

        // --- センサー ---
        private bool CanSeePlayer(float distanceToPlayer)
        {
            if (distanceToPlayer > _detectionRange) return false;

            Vector3 dirToPlayer = (_targetPlayer.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            
            if (angleToPlayer > _fieldOfViewAngle * 0.5f) return false;

            Vector3 rayOrigin = transform.position + Vector3.up * 1f;
            if (Physics.Raycast(rayOrigin, dirToPlayer, distanceToPlayer, _obstacleLayer)) return false;

            return true;
        }

        // --- 被ダメージ時の「ひるみ」と「ノックバック」 ---
        private void HandleDamageTaken(float damageAmount)
        {
            if (_stateMachine.CurrentState == EnemyState.Dead) return;

            // 一定以上のダメージでなければスーパーアーマー（怯まない）
            if (damageAmount < _staggerThreshold) return;

            // 既にひるみ中ならタイマーを上書きするなどの処理を追加可能ですが、今回はシンプルに新規コルーチンを回します
            StopAllCoroutines(); // 進行中の「攻撃アクション（溜め等）」も全てキャンセルされる！
            StartCoroutine(StaggerRoutine());
        }

        private IEnumerator StaggerRoutine()
        {
            _stateMachine.ChangeState(EnemyState.Stagger);
            
            if (_animator != null) _animator.SetTrigger("Hurt"); // ひるみアニメーション

            // ノックバック処理 (プレイヤーとは逆方向に弾き飛ばす)
            if (_targetPlayer != null && _agent.enabled && _agent.isOnNavMesh)
            {
                Vector3 knockbackDir = (transform.position - _targetPlayer.position).normalized;
                // ナビメッシュ上を考慮して無理やりMoveする
                _agent.Move(knockbackDir * _knockbackForce);
            }

            yield return new WaitForSeconds(_staggerDuration);

            // ひるみから復帰したら、怒ってプレイヤーを追跡する状態に強制移行
            if (_stateMachine.CurrentState != EnemyState.Dead)
            {
                _stateMachine.ChangeState(EnemyState.Chase);
            }
        }

        // --- ステートの移動・攻撃挙動 ---
        private void PatrolBehavior(float distanceToPlayer)
        {
            if (CanSeePlayer(distanceToPlayer))
            {
                Debug.Log($"<color=orange>[EnemyAI]</color> プレイヤー発見！追跡開始");
                _stateMachine.ChangeState(EnemyState.Chase);
                return;
            }

            if (!_agent.hasPath || _agent.remainingDistance < 0.5f)
            {
                _agent.SetDestination(GetRandomNavSphere(transform.position, _patrolRadius));
            }
        }

        private void ChaseBehavior(float distanceToPlayer)
        {
            if (distanceToPlayer <= _attackRange)
            {
                // 攻撃範囲に入ったら攻撃ルーチンを開始
                StartCoroutine(AttackRoutine());
                return;
            }
            
            if (distanceToPlayer > _detectionRange * 1.5f)
            {
                _stateMachine.ChangeState(EnemyState.Patrol);
                return;
            }

            _agent.SetDestination(_targetPlayer.position);
        }

        /// <summary>
        /// 攻撃の「予兆」→「発生」→「硬直」を管理する３フェーズ構造
        /// </summary>
        private IEnumerator AttackRoutine()
        {
            // --- フェーズ1: 予兆 (Telegraph) ---
            _stateMachine.ChangeState(EnemyState.Attacking);
            _agent.isStopped = true;
            
            // プレイヤーの方を振り向く（攻撃の瞬間までロックオン）
            transform.LookAt(new Vector3(_targetPlayer.position.x, transform.position.y, _targetPlayer.position.z));

            // 「ため」時間（ここでパーティクルを光らせる等の演出を入れると最高です）
            // ※この間にプレイヤーは「攻撃が来る！」と判断してAvoid（回避）できる
            yield return new WaitForSeconds(_attackTelegraphTime);

            // --- フェーズ2: 攻撃発生 (Execute) ---
            if (_animator != null) _animator.SetTrigger("Attack");

            // --- フェーズ3: 隙・硬直 (Recovery) ---
            // 攻撃を振り抜いた後に何もしない隙を晒す時間
            yield return new WaitForSeconds(_attackRecoveryTime);

            // 攻撃ルーチンがつつがなく完了したら、元のChaseへ戻る
            if (_stateMachine.CurrentState == EnemyState.Attacking)
            {
                _stateMachine.ChangeState(EnemyState.Chase);
            }
        }

        private void HandleDeath()
        {
            _stateMachine.ChangeState(EnemyState.Dead);
            if (_agent.enabled)
            {
                _agent.isStopped = true;
                _agent.enabled = false;
            }
            
            if (_animator != null) _animator.SetTrigger("Die");
            Destroy(gameObject, 5f);
        }

        private Vector3 GetRandomNavSphere(Vector3 origin, float dist)
        {
            Vector3 randomDirection = Random.insideUnitSphere * dist;
            NavMesh.SamplePosition(randomDirection + origin, out NavMeshHit navHit, dist, NavMesh.AllAreas);
            return navHit.position;
        }
    }
}
