using UnityEngine;
using Project.Systems.Input;

namespace Project.Core.CameraSystem
{
    /// <summary>
    /// カスタムカメラを手動で制御・追従させるマネージャー（壁めり込み対応版）
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Transform _target;   // 追従対象（プレイヤー）
        [SerializeField] private Transform _camTransform;

        [Header("Settings")]
        [SerializeField] private float _defaultDistance = 4.5f;
        [SerializeField] private float _height = 1.5f;
        [SerializeField] private float _sensitivityX = 0.5f;
        [SerializeField] private float _sensitivityY = 0.5f;

        [Header("Collision (壁のめり込み防止)")]
        [SerializeField] private LayerMask _collisionLayer;
        [SerializeField] private float _cameraRadius = 0.2f;   // カメラ判定の大きさ
        [SerializeField] private float _minDistance = 0.5f;    // ギリギリまで寄った時の最小距離

        private Vector2 _lookInput;
        private float _currentPitch;
        private float _currentYaw;
        private float _currentDistance;
        private Transform _lockOnTarget;

        public void SetLockOnTarget(Transform target)
        {
            _lockOnTarget = target;
        }

        private void Awake()
        {
            if (_camTransform == null)
            {
                _camTransform = UnityEngine.Camera.main.transform;
            }
            
            _currentDistance = _defaultDistance;

            // マウスカーソルのロック（テスト用）
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnLookEvent += HandleLookInput;
            }
        }

        private void OnDisable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnLookEvent -= HandleLookInput;
            }
        }

        private void HandleLookInput(Vector2 input) => _lookInput = input;

        private void LateUpdate()
        {
            if (_target == null) return;

            if (_lockOnTarget != null)
            {
                // ロックオン中の自動カメラ操作
                // ターゲットの少し上を注視する
                Vector3 targetAimPoint = _lockOnTarget.position + Vector3.up * 1.0f;
                // プレイヤー側の基準点
                Vector3 myPivot = _target.position + Vector3.up * _height;
                
                Vector3 dirToTarget = (targetAimPoint - myPivot).normalized;
                if (dirToTarget != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
                    
                    // Yaw (Y軸回転) のスムーズな追従
                    _currentYaw = Mathf.LerpAngle(_currentYaw, lookRot.eulerAngles.y, Time.deltaTime * 10f);
                    
                    // Pitch (X軸回転) のスムーズな追従
                    float targetPitch = lookRot.eulerAngles.x;
                    if (targetPitch > 180f) targetPitch -= 360f;
                    _currentPitch = Mathf.LerpAngle(_currentPitch, targetPitch, Time.deltaTime * 10f);
                }
            }
            else
            {
                // 通常時のマウスによる回転計算
                _currentYaw += _lookInput.x * _sensitivityX;
                _currentPitch -= _lookInput.y * _sensitivityY;
            }
            
            // 見上げ・見下ろしの角度制限
            _currentPitch = Mathf.Clamp(_currentPitch, -60f, 60f);

            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            
            // プレイヤーの頭上あたりをカメラの注視点の基準にする
            Vector3 targetPivot = _target.position + Vector3.up * _height;

            // 壁がない場合の本来のカメラ位置
            Vector3 desiredCameraPos = targetPivot + rotation * new Vector3(0, 0, -_defaultDistance);

            // 壁と衝突しているかのチェック (SphereCast)
            Vector3 dirToCam = (desiredCameraPos - targetPivot).normalized;
            
            if (Physics.SphereCast(targetPivot, _cameraRadius, dirToCam, out RaycastHit hit, _defaultDistance, _collisionLayer))
            {
                // カメラが壁の手前の位置になるように距離を調整する
                float hitDistance = hit.distance;
                _currentDistance = Mathf.Clamp(hitDistance, _minDistance, _defaultDistance);
            }
            else
            {
                // 壁がなければ徐々に元の標準距離へ戻す
                _currentDistance = Mathf.Lerp(_currentDistance, _defaultDistance, Time.deltaTime * 10f);
            }

            // 調整された現在の距離を元にカメラ位置を決定
            _camTransform.position = targetPivot + rotation * new Vector3(0, 0, -_currentDistance);
            _camTransform.rotation = rotation;
        }
    }
}
