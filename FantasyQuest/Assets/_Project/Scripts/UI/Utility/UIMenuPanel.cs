using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Project.UI.Utility
{
    /// <summary>
    /// 各メニュー画面（タイトルメイン画面、オプション画面など）の
    /// 一つの「ページ（パネル）」を管理するための汎用コンポーネント。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIMenuPanel : MonoBehaviour
    {
        [SerializeField] private Selectable _firstSelectedElement;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (_firstSelectedElement != null)
            {
                // コントローラー・キーボード操作用にフォーカスを当てる
                EventSystem.current.SetSelectedGameObject(_firstSelectedElement.gameObject);
            }
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
