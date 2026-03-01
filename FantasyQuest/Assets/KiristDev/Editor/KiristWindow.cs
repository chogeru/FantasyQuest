using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kirist.EditorTool
{
    public enum RemovalMode
    {
        RemoveComponentsOnly,
        DeleteGameObjects
    }

    public enum ScriptSearchMode { Scene, Prefab }
    public enum ScriptSceneSearchScope { CurrentOpenScenes, SpecificScenesFromFolder }
    public enum MaterialSearchMode { Scene, Prefab }
    public enum MaterialSceneSearchScope { CurrentOpenScenes, SpecificScenesFromFolder }
    public enum PrefabSearchMode { Scene, Prefab }
    public enum PrefabSceneSearchScope { CurrentOpenScenes, SpecificScenesFromFolder }
    public enum PrefabRemovalMode { UnpackPrefab, DeleteGameObject }

    public partial class KiristWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private readonly string[] tabNames = { "MISSING SCRIPT FINDER", "MISSING MATERIAL FIND", "MISSING PREFAB FINDER", "PREFAB ANALYZER", "SCENE ANALYZER", "ADDRESSABLE HELPER" };
        
        public enum BackgroundColorTheme
        {
            Default,
            Green,
            Blue,
            Purple,
            Orange,
            Red,
            Teal
        }
        
        public BackgroundColorTheme selectedBackgroundTheme = BackgroundColorTheme.Default;
        
        private MissingScriptFinder missingScriptFinder;
        private MissingMaterialFinder missingMaterialFinder;
        private MissingPrefabFinder missingPrefabFinder;
        private PrefabAnalyzer prefabAnalyzer;
        private SceneAnalyzer sceneAnalyzer;
        private AddressableHelper addressableHelper;

        public ScriptSearchMode scriptSearchMode = ScriptSearchMode.Scene;
        public ScriptSceneSearchScope scriptSceneSearchScope = ScriptSceneSearchScope.CurrentOpenScenes;
        public MaterialSearchMode materialSearchMode = MaterialSearchMode.Scene;
        public MaterialSceneSearchScope materialSceneSearchScope = MaterialSceneSearchScope.CurrentOpenScenes;
        public PrefabSearchMode prefabSearchMode = PrefabSearchMode.Scene;
        public PrefabSceneSearchScope prefabSceneSearchScope = PrefabSceneSearchScope.CurrentOpenScenes;

        private List<MissingScriptInfo> missingScriptResults = new List<MissingScriptInfo>();
        private bool showResults = false;
        private RemovalMode removalMode = RemovalMode.RemoveComponentsOnly;
        private List<MissingMaterialInfo> missingMaterialResults = new List<MissingMaterialInfo>();
        private bool showMaterialResults = false;
        private RemovalMode materialRemovalMode = RemovalMode.RemoveComponentsOnly;

        private List<MissingPrefabInfo> missingPrefabResults = new List<MissingPrefabInfo>();
        private bool showPrefabResults = false;
        private PrefabRemovalMode prefabRemovalMode = PrefabRemovalMode.UnpackPrefab;

        private List<GameObject> prefabPrefabsInFolder = new List<GameObject>();
        private List<bool> selectedPrefabPrefabs = new List<bool>();
        private string selectedPrefabPrefabFolder = "";

        private List<SceneAsset> prefabScenesInFolder = new List<SceneAsset>();
        private List<bool> selectedPrefabScenes = new List<bool>();
        private string selectedPrefabSceneFolder = "";
        private Vector2 prefabSceneListScrollPos;

        private List<GameObject> materialPrefabsInFolder = new List<GameObject>();
        private List<bool> selectedMaterialPrefabs = new List<bool>();
        private string selectedMaterialPrefabFolder = "";
        private Vector2 materialPrefabListScrollPos;

        private List<SceneAsset> materialScenesInFolder = new List<SceneAsset>();
        private List<bool> selectedMaterialScenes = new List<bool>();
        private string selectedMaterialSceneFolder = "";
        private Vector2 materialSceneListScrollPos;

        [MenuItem("Tools/Kirist/Open Kirist Window %#k")]
        public static void OpenWindow()
        {
            var window = GetWindow<KiristWindow>("Kirist Tool");
            window.minSize = new Vector2(500, 650);
            window.Show();
        }

        private void OnEnable()
        {
            missingScriptFinder = new MissingScriptFinder(this);
            missingMaterialFinder = new MissingMaterialFinder(this);
            missingPrefabFinder = new MissingPrefabFinder(this);
            prefabAnalyzer = new PrefabAnalyzer(this);
            sceneAnalyzer = new SceneAnalyzer(this);
            addressableHelper = new AddressableHelper(this);
        }

        private void OnGUI()
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;
            
            DrawGradientBackground();
            
            DrawHeader();
            DrawBackgroundColorPalette();
            DrawCustomTabs();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            switch (selectedTab)
            {
                case 0:
                    missingScriptFinder?.DrawUI();
                    break;
                case 1:
                    missingMaterialFinder?.DrawUI();
                    break;
                case 2:
                    missingPrefabFinder?.DrawUI();
                    break;
                case 3:
                    prefabAnalyzer?.DrawUI();
                    break;
                case 4:
                    sceneAnalyzer?.DrawUI();
                    break;
                case 5:
                    addressableHelper?.DrawUI();
                    break;
            }

            EditorGUILayout.EndScrollView();
            
            GUI.backgroundColor = originalColor;
        }
        
        private void DrawGradientBackground()
        {
            float height = position.height;
            int steps = 20;
            
            var backgroundColor = GetBackgroundColor();
            var darkerColor = backgroundColor * 0.7f;
            
            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / (steps - 1);
                float y = (height / steps) * i;
                float stepHeight = height / steps;
                
                Color currentColor = Color.Lerp(backgroundColor, darkerColor, t);
                EditorGUI.DrawRect(new Rect(0, y, position.width, stepHeight), currentColor);
            }
        }
        
        private Color GetBackgroundColor()
        {
            switch (selectedBackgroundTheme)
            {
                case BackgroundColorTheme.Default:
                    return new Color(0.22f, 0.22f, 0.22f, 1f);
                case BackgroundColorTheme.Green:
                    return new Color(0.2f, 0.3f, 0.22f, 1f);
                case BackgroundColorTheme.Blue:
                    return new Color(0.18f, 0.25f, 0.35f, 1f);
                case BackgroundColorTheme.Purple:
                    return new Color(0.3f, 0.2f, 0.35f, 1f);
                case BackgroundColorTheme.Orange:
                    return new Color(0.35f, 0.25f, 0.15f, 1f);
                case BackgroundColorTheme.Red:
                    return new Color(0.35f, 0.18f, 0.18f, 1f);
                case BackgroundColorTheme.Teal:
                    return new Color(0.15f, 0.3f, 0.3f, 1f);
                default:
                    return new Color(0.15f, 0.25f, 0.20f, 1f);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(15);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 1f, 1f) }
            };
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🔧 KIRIST DEV TOOL", titleStyle);
            EditorGUILayout.Space(5);
            
            var subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.4f, 0.4f, 0.4f, 1f) }
            };
            EditorGUILayout.LabelField("Let's treat our precious assets with care.", subtitleStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);
        }
        
        private void DrawCustomTabs()
        {
            EditorGUILayout.Space(10);
            
            var tabContainerStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = CreateGradientTexture(new Color(0.2f, 0.2f, 0.2f, 0.8f), new Color(0.15f, 0.15f, 0.15f, 0.8f)) },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(5, 5, 5, 5)
            };
            
            EditorGUILayout.BeginVertical(tabContainerStyle);
            EditorGUILayout.BeginHorizontal();
            
            for (int i = 0; i < tabNames.Length; i++)
            {
                bool isSelected = selectedTab == i;
                DrawTabButton(i, tabNames[i], isSelected);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawTabButton(int tabIndex, string tabName, bool isSelected)
        {
            Color tabColor;
            Color textColor;
            
            switch (tabIndex)
            {
                case 0:
                    tabColor = isSelected ? new Color(0.2f, 0.6f, 0.4f, 1f) : new Color(0.15f, 0.4f, 0.25f, 0.7f);
                    textColor = isSelected ? Color.white : new Color(0.8f, 1f, 0.9f, 1f);
                    break;
                case 1:
                    tabColor = isSelected ? new Color(0.6f, 0.3f, 0.8f, 1f) : new Color(0.4f, 0.2f, 0.5f, 0.7f);
                    textColor = isSelected ? Color.white : new Color(1f, 0.8f, 1f, 1f);
                    break;
                case 2:
                    tabColor = isSelected ? new Color(0.9f, 0.5f, 0.2f, 1f) : new Color(0.6f, 0.3f, 0.15f, 0.7f);
                    textColor = isSelected ? Color.white : new Color(1f, 0.9f, 0.7f, 1f);
                    break;
                case 3:
                    tabColor = isSelected ? new Color(0.2f, 0.7f, 0.7f, 1f) : new Color(0.15f, 0.5f, 0.5f, 0.7f);
                    textColor = isSelected ? Color.white : new Color(0.8f, 1f, 1f, 1f);
                    break;
                case 4:
                    tabColor = isSelected ? new Color(0.2f, 0.8f, 0.6f, 1f) : new Color(0.15f, 0.6f, 0.4f, 0.7f);
                    textColor = isSelected ? Color.white : new Color(0.8f, 1f, 0.9f, 1f);
                    break;
                case 5:
                    tabColor = isSelected ? new Color(0.8f, 0.4f, 0.2f, 1f) : new Color(0.6f, 0.3f, 0.1f, 0.7f);
                    textColor = isSelected ? Color.white : new Color(1f, 0.9f, 0.8f, 1f);
                    break;
                default:
                    tabColor = isSelected ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0.3f, 0.3f, 0.3f, 0.7f);
                    textColor = isSelected ? Color.white : new Color(0.9f, 0.9f, 0.9f, 1f);
                    break;
            }
            
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(2, 2, 2, 2),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { 
                    background = CreateRoundedRectTexture(120, 35, tabColor),
                    textColor = textColor
                },
                hover = { 
                    background = CreateRoundedRectTexture(120, 35, tabColor * 1.1f),
                    textColor = textColor
                },
                active = { 
                    background = CreateRoundedRectTexture(120, 35, tabColor * 0.9f),
                    textColor = textColor
                }
            };
            
            if (GUILayout.Button(tabName, buttonStyle, GUILayout.Width(120), GUILayout.Height(35)))
            {
                selectedTab = tabIndex;
            }
        }
        

        private Texture2D CreateGradientTexture(Color topColor, Color bottomColor)
        {
            var texture = new Texture2D(1, 2);
            texture.SetPixel(0, 0, topColor);
            texture.SetPixel(0, 1, bottomColor);
            texture.Apply();
            return texture;
        }
        
        private void DrawBackgroundColorPalette()
        {
            var buttonSize = 18f;
            var spacing = 6f;
            var totalWidth = (buttonSize + spacing) * 7 - spacing;
            var startX = position.width - totalWidth - 15;
            var y = 15;
            
            var themes = new[]
            {
                (BackgroundColorTheme.Default, new Color(0.4f, 0.4f, 0.4f, 1f)),
                (BackgroundColorTheme.Green, new Color(0.3f, 0.8f, 0.4f, 1f)),
                (BackgroundColorTheme.Blue, new Color(0.3f, 0.6f, 0.9f, 1f)),
                (BackgroundColorTheme.Purple, new Color(0.7f, 0.4f, 0.9f, 1f)),
                (BackgroundColorTheme.Orange, new Color(1f, 0.6f, 0.2f, 1f)),
                (BackgroundColorTheme.Red, new Color(0.9f, 0.3f, 0.3f, 1f)),
                (BackgroundColorTheme.Teal, new Color(0.2f, 0.8f, 0.8f, 1f))
            };
            
            for (int i = 0; i < themes.Length; i++)
            {
                var theme = themes[i].Item1;
                var color = themes[i].Item2;
                var x = startX + i * (buttonSize + spacing);
                
                DrawColorButton(new Rect(x, y, buttonSize, buttonSize), theme, color);
            }
        }
        
        
        private void DrawColorButton(Rect rect, BackgroundColorTheme theme, Color color)
        {
            bool isSelected = selectedBackgroundTheme == theme;
            
            var buttonStyle = new GUIStyle();
            buttonStyle.normal.background = CreateRoundedRectTexture(rect.width, rect.height, isSelected ? color : color * 0.9f);
            buttonStyle.hover.background = CreateRoundedRectTexture(rect.width, rect.height, color * 0.98f);
            buttonStyle.active.background = CreateRoundedRectTexture(rect.width, rect.height, color * 1.02f);
            
            if (isSelected)
            {
                var borderColor = Color.white;
                var borderRect = new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4);
                EditorGUI.DrawRect(borderRect, borderColor);
            }
            
            if (GUI.Button(rect, "", buttonStyle))
            {
                selectedBackgroundTheme = theme;
            }
            
            if (theme == BackgroundColorTheme.Default)
            {
                var iconStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                
                EditorGUI.LabelField(rect, "U", iconStyle);
            }
        }
        
        private Texture2D CreateRoundedRectTexture(float width, float height, Color color)
        {
            int w = Mathf.RoundToInt(width);
            int h = Mathf.RoundToInt(height);
            var texture = new Texture2D(w, h);
            var radius = Mathf.Min(w, h) * 0.2f;
            
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    bool inRect = IsPointInRoundedRect(x, y, w, h, radius);
                    texture.SetPixel(x, y, inRect ? color : Color.clear);
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        private bool IsPointInRoundedRect(int x, int y, int width, int height, float radius)
        {
            if (x < radius || x > width - radius)
            {
                if (y < radius || y > height - radius)
                {
                    var cornerX = x < radius ? radius : width - radius;
                    var cornerY = y < radius ? radius : height - radius;
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY));
                    return distance <= radius;
                }
            }
            return true;
        }

        private void DrawAddressableAutoGrouperTab()
        {
            var tealBackgroundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = CreateGradientTexture(new Color(0.25f, 0.35f, 0.35f, 1f), new Color(0.30f, 0.40f, 0.40f, 1f)) },
                border = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(15, 15, 15, 15)
            };
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginVertical(tealBackgroundStyle);
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.6f, 1f, 1f, 1f) }
            };
            EditorGUILayout.LabelField("📦 Addressable Auto Grouper", titleStyle);
            EditorGUILayout.Space(5);
            
            var contentStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = CreateGradientTexture(new Color(0.35f, 0.45f, 0.45f, 1f), new Color(0.40f, 0.50f, 0.50f, 1f)) },
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(2, 2, 2, 2)
            };
            EditorGUILayout.BeginVertical(contentStyle);
            
            var helpBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = new Color(0.8f, 0.9f, 0.9f, 1f) },
                fontSize = 12
            };
            EditorGUILayout.HelpBox("This feature is coming soon!", MessageType.Info);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }

        #region MISSING MATERIAL FINDER
        #endregion

        [System.Serializable]
        private class MissingScriptInfo
        {
            public GameObject gameObject;
            public int componentIndex;
            public string sceneName;
            public string scenePath;
            public string assetPath;
            public string gameObjectName;
            public string instanceID;
            public string componentTypeName;
        }

        [System.Serializable]
        private class MissingMaterialInfo
        {
            public GameObject gameObject;
            public Component component;
            public string propertyPath;
            public string sceneName;
            public string scenePath;
            public string assetPath;
            public string gameObjectName;
            public string instanceID;
            public string componentTypeName;
            public Material material;
            public string materialName;
            public string shaderName;
            public string errorReason;
            public int materialIndex;
            public Sprite sprite;
            public string spriteName;
        }

        [System.Serializable]
        private class MissingPrefabInfo
        {
            public GameObject gameObject;
            public string sceneName;
            public string assetPath;
            public string gameObjectName;
            public string instanceID;
            public string prefabPath;
            public string errorReason;
        }
    }
}