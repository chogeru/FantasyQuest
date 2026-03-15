using Project.Systems.Input;
using UnityEngine;
using Project.Core.CameraSystem;

namespace Project.Core.Player
{
    /// <summary>
    /// CharacterControllerを利用したTPS用の高度なプレイヤー制御スクリプト
    /// ステートマシンによる状態管理（疎結合化）を導入済み。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Transform _cameraTransform;
        
        public InputReader GetInputReader() => _inputReader;
        
        private CharacterController _controller;
        private PlayerStateMachine _stateMachine; // 追加：ステートマシン参照
        private TargetLockOn _targetLockOn;

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _sprintSpeed = 6.5f;
        [SerializeField] private float _rotationSmoothTime = 0.1f;
        private float _targetRotation;
        private float _rotationVelocity;

        [Header("Gravity & Jump")]
        [SerializeField] private float _gravity = -20.0f;
        [SerializeField] private float _jumpHeight = 2.0f;
        [SerializeField] private float _fallMultiplier = 2.5f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;

        [Header("Water & Swimming Settings")]
        [SerializeField] private LayerMask _waterLayer;
        [SerializeField] private float _swimSpeed = 2.5f;
        [SerializeField] private float _waterGravity = -1.5f;
        [SerializeField] private float _swimUpForce = 4.0f;
        private bool _isInWater;

        [Header("Advanced Feel Settings")]
        [Tooltip("崖っぷちで落ちた直後でも一瞬だけジャンプできる猶予時間")]
        [SerializeField] private float _coyoteTime = 0.15f;
        [Tooltip("着地直前にジャンプ入力した際に、着地後に即座に飛ぶ猶予時間")]
        [SerializeField] private float _jumpBufferTime = 0.15f;
        [Tooltip("登れない急斜面に居る際の滑り落ちる速度")]
        [SerializeField] private float _slopeSlideSpeed = 8f;

        private float _verticalVelocity;
        private bool _isGrounded;
        private float _coyoteCounter;
        private float _jumpBufferCounter;
        private Vector3 _hitNormal;
        private bool _isSliding;

        private Vector2 _moveInput;
        private bool _isSprinting;
        private float _jumpPhaseTimer; // ジャンプ直後の強制非接地タイマー

        // --- Properties & Events for Animation & External use ---
        public float CurrentSpeed 
        {
            get
            {
                if (_controller != null) 
                    return new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude;
                return 0f;
            }
        }
        public bool IsGrounded => _isGrounded;
        public bool IsInWater => _isInWater;
        public float VerticalVelocity => _verticalVelocity;
        public event System.Action OnJumpExecuted;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stateMachine = GetComponent<PlayerStateMachine>();
            _targetLockOn = GetComponent<TargetLockOn>();
            
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnMoveEvent += HandleMoveInput;
                _inputReader.OnJumpEvent += HandleJumpInput;
                _inputReader.OnSprintEvent += HandleSprintInput;
            }
        }

        private void OnDisable()
        {
            if (_inputReader != null)
            {
                _inputReader.OnMoveEvent -= HandleMoveInput;
                _inputReader.OnJumpEvent -= HandleJumpInput;
                _inputReader.OnSprintEvent -= HandleSprintInput;
            }
        }

        private void Update()
        {
            CheckGrounded();
            HandleGravityAndJump();
            HandleMovement();
        }

        // === Input Handlers ===
        private void HandleMoveInput(Vector2 input) => _moveInput = input;
        
        private void HandleJumpInput()
        {
            // 移動可能（Locomotion）状態でのみジャンプ入力を受け付ける
            if (_stateMachine.CanMove)
            {
                _jumpBufferCounter = _jumpBufferTime;
            }
        }

        private void HandleSprintInput(bool isSprinting) => _isSprinting = isSprinting;

        // === Logic ===
        private void CheckGrounded()
        {
            if (_jumpPhaseTimer > 0f)
            {
                _jumpPhaseTimer -= Time.deltaTime;
                _isGrounded = false;
                _isInWater = false;
            }
            else
            {
                if (_groundCheck != null)
                {
                    _isInWater = Physics.CheckSphere(transform.position + Vector3.up * 1f, 1f, _waterLayer, QueryTriggerInteraction.Collide);
                    if (!_isInWater)
                    {
                        _isGrounded = Physics.CheckSphere(_groundCheck.position, 0.2f, _groundLayer, QueryTriggerInteraction.Ignore);
                    }
                    else
                    {
                        _isGrounded = false; // 水中では接地とみなさない
                    }
                }
                else
                {
                    _isGrounded = _controller.isGrounded;
                    _isInWater = false;
                }
            }

            if (_isGrounded)
            {
                _coyoteCounter = _coyoteTime;
            }
            else
            {
                _coyoteCounter -= Time.deltaTime;
            }

            _isSliding = false;
            if (_isGrounded && Vector3.Angle(Vector3.up, _hitNormal) >= _controller.slopeLimit)
            {
                _isSliding = true;
            }
        }

        private void HandleGravityAndJump()
        {
            _jumpBufferCounter -= Time.deltaTime;

            if (_isInWater)
            {
                // 水中の浮力と重力
                if (_verticalVelocity < _waterGravity) 
                    _verticalVelocity = Mathf.Lerp(_verticalVelocity, _waterGravity, Time.deltaTime * 5f);
                else
                    _verticalVelocity -= 2f * Time.deltaTime; // 軽い水中重力

                // 水中ジャンプ（上昇）
                if (_jumpBufferCounter > 0f && _stateMachine.CanMove) 
                {
                    _verticalVelocity = _swimUpForce;
                    _jumpBufferCounter = 0f;
                    OnJumpExecuted?.Invoke();
                }
                return;
            }

            if (_isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = -2f; // Ground stick force
            }

            // ジャンプ処理
            if (_coyoteCounter > 0f && !_isSliding && _stateMachine.CanMove) 
            {
                if (_jumpBufferCounter > 0f) 
                {
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                    _jumpBufferCounter = 0f;
                    _coyoteCounter = 0f;
                    _jumpPhaseTimer = 0.1f; // ジャンプ直後の0.1秒間は接地判定を無効化
                    OnJumpExecuted?.Invoke();
                }
            }

            // 重力の適用 (常に毎フレーム適用する)
            float currentGravity = _verticalVelocity < 0 ? _gravity * _fallMultiplier : _gravity;
            _verticalVelocity += currentGravity * Time.deltaTime;
        }

        private void HandleMovement()
        {
            Vector3 movement = Vector3.zero;

            // ステートマシンが「移動不可能(攻撃中など)」を示している場合は重力以外の移動入力を無視する
            if (!_stateMachine.CanMove)
            {
                _moveInput = Vector2.zero;
            }

            if (_isSliding)
            {
                // 急斜面では滑り落ちる（垂直方向の力も含む）
                movement = new Vector3(_hitNormal.x, -_hitNormal.y, _hitNormal.z) * _slopeSlideSpeed;
            }
            else
            {
                if (_targetLockOn != null && _targetLockOn.IsLockedOn)
                {
                    // ロックオン状態：ターゲットの方向を向き続ける
                    Transform target = _targetLockOn.GetTarget();
                    if (target != null)
                    {
                        Vector3 dirToTarget = (target.position - transform.position).normalized;
                        dirToTarget.y = 0;
                        if (dirToTarget != Vector3.zero)
                        {
                            Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
                            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 15f);
                        }
                    }

                    if (_moveInput != Vector2.zero)
                    {
                        // カメラを基準として入力方向へ移動（カニ歩き/ストレイフ）
                        Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
                        _targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                        
                        Vector3 moveDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
                        float currentSpeed = _isInWater ? _swimSpeed : (_isSprinting ? _sprintSpeed : _walkSpeed);
                        movement = moveDirection * currentSpeed;
                    }
                }
                else if (_moveInput != Vector2.zero)
                {
                    // 通常状態：入力方向にキャラクターを回転させて移動
                    Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
                    _targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;

                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, _rotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0f, rotation, 0f);

                    Vector3 moveDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
                    float currentSpeed = _isInWater ? _swimSpeed : (_isSprinting ? _sprintSpeed : _walkSpeed);
                    movement = moveDirection * currentSpeed;
                }

                // 重力を適用（平地・空中・普通の坂道）
                movement.y = _verticalVelocity;
            }

            _controller.Move(movement * Time.deltaTime);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _hitNormal = hit.normal;
        }
    }
}
