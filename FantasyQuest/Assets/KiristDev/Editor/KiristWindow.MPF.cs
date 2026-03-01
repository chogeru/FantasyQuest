using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kirist.EditorTool
{
    public partial class KiristWindow
    {
        public class MissingPrefabFinder : BaseFinderBehaviour
        {
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
            
            private Vector2 prefabResultsScrollPos = Vector2.zero;
            private int selectedPrefabResultIndex = -1;
            private int visibleItemsCount = 50;
            private int scrollOffset = 0;
            
            public MissingPrefabFinder(KiristWindow parent) : base(parent)
            {
            }
            
            public override void DrawUI()
            {
                var orangeBackgroundStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = CreateGradientTexture(new Color(0.35f, 0.30f, 0.25f, 1f), new Color(0.40f, 0.35f, 0.30f, 1f)) },
                    border = new RectOffset(3, 3, 3, 3),
                    padding = new RectOffset(15, 15, 15, 15)
                };
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(orangeBackgroundStyle);
                
                DrawTitle("📦 Missing Prefab Finder");
                EditorGUILayout.Space(5);
                
                var orangeSectionStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = CreateGradientTexture(new Color(0.40f, 0.35f, 0.30f, 1f), new Color(0.45f, 0.40f, 0.35f, 1f)) },
                    border = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(8, 8, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2)
                };
                EditorGUILayout.BeginVertical(orangeSectionStyle);
                EditorGUILayout.BeginHorizontal();
                var searchModeStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = UIColors.ModernOrange }, fontStyle = FontStyle.Bold };
                GUILayout.Label("🔍 Search Target:", searchModeStyle, GUILayout.Width(100));
                parentWindow.prefabSearchMode = (PrefabSearchMode)EditorGUILayout.EnumPopup(parentWindow.prefabSearchMode, GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                
                if (parentWindow.prefabSearchMode == PrefabSearchMode.Scene)
                {
                    DrawSceneSearchMode();
                }
                else
                {
                    DrawPrefabSearchMode();
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginVertical(orangeSectionStyle);
                DrawSearchButton();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                
                if (showPrefabResults)
                {
                    DrawResults();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
                EditorGUILayout.EndVertical();
            }
            
            private Texture2D CreateGradientTexture(Color topColor, Color bottomColor)
            {
                var texture = new Texture2D(1, 2);
                texture.SetPixel(0, 0, topColor);
                texture.SetPixel(0, 1, bottomColor);
                texture.Apply();
                return texture;
            }
            
            private void DrawSceneSearchMode()
            {
                EditorGUILayout.BeginHorizontal();
                var scopeStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = UIColors.ModernGreen }, fontStyle = FontStyle.Bold };
                GUILayout.Label("🎬 Scene Scope:", scopeStyle, GUILayout.Width(100));
                parentWindow.prefabSceneSearchScope = (PrefabSceneSearchScope)EditorGUILayout.EnumPopup(parentWindow.prefabSceneSearchScope, GUILayout.Width(180));
                EditorGUILayout.EndHorizontal();
                
                if (parentWindow.prefabSceneSearchScope == PrefabSceneSearchScope.SpecificScenesFromFolder)
                {
                    EditorGUILayout.Space(3);
                    DrawSceneFolderSelection();
                }
            }
            
            private void DrawPrefabSearchMode()
            {
                DrawPrefabFolderSelection();
                if (prefabPrefabsInFolder.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    DrawPrefabList();
                }
            }
            
            private void DrawSceneFolderSelection()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Scene Folder:", GUILayout.Width(80));
                GUILayout.Label(string.IsNullOrEmpty(selectedPrefabSceneFolder) ? "None Selected" : selectedPrefabSceneFolder, EditorStyles.textField);
                
                if (GUILayout.Button("📁 Select", UIStyles.ButtonStyle, GUILayout.Width(80)))
                {
                    SelectPrefabSceneFolder();
                }
                EditorGUILayout.EndHorizontal();
                
                if (prefabScenesInFolder.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    DrawSceneList();
                }
            }
            
            private void DrawPrefabFolderSelection()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Prefab Folder:", GUILayout.Width(80));
                GUILayout.Label(string.IsNullOrEmpty(selectedPrefabPrefabFolder) ? "None Selected" : selectedPrefabPrefabFolder, EditorStyles.textField);
                
                if (GUILayout.Button("📁 Select", UIStyles.ButtonStyle, GUILayout.Width(80)))
                {
                    SelectPrefabPrefabFolder();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            private void DrawPrefabList()
            {
                EditorGUILayout.BeginVertical(UIStyles.CardStyle);
                GUILayout.Label($"📦 Error Prefabs in Folder ({prefabPrefabsInFolder.Count}):", EditorStyles.boldLabel);
                
                prefabSceneListScrollPos = EditorGUILayout.BeginScrollView(prefabSceneListScrollPos, GUILayout.MaxHeight(150));
                
                for (int i = 0; i < prefabPrefabsInFolder.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    selectedPrefabPrefabs[i] = EditorGUILayout.Toggle(selectedPrefabPrefabs[i], GUILayout.Width(20));
                    GUILayout.Label(prefabPrefabsInFolder[i].name, EditorStyles.label);
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            
            private void DrawSceneList()
            {
                EditorGUILayout.BeginVertical(UIStyles.CardStyle);
                GUILayout.Label($"🎬 Scenes in Folder ({prefabScenesInFolder.Count}):", EditorStyles.boldLabel);
                
                prefabSceneListScrollPos = EditorGUILayout.BeginScrollView(prefabSceneListScrollPos, GUILayout.MaxHeight(150));
                
                for (int i = 0; i < prefabScenesInFolder.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    selectedPrefabScenes[i] = EditorGUILayout.Toggle(selectedPrefabScenes[i], GUILayout.Width(20));
                    GUILayout.Label(prefabScenesInFolder[i].name, EditorStyles.label);
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            
            private void DrawSearchButton()
            {
                EditorGUILayout.BeginHorizontal();
                var removalStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = UIColors.ModernPurple }, fontStyle = FontStyle.Bold };
                GUILayout.Label("⚙️ Action Mode:", removalStyle, GUILayout.Width(100));
                prefabRemovalMode = (PrefabRemovalMode)EditorGUILayout.EnumPopup(prefabRemovalMode, GUILayout.Width(180));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                if (parentWindow.prefabSearchMode == PrefabSearchMode.Prefab)
                {
                    if (prefabPrefabsInFolder.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Select a prefab folder first", MessageType.Warning);
                    }
                }
                
                EditorGUILayout.Space(3);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("🔍 FIND MISSING PREFABS", UIStyles.SearchButtonStyle, GUILayout.Width(200), GUILayout.Height(30)))
                {
                    FindMissingPrefabs();
                }
                
                GUILayout.Space(10);
                
                if (GUILayout.Button("⚡ ANALYZE CURRENT SCENE", UIStyles.SearchButtonStyle, GUILayout.Width(200), GUILayout.Height(30)))
                {
                    FindMissingPrefabsInCurrentScene();
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            private void DrawResults()
            {
                EditorGUILayout.BeginVertical(UIStyles.GradientBackgroundStyle);
                
                var titleStyle = new GUIStyle(EditorStyles.boldLabel) 
                { 
                    fontSize = 16, 
                    fontStyle = FontStyle.Bold, 
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UIColors.ModernBlue }
                };
                EditorGUILayout.LabelField($"📊 Search Results ({missingPrefabResults.Count} found)", titleStyle);
                EditorGUILayout.Space(5);
                
                if (missingPrefabResults.Count > 0)
                {
                    Rect removeAllRect = EditorGUILayout.GetControlRect(false, 30);
                    string removeAllText = prefabRemovalMode == PrefabRemovalMode.DeleteGameObject ? 
                        "💥 Delete All GameObjects with Missing Prefabs" : 
                        "📦 Unpack All Missing Prefabs";
                    
                    if (DrawLargeStyledButton(removeAllRect, removeAllText, UIColors.Danger))
                    {
                        string dialogTitle = prefabRemovalMode == PrefabRemovalMode.DeleteGameObject ? 
                            "Delete All GameObjects" : 
                            "Unpack All Missing Prefabs";
                        string dialogMessage = prefabRemovalMode == PrefabRemovalMode.DeleteGameObject ?
                            $"⚠️ WARNING: This will DELETE {missingPrefabResults.Count} GameObject(s) with missing prefabs!\n\nThis action cannot be undone!" :
                            $"Are you sure you want to unpack {missingPrefabResults.Count} missing prefab(s) to regular GameObjects?";
                            
                        if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
                        {
                            RemoveAllMissingPrefabs();
                        }
                    }
                    
                    EditorGUILayout.Space(10);
                    DrawResultsList();
                }
                else
                {
                    DrawHelpBox("✅ No missing prefabs found!", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawResultsList()
            {
                DrawScrollControls($"Found {missingPrefabResults.Count} items", 
                    selectedPrefabResultIndex >= 0 && selectedPrefabResultIndex < missingPrefabResults.Count ? 
                    () => FindInScene(missingPrefabResults[selectedPrefabResultIndex].gameObject, 
                        missingPrefabResults[selectedPrefabResultIndex].sceneName, "Missing Prefab Finder") : null);
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical(GUILayout.Width(300));
                if (selectedPrefabResultIndex >= 0 && selectedPrefabResultIndex < missingPrefabResults.Count)
                {
                    DrawPrefabResultDetail(missingPrefabResults[selectedPrefabResultIndex]);
                }
                else
                {
                    EditorGUILayout.LabelField("Select an item from the list to view details", EditorStyles.helpBox);
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.BeginVertical();
                GUILayout.Label("📋 Items List:", EditorStyles.boldLabel);
                
                prefabResultsScrollPos = EditorGUILayout.BeginScrollView(prefabResultsScrollPos, GUILayout.Height(200));
                
                EditorGUILayout.BeginHorizontal();
                
                for (int i = 0; i < missingPrefabResults.Count; i++)
                {
                    DrawPrefabResultCard(missingPrefabResults[i], i);
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
            }
            
            private void DrawPrefabResultCard(MissingPrefabInfo result, int index)
            {
                bool isSelected = selectedPrefabResultIndex == index;
                
                EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200), GUILayout.Height(150));
                
                if (isSelected)
                {
                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 150), UIColors.Primary * 0.2f);
                }
                
                var nameStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = UIColors.Dark }
                };
                GUILayout.Label($"🎯 {result.gameObjectName}", nameStyle, GUILayout.Height(30));
                
                var sceneStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = UIColors.Info }
                };
                GUILayout.Label($"📍 {result.sceneName}", sceneStyle, GUILayout.Height(20));
                
                GUILayout.Label($"❌ {result.errorReason}", sceneStyle, GUILayout.Height(20));
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button(isSelected ? "✅ Selected" : "Select", UIStyles.ButtonStyle, GUILayout.Height(25)))
                {
                    selectedPrefabResultIndex = index;
                }
                
                if (result.gameObject != null)
                {
                    string buttonText = prefabRemovalMode == PrefabRemovalMode.DeleteGameObject ? 
                        "💥 Delete" : "📦 Unpack";
                    if (GUILayout.Button(buttonText, UIStyles.ButtonStyle, GUILayout.Height(25)))
                    {
                        string dialogTitle = prefabRemovalMode == PrefabRemovalMode.DeleteGameObject ? 
                            "Delete Missing Prefab" : "Unpack Missing Prefab";
                        string dialogMessage = prefabRemovalMode == PrefabRemovalMode.DeleteGameObject ?
                            $"Delete '{result.gameObjectName}'?" :
                            $"Unpack '{result.gameObjectName}' to regular GameObject?";
                        
                        if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
                        {
                            if (RemoveMissingPrefab(result))
                            {
                                if (selectedPrefabResultIndex >= missingPrefabResults.Count)
                                    selectedPrefabResultIndex = missingPrefabResults.Count - 1;
                            }
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawPrefabResultDetail(MissingPrefabInfo result)
            {
                GUILayout.Label("📄 Details:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginVertical(UIStyles.CardStyle);
                
                EditorGUILayout.LabelField("🎯 GameObject:", result.gameObjectName, EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("📍 Scene:", result.sceneName);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField("🔗 Prefab Path:", result.prefabPath);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField("❌ Error:", result.errorReason);
                EditorGUILayout.Space(5);
                
                if (result.gameObject != null)
                {
                    if (GUILayout.Button("🔍 Find in Scene", UIStyles.ButtonStyle, GUILayout.Height(30)))
                    {
                        FindInScene(result.gameObject, result.sceneName, "Missing Prefab Finder");
                    }
                }
                
                EditorGUILayout.Space(5);
                
                if (result.gameObject != null)
                {
                    Rect buttonRect = EditorGUILayout.GetControlRect(false, 40);
                    string buttonText = prefabRemovalMode == PrefabRemovalMode.DeleteGameObject ? 
                        "💥 Delete GameObject" : "📦 Unpack Prefab";
                    
                    if (DrawLargeStyledButton(buttonRect, buttonText, UIColors.Danger))
                    {
                        string dialogTitle = "Unpack Missing Prefab";
                        string dialogMessage = $"Are you sure you want to unpack the missing prefab '{result.gameObjectName}'?\n\nPrefab: {result.prefabPath}\nError: {result.errorReason}";
                        
                        if (prefabRemovalMode == PrefabRemovalMode.DeleteGameObject)
                        {
                            if (PrefabUtility.IsPartOfPrefabAsset(result.gameObject))
                            {
                                dialogTitle = "Delete Prefab File";
                                dialogMessage = $"⚠️ WARNING: This will DELETE the entire prefab file!\n\nPrefab: {result.gameObjectName}\n\nThis action cannot be undone!";
                            }
                            else
                            {
                                dialogTitle = "Delete GameObject";
                                dialogMessage = $"⚠️ WARNING: This will DELETE the entire GameObject!\n\nGameObject: {result.gameObjectName}\n\nThis action cannot be undone!";
                            }
                        }
                        else
                        {
                            dialogMessage = $"This will convert the missing prefab to a regular GameObject, keeping all components and children.\n\nPrefab: {result.prefabPath}\nError: {result.errorReason}";
                        }
                        
                        if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
                        {
                            if (RemoveMissingPrefab(result))
                            {
                                if (selectedPrefabResultIndex >= missingPrefabResults.Count)
                                    selectedPrefabResultIndex = missingPrefabResults.Count - 1;
                            }
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            public override void ClearResults()
            {
                missingPrefabResults.Clear();
                showPrefabResults = false;
                selectedPrefabResultIndex = -1;
            }
            
            private void SelectPrefabPrefabFolder()
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Prefab Folder", "Assets", "");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    selectedPrefabPrefabFolder = folderPath;
                    LoadPrefabPrefabsFromFolder(folderPath);
                }
            }
            
            private void SelectPrefabSceneFolder()
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Scene Folder", "Assets", "");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    selectedPrefabSceneFolder = folderPath;
                    LoadPrefabScenesFromFolder(folderPath);
                }
            }
            
            private void LoadPrefabPrefabsFromFolder(string folderPath)
            {
                prefabPrefabsInFolder.Clear();
                selectedPrefabPrefabs.Clear();
                
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        prefabPrefabsInFolder.Add(prefab);
                        selectedPrefabPrefabs.Add(false);
                    }
                }
            }
            
            private void LoadPrefabScenesFromFolder(string folderPath)
            {
                prefabScenesInFolder.Clear();
                selectedPrefabScenes.Clear();
                
                string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    if (scene != null)
                    {
                        prefabScenesInFolder.Add(scene);
                        selectedPrefabScenes.Add(false);
                    }
                }
            }
            
            private void FindMissingPrefabs()
            {
                missingPrefabResults.Clear();
                showPrefabResults = true;
                selectedPrefabResultIndex = -1;
                
                if (parentWindow.prefabSearchMode == PrefabSearchMode.Scene)
                {
                    FindMissingPrefabsInScenes();
                }
                else
                {
                    FindMissingPrefabsInPrefabs();
                }
            }
            
            private void FindMissingPrefabsInCurrentScene()
            {
                missingPrefabResults.Clear();
                showPrefabResults = true;
                selectedPrefabResultIndex = -1;
                
                LogInfo("Starting Missing Prefab Analysis in Current Scene...");
                
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    if (scene.IsValid())
                    {
                        LogInfo($"Analyzing scene: {scene.name}");
                        FindMissingPrefabsInScene(scene);
                    }
                }
                
                LogInfo($"Missing Prefab Analysis completed. Found {missingPrefabResults.Count} missing prefabs.");
            }
            
            private void FindMissingPrefabsInScenes()
            {
                if (parentWindow.prefabSceneSearchScope == PrefabSceneSearchScope.CurrentOpenScenes)
                {
                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                    {
                        var scene = EditorSceneManager.GetSceneAt(i);
                        if (scene.IsValid())
                        {
                            FindMissingPrefabsInScene(scene);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < selectedPrefabScenes.Count; i++)
                    {
                        if (selectedPrefabScenes[i])
                        {
                            var scenePath = AssetDatabase.GetAssetPath(prefabScenesInFolder[i]);
                            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                            FindMissingPrefabsInScene(scene);
                        }
                    }
                }
            }
            
            private void FindMissingPrefabsInPrefabs()
            {
                for (int i = 0; i < selectedPrefabPrefabs.Count; i++)
                {
                    if (selectedPrefabPrefabs[i])
                    {
                        FindMissingPrefabsInPrefab(prefabPrefabsInFolder[i]);
                    }
                }
            }
            
            private void FindMissingPrefabsInScene(Scene scene)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    CheckGameObjectForMissingPrefabs(obj, scene.name, scene.path);
                }
            }
            
            private void FindMissingPrefabsInPrefab(GameObject prefab)
            {
                CheckGameObjectForMissingPrefabs(prefab, "Prefab", AssetDatabase.GetAssetPath(prefab));
            }
            
            private void CheckGameObjectForMissingPrefabs(GameObject obj, string sceneName, string assetPath)
            {
                if (PrefabUtility.IsPrefabAssetMissing(obj))
                {
                    AddMissingPrefabInfo(obj, sceneName, assetPath, "Missing Prefab Asset");
                }
                else if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabAsset == null)
                    {
                        AddMissingPrefabInfo(obj, sceneName, assetPath, "Broken Prefab Instance");
                    }
                }
                
                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForMissingPrefabs(child.gameObject, sceneName, assetPath);
                }
            }
            
            private void AddMissingPrefabInfo(GameObject obj, string sceneName, string assetPath, string errorReason)
            {
                var newInfo = new MissingPrefabInfo
                {
                    gameObject = obj,
                    sceneName = sceneName,
                    assetPath = assetPath,
                    gameObjectName = obj.name,
                    instanceID = obj.GetInstanceID().ToString(),
                    prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj),
                    errorReason = errorReason
                };
                
                missingPrefabResults.Add(newInfo);
                LogInfo($"Found missing prefab: {obj.name} - {errorReason}");
            }
            
            private bool RemoveMissingPrefab(MissingPrefabInfo prefabInfo)
            {
                if (prefabInfo.gameObject != null)
                {
                    LogInfo($"Attempting to remove missing prefab from {prefabInfo.gameObject.name}");
                    
                    if (prefabRemovalMode == PrefabRemovalMode.DeleteGameObject)
                    {
                        return DeleteGameObjectWithMissingPrefab(prefabInfo);
                    }
                    
                    if (PrefabUtility.IsPartOfPrefabAsset(prefabInfo.gameObject))
                    {
                        return DeletePrefabFile(prefabInfo);
                    }
                    
                    bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(prefabInfo.gameObject);
                    
                    if (isPrefabInstance)
                    {
                        LogInfo($"Unpacking prefab instance: {prefabInfo.gameObject.name}");
                        PrefabUtility.UnpackPrefabInstance(prefabInfo.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                    }
                    
                    if (PrefabUtility.IsPrefabAssetMissing(prefabInfo.gameObject))
                    {
                        return UnpackMissingPrefab(prefabInfo);
                    }
                    else
                    {
                        PrefabUtility.UnpackPrefabInstance(prefabInfo.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                    }
                    
                    missingPrefabResults.Remove(prefabInfo);
                    
                    LogInfo($"Successfully removed missing prefab from {prefabInfo.gameObject.name}");
                    return true;
                }
                
                return false;
            }
            
            private bool DeleteGameObjectWithMissingPrefab(MissingPrefabInfo prefabInfo)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(prefabInfo.gameObject))
                {
                    return DeletePrefabFile(prefabInfo);
                }
                else
                {
                    if (prefabInfo.gameObject.scene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(prefabInfo.gameObject.scene);
                    }
                    GameObject.DestroyImmediate(prefabInfo.gameObject);
                    missingPrefabResults.Remove(prefabInfo);
                    LogInfo($"Deleted GameObject with missing prefab: {prefabInfo.gameObjectName}");
                    return true;
                }
            }
            
            private bool DeletePrefabFile(MissingPrefabInfo prefabInfo)
            {
                string assetPath = AssetDatabase.GetAssetPath(prefabInfo.gameObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    missingPrefabResults.Remove(prefabInfo);
                    LogInfo($"Deleted prefab file: {assetPath}");
                    return true;
                }
                return false;
            }
            
            private bool UnpackMissingPrefab(MissingPrefabInfo prefabInfo)
            {
                try
                {
                    LogInfo($"Unpacking missing prefab: {prefabInfo.gameObject.name}");
                    
                    if (PrefabUtility.IsPartOfPrefabInstance(prefabInfo.gameObject))
                    {
                        PrefabUtility.UnpackPrefabInstance(prefabInfo.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                    }
                    
                    if (prefabInfo.gameObject.scene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(prefabInfo.gameObject.scene);
                    }
                    
                    missingPrefabResults.Remove(prefabInfo);
                    
                    LogInfo($"Successfully unpacked missing prefab: {prefabInfo.gameObject.name}");
                    return true;
                }
                catch (System.Exception e)
                {
                    LogError($"Error unpacking missing prefab {prefabInfo.gameObject.name}: {e.Message}");
                    return false;
                }
            }
            
            private void RemoveAllMissingPrefabs()
            {
                for (int i = missingPrefabResults.Count - 1; i >= 0; i--)
                {
                    RemoveMissingPrefab(missingPrefabResults[i]);
                }
                
                if (missingPrefabResults.Count == 0)
                {
                    showPrefabResults = false;
                    selectedPrefabResultIndex = -1;
                }
            }
        }
    }
}