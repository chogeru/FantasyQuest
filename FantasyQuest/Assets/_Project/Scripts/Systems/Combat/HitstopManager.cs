using System.Collections;
using UnityEngine;

namespace Project.Systems.Combat
{
    /// <summary>
    /// ゲーム全体のヒットストップ（スローモーション）を管理するクラス。
    /// 複数の攻撃が同時に当たった場合の多重スロー化を防ぐためのシングルトン構造。
    /// </summary>
    public class HitstopManager : MonoBehaviour
    {
        public static HitstopManager Instance { get; private set; }

        private bool _isHitstoppping;
        private Coroutine _hitstopCoroutine;
        private float _originalTimeScale = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _originalTimeScale = Time.timeScale;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void TriggerHitstop(float duration, float slowScale)
        {
            if (_isHitstoppping)
            {
                // 既にヒットストップ中の場合は、既存のものをキャンセルして新しいもので上書きする
                StopCoroutine(_hitstopCoroutine);
            }
            else
            {
                _originalTimeScale = Time.timeScale; // 開始前のスケールを保存
            }

            _hitstopCoroutine = StartCoroutine(HitstopRoutine(duration, slowScale));
        }

        private IEnumerator HitstopRoutine(float duration, float slowScale)
        {
            _isHitstoppping = true;
            Time.timeScale = slowScale;
            
            yield return new WaitForSecondsRealtime(duration);
            
            Time.timeScale = _originalTimeScale;
            _isHitstoppping = false;
        }

        private void OnDestroy()
        {
            // マネージャーが破棄される際は、絶対に元の時間に強制的に戻す
            if (Instance == this && _isHitstoppping)
            {
                Time.timeScale = _originalTimeScale;
            }
        }
    }
}
