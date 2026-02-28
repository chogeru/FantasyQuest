using UnityEngine;

namespace Project.Core.AI
{
    public enum EnemyState
    {
        Idle,
        Patrol,     // 巡回中（視覚センサー稼働）
        Chase,      // プレイヤーを追跡中
        Attacking,  // 攻撃行動中（予兆・判定・硬直を含む。移動不可）
        Stagger,    // 被ダメージによるひるみ・ノックバック中（移動・攻撃不可）
        Dead        // 死亡
    }

    /// <summary>
    /// 敵の現在状態を一元管理するステートマシン。
    /// 攻撃中やひるみ中の不用意な移動・重複処理を防ぐための基盤。
    /// </summary>
    public class EnemyStateMachine : MonoBehaviour
    {
        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

        public delegate void StateChangedHandler(EnemyState oldState, EnemyState newState);
        public event StateChangedHandler OnStateChanged;

        public bool ChangeState(EnemyState newState)
        {
            if (CurrentState == newState || CurrentState == EnemyState.Dead) return false;

            // 例: ひるみ中にパトロールへの遷移は拒否する、などの厳格なルールをここに集約できます
            if (CurrentState == EnemyState.Stagger && (newState == EnemyState.Patrol || newState == EnemyState.Chase))
            {
                // Staggerからの復帰は専用の経路を経由させる等の場合はfalseを返すことも可能
            }

            EnemyState oldState = CurrentState;
            CurrentState = newState;
            
            OnStateChanged?.Invoke(oldState, newState);
            return true;
        }

        // 状態判定用のヘルパー
        public bool CanMove => CurrentState == EnemyState.Patrol || CurrentState == EnemyState.Chase;
        public bool IsStaggered => CurrentState == EnemyState.Stagger;
        public bool IsAttacking => CurrentState == EnemyState.Attacking;
    }
}
