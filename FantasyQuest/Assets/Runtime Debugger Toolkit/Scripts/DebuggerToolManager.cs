using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace ThornyDevtudio.RuntimeDebuggerToolkit
{
    public class DebuggerToolManager : MonoBehaviour
    {
        [Header("Tool's Canvas")]
        [SerializeField] GameObject FPS_Tracker;
        [SerializeField] GameObject Console_Display;
        [SerializeField] GameObject ColliderVisualizer;
        [SerializeField] GameObject TimeScaler;

        [Header("UI Toggles")]
        [SerializeField] Toggle use_FPS_Tracker_Toggle;
        [SerializeField] Toggle use_Console_Display_Toggle;
        [SerializeField] Toggle use_ColliderVisualizer_Toggle;
        [SerializeField] Toggle use_TimeScaler_Toggle;

        [Header("Optional")]
        public bool useTabToUnlockAndLockMouse = true;
        public GameObject textMeshPro_GameObject;

        private void Awake()
        {
            if (useTabToUnlockAndLockMouse)
                textMeshPro_GameObject.SetActive(true);
            else textMeshPro_GameObject.SetActive(false);

            if (use_FPS_Tracker_Toggle != null)
                use_FPS_Tracker_Toggle.onValueChanged.AddListener(val =>
                {
                    FPS_Tracker.SetActive(val);
                });

            if (use_Console_Display_Toggle != null)
                use_Console_Display_Toggle.onValueChanged.AddListener(val =>
                {
                    Console_Display.SetActive(val);
                });

            if (use_ColliderVisualizer_Toggle != null)
                use_ColliderVisualizer_Toggle.onValueChanged.AddListener(val =>
                {
                    ColliderVisualizer.SetActive(val);
                });
            if (use_TimeScaler_Toggle != null)
                use_TimeScaler_Toggle.onValueChanged.AddListener(val =>
                {
                    TimeScaler.SetActive(val);
                });
        }

        private void Start()
        {
            if (use_FPS_Tracker_Toggle != null)
                FPS_Tracker.SetActive(use_FPS_Tracker_Toggle.isOn);

            if (use_Console_Display_Toggle != null)
                Console_Display.SetActive(use_Console_Display_Toggle.isOn);

            if (use_ColliderVisualizer_Toggle != null)
                ColliderVisualizer.SetActive(use_ColliderVisualizer_Toggle.isOn);

            if (use_TimeScaler_Toggle != null)
                TimeScaler.SetActive(use_TimeScaler_Toggle.isOn);
        }

        private void Update()
        {
            if (useTabToUnlockAndLockMouse)
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    ToggleMouseLock();
                }
#else
			if (Input.GetKeyDown("tab"))
            {
                ToggleMouseLock();
            }
#endif
        }

        private void ToggleMouseLock()
        {
            if (Cursor.lockState == CursorLockMode.None)
            {
                LockMouse();
            }
            else
            {
                UnlockMouse();
            }
        }

        private void UnlockMouse()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.fullScreenMode = FullScreenMode.Windowed;
        }

        private void LockMouse()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
