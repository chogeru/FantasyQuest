using UnityEngine;
using Project.Systems.Input;

namespace Project.Core.CameraSystem
{
    /// <summary>
    /// 周辺の敵を検索し、カメラの注視対象をロックオンする機能。
    /// 毎フレームのSphereCastを廃止し、定周期のポーリングによってCPU負荷を大きく削減（最適化済）。
    /// </summary>
    public class TargetLockOn : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private CameraManager _cameraManager;

        [Header("Lock-On Settings")]
        [Tooltip("索敵する半径")]
        [SerializeField] private float _detectionRadius = 15f;
        [Tooltip("ロックオン対象のレイヤー")]
        [SerializeField] private LayerMask _enemyLayer;

        private Transform _currentTarget;
        private bool _isLockedOn;

        // パフォーマンス改善: Updateではなく一定間隔で敵のロストをチェックするためのタイマー
        private float _checkTimer;
        private const float CHECK_INTERVAL = 0.2f;

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab)) 
            {
                ToggleLockOn();
            }

            if (_isLockedOn)
            {
                _checkTimer -= Time.deltaTime;
                if (_checkTimer <= 0f)
                {
                    _checkTimer = CHECK_INTERVAL;
                    ValidateLockOnTarget();
                }
            }
        }

        public void ToggleLockOn()
        {
            if (_isLockedOn) 
            {
                ClearLockOn();
            } 
            else 
            {
                FindNearestTarget(); // ボタンを押した瞬間だけSphereCastを実行（負荷軽減）
            }
        }

        private void FindNearestTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _enemyLayer);
            
            float closestDistance = float.MaxValue;
            Transform closestTarget = null;

            foreach (var hit in hits)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestTarget = hit.transform;
                }
            }

            if (closestTarget != null)
            {
                _currentTarget = closestTarget;
                _isLockedOn = true;
                _checkTimer = CHECK_INTERVAL;
                Debug.Log($"[TargetLockOn] {_currentTarget.name} をロックオン！");
                
                if (_cameraManager != null)
                {
                    _cameraManager.SetLockOnTarget(_currentTarget);
                }
            }
        }

        private void ValidateLockOnTarget()
        {
            // 敵が破壊された、または索敵範囲の1.5倍以上離れたらロックオン解除
            if (_currentTarget == null || Vector3.Distance(transform.position, _currentTarget.position) > _detectionRadius * 1.5f)
            {
                ClearLockOn();
            }
        }

        private void ClearLockOn()
        {
            _isLockedOn = false;
            _currentTarget = null;
            Debug.Log("[TargetLockOn] ロックオン解除");

            if (_cameraManager != null)
            {
                _cameraManager.SetLockOnTarget(null);
            }
        }

        public Transform GetTarget() => _currentTarget;
        public bool IsLockedOn => _isLockedOn;
    }
}
