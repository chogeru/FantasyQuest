using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// 「アイテムを入手した」「セーブしました」などのシステムメッセージを
    /// 右下等にフワッと表示する汎用通知（トースト）システム。
    /// （※CanvasのRender Modeは「Screen Space - Overlay」を想定）
    /// </summary>
    public class NotificationUI : MonoBehaviour
    {
        public static NotificationUI Instance { get; private set; }

        [Header("References")]
        [Tooltip("通知テキストのPrefab（レイアウトグループ等で縦に並べる小枠）")]
        [SerializeField] private GameObject _notificationPrefab;
        [Tooltip("通知を追加していく親オブジェクト（VerticalLayoutGroup推奨）")]
        [SerializeField] private Transform _notificationContainer;

        [Header("Settings")]
        [Tooltip("画面に表示され続ける時間（秒）")]
        [SerializeField] private float _displayDuration = 3f;
        [Tooltip("フェードアウトにかかる時間")]
        [SerializeField] private float _fadeDuration = 0.5f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// どのスクリプトからも「NotificationUI.Instance.ShowNotification("テスト")」で
        /// リッチな通知を画面に出せます。
        /// </summary>
        public void ShowNotification(string message)
        {
            if (_notificationPrefab == null || _notificationContainer == null) return;

            // 新しく通知パネルを生成
            GameObject notificationObj = Instantiate(_notificationPrefab, _notificationContainer);
            notificationObj.SetActive(true);

            // テキストを差し替え
            var textComp = notificationObj.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                textComp.text = message;
            }

            // 指定時間後に消えるアニメーションを開始
            StartCoroutine(AnimateNotificationRoutine(notificationObj, textComp));
        }

        private IEnumerator AnimateNotificationRoutine(GameObject panelObj, Text textComp)
        {
            // まずは表示をキープ
            yield return new WaitForSeconds(_displayDuration);

            if (textComp == null)
            {
                Destroy(panelObj);
                yield break;
            }

            // スーッと透明になっていくフェードアウト処理
            float timer = 0f;
            Color startColor = textComp.color;

            // 背景パネル(Image)があれば同時にフェードアウトさせる処理をここに追加できます
            var images = panelObj.GetComponentsInChildren<Image>();

            while (timer < _fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);

                Color newColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
                textComp.color = newColor;

                foreach (var img in images)
                {
                    Color ic = img.color;
                    ic.a = alpha;
                    img.color = ic;
                }

                yield return null;
            }

            // 完全に透明になったら削除してメモリを解放
            Destroy(panelObj);
        }
    }
}
