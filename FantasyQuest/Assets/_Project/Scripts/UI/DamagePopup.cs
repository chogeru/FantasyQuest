using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// ダメージ発生時にワールド空間上に数値を表示し、上にフワッと消えていくアニメーションを行うスクリプト。
    /// 本来はTextMeshProを使用しますが、標準機能のみで動作するようTextコンポーネントを用いています。
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private Text _damageText;
        [SerializeField] private float _moveYAmount = 2f;
        [SerializeField] private float _destroyTime = 1f;
        
        private float _fadeTimer;
        private Color _textColor;

        public void Setup(float damageAmount)
        {
            if (_damageText == null) _damageText = GetComponentInChildren<Text>();
            
            _damageText.text = damageAmount.ToString("0");
            _textColor = _damageText.color;
            _fadeTimer = _destroyTime;
            
            // 多少ランダムに散らす
            transform.position += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);

            Destroy(gameObject, _destroyTime);
        }

        private void Update()
        {
            // 上に移動
            transform.position += Vector3.up * _moveYAmount * Time.deltaTime;

            // フェードアウト
            _fadeTimer -= Time.deltaTime;
            _textColor.a = _fadeTimer / _destroyTime;
            _damageText.color = _textColor;

            // 常にカメラの方を向く（ビルボード）
            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            }
        }
    }
}
