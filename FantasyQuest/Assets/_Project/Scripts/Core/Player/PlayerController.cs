using Project.Systems.Input;
using UnityEngine;

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
        
        private CharacterController _controller;
        private PlayerStateMachine _stateMachine; // 追加：ステートマシン参照

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _sprintSpeed = 6.5f;
        [SerializeField] private float _rotationSmoothTime = 0.1f;
        private float _targetRotation;
        private float _rotationVelocity;

        [Header("Gravity & Jump")]
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _jumpHeight = 1.3f;
        [SerializeField] private float _fallMultiplier = 2.0f;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;

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

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _stateMachine = GetComponent<PlayerStateMachine>();
            
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
            if (_groundCheck != null)
            {
                _isGrounded = Physics.CheckSphere(_groundCheck.position, 0.2f, _groundLayer, QueryTriggerInteraction.Ignore);
            }
            else
            {
                _isGrounded = _controller.isGrounded;
            }

            if (_isGrounded)
            {
                _coyoteCounter = _coyoteTime;
                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;
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

            // ジャンプ処理
            if (_coyoteCounter > 0f && !_isSliding && _stateMachine.CanMove) 
            {
                if (_jumpBufferCounter > 0f) 
                {
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                    _jumpBufferCounter = 0f;
                    _coyoteCounter = 0f;
                }
            }
            else
            {
                float currentGravity = _verticalVelocity < 0 ? _gravity * _fallMultiplier : _gravity;
                _verticalVelocity += currentGravity * Time.deltaTime;
            }
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
                movement = new Vector3(_hitNormal.x, -_hitNormal.y, _hitNormal.z) * _slopeSlideSpeed;
            }
            else if (_moveInput != Vector2.zero)
            {
                Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
                _targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, _rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, rotation, 0f);

                Vector3 moveDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
                float currentSpeed = _isSprinting ? _sprintSpeed : _walkSpeed;
                movement = moveDirection * currentSpeed;
            }

            movement.y = _verticalVelocity;
            _controller.Move(movement * Time.deltaTime);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _hitNormal = hit.normal;
        }
    }
}
