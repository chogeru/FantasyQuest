using UnityEngine;
using UnityEngine.Events;
using Project.Core.Interfaces;

namespace Project.Systems.Combat
{
    /// <summary>
    /// ダメージを受け取る側の判定。IDamageableを実装し、
    /// ダメージイベントを他のコンポーネント（Stats等）へ中継します。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hurtbox : MonoBehaviour, IDamageable
    {
        [Tooltip("ダメージを受けた際に発行されるイベント")]
        public UnityEvent<float> OnTakeDamage;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null && col.isTrigger) 
            {
                Debug.LogWarning($"[Hurtbox] {gameObject.name} のColliderがIsTriggerになっています。弾かれる挙動等が必要な場合は外してください。");
            }
        }

        /// <summary>
        /// IDamageableの実装メソッド。Hitbox等から呼ばれる。
        /// </summary>
        public void TakeDamage(float damageAmount)
        {
            Debug.Log($"[Hurtbox] {gameObject.name} が {damageAmount} のダメージを受けた！");
            OnTakeDamage?.Invoke(damageAmount);
        }
    }
}
