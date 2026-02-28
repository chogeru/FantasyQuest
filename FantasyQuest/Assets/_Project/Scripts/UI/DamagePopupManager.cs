using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// ダメージ数値のUIポップアップを管理・制御するシステム。
    /// （※実際にはTextコンポーネントやTextMeshProのPrefabを対象に制御します）
    /// 高負荷を避けるためのシンプルなオブジェクトプールを内蔵しています。
    /// </summary>
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("ポップアップ用のテキストUI（シーン上の非アクティブなオブジェクト等を仮指定）")]
        [SerializeField] private GameObject _damageTextPrefab;
        [Tooltip("UIを表示するCanvas（WorldSpace設定推奨）")]
        [SerializeField] private Transform _canvasTransform;
        [Tooltip("画面に同時に出せるポップアップの最大数")]
        [SerializeField] private int _poolSize = 20;

        [Header("Animation Settings")]
        [SerializeField] private float _floatSpeed = 2f;
        [SerializeField] private float _fadeDuration = 1f;

        private Queue<GameObject> _popupPool = new Queue<GameObject>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializePool();
        }

        private void InitializePool()
        {
            if (_damageTextPrefab == null || _canvasTransform == null) return;

            for (int i = 0; i < _poolSize; i++)
            {
                var obj = Instantiate(_damageTextPrefab, _canvasTransform);
                obj.SetActive(false);
                _popupPool.Enqueue(obj);
            }
        }

        /// <summary>
        /// ワールド座標にダメージ数値を表示する
        /// </summary>
        public void CreatePopup(Vector3 worldPosition, float damage)
        {
            if (_popupPool.Count == 0 || _canvasTransform == null) return;

            // プールから1つ取り出す
            GameObject popup = _popupPool.Dequeue();
            popup.SetActive(true);

            // 対象の頭上より少し上に配置
            popup.transform.position = worldPosition + Vector3.up * 1.5f;

            // --- 実際のテキスト変更処理（TMProなどを使う想定） ---
            var textComponent = popup.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = Mathf.RoundToInt(damage).ToString();
            }

            // シンプルな制御スクリプト（またはコルーチン）でアニメーションと自動返却を行う
            StartCoroutine(AnimatePopup(popup, textComponent));
        }

        private System.Collections.IEnumerator AnimatePopup(GameObject popup, Text textComponent)
        {
            float timer = 0f;
            Color startColor = textComponent != null ? textComponent.color : Color.white;
            startColor.a = 1f;

            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                float normalizedTime = timer / _fadeDuration;

                // 上方向へフワッと移動
                popup.transform.position += Vector3.up * _floatSpeed * Time.deltaTime;

                // 徐々に透明にする
                if (textComponent != null)
                {
                    Color c = startColor;
                    c.a = Mathf.Lerp(1f, 0f, normalizedTime);
                    textComponent.color = c;
                }

                yield return null;
            }

            // 終了したら非表示にしてプールへ戻す
            popup.SetActive(false);
            _popupPool.Enqueue(popup);
        }
    }
}
