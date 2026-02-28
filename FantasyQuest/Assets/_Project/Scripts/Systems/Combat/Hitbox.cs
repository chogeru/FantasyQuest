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
                    if (_enableHitstop)
                    {
                        // 念のため既に動いていれば多重に止めないなどの制御をここで行うとより安全です
                        StartCoroutine(HitstopRoutine());
                    }

                    SetActive(false); // 多段ヒット防止のため、即座に判定をオフにする
                }
            }
        }

        /// <summary>
        /// 当たった瞬間にわずかにゲームの時間を遅くし、すぐに元に戻すコルーチン
        /// </summary>
        private IEnumerator HitstopRoutine()
        {
            // 現在のTimeScaleを保存しておく（すでに別のスロー演出などが掛かっている場合を考慮）
            float originalTimeScale = Time.timeScale;
            Time.timeScale = _hitstopTimeScale;
            
            // TimeScaleが遅くなっているため、現実の秒数を待つ「WaitForSecondsRealtime」を使用する
            yield return new WaitForSecondsRealtime(_hitstopDuration);
            
            // 時間を元に戻す
            Time.timeScale = originalTimeScale;
        }
    }
}
