using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kirist.EditorTool
{
    public partial class KiristWindow : EditorWindow
    {
        public class MissingScriptFinder : BaseFinderBehaviour
        {
            private List<MissingScriptInfo> missingScriptResults = new List<MissingScriptInfo>();
            private bool showScriptResults = false;
            private RemovalMode removalMode = RemovalMode.RemoveComponentsOnly;
            
            private List<GameObject> prefabsInFolder = new List<GameObject>();
            private List<bool> selectedPrefabs = new List<bool>();
            private string selectedPrefabFolder = "";
            
            private List<SceneAsset> scenesInFolder = new List<SceneAsset>();
            private List<bool> selectedScenes = new List<bool>();
            private string selectedSceneFolder = "";
            private Vector2 sceneListScrollPos;
            
            private Vector2 resultsScrollPos = Vector2.zero;
            private int selectedScriptResultIndex = -1;
            private int scriptVisibleItemsCount = 50;
            private int scriptScrollOffset = 0;
            
            public MissingScriptFinder(KiristWindow parent) : base(parent)
            {
            }
            
            public override void DrawUI()
            {
                var greenBackgroundStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = CreateGradientTexture(new Color(0.25f, 0.35f, 0.30f, 1f), new Color(0.30f, 0.40f, 0.35f, 1f)) },
                    border = new RectOffset(3, 3, 3, 3),
                    padding = new RectOffset(15, 15, 15, 15)
                };
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(greenBackgroundStyle);
                
                DrawTitle("🔍 MISSING SCRIPT FINDER");
                EditorGUILayout.Space(5);
                
                var greenSectionStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = CreateGradientTexture(new Color(0.30f, 0.40f, 0.35f, 1f), new Color(0.35f, 0.45f, 0.40f, 1f)) },
                    border = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(8, 8, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2)
                };
                EditorGUILayout.BeginVertical(greenSectionStyle);
                EditorGUILayout.BeginHorizontal();
                var searchModeStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = UIColors.ModernGreen }, fontStyle = FontStyle.Bold };
                GUILayout.Label("🔍 Search Target:", searchModeStyle, GUILayout.Width(100));
                parentWindow.scriptSearchMode = (ScriptSearchMode)EditorGUILayout.EnumPopup(parentWindow.scriptSearchMode, GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                
                if (parentWindow.scriptSearchMode == ScriptSearchMode.Scene)
                {
                    DrawSceneSearchMode();
                }
                else
                {
                    DrawPrefabSearchMode();
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginVertical(greenSectionStyle);
                DrawSearchButton();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                
                if (showScriptResults)
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
                parentWindow.scriptSceneSearchScope = (ScriptSceneSearchScope)EditorGUILayout.EnumPopup(parentWindow.scriptSceneSearchScope, GUILayout.Width(180));
                EditorGUILayout.EndHorizontal();

                if (parentWindow.scriptSceneSearchScope == ScriptSceneSearchScope.SpecificScenesFromFolder)
                {
                    EditorGUILayout.Space(3);
                    DrawSceneFolderSelection();
                }
            }
            
            private void DrawPrefabSearchMode()
            {
                DrawPrefabFolderSelection();
                if (prefabsInFolder.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    DrawPrefabList();
                }
            }

            private void DrawSceneFolderSelection()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Scene Folder:", GUILayout.Width(80));
                GUILayout.Label(string.IsNullOrEmpty(selectedSceneFolder) ? "None Selected" : selectedSceneFolder, EditorStyles.textField);
                
                if (GUILayout.Button("📁 Select", UIStyles.ButtonStyle, GUILayout.Width(80)))
                {
                    SelectSceneFolder();
                }
                EditorGUILayout.EndHorizontal();

                if (scenesInFolder.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    DrawSceneList();
                }
            }

            private void DrawPrefabFolderSelection()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Prefab Folder:", GUILayout.Width(80));
                GUILayout.Label(string.IsNullOrEmpty(selectedPrefabFolder) ? "None Selected" : selectedPrefabFolder, EditorStyles.textField);
                
                if (GUILayout.Button("📁 Select", UIStyles.ButtonStyle, GUILayout.Width(80)))
                {
                    SelectPrefabFolder();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            private void DrawPrefabList()
            {
                EditorGUILayout.BeginVertical(UIStyles.CardStyle);
                GUILayout.Label($"📦 Error Prefabs in Folder ({prefabsInFolder.Count}):", EditorStyles.boldLabel);
                
                sceneListScrollPos = EditorGUILayout.BeginScrollView(sceneListScrollPos, GUILayout.MaxHeight(150));
                
                for (int i = 0; i < prefabsInFolder.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    selectedPrefabs[i] = EditorGUILayout.Toggle(selectedPrefabs[i], GUILayout.Width(20));
                    GUILayout.Label(prefabsInFolder[i].name, EditorStyles.label);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            
            private void DrawSceneList()
            {
                EditorGUILayout.BeginVertical(UIStyles.CardStyle);
                GUILayout.Label($"🎬 Scenes in Folder ({scenesInFolder.Count}):", EditorStyles.boldLabel);
                
                sceneListScrollPos = EditorGUILayout.BeginScrollView(sceneListScrollPos, GUILayout.MaxHeight(150));
                
                for (int i = 0; i < scenesInFolder.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    selectedScenes[i] = EditorGUILayout.Toggle(selectedScenes[i], GUILayout.Width(20));
                    GUILayout.Label(scenesInFolder[i].name, EditorStyles.label);
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
                removalMode = (RemovalMode)EditorGUILayout.EnumPopup(removalMode, GUILayout.Width(180));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (parentWindow.scriptSearchMode == ScriptSearchMode.Prefab)
                {
                    if (prefabsInFolder.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Select a prefab folder first", MessageType.Warning);
                    }
                }
                
                EditorGUILayout.Space(3);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("🔍 FIND MISSING SCRIPTS", UIStyles.SearchButtonStyle, GUILayout.Width(200), GUILayout.Height(30)))
                {
                    FindMissingScripts();
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
                EditorGUILayout.LabelField($"📊 Search Results ({missingScriptResults.Count} found)", titleStyle);
                EditorGUILayout.Space(5);
                
                if (missingScriptResults.Count > 0)
                {
                    Rect removeAllRect = EditorGUILayout.GetControlRect(false, 30);
                    string removeAllText = removalMode == RemovalMode.DeleteGameObjects ? 
                        "💥 Delete All GameObjects with Missing Scripts" : 
                        "🗑️ Remove All Missing Scripts";
                    
                    if (DrawLargeStyledButton(removeAllRect, removeAllText, UIColors.Danger))
                    {
                        string dialogTitle = removalMode == RemovalMode.DeleteGameObjects ? 
                            "Delete All GameObjects" : 
                            "Remove Missing Scripts";
                        string dialogMessage = removalMode == RemovalMode.DeleteGameObjects ?
                            $"⚠️ WARNING: This will DELETE {missingScriptResults.Count} GameObject(s) with missing scripts!\n\nThis action cannot be undone!" :
                            $"Are you sure you want to remove {missingScriptResults.Count} missing script reference(s)?";
                            
                        if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
                        {
                            RemoveAllMissingScripts();
                        }
                    }
                    
                    EditorGUILayout.Space(10);
                    DrawResultsList();
                }
                else
                {
                    DrawHelpBox("✅ No missing scripts found!", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }

            private void DrawResultsList()
            {
                DrawScrollControls($"Found {missingScriptResults.Count} items", 
                    selectedScriptResultIndex >= 0 && selectedScriptResultIndex < missingScriptResults.Count ? 
                    () => FindInScene(missingScriptResults[selectedScriptResultIndex].gameObject, 
                        missingScriptResults[selectedScriptResultIndex].gameObjectName,
                        missingScriptResults[selectedScriptResultIndex].sceneName, 
                        missingScriptResults[selectedScriptResultIndex].scenePath, "Missing Script Finder") : null);
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical(GUILayout.Width(300));
                if (selectedScriptResultIndex >= 0 && selectedScriptResultIndex < missingScriptResults.Count)
                {
                    DrawScriptResultDetail(missingScriptResults[selectedScriptResultIndex]);
                }
                else
                {
                    EditorGUILayout.LabelField("Select an item from the list to view details", EditorStyles.helpBox);
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.BeginVertical();
                GUILayout.Label("📋 Items List:", EditorStyles.boldLabel);
                
                resultsScrollPos = EditorGUILayout.BeginScrollView(resultsScrollPos, GUILayout.Height(200));
                
                EditorGUILayout.BeginHorizontal();
                
                for (int i = 0; i < missingScriptResults.Count; i++)
                {
                    DrawScriptResultCard(missingScriptResults[i], i);
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
            }
            
            private void DrawScriptResultCard(MissingScriptInfo result, int index)
            {
                bool isSelected = selectedScriptResultIndex == index;
                
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
                
                GUILayout.Label($"🔗 {result.componentTypeName}", sceneStyle, GUILayout.Height(20));
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button(isSelected ? "✅ Selected" : "Select", UIStyles.ButtonStyle, GUILayout.Height(25)))
                {
                    selectedScriptResultIndex = index;
                }
                
                if (result.gameObject != null)
                {
                    string buttonText = removalMode == RemovalMode.DeleteGameObjects ? 
                        "💥 Delete" : "🗑️ Remove";
                    if (GUILayout.Button(buttonText, UIStyles.ButtonStyle, GUILayout.Height(25)))
                    {
                        string dialogTitle = removalMode == RemovalMode.DeleteGameObjects ? 
                            "Delete Missing Script" : "Remove Missing Script";
                        string dialogMessage = removalMode == RemovalMode.DeleteGameObjects ?
                            $"Delete '{result.gameObjectName}'?" :
                            $"Remove missing script from '{result.gameObjectName}'?";
                        
                        if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
                        {
                            if (RemoveMissingScript(result))
                            {
                                if (selectedScriptResultIndex >= missingScriptResults.Count)
                                    selectedScriptResultIndex = missingScriptResults.Count - 1;
                            }
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawScriptResultDetail(MissingScriptInfo result)
            {
                GUILayout.Label("📄 Details:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginVertical(UIStyles.CardStyle);
                
                EditorGUILayout.LabelField("🎯 GameObject:", result.gameObjectName, EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("📍 Scene:", result.sceneName);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField("🔗 Component:", result.componentTypeName);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField("❌ Error:", "Missing Script");
                EditorGUILayout.Space(5);
                
                if (result.gameObject != null)
                {
                    string buttonText = PrefabUtility.IsPartOfPrefabAsset(result.gameObject) ? 
                        "🔍 Find in Folder" : "🔍 Find in Scene";
                    
                    if (GUILayout.Button(buttonText, UIStyles.ButtonStyle, GUILayout.Height(30)))
                    {
                        FindInScene(result.gameObject, result.gameObjectName, result.sceneName, result.scenePath, "Missing Script Finder");
                    }
                }
                
                EditorGUILayout.Space(5);
                
                if (result.gameObject != null)
                {
                    Rect buttonRect = EditorGUILayout.GetControlRect(false, 40);
                    string buttonText = removalMode == RemovalMode.DeleteGameObjects ? 
                        "💥 Delete GameObject" : "🗑️ Remove Missing Script";
                    
                    if (DrawLargeStyledButton(buttonRect, buttonText, UIColors.Danger))
                    {
                        string dialogTitle = "Remove Missing Script";
                        string dialogMessage = $"Are you sure you want to remove the missing script from '{result.gameObjectName}'?";
                        
                        if (removalMode == RemovalMode.DeleteGameObjects)
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
                        
                        if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
                        {
                            if (RemoveMissingScript(result))
                            {
                                if (selectedScriptResultIndex >= missingScriptResults.Count)
                                    selectedScriptResultIndex = missingScriptResults.Count - 1;
                            }
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            public override void ClearResults()
            {
                missingScriptResults.Clear();
                showScriptResults = false;
                selectedScriptResultIndex = -1;
            }
            
            private void SelectPrefabFolder()
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Prefab Folder", "Assets", "");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    selectedPrefabFolder = folderPath;
                    string assetPath = ConvertToAssetPath(folderPath);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        LoadPrefabsFromFolder(assetPath);
                    }
                }
            }
            
            private void SelectSceneFolder()
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Scene Folder", "Assets", "");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    selectedSceneFolder = folderPath;
                    string assetPath = ConvertToAssetPath(folderPath);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        LoadScenesFromFolder(assetPath);
                    }
                }
            }
            
            private string ConvertToAssetPath(string absolutePath)
            {
                if (string.IsNullOrEmpty(absolutePath))
                    return null;
                
                string projectPath = System.IO.Directory.GetCurrentDirectory();
                projectPath = projectPath.Replace("\\", "/");
                
                LogInfo($"Converting path: {absolutePath}");
                LogInfo($"Project path: {projectPath}");
                
                if (absolutePath.StartsWith(projectPath))
                {
                    string relativePath = absolutePath.Substring(projectPath.Length);
                    if (relativePath.StartsWith("/"))
                        relativePath = relativePath.Substring(1);
                    
                    LogInfo($"Converted to asset path: {relativePath}");
                    return relativePath;
                }
                
                LogWarning($"Path is outside project folder: {absolutePath}");
                return null;
            }
            
            private void LoadPrefabsFromFolder(string folderPath)
            {
                prefabsInFolder.Clear();
                selectedPrefabs.Clear();

                LogInfo($"Loading prefabs from folder: {folderPath}");
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                LogInfo($"Found {guids.Length} prefab files in folder");
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        LogInfo($"Checking prefab: {prefab.name} for errors");
                        
                        if (HasPrefabErrors(prefab))
                        {
                            LogInfo($"Prefab {prefab.name} has missing scripts, adding to list");
                            prefabsInFolder.Add(prefab);
                            selectedPrefabs.Add(false);
                        }
                        else
                        {
                            LogInfo($"Prefab {prefab.name} has no missing scripts, skipping");
                        }
                    }
                }
                
                LogInfo($"Loaded {prefabsInFolder.Count} prefabs with errors");
            }
            
            private bool HasPrefabErrors(GameObject prefab)
            {
                return CheckGameObjectForErrors(prefab);
            }
            
            private bool CheckGameObjectForErrors(GameObject obj)
            {
                Component[] components = obj.GetComponents<Component>();
                
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        return true;
                    }
                }
                
                foreach (Transform child in obj.transform)
                {
                    if (CheckGameObjectForErrors(child.gameObject))
                        return true;
                }
                
                return false;
            }
            
            
            private void LoadScenesFromFolder(string folderPath)
            {
                scenesInFolder.Clear();
                selectedScenes.Clear();

                string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    if (scene != null)
                    {
                        scenesInFolder.Add(scene);
                        selectedScenes.Add(false);
                    }
                }
            }

            private void FindMissingScripts()
            {
                missingScriptResults.Clear();
                showScriptResults = true;
                selectedScriptResultIndex = -1;

                LogInfo($"Starting missing script search. Mode: {parentWindow.scriptSearchMode}");
                LogInfo($"Prefab folder: {selectedPrefabFolder}");
                LogInfo($"Prefabs in folder count: {prefabsInFolder.Count}");

                if (parentWindow.scriptSearchMode == ScriptSearchMode.Scene)
                {
                    LogInfo("Searching in scenes");
                    FindMissingScriptsInScenes();
                }
                else
                {
                    LogInfo("Searching in prefabs");
                    FindMissingScriptsInPrefabs();
                }
            }

            private void FindMissingScriptsInScenes()
            {
                if (parentWindow.scriptSceneSearchScope == ScriptSceneSearchScope.CurrentOpenScenes)
                {
                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                    {
                        var scene = EditorSceneManager.GetSceneAt(i);
                        if (scene.IsValid())
                        {
                            FindMissingScriptsInScene(scene);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < selectedScenes.Count; i++)
                    {
                        if (selectedScenes[i])
                        {
                            var scenePath = AssetDatabase.GetAssetPath(scenesInFolder[i]);
                            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                            FindMissingScriptsInScene(scene);
                        }
                    }
                }
            }

            private void FindMissingScriptsInPrefabs()
            {
                LogInfo($"Starting prefab search. Found {prefabsInFolder.Count} error ");
                
                if (prefabsInFolder.Count == 0)
                {
                    LogWarning("No prefabs found in folder. Please select a prefab folder first.");
                    return;
                }
                
                bool hasSelectedPrefabs = false;
                for (int i = 0; i < selectedPrefabs.Count; i++)
                {
                    if (selectedPrefabs[i])
                    {
                        hasSelectedPrefabs = true;
                        break;
                    }
                }
                
                if (!hasSelectedPrefabs)
                {
                    LogInfo("No prefabs selected, checking all error prefabs in folder");
                    for (int i = 0; i < prefabsInFolder.Count; i++)
                    {
                        LogInfo($"Checking prefab {i+1}/{prefabsInFolder.Count}: {prefabsInFolder[i].name}");
                        FindMissingScriptsInPrefab(prefabsInFolder[i]);
                    }
                }
                else
                {
                    LogInfo("Checking selected prefabs only");
                    for (int i = 0; i < selectedPrefabs.Count; i++)
                    {
                        if (selectedPrefabs[i])
                        {
                            LogInfo($"Checking selected prefab: {prefabsInFolder[i].name}");
                            FindMissingScriptsInPrefab(prefabsInFolder[i]);
                        }
                    }
                }
                
                LogInfo($"Prefab search completed. Found {missingScriptResults.Count} missing scripts");
            }
            
            private void FindMissingScriptsInScene(Scene scene)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    CheckGameObjectForMissingScripts(obj, scene.name, scene.path);
                }
            }
            
            private void FindMissingScriptsInPrefab(GameObject prefab)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                LogInfo($"Checking Prefab: {prefab.name} at {prefabPath}");
                
                CheckGameObjectForMissingScripts(prefab, "Prefab Asset", prefabPath);
                
                FindPrefabInstancesInAllScenes(prefab);
            }
            
            private void FindPrefabInstancesInOpenScenes(GameObject prefabAsset)
            {
                LogInfo($"Searching for instances of prefab: {prefabAsset.name} in open scenes");
                
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    Scene scene = EditorSceneManager.GetSceneAt(i);
                    if (scene.IsValid())
                    {
                        LogInfo($"Checking scene: {scene.name}");
                        CheckPrefabInstancesInScene(scene, prefabAsset);
                    }
                }
            }
            
            private void CheckPrefabInstancesInScene(Scene scene, GameObject prefabAsset)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject rootObj in rootObjects)
                {
                    CheckGameObjectForPrefabInstances(rootObj, prefabAsset, scene.name, scene.path);
                }
            }
            
            private void FindPrefabInstancesInAllScenes(GameObject prefabAsset)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
                LogInfo($"Searching for instances of prefab: {prefabAsset.name} in all scenes");
                
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
                List<string> scenesWithPrefab = new List<string>();
                
                foreach (string sceneGuid in sceneGuids)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                    if (!string.IsNullOrEmpty(scenePath))
                    {
                        if (IsPackageScene(scenePath))
                        {
                            LogInfo($"Skipping package scene: {scenePath}");
                            continue;
                        }
                        
                        if (SceneContainsPrefab(scenePath, prefabAsset))
                        {
                            scenesWithPrefab.Add(scenePath);
                            LogInfo($"Found prefab instances in scene: {scenePath}");
                        }
                    }
                }
                
                foreach (string scenePath in scenesWithPrefab)
                {
                    CheckPrefabInstancesInScene(scenePath, prefabAsset);
                }
            }
            
            private bool IsPackageScene(string scenePath)
            {
                try
                {
                    if (scenePath.Contains("Packages/") || scenePath.StartsWith("Packages/"))
                    {
                        return true;
                    }
                    
                    SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    if (sceneAsset != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(sceneAsset);
                        return assetPath.Contains("Packages/");
                    }
                }
                catch (System.Exception e)
                {
                    LogWarning($"Error checking if scene is package scene: {e.Message}");
                }
                
                return false;
            }
            
            private bool SceneContainsPrefab(string scenePath, GameObject prefabAsset)
            {
                try
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    if (scene.IsValid())
                    {
                        bool containsPrefab = false;
                        GameObject[] rootObjects = scene.GetRootGameObjects();
                        foreach (GameObject rootObj in rootObjects)
                        {
                            if (CheckGameObjectForPrefabReference(rootObj, prefabAsset))
                            {
                                containsPrefab = true;
                                break;
                            }
                        }
                        
                        EditorSceneManager.CloseScene(scene, true);
                        return containsPrefab;
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"Error checking scene {scenePath}: {e.Message}");
                }
                return false;
            }
            
            private bool CheckGameObjectForPrefabReference(GameObject obj, GameObject prefabAsset)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabSource == prefabAsset)
                    {
                        return true;
                    }
                }
                
                foreach (Transform child in obj.transform)
                {
                    if (CheckGameObjectForPrefabReference(child.gameObject, prefabAsset))
                        return true;
                }
                
                return false;
            }
            
            private void CheckPrefabInstancesInScene(string scenePath, GameObject prefabAsset)
            {
                try
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    if (scene.IsValid())
                    {
                        LogInfo($"Checking prefab instances in scene: {scene.name}");
                        
                        GameObject[] rootObjects = scene.GetRootGameObjects();
                        foreach (GameObject rootObj in rootObjects)
                        {
                            CheckGameObjectForPrefabInstances(rootObj, prefabAsset, scene.name, scenePath);
                        }
                        
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"Error checking scene {scenePath}: {e.Message}");
                }
            }
            
            private void CheckGameObjectForPrefabInstances(GameObject obj, GameObject prefabAsset, string sceneName, string scenePath)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabSource == prefabAsset)
                    {
                        LogInfo($"Found prefab instance: {obj.name} in scene: {sceneName}");
                        string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                        CheckGameObjectForMissingScripts(obj, sceneFileName, scenePath);
                    }
                }
                
                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForPrefabInstances(child.gameObject, prefabAsset, sceneName, scenePath);
                }
            }
            
            private void CheckGameObjectForMissingScripts(GameObject obj, string sceneName, string assetPath)
            {
                LogInfo($"Checking GameObject: {obj.name} in {sceneName} for missing scripts");
                Component[] components = obj.GetComponents<Component>();
                LogInfo($"GameObject {obj.name} has {components.Length} components");
                
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        LogInfo($"Found missing script at component index {i} on {obj.name}");
                        AddMissingScriptInfo(obj, i, sceneName, assetPath);
                    }
                }
                
                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForMissingScripts(child.gameObject, sceneName, assetPath);
                }
            }

            private void AddMissingScriptInfo(GameObject obj, int componentIndex, string sceneName, string assetPath)
            {
                var newInfo = new MissingScriptInfo
                {
                    gameObject = obj,
                    componentIndex = componentIndex,
                    sceneName = sceneName,
                    scenePath = assetPath,
                    assetPath = assetPath,
                    gameObjectName = obj.name,
                    instanceID = obj.GetInstanceID().ToString(),
                    componentTypeName = "Missing Script"
                };
                
                missingScriptResults.Add(newInfo);
                LogInfo($"Found missing script: {obj.name} - Component {componentIndex}");
            }

            private bool RemoveMissingScript(MissingScriptInfo scriptInfo)
            {
                if (scriptInfo.gameObject != null)
                {
                    LogInfo($"Attempting to remove missing script from {scriptInfo.gameObject.name}");
                    
                    if (removalMode == RemovalMode.DeleteGameObjects)
                    {
                        return DeleteGameObjectWithMissingScript(scriptInfo);
                    }
                    
                    if (PrefabUtility.IsPartOfPrefabAsset(scriptInfo.gameObject))
                    {
                        return DeletePrefabFile(scriptInfo);
                    }
                    
                    var components = scriptInfo.gameObject.GetComponents<Component>();
                    if (scriptInfo.componentIndex < components.Length && components[scriptInfo.componentIndex] == null)
                    {
                        LogWarning($"Cannot directly remove missing script component. Consider using Delete mode or manually fixing the prefab.");
                        return false;
                    }
                    
                    missingScriptResults.Remove(scriptInfo);
                    LogInfo($"Successfully processed missing script from {scriptInfo.gameObject.name}");
                    return true;
                }
                
                return false;
            }
            
            private bool DeleteGameObjectWithMissingScript(MissingScriptInfo scriptInfo)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(scriptInfo.gameObject))
                {
                    return DeletePrefabFile(scriptInfo);
                }
                else
                {
                    if (scriptInfo.gameObject.scene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(scriptInfo.gameObject.scene);
                    }
                    GameObject.DestroyImmediate(scriptInfo.gameObject);
                    missingScriptResults.Remove(scriptInfo);
                    LogInfo($"Deleted GameObject with missing script: {scriptInfo.gameObjectName}");
                    return true;
                }
            }
            
            private bool DeletePrefabFile(MissingScriptInfo scriptInfo)
            {
                string assetPath = AssetDatabase.GetAssetPath(scriptInfo.gameObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    missingScriptResults.Remove(scriptInfo);
                    LogInfo($"Deleted prefab file: {assetPath}");
                    return true;
                }
                return false;
            }

            private void RemoveAllMissingScripts()
            {
                for (int i = missingScriptResults.Count - 1; i >= 0; i--)
                {
                    RemoveMissingScript(missingScriptResults[i]);
                }
                
                if (missingScriptResults.Count == 0)
                {
                    showScriptResults = false;
                    selectedScriptResultIndex = -1;
                }
            }
        }
    }
}