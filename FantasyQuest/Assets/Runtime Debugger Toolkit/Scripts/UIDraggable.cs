using UnityEngine;
using UnityEngine.EventSystems;


namespace ThornyDevtudio.RuntimeDebuggerToolkit
{
    public class DraggableUI : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private RectTransform rectTransform;
        private Canvas canvas;
        private Vector2 offset;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
                Debug.LogError("No canva found in parent !");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPointerPos))
            {
                offset = rectTransform.anchoredPosition - localPointerPos;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPointerPos))
            {
                rectTransform.anchoredPosition = localPointerPos + offset;
            }
        }
    }
}
