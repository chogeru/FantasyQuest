using System.Collections;
using UnityEngine;
using Project.Core.Interfaces;

namespace Project.Systems.Combat
{
    /// <summary>
    /// ダメージを与える武器や魔法の弾などの判定基盤。
    /// （ブラッシュアップ版：ヒットストップ演出追加により打撃感を向上）
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        [Tooltip("この判定が接触した際に与えるダメージ量")]
        [SerializeField] private float _damageAmount = 10f;
        
        [Tooltip("どのレイヤーに対して判定を有効にするか")]
        [SerializeField] private LayerMask _targetLayer;

        [Header("Juice (打撃感の演出)")]
        [SerializeField] private bool _enableHitstop = true;
        [Tooltip("ヒットストップで時間を止める期間（秒）")]
        [SerializeField] private float _hitstopDuration = 0.08f;
        [Tooltip("ヒットストップ時の時間の流れ（0で完全停止、0.1でスローモーションなど）")]
        [SerializeField] private float _hitstopTimeScale = 0.05f;
        
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true; 
            SetActive(false);
        }

        public void SetActive(bool isActive)
        {
            if (_collider != null) _collider.enabled = isActive;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & _targetLayer) != 0)
            {
                if (other.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(_damageAmount);
                    
                    // 攻撃が当たった瞬間に「ヒットストップ(時間停止)」をかける
                    if (_enableHitstop && HitstopManager.Instance != null)
                    {
                        HitstopManager.Instance.TriggerHitstop(_hitstopDuration, _hitstopTimeScale);
                    }

                    SetActive(false); // 多段ヒット防止のため、即座に判定をオフにする
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_collider != null && _collider.enabled)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = new Color(1f, 0f, 0f, 0.4f); // 半透明の赤
                
                if (_collider is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                    Gizmos.DrawWireCube(box.center, box.size);
                }
                else if (_collider is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                }
            }
        }
#endif
    }
}
