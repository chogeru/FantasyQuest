using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Systems.Input
{
    /// <summary>
    /// Input System アセットからの入力を受け取り、ゲーム内にイベントとして配信する共通クラス。
    /// （PlayerInputコンポーネントを介さず直接コードで制御するための仕組み）
    /// </summary>
    [CreateAssetMenu(fileName = "InputReader", menuName = "Project/Input/Input Reader")]
    public class InputReader : ScriptableObject
    {
        // === Events ===
        public event Action<Vector2> OnMoveEvent = delegate { };
        public event Action<Vector2> OnLookEvent = delegate { };
        public event Action OnJumpEvent = delegate { };
        public event Action OnJumpCanceledEvent = delegate { };
        public event Action OnAttackEvent = delegate { };
        
        // [追加] スプリントイベント
        public event Action<bool> OnSprintEvent = delegate { };

        [Header("Actions")]
        [SerializeField] private InputActionAsset _inputActionAsset;

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;
        private InputAction _sprintAction;

        private void OnEnable()
        {
            if (_inputActionAsset == null)
            {
                Debug.LogWarning("[InputReader] InputActionAssetがアサインされていません。");
                return;
            }

            // "Player" アクションマップを想定
            var playerMap = _inputActionAsset.FindActionMap("Player");
            if (playerMap == null) return;

            _moveAction = playerMap.FindAction("Move");
            _lookAction = playerMap.FindAction("Look");
            _jumpAction = playerMap.FindAction("Jump");
            _attackAction = playerMap.FindAction("Attack");
            _sprintAction = playerMap.FindAction("Sprint");

            if (_moveAction != null)
            {
                _moveAction.performed += ctx => OnMoveEvent.Invoke(ctx.ReadValue<Vector2>());
                _moveAction.canceled += ctx => OnMoveEvent.Invoke(Vector2.zero);
            }

            if (_lookAction != null)
            {
                _lookAction.performed += ctx => OnLookEvent.Invoke(ctx.ReadValue<Vector2>());
                _lookAction.canceled += ctx => OnLookEvent.Invoke(Vector2.zero);
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed += ctx => OnJumpEvent.Invoke();
                _jumpAction.canceled += ctx => OnJumpCanceledEvent.Invoke();
            }

            if (_attackAction != null)
            {
                _attackAction.performed += ctx => OnAttackEvent.Invoke();
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed += ctx => OnSprintEvent.Invoke(true);
                _sprintAction.canceled += ctx => OnSprintEvent.Invoke(false);
            }

            EnablePlayerInput();
        }

        private void OnDisable()
        {
            DisablePlayerInput();
        }

        public void EnablePlayerInput()
        {
            _inputActionAsset?.FindActionMap("Player")?.Enable();
        }

        public void DisablePlayerInput()
        {
            _inputActionAsset?.FindActionMap("Player")?.Disable();
        }
    }
}
