using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Project.Core.Stats;

namespace Project.Core.AI
{
    public enum EnemyMovementType
    {
        Land,
        Water,
        Air
    }

    /// <summary>
    /// NavMeshAgentとEnemyStateMachineを利用した高度な敵AI。
    /// （ブラッシュアップ：水・陸・空の全ての敵をサポートする移動タイプを導入）
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(EnemyStateMachine))]
    [RequireComponent(typeof(CharacterController))]
    public class EnemyAIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        private NavMeshAgent _agent;
        private CharacterStats _stats;
        private EnemyStateMachine _stateMachine;
        private CharacterController _characterController;
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

        [Header("Movement Type Settings (水・陸・空対応)")]
        [SerializeField] private EnemyMovementType _movementType = EnemyMovementType.Land;
        [Tooltip("Land以外の移動速度")]
        [SerializeField] private float _baseMoveSpeed = 3.5f; 
        [Tooltip("Air時の浮遊高さの基準")]
        [SerializeField] private float _flyHeight = 2.0f;

        [Header("Gravity & Water Settings")]
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private LayerMask _waterLayer;
        [SerializeField] private float _swimSpeed = 2f;
        [SerializeField] private float _waterGravity = -2f;
        private float _verticalVelocity;
        private bool _isInWater;

        private Vector3 _patrolTarget;
        private float _aiTickTimer;
        private const float AI_TICK_RATE = 0.2f;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _stats = GetComponent<CharacterStats>();
            _stateMachine = GetComponent<EnemyStateMachine>();
            _characterController = GetComponent<CharacterController>();

            // 死亡と被ダメージイベントを購読
            _stats.OnDeath += HandleDeath;
            _stats.OnDamageTaken += HandleDamageTaken;
        }

        private void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _targetPlayer = playerObj.transform;

            _agent.updatePosition = false;
            _agent.updateRotation = false;

            if (_movementType != EnemyMovementType.Land)
            {
                // 空・水中の場合はNavMeshを使わない
                _agent.enabled = false; 
            }

            _patrolTarget = transform.position;
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

            // 水の判定
            _isInWater = Physics.CheckSphere(transform.position + Vector3.up * 1f, 1f, _waterLayer, QueryTriggerInteraction.Collide);

            // 重力処理
            if (_movementType == EnemyMovementType.Air)
            {
                _verticalVelocity = 0f; // 飛行中は重力無視（ホバリング等必要ならここで微調整）
            }
            else if (_isInWater || _movementType == EnemyMovementType.Water)
            {
                // 水中あるいは水生生物
                _verticalVelocity -= 2f * Time.deltaTime; // 軽い重力
                if (_verticalVelocity < _waterGravity) _verticalVelocity = _waterGravity;
            }
            else
            {
                if (_characterController.isGrounded && _verticalVelocity < 0)
                    _verticalVelocity = -2f;
                else
                    _verticalVelocity += _gravity * Time.deltaTime;
            }

            // ひるみ中や攻撃中（移動不可）の場合はAIの思考プロセスを一時停止する
            if (!_stateMachine.CanMove) 
            {
                if (_movementType == EnemyMovementType.Land && _agent.enabled && _agent.isOnNavMesh) 
                {
                    _agent.isStopped = true;
                }

                // Air以外は重力だけ適用。Airはその場に留まる
                float verticalMovement = (_movementType == EnemyMovementType.Air) ? 0f : _verticalVelocity;
                _characterController.Move(new Vector3(0, verticalMovement, 0) * Time.deltaTime);
                
                if (_movementType == EnemyMovementType.Land) SyncAgentPosition();
                return;
            }

            // 移動可能な状態ならエージェントを動かす
            if (_movementType == EnemyMovementType.Land && _agent.enabled && _agent.isOnNavMesh) 
            {
                _agent.isStopped = false;
            }

            Vector3 moveDirection = Vector3.zero;

            // 移動処理
            switch (_movementType)
            {
                case EnemyMovementType.Land:
                    HandleLandMovement(ref moveDirection);
                    break;
                case EnemyMovementType.Water:
                    HandleWaterMovement(ref moveDirection);
                    break;
                case EnemyMovementType.Air:
                    HandleAirMovement(ref moveDirection);
                    break;
            }

            // CCで実際に動かす
            _characterController.Move(moveDirection * Time.deltaTime);

            // アニメーターへ反映
            if (_animator != null)
            {
                Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z);
                _animator.SetFloat("Speed", horizontalVelocity.magnitude);
                // 水中かどうかのフラグ
                _animator.SetBool("IsSwimming", _isInWater || _movementType == EnemyMovementType.Water);
                // 空中かどうかのフラグ
                _animator.SetBool("IsFlying", _movementType == EnemyMovementType.Air);
            }

            if (_movementType == EnemyMovementType.Land)
            {
                SyncAgentPosition();
            }

            // AIの思考ポーリング（軽い処理）
            _aiTickTimer -= Time.deltaTime;
            if (_aiTickTimer <= 0f)
            {
                _aiTickTimer = AI_TICK_RATE;
                ExecuteAIState();
            }
        }

        private void HandleLandMovement(ref Vector3 moveDirection)
        {
            if (_agent.enabled && _agent.hasPath)
            {
                Vector3 targetDirection = (_agent.steeringTarget - transform.position);
                targetDirection.y = 0; // xz平面の方向
                
                if (targetDirection.magnitude > 0.1f)
                {
                    moveDirection = targetDirection.normalized * _agent.speed;
                    
                    // キャラクターの回転
                    Quaternion lookRotation = Quaternion.LookRotation(targetDirection.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
                }
            }
            moveDirection.y += _verticalVelocity;
        }

        private void HandleWaterMovement(ref Vector3 moveDirection)
        {
            Vector3 targetDirection = GetSteeredDirection(_patrolTarget);
            
            if (_stateMachine.CurrentState == EnemyState.Chase && _targetPlayer != null)
            {
                targetDirection = GetSteeredDirection(_targetPlayer.position);
            }
            
            if (targetDirection.magnitude > 0.1f)
            {
                moveDirection = targetDirection.normalized * _baseMoveSpeed;
                
                // 向いている方向を向く
                Vector3 lookDir = targetDirection;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(lookDir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
                }
            }
            moveDirection.y += _verticalVelocity;
        }

        private void HandleAirMovement(ref Vector3 moveDirection)
        {
            // 飛行エネミーのアプローチ。ターゲットの少し上を飛ぶイメージ
            Vector3 targetPos = _patrolTarget;
            
            if (_stateMachine.CurrentState == EnemyState.Chase && _targetPlayer != null)
            {
                targetPos = _targetPlayer.position + Vector3.up * _flyHeight;
            }

            Vector3 targetDirection = GetSteeredDirection(targetPos);
            
            if (targetDirection.magnitude > 0.1f)
            {
                moveDirection = targetDirection.normalized * _baseMoveSpeed;
                
                // 回転
                Vector3 lookDir = targetDirection;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(lookDir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
                }
            }
        }

        /// <summary>
        /// 進行方向に障害物がある場合、回避方向を合成して返す（水・空専用）
        /// </summary>
        private Vector3 GetSteeredDirection(Vector3 targetPos)
        {
            Vector3 desiredDir = (targetPos - transform.position).normalized;
            Vector3 origin = transform.position + Vector3.up * 1f;
            float avoidanceDistance = 3f;

            // 前方に障害物があるかチェック
            if (Physics.SphereCast(origin, 0.5f, transform.forward, out RaycastHit hit, avoidanceDistance, _obstacleLayer))
            {
                // 法線ベクトルを活用して障害物から離れる方向を算出
                Vector3 avoidanceDir = Vector3.Reflect(transform.forward, hit.normal).normalized;
                
                // 完全な反射方向だと不自然なので、目標方向と回避方向をブレンド
                // 距離が近いほど回避のウェイトを高める
                float dodgeWeight = 1f - (hit.distance / avoidanceDistance);
                Vector3 blendedDir = Vector3.Lerp(desiredDir, avoidanceDir, dodgeWeight).normalized;
                return blendedDir;
            }

            return desiredDir;
        }

        private void SyncAgentPosition()
        {
            if (_agent.enabled)
            {
                _agent.nextPosition = transform.position;
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
            if (_targetPlayer != null)
            {
                Vector3 knockbackDir = (transform.position - _targetPlayer.position).normalized;
                knockbackDir.y = 0;
                // CCを使って弾き飛ばす
                _characterController.Move(knockbackDir * _knockbackForce);
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

            if (_movementType == EnemyMovementType.Land)
            {
                if (!_agent.hasPath || _agent.remainingDistance < 0.5f)
                {
                    _agent.SetDestination(GetRandomNavSphere(transform.position, _patrolRadius));
                }
            }
            else
            {
                // 空中・水中パトロール用（シンプルなランダム座標移動）
                if (Vector3.Distance(transform.position, _patrolTarget) < 1.0f || _patrolTarget == transform.position)
                {
                    Vector3 randDir = Random.insideUnitSphere * _patrolRadius;
                    if (_movementType == EnemyMovementType.Water) randDir.y = 0; // 水中なら高さを変えない/水面を泳ぐかはお好み
                    _patrolTarget = transform.position + randDir;
                }
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

            if (_movementType == EnemyMovementType.Land && _agent.enabled)
            {
                _agent.SetDestination(_targetPlayer.position);
            }
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

#if UNITY_EDITOR
        // === Visual Debugging (Gizmos) ===
        private void OnDrawGizmosSelected()
        {
            // 索敵範囲の描画 (黄色)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // 攻撃範囲の描画 (赤色)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            // 視界(FOV)の描画 (オレンジ色)
            Vector3 forward = transform.forward;
            Vector3 leftRay = Quaternion.Euler(0, -_fieldOfViewAngle * 0.5f, 0) * forward;
            Vector3 rightRay = Quaternion.Euler(0, _fieldOfViewAngle * 0.5f, 0) * forward;

            Gizmos.color = new Color(1f, 0.5f, 0f); // オレンジ
            Vector3 rayOrigin = transform.position + Vector3.up * 1f;

            // 視界の扇形を表現するための境界線
            Gizmos.DrawRay(rayOrigin, leftRay * _detectionRange);
            Gizmos.DrawRay(rayOrigin, rightRay * _detectionRange);

            // ターゲットがいる場合は、ターゲットへの線を引く
            if (_targetPlayer != null && CurrentStateIs(EnemyState.Chase, EnemyState.Attacking))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(rayOrigin, _targetPlayer.position + Vector3.up * 1f);
            }
        }

        // 状態チェック用ヘルパー（Gizmo描画時など、_stateMachineがNullの時のエラー防止用）
        private bool CurrentStateIs(params EnemyState[] states)
        {
            if (_stateMachine == null) return false;
            foreach(var state in states)
            {
                if (_stateMachine.CurrentState == state) return true;
            }
            return false;
        }
#endif
    }
}
