using UnityEngine;
using UnityEngine.UI;



namespace ThornyDevtudio.RuntimeDebuggerToolkit
{
    public class FoldableMenu : MonoBehaviour
    {
        public Button headerButton;
        public GameObject contentPanel;
        public Transform arrowTransform;

        private bool isExpanded = false;

        void Start()
        {
            headerButton.onClick.AddListener(ToggleMenu);
        }

        void ToggleMenu()
        {
            isExpanded = !isExpanded;
            Vector3 scale = arrowTransform.localScale;
            scale.y = -scale.y;
            arrowTransform.localScale = scale;
            contentPanel.SetActive(isExpanded);
        }
    }
}
