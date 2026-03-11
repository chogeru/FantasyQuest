using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI.Utility
{
    /// <summary>
    /// シーン遷移時のフェードインやフェードアウトを管理する汎用クラス
    /// 今回はDOTWeenがない想定でコルーチンとCanvasGroupで実装します。
    /// 簡単な呼び出し: ScreenFader.Instance.FadeOut(0.5f, callback);
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private CanvasGroup _faderCanvasGroup;
        [SerializeField] private Image _faderImage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_faderCanvasGroup == null)
            {
                // 自動セットアップ
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();

                var imgGo = new GameObject("FadeImage");
                imgGo.transform.SetParent(transform, false);
                _faderImage = imgGo.AddComponent<Image>();
                _faderImage.color = Color.black;
                
                var rect = _faderImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                _faderCanvasGroup = imgGo.AddComponent<CanvasGroup>();
                _faderCanvasGroup.alpha = 0f; // 初期は透明
                _faderCanvasGroup.blocksRaycasts = false; // 初期はクリック妨害しない
            }
        }

        public void FadeIn(float duration = 1f, Action onComplete = null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1f, 0f, duration, onComplete));
        }

        public void FadeOut(float duration = 1f, Action onComplete = null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0f, 1f, duration, onComplete));
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, Action onComplete)
        {
            _faderCanvasGroup.blocksRaycasts = true; // 遷移中はクリックガード
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                _faderCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
                yield return null;
            }

            _faderCanvasGroup.alpha = endAlpha;
            _faderCanvasGroup.blocksRaycasts = (endAlpha > 0.5f); // 完全透明ならクリックガード解除

            onComplete?.Invoke();
        }
    }
}
