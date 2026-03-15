using UnityEngine;

namespace Project.Core.Player
{
    /// <summary>
    /// Animatorのパラメータ名（設定）をインスペクターから簡単に変更できるようにするクラス
    /// </summary>
    [System.Serializable]
    public class PlayerAnimationSettings
    {
        [Tooltip("移動速度のパラメータ名 (float)")]
        public string SpeedParam = "Speed";
        [Tooltip("接地判定のパラメータ名 (bool)")]
        public string IsGroundedParam = "IsGrounded";
        [Tooltip("上下の速度のパラメータ名 (float)")]
        public string VerticalVelocityParam = "VerticalVelocity";
        [Tooltip("ジャンプのトリガー名 (trigger)")]
        public string JumpTriggerParam = "Jump";
        [Tooltip("水泳判定のパラメータ名 (bool)")]
        public string IsSwimmingParam = "IsSwimming";

        [HideInInspector] public int SpeedHash;
        [HideInInspector] public int IsGroundedHash;
        [HideInInspector] public int VerticalVelocityHash;
        [HideInInspector] public int JumpTriggerHash;
        [HideInInspector] public int IsSwimmingHash;

        public void Initialize()
        {
            SpeedHash = Animator.StringToHash(SpeedParam);
            IsGroundedHash = Animator.StringToHash(IsGroundedParam);
            VerticalVelocityHash = Animator.StringToHash(VerticalVelocityParam);
            JumpTriggerHash = Animator.StringToHash(JumpTriggerParam);
            IsSwimmingHash = Animator.StringToHash(IsSwimmingParam);
        }
    }

    /// <summary>
    /// PlayerControllerから値を受け取り、アニメーションを制御するコンポーネント。
    /// アニメーション関連の処理を分離し、簡単にセットアップできるようにします。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        
        [Header("Animation Settings")]
        [SerializeField] private PlayerAnimationSettings _animSettings = new PlayerAnimationSettings();

        [Header("Damping Settings")]
        [Tooltip("移動速度がアニメーションへ反映される際の滑らかさ")]
        [SerializeField] private float _speedDampTime = 0.1f;

        private PlayerController _playerController;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _animSettings.Initialize();
        }

        private void OnEnable()
        {
            if (_playerController != null)
            {
                _playerController.OnJumpExecuted += HandleJump;
            }
        }

        private void OnDisable()
        {
            if (_playerController != null)
            {
                _playerController.OnJumpExecuted -= HandleJump;
            }
        }

        private void Update()
        {
            if (_animator == null || _playerController == null) return;

            // アニメーターの更新
            // Animator.SetFloatにDampTimeとdeltaTimeを渡すことで、滑らかなブレンドツリー遷移を実現
            _animator.SetFloat(_animSettings.SpeedHash, _playerController.CurrentSpeed, _speedDampTime, Time.deltaTime);
            _animator.SetBool(_animSettings.IsGroundedHash, _playerController.IsGrounded);
            _animator.SetFloat(_animSettings.VerticalVelocityHash, _playerController.VerticalVelocity);
            _animator.SetBool(_animSettings.IsSwimmingHash, _playerController.IsInWater);
        }

        private void HandleJump()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(_animSettings.JumpTriggerHash);
            }
        }
        
        /// <summary>
        /// 外部からAnimatorを簡単にセットするためのメソッド（インスペクタやスクリプトから呼び出し可能）
        /// </summary>
        public void SetAnimator(Animator animator)
        {
            _animator = animator;
        }
    }
}
