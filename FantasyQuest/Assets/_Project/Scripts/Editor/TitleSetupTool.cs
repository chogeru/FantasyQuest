using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

using Project.UI.Title;
using Project.UI.Utility;
using Project.Systems.Audio;
using Project.Systems.Input;

namespace Project.Editor
{
    public class TitleSetupTool : EditorWindow
    {
        [MenuItem("Project/Setup Title Scene")]
        public static void SetupTitleScene()
        {
            Debug.Log("Starting Title Scene Setup...");

            CleanupExistingSetup();

            // 1. Audio Setup
            var commonAudioConfig = CreateAudioConfig("CommonAudioConfig");
            var titleAudioConfig = CreateAudioConfig("TitleAudioConfig");
            CreateAudioManager(commonAudioConfig);
            CreateAudioConfigLoader(titleAudioConfig);

            // 2. Load InputReader
            var inputReader = LoadInputReader();

            // 3. UI System Setup
            CreateEventSystem();
            Canvas canvas = CreateCanvas();

            // 4. Create UI Containers & Elements
            var pressAnyButtonPanel = CreatePressAnyButtonUI(canvas.transform);
            var mainMenuPanel = CreateMainMenuUI(canvas.transform);
            var optionsPanel = CreateOptionsUI(canvas.transform);
            var loadingPanel = CreateLoadingUI(canvas.transform);
            var cursor = CreateCursorUI(canvas.transform);

            // 5. Setup Managers
            CreateTitleManager(pressAnyButtonPanel, mainMenuPanel, optionsPanel, loadingPanel, cursor, inputReader);

            Debug.Log("<color=green>Title Scene Setup Complete!</color>");
        }

        private static void CleanupExistingSetup()
        {
            var oldCanvas = GameObject.Find("TitleCanvas");
            if (oldCanvas != null) DestroyImmediate(oldCanvas);

            var oldManager = GameObject.Find("TitleManager");
            if (oldManager != null) DestroyImmediate(oldManager);
        }

        private static AudioDataConfig CreateAudioConfig(string configName)
        {
            string dir = "Assets/_Project/Audio";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string path = $"{dir}/{configName}.asset";
            AudioDataConfig config = AssetDatabase.LoadAssetAtPath<AudioDataConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AudioDataConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
            }

            return config;
        }

        private static void CreateAudioManager(AudioDataConfig config)
        {
            if (Object.FindObjectOfType<AudioManager>() != null) return;

            GameObject obj = new GameObject("AudioManager");
            var manager = obj.AddComponent<AudioManager>();

            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("_commonConfig").objectReferenceValue = config;
            serializedManager.ApplyModifiedProperties();
        }

        private static void CreateAudioConfigLoader(AudioDataConfig config)
        {
            if (Object.FindObjectOfType<AudioConfigLoader>() != null) return;

            GameObject obj = new GameObject("SceneAudioConfigLoader");
            var loader = obj.AddComponent<AudioConfigLoader>();
            
            SerializedObject serializedLoader = new SerializedObject(loader);
            serializedLoader.FindProperty("_sceneAudioConfig").objectReferenceValue = config;
            serializedLoader.ApplyModifiedProperties();
        }

        private static InputReader LoadInputReader()
        {
            string path = "Assets/_Project/Input/InputReader.asset";
            var reader = AssetDatabase.LoadAssetAtPath<InputReader>(path);
            if (reader == null)
            {
                Debug.LogWarning("InputReader.asset not found at " + path + ". Please run 'Setup Action Game Scene' first or assign it manually.");
            }
            return reader;
        }

        private static void CreateEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private static Canvas CreateCanvas()
        {
            var canvasObj = GameObject.Find("TitleCanvas");
            Canvas canvas;
            if (canvasObj == null)
            {
                canvasObj = new GameObject("TitleCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }
            return canvas;
        }

        private static UIMenuPanel CreatePressAnyButtonUI(Transform parentCanvas)
        {
            GameObject container = new GameObject("PressAnyButtonContainer", typeof(RectTransform));
            container.transform.SetParent(parentCanvas, false);
            SetFullScreenRect(container.GetComponent<RectTransform>());

            var panel = container.AddComponent<UIMenuPanel>();
            var canvasGroup = container.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1;

            GameObject textObj = new GameObject("Text_PressAnyBtn", typeof(RectTransform));
            textObj.transform.SetParent(container.transform, false);
            var text = textObj.AddComponent<Text>();
            text.text = "Press Any Button";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 64;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;

            SetFullScreenRect(textObj.GetComponent<RectTransform>());

            return panel;
        }

        private static UIMenuPanel CreateMainMenuUI(Transform parentCanvas)
        {
            GameObject container = new GameObject("MainMenuContainer", typeof(RectTransform));
            container.transform.SetParent(parentCanvas, false);
            
            var rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 600);

            var panel = container.AddComponent<UIMenuPanel>();
            var canvasGroup = container.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0; // Starts hidden

            var layoutGroup = container.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = 30;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;

            // Create Buttons
            Button newGameBtn = CreateButton("New Game Button", "New Game", container.transform);
            Button continueBtn = CreateButton("Continue Button", "Continue", container.transform);
            Button optionsBtn = CreateButton("Options Button", "Options", container.transform);
            Button quitBtn = CreateButton("Quit Button", "Quit", container.transform);

            // Set UIMenuPanel first selection
            SerializedObject serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("_firstSelectedElement").objectReferenceValue = newGameBtn;
            serializedPanel.ApplyModifiedProperties();

            // Set up explicit navigation (optional but good practice)
            SetupVerticalNavigation(new Button[] { newGameBtn, continueBtn, optionsBtn, quitBtn });

            container.SetActive(false); // Default inactive for the start logic

            return panel;
        }

        private static UIMenuPanel CreateOptionsUI(Transform parentCanvas)
        {
            GameObject container = new GameObject("OptionsPanel", typeof(RectTransform));
            container.transform.SetParent(parentCanvas, false);
            SetFullScreenRect(container.GetComponent<RectTransform>());

            var panel = container.AddComponent<UIMenuPanel>();
            var canvasGroup = container.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0; // Starts hidden

            var img = container.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.9f);

            GameObject textObj = new GameObject("Text_OptionsTitle", typeof(RectTransform));
            textObj.transform.SetParent(container.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0, -100);
            textRect.sizeDelta = new Vector2(400, 100);

            var text = textObj.AddComponent<Text>();
            text.text = "Options Menu (WIP)";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            // Optional: Close button
            Button closeBtn = CreateButton("Close Options Button", "Close", container.transform);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0, 100);

            SerializedObject serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("_firstSelectedElement").objectReferenceValue = closeBtn;
            serializedPanel.ApplyModifiedProperties();

            container.SetActive(false);
            return panel;
        }

        private static GameObject CreateLoadingUI(Transform parentCanvas)
        {
            GameObject container = new GameObject("LoadingPanel", typeof(RectTransform));
            container.transform.SetParent(parentCanvas, false);
            SetFullScreenRect(container.GetComponent<RectTransform>());

            var img = container.AddComponent<Image>();
            img.color = Color.black;

            // Progress bar (Slider)
            GameObject sliderObj = new GameObject("LoadingSlider", typeof(RectTransform));
            sliderObj.transform.SetParent(container.transform, false);
            var slider = sliderObj.AddComponent<Slider>();
            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(0, -100);
            sliderRect.sizeDelta = new Vector2(800, 40);

            GameObject backgroundObj = new GameObject("Background", typeof(RectTransform));
            backgroundObj.transform.SetParent(sliderObj.transform, false);
            var bgImg = backgroundObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            SetFullScreenRect(backgroundObj.GetComponent<RectTransform>());

            GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            SetFullScreenRect(fillAreaRect);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = Color.white;
            var fillRect = fillObj.GetComponent<RectTransform>();
            SetFullScreenRect(fillRect);

            slider.targetGraphic = fillImg;
            slider.fillRect = fillRect;
            slider.interactable = false;

            // Loading Text
            GameObject textObj = new GameObject("LoadingText", typeof(RectTransform));
            textObj.transform.SetParent(container.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0, 0);
            textRect.sizeDelta = new Vector2(400, 100);

            var text = textObj.AddComponent<Text>();
            text.text = "Loading...";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            container.SetActive(false);
            return container;
        }

        private static Button CreateButton(string objName, string label, Transform parent)
        {
            GameObject btnObj = new GameObject(objName, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 80);

            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var btn = btnObj.AddComponent<Button>();
            
            // Allow highlight color change
            var colors = btn.colors;
            colors.highlightedColor = Color.gray;
            btn.colors = colors;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            SetFullScreenRect(textRect);

            var text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return btn;
        }

        private static void SetupVerticalNavigation(Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var nav = new Navigation();
                nav.mode = Navigation.Mode.Explicit;

                nav.selectOnUp = i > 0 ? buttons[i - 1] : buttons[buttons.Length - 1];
                nav.selectOnDown = i < buttons.Length - 1 ? buttons[i + 1] : buttons[0];

                buttons[i].navigation = nav;
            }
        }

        private static RectTransform CreateCursorUI(Transform parentCanvas)
        {
            GameObject cursorObj = new GameObject("Cursor", typeof(RectTransform));
            cursorObj.transform.SetParent(parentCanvas, false);
            
            var rect = cursorObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(30, 30);
            
            var img = cursorObj.AddComponent<Image>();
            img.color = Color.yellow;
            // You can replace this with an actual sprite icon later using:
            // img.sprite = ...

            // Draw a triangle shape using a custom simple texture or just leave as a colored square
            cursorObj.SetActive(false);

            return rect;
        }

        private static void CreateTitleManager(UIMenuPanel pressAnyBtn, UIMenuPanel mainMenu, UIMenuPanel optionsMenu, GameObject loadingPanel, RectTransform cursor, InputReader input)
        {
            GameObject managerObj = GameObject.Find("TitleManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("TitleManager");
            }

            var sceneManager = managerObj.GetComponent<TitleSceneManager>();
            if (sceneManager == null) sceneManager = managerObj.AddComponent<TitleSceneManager>();

            var uiController = managerObj.GetComponent<TitleUIController>();
            if (uiController == null) uiController = managerObj.AddComponent<TitleUIController>();

            // Setup UI Controller References
            SerializedObject serializedUI = new SerializedObject(uiController);
            serializedUI.FindProperty("_pressAnyButtonPanel").objectReferenceValue = pressAnyBtn;
            serializedUI.FindProperty("_mainMenuPanel").objectReferenceValue = mainMenu;
            serializedUI.FindProperty("_optionsPanel").objectReferenceValue = optionsMenu;
            
            // Map buttons
            Transform mainMenuTransform = mainMenu.transform;
            serializedUI.FindProperty("_newGameButton").objectReferenceValue = mainMenuTransform.Find("New Game Button").GetComponent<Button>();
            serializedUI.FindProperty("_continueButton").objectReferenceValue = mainMenuTransform.Find("Continue Button").GetComponent<Button>();
            serializedUI.FindProperty("_optionsButton").objectReferenceValue = mainMenuTransform.Find("Options Button").GetComponent<Button>();
            serializedUI.FindProperty("_quitButton").objectReferenceValue = mainMenuTransform.Find("Quit Button").GetComponent<Button>();

            serializedUI.FindProperty("_cursorRect").objectReferenceValue = cursor;
            serializedUI.ApplyModifiedProperties();

            var closeBtn = optionsMenu.transform.Find("Close Options Button").GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtn.onClick, new UnityEngine.Events.UnityAction(uiController.ShowMainMenu));

            // Setup Scene Manager References
            SerializedObject serializedScene = new SerializedObject(sceneManager);
            serializedScene.FindProperty("_titleUIController").objectReferenceValue = uiController;
            serializedScene.FindProperty("_loadingPanel").objectReferenceValue = loadingPanel;
            serializedScene.FindProperty("_loadingProgressBar").objectReferenceValue = loadingPanel.transform.Find("LoadingSlider").GetComponent<Slider>();
            serializedScene.FindProperty("_loadingText").objectReferenceValue = loadingPanel.transform.Find("LoadingText").GetComponent<Text>();
            
            if (input != null)
            {
                serializedScene.FindProperty("_inputReader").objectReferenceValue = input;
            }
            serializedScene.ApplyModifiedProperties();
        }

        private static void SetFullScreenRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.anchoredPosition3D = Vector3.zero;
        }
    }
}
