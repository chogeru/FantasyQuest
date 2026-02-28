using UnityEngine;

namespace Project.Core.Player
{
    public enum PlayerState
    {
        Locomotion,     // 通常の移動・待機状態
        Attacking,      // 攻撃中（移動が制限される）
        Dodging,        // 回避中
        Disabled        // イベント中や死亡時など操作不可
    }

    /// <summary>
    /// プレイヤーの「現在状態」を一括管理するステートマシン。
    /// PlayerController(移動)やCombatController(攻撃)等の密結合を防ぐ「仲介役(Mediator)」として機能します。
    /// </summary>
    public class PlayerStateMachine : MonoBehaviour
    {
        public PlayerState CurrentState { get; private set; } = PlayerState.Locomotion;

        public delegate void StateChangedHandler(PlayerState oldState, PlayerState newState);
        public event StateChangedHandler OnStateChanged;

        /// <summary>
        /// 状態の変更を試みる。
        /// </summary>
        public bool ChangeState(PlayerState newState)
        {
            // 同じ状態への遷移は無視
            if (CurrentState == newState) return false;

            // TODO: より厳密に「攻撃中から移動中」への遷移許可等のルールをここに集約可能

            PlayerState oldState = CurrentState;
            CurrentState = newState;
            
            OnStateChanged?.Invoke(oldState, newState);
            return true;
        }

        /// <summary>
        /// 現在移動可能かどうか
        /// </summary>
        public bool CanMove => CurrentState == PlayerState.Locomotion;

        /// <summary>
        /// 現在攻撃可能かどうか
        /// </summary>
        public bool CanAttack => CurrentState == PlayerState.Locomotion;
    }
}
