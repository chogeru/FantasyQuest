using UnityEngine;
using Project.Systems.Input;
using Project.Core.Player;

namespace Project.Core.CameraSystem
{
    /// <summary>
    /// カスタムカメラを手動で制御・追従させるマネージャー（高品質な三人称視点へ改善版）
    /// - カメラの追尾遅延(スムージング)によるリッチな手触り
    /// - 肩越し(Over the Shoulder)オフセット
    /// - 壁めり込み回避の滑らかレイキャスト
    /// - ダッシュ時にFOVを少し広げるダイナミック演出
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Transform _target;   // 追従対象（プレイヤー）
        [SerializeField] private Transform _camTransform;

        [Header("Distance & Offset")]
        [SerializeField] private float _defaultDistance = 4.0f;
        [SerializeField] private float _height = 1.3f;
        [Tooltip("右肩越しにするための左右オフセット(ゼロにすると中心)")]
        [SerializeField] private float _shoulderOffset = 0.5f; 

        [Header("Camera Feel (感度 & スムージー)")]
        [SerializeField] private float _sensitivityX = 0.8f;
        [SerializeField] private float _sensitivityY = 0.6f;
        [Tooltip("カメラがプレイヤーの移動に真っ直ぐ追従する遅延時間(小さくするとキビキビ)")]
        [SerializeField] private float _followSmoothTime = 0.08f;
        [Tooltip("視点回転の滑らかさ(0で即座)")]
        [SerializeField] private float _rotationSmoothTime = 0.02f;

        [Header("Collision (めり込み回避)")]
        [SerializeField] private LayerMask _collisionLayer;
        [SerializeField] private float _cameraRadius = 0.25f;
        [SerializeField] private float _minDistance = 0.5f;

        [Header("Dynamic FOV (動的ズーム)")]
        [SerializeField] private bool _enableDynamicFov = true;
        [SerializeField] private float _normalFov = 60f;
        [SerializeField] private float _sprintFov = 75f;
        [SerializeField] private float _fovLerpSpeed = 5f;

        private Vector2 _lookInput;
        private float _currentPitch;
        private float _currentYaw;
        private float _currentDistance;
        
        private float _smoothYaw;
        private float _smoothPitch;
        private float _yawVelocity;
        private float _pitchVelocity;

        private Vector3 _currentPivotPosition;
        private Vector3 _pivotVelocity;

        private Transform _lockOnTarget;
        private Camera _actualCamera;
        private bool _isSprinting;

        public void SetLockOnTarget(Transform target) => _lockOnTarget = target;

        private void Awake()
        {
            if (_camTransform == null)
            {
                _camTransform = UnityEngine.Camera.main?.transform;
            }
            if (_camTransform != null)
            {
                _actualCamera = _camTransform.GetComponent<Camera>();
                if (_actualCamera != null) _normalFov = _actualCamera.fieldOfView;
            }

            if (_target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _target = player.transform;
            }

            // InputReaderの自動取得(アタッチ漏れ対策)
            if (_inputReader == null && _target != null)
            {
                var pc = _target.GetComponent<PlayerController>();
                if (pc != null) _inputReader = pc.GetInputReader();
            }
            
            _currentDistance = _defaultDistance;
            if (_target != null) _currentPivotPosition = _target.position + Vector3.up * _height;

            // マウスカーソルのロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnLookEvent += HandleLookInput;
                _inputReader.OnSprintEvent += HandleSprintInput;
            }
        }

        private void OnDisable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnLookEvent -= HandleLookInput;
                _inputReader.OnSprintEvent -= HandleSprintInput;
            }
        }

        private void HandleLookInput(Vector2 input) => _lookInput = input;
        private void HandleSprintInput(bool isSprinting) => _isSprinting = isSprinting;

        private void LateUpdate()
        {
            if (_target == null || _camTransform == null) return;

            // --- 1. 回転の更新 ---
            if (_lockOnTarget != null)
            {
                Vector3 targetAimPoint = _lockOnTarget.position + Vector3.up * 1.0f;
                Vector3 myPivot = _target.position + Vector3.up * _height;
                Vector3 dirToTarget = (targetAimPoint - myPivot).normalized;
                
                if (dirToTarget != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
                    _currentYaw = lookRot.eulerAngles.y;
                    
                    float targetPitch = lookRot.eulerAngles.x;
                    if (targetPitch > 180f) targetPitch -= 360f;
                    _currentPitch = targetPitch;
                }
            }
            else
            {
                // マウス・右スティックによる回転計算と制限
                _currentYaw += _lookInput.x * _sensitivityX;
                _currentPitch -= _lookInput.y * _sensitivityY;
                _currentPitch = Mathf.Clamp(_currentPitch, -75f, 75f);
            }

            // スムーズな回転(手ブレ感の排除)
            _smoothYaw = Mathf.SmoothDampAngle(_smoothYaw, _currentYaw, ref _yawVelocity, _rotationSmoothTime);
            _smoothPitch = Mathf.SmoothDampAngle(_smoothPitch, _currentPitch, ref _pitchVelocity, _rotationSmoothTime);
            Quaternion rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0f);

            // --- 2. ピボットの追従 (スムーズ移動) ---
            Vector3 targetPivot = _target.position + Vector3.up * _height;
            // 右肩越しのオフセットを回転方向に合わせて追加
            targetPivot += Quaternion.Euler(0, _smoothYaw, 0) * Vector3.right * _shoulderOffset;

            // 遅延をかけて追跡することで、プレイヤーの動きが先行する躍動感を出す
            _currentPivotPosition = Vector3.SmoothDamp(_currentPivotPosition, targetPivot, ref _pivotVelocity, _followSmoothTime);

            // --- 3. カメラ位置の計算と障害物回避 ---
            Vector3 desiredCameraPos = _currentPivotPosition + rotation * new Vector3(0, 0, -_defaultDistance);
            Vector3 dirToCam = (desiredCameraPos - _currentPivotPosition).normalized;
            
            if (Physics.SphereCast(_currentPivotPosition, _cameraRadius, dirToCam, out RaycastHit hit, _defaultDistance, _collisionLayer))
            {
                float hitDistance = Mathf.Clamp(hit.distance, _minDistance, _defaultDistance);
                // めり込み時は即座に距離を近づける
                _currentDistance = Mathf.Lerp(_currentDistance, hitDistance, Time.deltaTime * 30f);
            }
            else
            {
                // 障害物がなくなったらゆっくり元に戻る
                _currentDistance = Mathf.Lerp(_currentDistance, _defaultDistance, Time.deltaTime * 5f);
            }

            _camTransform.position = _currentPivotPosition + rotation * new Vector3(0, 0, -_currentDistance);
            _camTransform.rotation = rotation;

            // --- 4. 動的FOV (スピード感の演出) ---
            if (_enableDynamicFov && _actualCamera != null)
            {
                float targetFov = _isSprinting ? _sprintFov : _normalFov;
                _actualCamera.fieldOfView = Mathf.Lerp(_actualCamera.fieldOfView, targetFov, Time.deltaTime * _fovLerpSpeed);
            }
        }
    }
}
