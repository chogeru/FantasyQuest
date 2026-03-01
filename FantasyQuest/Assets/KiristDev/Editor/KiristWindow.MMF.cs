using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Kirist.EditorTool
{
    public partial class KiristWindow
    {
        public class MissingMaterialFinder : BaseFinderBehaviour
        {
            private List<MissingMaterialInfo> missingMaterialResults = new List<MissingMaterialInfo>();
            private bool showMaterialResults = false;
            private RemovalMode materialRemovalMode = RemovalMode.RemoveComponentsOnly;
            
            private List<GameObject> materialPrefabsInFolder = new List<GameObject>();
            private List<bool> selectedMaterialPrefabs = new List<bool>();
            private string selectedMaterialPrefabFolder = "";
            
            private bool autoScanPrefabFolder = false;
            private bool includePrefabSubfolders = true;
            private string scanPrefabFolderPath = "Assets";
            
            private List<SceneAsset> materialScenesInFolder = new List<SceneAsset>();
            private List<bool> selectedMaterialScenes = new List<bool>();
            private string selectedMaterialSceneFolder = "";
            private Vector2 materialSceneListScrollPos;
            
            private Vector2 materialResultsScrollPos = Vector2.zero;
            private int selectedMaterialResultIndex = -1;
            private int materialVisibleItemsCount = 50;
            private int materialScrollOffset = 0;
            
            public MissingMaterialFinder(KiristWindow parent) : base(parent)
            {
            }
            
            public override void DrawUI()
            {
                var purpleBackgroundStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = CreateGradientTexture(new Color(0.25f, 0.20f, 0.30f, 1f), new Color(0.30f, 0.25f, 0.35f, 1f)) },
                    border = new RectOffset(3, 3, 3, 3),
                    padding = new RectOffset(15, 15, 15, 15)
                };
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(purpleBackgroundStyle);
                
                DrawTitle("🎨 MISSING MATERIAL FINDER");
                EditorGUILayout.Space(5);
                
                var purpleSectionStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = CreateGradientTexture(new Color(0.35f, 0.30f, 0.40f, 1f), new Color(0.40f, 0.35f, 0.45f, 1f)) },
                    border = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(8, 8, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2)
                };
                EditorGUILayout.BeginVertical(purpleSectionStyle);
                EditorGUILayout.BeginHorizontal();
                var searchModeStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = UIColors.ModernPurple }, fontStyle = FontStyle.Bold };
                GUILayout.Label("🔍 Search Target:", searchModeStyle, GUILayout.Width(100));
                parentWindow.materialSearchMode = (MaterialSearchMode)EditorGUILayout.EnumPopup(parentWindow.materialSearchMode, GUILayout.Width(120));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                
                if (parentWindow.materialSearchMode == MaterialSearchMode.Scene)
                {
                    DrawSceneSearchMode();
                }
                else
                {
                    DrawPrefabSearchMode();
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginVertical(purpleSectionStyle);
                DrawSearchButton();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                
                if (showMaterialResults)
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
                parentWindow.materialSceneSearchScope = (MaterialSceneSearchScope)EditorGUILayout.EnumPopup(parentWindow.materialSceneSearchScope, GUILayout.Width(180));
                EditorGUILayout.EndHorizontal();

                if (parentWindow.materialSceneSearchScope == MaterialSceneSearchScope.SpecificScenesFromFolder)
                {
                    EditorGUILayout.Space(3);
                    DrawSceneFolderSelection();
                }
            }
            
            private void DrawPrefabSearchMode()
            {
                DrawPrefabFolderSelection();
                
                EditorGUILayout.Space(5);
                DrawPrefabFolderScanOptions();
                
                if (materialPrefabsInFolder.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    DrawPrefabList();
                }
            }
            
            private void DrawSceneFolderSelection()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Scene Folder:", GUILayout.Width(80));
                GUILayout.Label(string.IsNullOrEmpty(selectedMaterialSceneFolder) ? "None Selected" : selectedMaterialSceneFolder, EditorStyles.textField);
                
                if (GUILayout.Button("📁 Select", UIStyles.ButtonStyle, GUILayout.Width(80)))
                {
                    SelectMaterialSceneFolder();
                }
                EditorGUILayout.EndHorizontal();

                if (materialScenesInFolder.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    DrawSceneList();
                }
            }
            
            private void DrawPrefabFolderSelection()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Prefab Folder:", GUILayout.Width(80));
                GUILayout.Label(string.IsNullOrEmpty(selectedMaterialPrefabFolder) ? "None Selected" : selectedMaterialPrefabFolder, EditorStyles.textField);
                
                if (GUILayout.Button("📁 Select", UIStyles.ButtonStyle, GUILayout.Width(80)))
                {
                    SelectMaterialPrefabFolder();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            private void DrawPrefabFolderScanOptions()
            {
                try
                {
                    var headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 11,
                        normal = { textColor = UIColors.ModernPurple }
                    };
                    EditorGUILayout.LabelField("📁 AUTO SCAN OPTIONS", headerStyle);
                    
                    EditorGUILayout.Space(3);
                    
                    autoScanPrefabFolder = EditorGUILayout.Toggle("Auto Scan Prefab Folder", autoScanPrefabFolder);
                    
                    if (autoScanPrefabFolder)
                    {
                        EditorGUI.indentLevel++;
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Scan Folder:", GUILayout.Width(80));
                        scanPrefabFolderPath = EditorGUILayout.TextField(scanPrefabFolderPath);
                        if (GUILayout.Button("📁", GUILayout.Width(30)))
                        {
                            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder to Scan", "Assets", "");
                            if (!string.IsNullOrEmpty(selectedPath))
                            {
                                scanPrefabFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        includePrefabSubfolders = EditorGUILayout.Toggle("Include Subfolders", includePrefabSubfolders);
                        
                        EditorGUI.indentLevel--;
                        
                        EditorGUILayout.Space(3);
                        
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("🔍 Scan Folder", UIStyles.ButtonStyle, GUILayout.Height(25)))
                        {
                            ScanPrefabsInFolderForMaterials();
                        }
                        if (GUILayout.Button("🎯 Scan Scene Instances", UIStyles.ButtonStyle, GUILayout.Height(25)))
                        {
                            ScanPrefabInstancesInScenesForMaterials();
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        if (materialPrefabsInFolder.Count > 0)
                        {
                            EditorGUILayout.Space(3);
                            EditorGUILayout.LabelField($"Found {materialPrefabsInFolder.Count} prefabs:", EditorStyles.miniLabel);
                            
                            var scrollStyle = new GUIStyle(GUI.skin.box)
                            {
                                normal = { background = CreateGradientTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f), new Color(0.15f, 0.15f, 0.15f, 0.5f)) }
                            };
                            
                            EditorGUILayout.BeginVertical(scrollStyle);
                            for (int i = 0; i < materialPrefabsInFolder.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                selectedMaterialPrefabs[i] = EditorGUILayout.Toggle(selectedMaterialPrefabs[i], GUILayout.Width(20));
                                EditorGUILayout.LabelField(materialPrefabsInFolder[i].name, EditorStyles.miniLabel);
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUILayout.EndVertical();
                            
                            EditorGUILayout.Space(3);
                            
                            int selectedCount = selectedMaterialPrefabs.Count(s => s);
                            if (selectedCount > 0)
                            {
                                if (GUILayout.Button($"🔍 Analyze {selectedCount} Selected Prefabs", UIStyles.ButtonStyle, GUILayout.Height(25)))
                                {
                                    AnalyzeSelectedPrefabsForMaterials();
                                }
                            }
                            
                            EditorGUILayout.Space(5);
                            
                            if (GUILayout.Button("🔍 Analyze All Standalone Materials", UIStyles.ButtonStyle, GUILayout.Height(25)))
                            {
                                AnalyzeStandaloneMaterials();
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"Error in DrawPrefabFolderScanOptions: {e.Message}");
                }
            }
            
            private void DrawPrefabList()
            {
                EditorGUILayout.BeginVertical(UIStyles.CardStyle);
                GUILayout.Label($"📦 Error Prefabs in Folder ({materialPrefabsInFolder.Count}):", EditorStyles.boldLabel);
                
                materialSceneListScrollPos = EditorGUILayout.BeginScrollView(materialSceneListScrollPos, GUILayout.MaxHeight(150));

                for (int i = 0; i < materialPrefabsInFolder.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    selectedMaterialPrefabs[i] = EditorGUILayout.Toggle(selectedMaterialPrefabs[i], GUILayout.Width(20));
                    GUILayout.Label(materialPrefabsInFolder[i].name, EditorStyles.label);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            
            private void DrawSceneList()
            {
                EditorGUILayout.BeginVertical(UIStyles.CardStyle);
                GUILayout.Label($"🎬 Scenes in Folder ({materialScenesInFolder.Count}):", EditorStyles.boldLabel);
                
                materialSceneListScrollPos = EditorGUILayout.BeginScrollView(materialSceneListScrollPos, GUILayout.MaxHeight(150));
                
                for (int i = 0; i < materialScenesInFolder.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    selectedMaterialScenes[i] = EditorGUILayout.Toggle(selectedMaterialScenes[i], GUILayout.Width(20));
                    GUILayout.Label(materialScenesInFolder[i].name, EditorStyles.label);
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
                materialRemovalMode = (RemovalMode)EditorGUILayout.EnumPopup(materialRemovalMode, GUILayout.Width(180));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                if (parentWindow.materialSearchMode == MaterialSearchMode.Prefab)
                {
                    if (materialPrefabsInFolder.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Select a prefab folder first", MessageType.Warning);
                    }
                }
                
                EditorGUILayout.Space(3);
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("🔍 FIND MISSING MATERIALS", UIStyles.SearchButtonStyle, GUILayout.Width(200), GUILayout.Height(30)))
                {
                    FindMissingMaterials();
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
                EditorGUILayout.LabelField($"📊 Search Results ({missingMaterialResults.Count} found)", titleStyle);
                EditorGUILayout.Space(5);
                
                if (missingMaterialResults.Count > 0)
                {
                    Rect removeAllRect = EditorGUILayout.GetControlRect(false, 30);
                    string removeAllText = materialRemovalMode == RemovalMode.DeleteGameObjects ? 
                        "💥 Delete All GameObjects with Missing Materials" : 
                        "🗑️ Remove All Missing Materials";
                    
                    if (DrawLargeStyledButton(removeAllRect, removeAllText, UIColors.Danger))
                    {
                        string dialogTitle = materialRemovalMode == RemovalMode.DeleteGameObjects ? 
                            "Delete All GameObjects" : 
                            "Remove Missing Materials";
                        string dialogMessage = materialRemovalMode == RemovalMode.DeleteGameObjects ?
                            $"⚠️ WARNING: This will DELETE {missingMaterialResults.Count} GameObject(s) with missing materials!\n\nThis action cannot be undone!" :
                            $"Are you sure you want to remove {missingMaterialResults.Count} missing material reference(s)?";
                            
                        if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
                        {
                            RemoveAllMissingMaterials();
                        }
                    }

                    EditorGUILayout.Space(10);
                    DrawResultsList();
                }
                else
                {
                    DrawHelpBox("✅ No missing materials found!", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }

            private void DrawResultsList()
            {
                DrawScrollControls($"Found {missingMaterialResults.Count} items", 
                    selectedMaterialResultIndex >= 0 && selectedMaterialResultIndex < missingMaterialResults.Count ? 
                    () => FindInScene(missingMaterialResults[selectedMaterialResultIndex].gameObject, 
                        missingMaterialResults[selectedMaterialResultIndex].gameObjectName,
                        missingMaterialResults[selectedMaterialResultIndex].sceneName, 
                        missingMaterialResults[selectedMaterialResultIndex].scenePath, "Missing Material Finder") : null);
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.BeginVertical(GUILayout.Width(300));
                if (selectedMaterialResultIndex >= 0 && selectedMaterialResultIndex < missingMaterialResults.Count)
                {
                    DrawMaterialResultDetail(missingMaterialResults[selectedMaterialResultIndex]);
                }
                else
                {
                    EditorGUILayout.LabelField("Select an item from the list to view details", EditorStyles.helpBox);
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.BeginVertical();
                GUILayout.Label("📋 Items List:", EditorStyles.boldLabel);
                
                materialResultsScrollPos = EditorGUILayout.BeginScrollView(materialResultsScrollPos, GUILayout.Height(200));
                
                EditorGUILayout.BeginHorizontal();
                
                for (int i = 0; i < missingMaterialResults.Count; i++)
                {
                    DrawMaterialResultCard(missingMaterialResults[i], i);
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
            }
            
            private void DrawMaterialResultCard(MissingMaterialInfo result, int index)
            {
                bool isSelected = selectedMaterialResultIndex == index;
                
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
                
                if (result.sprite != null || !string.IsNullOrEmpty(result.spriteName))
                {
                    GUILayout.Label($"🖼️ {result.spriteName}", sceneStyle, GUILayout.Height(20));
                }
                else
                {
                    GUILayout.Label($"🎨 {result.materialName}", sceneStyle, GUILayout.Height(20));
                }
                
                GUILayout.Label($"❌ {result.errorReason}", sceneStyle, GUILayout.Height(20));
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button(isSelected ? "✅ Selected" : "Select", UIStyles.ButtonStyle, GUILayout.Height(25)))
                {
                    selectedMaterialResultIndex = index;
                }
                
                string buttonText = materialRemovalMode == RemovalMode.DeleteGameObjects ? 
                    "💥 Delete" : "🗑️ Remove";
                if (GUILayout.Button(buttonText, UIStyles.ButtonStyle, GUILayout.Height(25)))
                {
                    string dialogTitle = materialRemovalMode == RemovalMode.DeleteGameObjects ? 
                        "Delete Missing Material" : "Remove Missing Material";
                    string dialogMessage = materialRemovalMode == RemovalMode.DeleteGameObjects ?
                        $"Delete '{result.gameObjectName}'?" :
                        $"Remove missing material from '{result.gameObjectName}'?";
                    
                    if (EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Yes", "Cancel"))
                    {
                        if (RemoveMissingMaterial(result))
                        {
                            if (selectedMaterialResultIndex >= missingMaterialResults.Count)
                                selectedMaterialResultIndex = missingMaterialResults.Count - 1;
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawMaterialResultDetail(MissingMaterialInfo result)
            {
                GUILayout.Label("📄 Material Analysis Details:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginVertical(UIStyles.CardStyle);
                
                EditorGUILayout.LabelField("🎯 GameObject:", result.gameObjectName, EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("📍 Scene:", result.sceneName);
                EditorGUILayout.Space(3);
                
                if (result.sprite != null || !string.IsNullOrEmpty(result.spriteName))
                {
                    EditorGUILayout.LabelField("🖼️ Sprite:", result.spriteName);
                    EditorGUILayout.Space(3);
                }
                else
                {
                    EditorGUILayout.LabelField("🎨 Material:", result.materialName);
                    EditorGUILayout.Space(3);
                    
                    EditorGUILayout.LabelField("🔗 Shader:", result.shaderName);
                    EditorGUILayout.Space(3);
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("🔍 Detailed Analysis:", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                string errorReason = result.errorReason;
                if (errorReason.Contains("[CRITICAL]"))
                {
                    EditorGUILayout.LabelField("🚨 Severity: CRITICAL", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
                }
                else if (errorReason.Contains("[HIGH]"))
                {
                    EditorGUILayout.LabelField("⚠️ Severity: HIGH", new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.5f, 0f) } });
                }
                else if (errorReason.Contains("[MEDIUM]"))
                {
                    EditorGUILayout.LabelField("⚡ Severity: MEDIUM", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow } });
                }
                else
                {
                    EditorGUILayout.LabelField("ℹ️ Severity: LOW", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.blue } });
                }
                
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("📋 Issue Type:", errorReason.Split(':')[0].Replace("[", "").Replace("]", ""));
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("📝 Description:", errorReason.Split(':')[1].Trim());
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("💡 Suggested Solutions:", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                if (errorReason.Contains("Missing Shader File"))
                {
                    EditorGUILayout.LabelField("• Assign a valid shader to the material", EditorStyles.helpBox);
                    EditorGUILayout.LabelField("• Check if the shader file was moved or deleted", EditorStyles.helpBox);
                }
                else if (errorReason.Contains("Compilation Failed"))
                {
                    EditorGUILayout.LabelField("• Fix shader compilation errors", EditorStyles.helpBox);
                    EditorGUILayout.LabelField("• Check shader syntax and platform compatibility", EditorStyles.helpBox);
                }
                else if (errorReason.Contains("Unity Error Shader"))
                {
                    EditorGUILayout.LabelField("• Replace with a working shader", EditorStyles.helpBox);
                    EditorGUILayout.LabelField("• Check original shader for errors", EditorStyles.helpBox);
                }
                else if (errorReason.Contains("Unsupported Shader"))
                {
                    EditorGUILayout.LabelField("• Use a platform-compatible shader", EditorStyles.helpBox);
                    EditorGUILayout.LabelField("• Check shader target platform settings", EditorStyles.helpBox);
                }
                else
                {
                    EditorGUILayout.LabelField("• Review material and shader settings", EditorStyles.helpBox);
                    EditorGUILayout.LabelField("• Check for missing dependencies", EditorStyles.helpBox);
                }
                
                EditorGUILayout.Space(5);
                
                if (result.gameObject != null)
                {
                    string findButtonText = PrefabUtility.IsPartOfPrefabAsset(result.gameObject) ? 
                        "🔍 Find in Folder" : "🔍 Find in Scene";
                    
                    if (GUILayout.Button(findButtonText, UIStyles.ButtonStyle, GUILayout.Height(30)))
                    {
                        FindInScene(result.gameObject, result.gameObjectName, result.sceneName, result.scenePath, "Missing Material Finder");
                    }
                }
                
                EditorGUILayout.Space(5);
                
                Rect buttonRect = EditorGUILayout.GetControlRect(false, 40);
                string buttonText = materialRemovalMode == RemovalMode.DeleteGameObjects ? 
                    "💥 Delete GameObject" : "🗑️ Remove Missing Material";
                
                if (DrawLargeStyledButton(buttonRect, buttonText, UIColors.Danger))
                {
                    string dialogTitle = "Remove Missing Material";
                    string resourceInfo = result.sprite != null || !string.IsNullOrEmpty(result.spriteName) 
                        ? $"Sprite: {result.spriteName}" 
                        : $"Material: {result.materialName}";
                    string dialogMessage = $"Are you sure you want to remove the missing resource from '{result.gameObjectName}'?\n\n{resourceInfo}\nError: {result.errorReason}";
                    
                    if (materialRemovalMode == RemovalMode.DeleteGameObjects)
                    {
                        if (result.gameObject != null && PrefabUtility.IsPartOfPrefabAsset(result.gameObject))
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
                        if (RemoveMissingMaterial(result))
                        {
                            if (selectedMaterialResultIndex >= missingMaterialResults.Count)
                                selectedMaterialResultIndex = missingMaterialResults.Count - 1;
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            public override void ClearResults()
            {
                missingMaterialResults.Clear();
                showMaterialResults = false;
                selectedMaterialResultIndex = -1;
            }
            
            private void ScanPrefabsInFolderForMaterials()
            {
                LogInfo($"Scanning prefabs in folder for materials: {scanPrefabFolderPath} (Include subfolders: {includePrefabSubfolders})");
                
                materialPrefabsInFolder.Clear();
                selectedMaterialPrefabs.Clear();
                
                try
                {
                    string searchPattern = includePrefabSubfolders ? "t:Prefab" : "t:Prefab";
                    string[] guids = AssetDatabase.FindAssets(searchPattern, new[] { scanPrefabFolderPath });
                    
                    LogInfo($"Found {guids.Length} prefabs in folder");
                    
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (prefab != null)
                            {
                                materialPrefabsInFolder.Add(prefab);
                                selectedMaterialPrefabs.Add(true); 
                                LogInfo($"  - Found prefab: {prefab.name} at {path}");
                            }
                        }
                    }
                    
                    LogInfo($"Successfully loaded {materialPrefabsInFolder.Count} prefabs from folder for material analysis");
                }
                catch (System.Exception e)
                {
                    LogError($"Error scanning prefabs in folder for materials: {e.Message}");
                }
            }
            
            private void ScanPrefabInstancesInScenesForMaterials()
            {
                LogInfo("Scanning for prefab instances in all scenes for material analysis...");
                
                materialPrefabsInFolder.Clear();
                selectedMaterialPrefabs.Clear();
                
                try
                {
                    string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
                    HashSet<GameObject> uniquePrefabs = new HashSet<GameObject>();
                    
                    foreach (string sceneGuid in sceneGuids)
                    {
                        string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                        if (!string.IsNullOrEmpty(scenePath) && !IsPackageScene(scenePath))
                        {
                            LogInfo($"Checking scene for prefab instances: {scenePath}");
                            
                            try
                            {
                                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                                if (scene.IsValid())
                                {
                                    GameObject[] rootObjects = scene.GetRootGameObjects();
                                    foreach (GameObject rootObj in rootObjects)
                                    {
                                        FindPrefabInstancesRecursiveForMaterials(rootObj, uniquePrefabs);
                                    }
                                    
                                    EditorSceneManager.CloseScene(scene, true);
                                }
                            }
                            catch (System.Exception e)
                            {
                                LogError($"Error checking scene {scenePath}: {e.Message}");
                            }
                        }
                    }
                    
                    foreach (GameObject prefab in uniquePrefabs)
                    {
                        materialPrefabsInFolder.Add(prefab);
                        selectedMaterialPrefabs.Add(true);
                        LogInfo($"  - Found prefab instance for material analysis: {prefab.name}");
                    }
                    
                    LogInfo($"Found {materialPrefabsInFolder.Count} unique prefab instances across all scenes for material analysis");
                }
                catch (System.Exception e)
                {
                    LogError($"Error scanning prefab instances for materials: {e.Message}");
                }
            }
            
            private void FindPrefabInstancesRecursiveForMaterials(GameObject obj, HashSet<GameObject> uniquePrefabs)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabAsset != null && !uniquePrefabs.Contains(prefabAsset))
                    {
                        uniquePrefabs.Add(prefabAsset);
                        LogInfo($"    - Found prefab instance for material analysis: {obj.name} -> {prefabAsset.name}");
                    }
                }
                
                foreach (Transform child in obj.transform)
                {
                    FindPrefabInstancesRecursiveForMaterials(child.gameObject, uniquePrefabs);
                }
            }
            
            private bool IsPackageScene(string scenePath)
            {
                return scenePath.Contains("Packages/") || scenePath.Contains("Library/");
            }
            
            private void AnalyzeSelectedPrefabsForMaterials()
            {
                try
                {
                    LogInfo($"Starting batch material analysis of {selectedMaterialPrefabs.Count(s => s)} selected prefabs");
                    
                    missingMaterialResults.Clear();
                    
                    for (int i = 0; i < materialPrefabsInFolder.Count; i++)
                    {
                        if (selectedMaterialPrefabs[i])
                        {
                            LogInfo($"Analyzing prefab for materials: {materialPrefabsInFolder[i].name}");
                            
                            FindMissingMaterialsInPrefab(materialPrefabsInFolder[i]);
                        }
                    }
                    
                    showMaterialResults = true;
                    
                    LogInfo($"Batch material analysis completed: {missingMaterialResults.Count} missing materials found");
                }
                catch (System.Exception e)
                {
                    LogError($"Error in AnalyzeSelectedPrefabsForMaterials: {e.Message}");
                }
            }
            
            private void SelectMaterialPrefabFolder()
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Prefab Folder", "Assets", "");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    string relativePath = ConvertToAssetPath(folderPath);
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        selectedMaterialPrefabFolder = relativePath;
                        LoadMaterialPrefabsFromFolder(relativePath);
                    }
                    else
                    {
                        LogError($"Selected folder is not within the Assets directory: {folderPath}");
                    }
                }
            }
            
            private void AnalyzeStandaloneMaterials()
            {
                LogInfo("=== ANALYZING STANDALONE MATERIAL FILES ===");
                
                try
                {
                    
                    missingMaterialResults.Clear();
                    showMaterialResults = true;
                    selectedMaterialResultIndex = -1;
                    
                  
                    string[] materialGuids = AssetDatabase.FindAssets("t:Material");
                    LogInfo($"Found {materialGuids.Length} material files");
                    
                    int errorMaterialsFound = 0;
                    
                    foreach (string guid in materialGuids)
                    {
                        string materialPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (string.IsNullOrEmpty(materialPath)) continue;
                        
                        if (IsPackageAsset(materialPath)) continue;
                        
                        LogInfo($"Analyzing material file: {materialPath}");
                        
                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if (material == null)
                        {
                            LogInfo($"  - ERROR: Failed to load material from {materialPath}");
                            AddDetailedMaterialInfo(null, null, 0, "Standalone", materialPath, "Failed to load material file", "CRITICAL", "Material file corrupted or missing");
                            errorMaterialsFound++;
                            continue;
                        }
                        
                        LogInfo($"  - Material: {material.name}");
                        LogInfo($"  - Shader: {(material.shader != null ? material.shader.name : "NULL")}");
                        
                    
                        AddDetailedMaterialInfo(null, null, 0, "Standalone", materialPath, "Material Analysis", "MEDIUM", $"Material '{material.name}' with shader '{material.shader.name}' analyzed");
                        
                      
                        AnalyzeMaterialDetails(material, materialPath, "Standalone");
                    }
                    
                    LogInfo($"Standalone material analysis completed: {missingMaterialResults.Count} error materials found");
                    
                    if (missingMaterialResults.Count > 0)
                    {
                        LogInfo($"Found {missingMaterialResults.Count} problematic materials - showing results");
                    }
                    else
                    {
                        LogInfo("No problematic materials found - all materials are OK");
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"Error in AnalyzeStandaloneMaterials: {e.Message}");
                }
            }
            
            private void AnalyzeMaterialDetails(Material material, string materialPath, string context)
            {
                LogInfo($"=== COMPREHENSIVE MATERIAL ANALYSIS: {material.name} ===");
                
                LogInfo($"Material Name: {material.name}");
                LogInfo($"Material Path: {materialPath}");
                LogInfo($"Material Shader: {(material.shader != null ? material.shader.name : "NULL")}");
                
                if (material == null)
                {
                    LogInfo("  - MATERIAL ISSUE: NULL Material");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "NULL Material", "CRITICAL", "Material object is null");
                    return;
                }
                
                if (string.IsNullOrEmpty(material.name))
                {
                    LogInfo("  - MATERIAL ISSUE: Empty Material Name");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Empty Material Name", "MEDIUM", "Material has no name assigned");
                }
                
                if (material.shader == null)
                {
                    LogInfo("  - SHADER ISSUE: NULL Shader");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "NULL Shader", "CRITICAL", "Material has no shader assigned");
                    return;
                }
                
                string shaderPath = AssetDatabase.GetAssetPath(material.shader);
                LogInfo($"Shader Path: {shaderPath}");
                
                if (string.IsNullOrEmpty(shaderPath))
                {
                    LogInfo("  - SHADER ISSUE: Shader Path is Empty");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Shader Path Empty", "HIGH", $"Shader path is empty for shader: {material.shader.name}");
                    return;
                }
                
                if (!System.IO.File.Exists(shaderPath))
                {
                    LogInfo("  - SHADER ISSUE: Missing Shader File");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Missing Shader File", "HIGH", $"Shader file not found: {material.shader.name} at path: {shaderPath}");
                    return;
                }
                
                try
                {
                    var shaderGUID = AssetDatabase.AssetPathToGUID(shaderPath);
                    if (string.IsNullOrEmpty(shaderGUID))
                    {
                        LogInfo("  - SHADER ISSUE: Invalid Shader GUID");
                        AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Invalid Shader GUID", "HIGH", "Shader has invalid GUID reference");
                        return;
                    }
                    
                    var assetFromGUID = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                    if (assetFromGUID == null)
                    {
                        LogInfo("  - SHADER ISSUE: Shader Asset Not Found");
                        AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Shader Asset Not Found", "HIGH", $"Shader asset cannot be loaded from path: {shaderPath}");
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    LogInfo($"  - SHADER ISSUE: GUID Check Failed - {e.Message}");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Shader GUID Check Failed", "HIGH", $"Cannot validate shader GUID: {e.Message}");
                    return;
                }
                
                if (!material.shader.isSupported && !IsUnityBuiltinShader(material.shader.name))
                {
                    LogInfo("  - SHADER ISSUE: Unsupported Shader");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Unsupported Shader", "HIGH", $"Shader not supported on current platform: {material.shader.name}");
                    return;
                }
                
                if (material.shader.passCount == 0 && !IsUnityBuiltinShader(material.shader.name))
                {
                    LogInfo("  - SHADER ISSUE: No Passes (Compilation Failed)");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Compilation Failed", "HIGH", $"Shader compilation failed - no passes available: {material.shader.name}");
                    return;
                }
                
                string shaderName = material.shader.name;
                if (shaderName.Contains("Hidden/InternalErrorShader"))
                {
                    LogInfo("  - SHADER ISSUE: Unity Internal Error Shader");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Unity Error Shader", "CRITICAL", "Material using Unity's internal error shader - original shader failed");
                    return;
                }
                else if (shaderName.Contains("Hidden/Internal-Colored"))
                {
                    LogInfo("  - SHADER ISSUE: Unity Internal Colored Shader");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Unity Fallback Shader", "MEDIUM", "Material using Unity's fallback colored shader");
                    return;
                }
                else if (shaderName.Contains("Hidden/InternalError"))
                {
                    LogInfo("  - SHADER ISSUE: Unity Internal Error Variant");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Unity Error Variant", "CRITICAL", "Material using Unity's internal error shader variant");
                    return;
                }
                
                if (shaderName == "Hidden/InternalErrorShader" || 
                    shaderName == "Hidden/Internal-Colored" ||
                    shaderName == "Hidden/InternalError")
                {
                    LogInfo("  - SHADER ISSUE: Unity Error Shader");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Unity Error Shader", "CRITICAL", $"Material using Unity's error shader: {shaderName}");
                    return;
                }
                
                if (material.HasProperty("_Color"))
                {
                    try
                    {
                        Color color = material.GetColor("_Color");
                        if (color.r > 0.9f && color.g < 0.1f && color.b > 0.9f)
                        {
                            LogInfo("  - MATERIAL ISSUE: Magenta Error Color");
                            AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Magenta Error Color", "MEDIUM", "Material has Unity's error magenta color (R=1, G=0, B=1)");
                            return;
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogInfo($"  - MATERIAL ISSUE: Color Property Access Failed - {e.Message}");
                        AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Color Access Failed", "MEDIUM", $"Cannot access material color property: {e.Message}");
                        return;
                    }
                }
                
                CheckMaterialTextureReferences(material, materialPath, context);
                
                CheckMaterialKeywords(material, materialPath, context);
                
                CheckMaterialRenderQueue(material, materialPath, context);
                
                try
                {
                    var shaderProperties = ShaderUtil.GetPropertyCount(material.shader);
                    LogInfo($"Shader Properties Count: {shaderProperties}");
                    
                    for (int i = 0; i < shaderProperties; i++)
                    {
                        try
                        {
                            var propertyName = ShaderUtil.GetPropertyName(material.shader, i);
                            var propertyType = ShaderUtil.GetPropertyType(material.shader, i);
                            LogInfo($"  Property {i}: {propertyName} ({propertyType})");
                        }
                        catch (System.Exception e)
                        {
                            LogInfo($"  - SHADER ISSUE: Property Access Failed - {e.Message}");
                            AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Shader Property Access Failed", "HIGH", $"Cannot access shader property {i}: {e.Message}");
                            return;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    LogInfo($"  - SHADER ISSUE: Shader Analysis Failed - {e.Message}");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Shader Analysis Failed", "HIGH", $"Cannot analyze shader properties: {e.Message}");
                    return;
                }
                
                CheckMaterialInstancing(material, materialPath, context);
                
                CheckMaterialLODGroup(material, materialPath, context);
                
                LogInfo("  - Material is OK - No issues detected");
            }
            
            private void CheckMaterialTextureReferences(Material material, string materialPath, string context)
            {
                try
                {
                    var shader = material.shader;
                    int propertyCount = ShaderUtil.GetPropertyCount(shader);
                    
                    for (int i = 0; i < propertyCount; i++)
                    {
                        var propertyType = ShaderUtil.GetPropertyType(shader, i);
                        if (propertyType == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            var propertyName = ShaderUtil.GetPropertyName(shader, i);
                            if (material.HasProperty(propertyName))
                            {
                                var texture = material.GetTexture(propertyName);
                                if (texture != null)
                                {
                                    string texturePath = AssetDatabase.GetAssetPath(texture);
                                    if (!string.IsNullOrEmpty(texturePath) && !System.IO.File.Exists(texturePath))
                                    {
                                        LogInfo($"  - TEXTURE ISSUE: Missing Texture File - {propertyName}");
                                        AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Missing Texture File", "MEDIUM", $"Texture file not found for property '{propertyName}': {texture.name}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    LogInfo($"  - TEXTURE ISSUE: Texture Reference Check Failed - {e.Message}");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Texture Reference Check Failed", "MEDIUM", $"Cannot check texture references: {e.Message}");
                }
            }
            
            private void CheckMaterialKeywords(Material material, string materialPath, string context)
            {
                try
                {
                    var keywords = material.shaderKeywords;
                    var validKeywords = material.shader.keywordSpace.keywords;
                    
                    foreach (var keyword in keywords)
                    {
                        bool isValid = false;
                        foreach (var validKeyword in validKeywords)
                        {
                            if (validKeyword.name == keyword)
                            {
                                isValid = true;
                                break;
                            }
                        }
                        
                        if (!isValid)
                        {
                            LogInfo($"  - KEYWORD ISSUE: Invalid Shader Keyword - {keyword}");
                            AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Invalid Shader Keyword", "MEDIUM", $"Material uses invalid shader keyword: {keyword}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    LogInfo($"  - KEYWORD ISSUE: Keyword Check Failed - {e.Message}");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Keyword Check Failed", "MEDIUM", $"Cannot check shader keywords: {e.Message}");
                }
            }
            
            private void CheckMaterialRenderQueue(Material material, string materialPath, string context)
            {
                try
                {
                    int renderQueue = material.renderQueue;
                    if (renderQueue < 0 || renderQueue > 5000)
                    {
                        LogInfo($"  - RENDER QUEUE ISSUE: Invalid Render Queue - {renderQueue}");
                        AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Invalid Render Queue", "MEDIUM", $"Material has invalid render queue value: {renderQueue}");
                    }
                }
                catch (System.Exception e)
                {
                    LogInfo($"  - RENDER QUEUE ISSUE: Render Queue Check Failed - {e.Message}");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Render Queue Check Failed", "MEDIUM", $"Cannot check render queue: {e.Message}");
                }
            }
            
            private void CheckMaterialInstancing(Material material, string materialPath, string context)
            {
                try
                {
                    if (material.enableInstancing)
                    {
                        if (!material.shader.name.Contains("Instanced"))
                        {
                            LogInfo("  - INSTANCING ISSUE: Instancing Enabled but Shader Not Instanced");
                            AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Instancing Mismatch", "LOW", "Material has instancing enabled but shader doesn't support instancing");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    LogInfo($"  - INSTANCING ISSUE: Instancing Check Failed - {e.Message}");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Instancing Check Failed", "MEDIUM", $"Cannot check instancing: {e.Message}");
                }
            }
            
            private void CheckMaterialLODGroup(Material material, string materialPath, string context)
            {
                try
                {
                    if (material.HasProperty("_LOD"))
                    {
                        float lod = material.GetFloat("_LOD");
                        if (lod < 0 || lod > 1)
                        {
                            LogInfo($"  - LOD ISSUE: Invalid LOD Value - {lod}");
                            AddDetailedMaterialInfo(null, null, 0, context, materialPath, "Invalid LOD Value", "LOW", $"Material has invalid LOD value: {lod}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    LogInfo($"  - LOD ISSUE: LOD Check Failed - {e.Message}");
                    AddDetailedMaterialInfo(null, null, 0, context, materialPath, "LOD Check Failed", "MEDIUM", $"Cannot check LOD: {e.Message}");
                }
            }
            
            private void AddDetailedMaterialInfo(GameObject obj, Component component, int materialIndex, string sceneName, string assetPath, string issueType, string severity, string description)
            {
                var newInfo = new MissingMaterialInfo
                {
                    gameObject = obj,
                    component = component,
                    materialIndex = materialIndex,
                    sceneName = sceneName,
                    assetPath = assetPath,
                    errorReason = $"[{severity}] {issueType}: {description}",
                    gameObjectName = obj != null ? obj.name : "Standalone Material",
                    componentTypeName = component != null ? component.GetType().Name : "Material",
                    materialName = "Unknown"
                };
                
                missingMaterialResults.Add(newInfo);
                LogInfo($"Added detailed material issue: {issueType} - {description}");
            }
            
            private bool IsPackageAsset(string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath)) return false;
                
                return assetPath.StartsWith("Packages/") || assetPath.Contains("/Packages/");
            }
            
            private bool IsUnityBuiltinShader(string shaderName)
            {
                if (string.IsNullOrEmpty(shaderName)) return false;

                return shaderName.StartsWith("Universal Render Pipeline/") ||
                       shaderName.StartsWith("HDRP/") ||
                       shaderName.StartsWith("Built-in/") ||
                       shaderName.StartsWith("Legacy Shaders/") ||
                       shaderName.StartsWith("Sprites/") ||
                       shaderName.StartsWith("UI/") ||
                       shaderName.StartsWith("Standard") ||
                       shaderName.StartsWith("Mobile/") ||
                       shaderName.StartsWith("Particles/") ||
                       shaderName.StartsWith("Skybox/") ||
                       shaderName.StartsWith("Terrain/") ||
                       shaderName.StartsWith("Nature/") ||
                       shaderName.StartsWith("GUI/") ||
                       shaderName.StartsWith("Hidden/") ||
                       shaderName == "Unlit/Transparent" ||
                       shaderName == "Unlit/Transparent Cutout" ||
                       shaderName == "Unlit/Texture" ||
                       shaderName == "Diffuse" ||
                       shaderName == "Specular" ||
                       shaderName == "Bumped Diffuse" ||
                       shaderName == "Bumped Specular" ||
                       shaderName == "VertexLit" ||
                       shaderName == "Self-Illumin/Diffuse" ||
                       shaderName == "Self-Illumin/Bumped Diffuse" ||
                       shaderName == "Self-Illumin/Specular" ||
                       shaderName == "Self-Illumin/Bumped Specular" ||
                       shaderName == "Reflective/Diffuse" ||
                       shaderName == "Reflective/Specular" ||
                       shaderName == "Reflective/Bumped Diffuse" ||
                       shaderName == "Reflective/Bumped Specular" ||
                       shaderName == "Reflective/VertexLit" ||
                       shaderName == "Lightmapped/Diffuse" ||
                       shaderName == "Lightmapped/Specular" ||
                       shaderName == "Lightmapped/VertexLit" ||
                       shaderName == "Lightmapped/Bumped Diffuse" ||
                       shaderName == "Lightmapped/Bumped Specular" ||
                       shaderName == "Transparent/Diffuse" ||
                       shaderName == "Transparent/Specular" ||
                       shaderName == "Transparent/Bumped Diffuse" ||
                       shaderName == "Transparent/Bumped Specular" ||
                       shaderName == "Transparent/VertexLit" ||
                       shaderName == "Transparent/Cutout/Diffuse" ||
                       shaderName == "Transparent/Cutout/Specular" ||
                       shaderName == "Transparent/Cutout/Bumped Diffuse" ||
                       shaderName == "Transparent/Cutout/Bumped Specular" ||
                       shaderName == "Transparent/Cutout/VertexLit";
            }
            
            private string ConvertToAssetPath(string absolutePath)
            {
                string assetsPath = Application.dataPath;
                
                if (absolutePath.StartsWith(assetsPath))
                {
                    string relativePath = "Assets" + absolutePath.Substring(assetsPath.Length);
                    return relativePath.Replace("\\", "/");
                }
                
                return null;
            }
            
            private void SelectMaterialSceneFolder()
            {
                string folderPath = EditorUtility.OpenFolderPanel("Select Scene Folder", "Assets", "");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    string relativePath = ConvertToAssetPath(folderPath);
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        selectedMaterialSceneFolder = relativePath;
                        LoadMaterialScenesFromFolder(relativePath);
                    }
                    else
                    {
                        LogError($"Selected folder is not within the Assets directory: {folderPath}");
                    }
                }
            }
            
            private void LoadMaterialPrefabsFromFolder(string folderPath)
            {
                LogInfo($"Loading prefabs from folder: {folderPath}");
                
                materialPrefabsInFolder.Clear();
                selectedMaterialPrefabs.Clear();

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                LogInfo($"Found {guids.Length} prefab GUIDs in folder: {folderPath}");
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        if (HasPrefabErrors(prefab))
                    {
                        materialPrefabsInFolder.Add(prefab);
                        selectedMaterialPrefabs.Add(false);
                            LogInfo($"Loaded prefab with errors: {prefab.name} from {path}");
                        }
                        else
                        {
                            LogInfo($"Prefab has no errors, skipping: {prefab.name} from {path}");
                        }
                    }
                    else
                    {
                        LogWarning($"Failed to load prefab from path: {path}");
                    }
                }
                
                LogInfo($"Successfully loaded {materialPrefabsInFolder.Count} prefabs with errors from folder: {folderPath}");
            }
            
            private bool HasPrefabErrors(GameObject prefab)
            {
                return CheckGameObjectForErrors(prefab);
            }
            
            private bool CheckGameObjectForErrors(GameObject obj)
            {
                var meshRenderer = obj.GetComponent<MeshRenderer>();
                if (meshRenderer != null && HasRendererErrors(meshRenderer))
                    return true;
                
                var skinMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                if (skinMeshRenderer != null && HasRendererErrors(skinMeshRenderer))
                    return true;
                
                var spriteRenderer = obj.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && HasRendererErrors(spriteRenderer))
                    return true;
                
                var spriteMask = obj.GetComponent<SpriteMask>();
                if (spriteMask != null && spriteMask.sprite == null)
                    return true;
                
                var canvasRenderer = obj.GetComponent<CanvasRenderer>();
                if (canvasRenderer != null && HasCanvasRendererErrors(canvasRenderer))
                    return true;
                
                var graphic = obj.GetComponent<UnityEngine.UI.Graphic>();
                if (graphic != null && HasGraphicErrors(graphic))
                    return true;

                var lineRenderer = obj.GetComponent<LineRenderer>();
                if (lineRenderer != null && HasRendererErrors(lineRenderer))
                    return true;
                
                var trailRenderer = obj.GetComponent<TrailRenderer>();
                if (trailRenderer != null && HasRendererErrors(trailRenderer))
                    return true;
                
                var particleSystemRenderer = obj.GetComponent<ParticleSystemRenderer>();
                if (particleSystemRenderer != null && HasRendererErrors(particleSystemRenderer))
                    return true;
                
                foreach (Transform child in obj.transform)
                {
                    if (CheckGameObjectForErrors(child.gameObject))
                        return true;
                }
                
                return false;
            }
            
            private bool HasRendererErrors(Renderer renderer)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || IsErrorMaterial(materials[i]))
                        return true;
                }
                return false;
            }
            
            private bool HasCanvasRendererErrors(CanvasRenderer canvasRenderer)
            {
                Material material = canvasRenderer.GetMaterial();
                return material == null || IsErrorMaterial(material);
            }
            
            private bool HasGraphicErrors(UnityEngine.UI.Graphic graphic)
            {
                Material material = graphic.material;
                if (material == null || IsErrorMaterial(material))
                    return true;
                
                var image = graphic as UnityEngine.UI.Image;
                if (image != null && image.sprite == null)
                    return true;
                
                return false;
            }
            
            private void LoadMaterialScenesFromFolder(string folderPath)
            {
                LogInfo($"Loading scenes from folder: {folderPath}");
                
                materialScenesInFolder.Clear();
                selectedMaterialScenes.Clear();

                string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
                LogInfo($"Found {guids.Length} scene GUIDs in folder: {folderPath}");
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    if (scene != null)
                    {
                        materialScenesInFolder.Add(scene);
                        selectedMaterialScenes.Add(false);
                        LogInfo($"Loaded scene: {scene.name} from {path}");
                    }
                    else
                    {
                        LogWarning($"Failed to load scene from path: {path}");
                }
                }
                
                LogInfo($"Successfully loaded {materialScenesInFolder.Count} scenes from folder: {folderPath}");
            }

            private void FindMissingMaterials()
            {
                missingMaterialResults.Clear();
                showMaterialResults = true;
                selectedMaterialResultIndex = -1;

                if (parentWindow.materialSearchMode == MaterialSearchMode.Scene)
                {
                    FindMissingMaterialsInScenes();
                }
                else
                {
                    FindMissingMaterialsInPrefabs();
                }
            }

            private void FindMissingMaterialsInScenes()
            {
                if (parentWindow.materialSceneSearchScope == MaterialSceneSearchScope.CurrentOpenScenes)
                {
                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                    {
                        var scene = EditorSceneManager.GetSceneAt(i);
                        if (scene.IsValid())
                        {
                            FindMissingMaterialsInScene(scene);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < selectedMaterialScenes.Count; i++)
                    {
                        if (selectedMaterialScenes[i])
                        {
                            var scenePath = AssetDatabase.GetAssetPath(materialScenesInFolder[i]);
                            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                            FindMissingMaterialsInScene(scene);
                        }
                    }
                }
            }

            private void FindMissingMaterialsInPrefabs()
            {
                LogInfo($"Starting prefab search. Selected prefabs: {selectedMaterialPrefabs.Count(s => s)} out of {materialPrefabsInFolder.Count}");
                
                for (int i = 0; i < selectedMaterialPrefabs.Count; i++)
                {
                    if (selectedMaterialPrefabs[i])
                    {
                        LogInfo($"Checking prefab: {materialPrefabsInFolder[i].name}");
                        FindMissingMaterialsInPrefab(materialPrefabsInFolder[i]);
                    }
                }
                
                LogInfo($"Prefab search completed. Found {missingMaterialResults.Count} missing materials.");
            }
            
            private void FindMissingMaterialsInScene(Scene scene)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                    CheckGameObjectForMissingMaterials(obj, sceneFileName, scene.path);
                }
            }
            
            private void CheckGameObjectForMissingMaterials(GameObject obj, string sceneName, string assetPath)
            {
                LogInfo($"Checking GameObject: {obj.name} in {sceneName}");
                
                var meshRenderer = obj.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    CheckRendererForMissingMaterials(meshRenderer, obj, sceneName, assetPath);
                }
                
                var skinMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                if (skinMeshRenderer != null)
                {
                    CheckRendererForMissingMaterials(skinMeshRenderer, obj, sceneName, assetPath);
                }
                
                var spriteRenderer = obj.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    CheckRendererForMissingMaterials(spriteRenderer, obj, sceneName, assetPath);
                }
                
                var spriteMask = obj.GetComponent<SpriteMask>();
                if (spriteMask != null)
                {
                    CheckSpriteMaskForMissingMaterials(spriteMask, obj, sceneName, assetPath);
                }
                
                var canvasRenderer = obj.GetComponent<CanvasRenderer>();
                if (canvasRenderer != null)
                {
                    CheckCanvasRendererForMissingMaterials(canvasRenderer, obj, sceneName, assetPath);
                }
                
                var graphic = obj.GetComponent<UnityEngine.UI.Graphic>();
                if (graphic != null)
                {
                    CheckGraphicForMissingMaterials(graphic, obj, sceneName, assetPath);
                }
                
                var lineRenderer = obj.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    CheckRendererForMissingMaterials(lineRenderer, obj, sceneName, assetPath);
                }
                
                var trailRenderer = obj.GetComponent<TrailRenderer>();
                if (trailRenderer != null)
                {
                    CheckRendererForMissingMaterials(trailRenderer, obj, sceneName, assetPath);
                }
                
                var particleSystemRenderer = obj.GetComponent<ParticleSystemRenderer>();
                if (particleSystemRenderer != null)
                {
                    CheckRendererForMissingMaterials(particleSystemRenderer, obj, sceneName, assetPath);
                }
                
                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForMissingMaterials(child.gameObject, sceneName, assetPath);
                }
            }
            
            private void FindMissingMaterialsInPrefab(GameObject prefab)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                LogInfo($"Checking Prefab: {prefab.name} at {prefabPath}");
                
                CheckGameObjectForMissingMaterials(prefab, "Prefab Asset", prefabPath);
                
                FindPrefabInstancesInAllScenes(prefab);
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
            
            private bool SceneContainsPrefab(string scenePath, GameObject prefabAsset)
            {
                try
                {
                    if (IsPackageScene(scenePath))
                    {
                        LogInfo($"Skipping package scene: {scenePath}");
                        return false;
                    }
                    
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
                    if (IsPackageScene(scenePath))
                    {
                        LogInfo($"Skipping package scene: {scenePath}");
                        return;
                    }
                    
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
                        CheckGameObjectForMissingMaterials(obj, sceneFileName, scenePath);
                    }
                }
                
                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForPrefabInstances(child.gameObject, prefabAsset, sceneName, scenePath);
                }
            }
            
            
            
            private void CheckRendererForMissingMaterials(Renderer renderer, GameObject obj, string sceneName, string assetPath)
            {
                Material[] materials = renderer.sharedMaterials;
                LogInfo($"Checking {renderer.GetType().Name} on {obj.name} - {materials.Length} materials");
                
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        LogInfo($"Found missing material at index {i} on {obj.name}");
                        AddMissingMaterialInfo(obj, renderer, i, sceneName, assetPath, "Missing Material");
                    }
                    else if (IsErrorMaterial(materials[i]))
                    {
                        LogInfo($"Found error material '{materials[i].name}' at index {i} on {obj.name}");
                        AddMissingMaterialInfo(obj, renderer, i, sceneName, assetPath, "Error Shader");
                    }
                }
            }
            
            private void CheckSpriteMaskForMissingMaterials(SpriteMask spriteMask, GameObject obj, string sceneName, string assetPath)
            {
                if (spriteMask.sprite == null)
                {
                    AddMissingSpriteInfo(obj, spriteMask, sceneName, assetPath, "Missing Sprite");
                }
            }
            
            private void CheckCanvasRendererForMissingMaterials(CanvasRenderer canvasRenderer, GameObject obj, string sceneName, string assetPath)
            {
                Material material = canvasRenderer.GetMaterial();
                if (material == null)
                {
                    AddMissingMaterialInfoForComponent(obj, canvasRenderer, 0, sceneName, assetPath, "Missing Material", material);
                }
                else if (IsErrorMaterial(material))
                {
                    AddMissingMaterialInfoForComponent(obj, canvasRenderer, 0, sceneName, assetPath, "Error Shader", material);
                }
            }
            
            private void CheckGraphicForMissingMaterials(UnityEngine.UI.Graphic graphic, GameObject obj, string sceneName, string assetPath)
            {
                Material material = graphic.material;
                if (material == null)
                {
                    AddMissingMaterialInfoForComponent(obj, graphic, 0, sceneName, assetPath, "Missing Material", material);
                }
                else if (IsErrorMaterial(material))
                {
                    AddMissingMaterialInfoForComponent(obj, graphic, 0, sceneName, assetPath, "Error Shader", material);
                }
                
                var image = graphic as UnityEngine.UI.Image;
                if (image != null && image.sprite == null)
                {
                    AddMissingSpriteInfo(obj, image, sceneName, assetPath, "Missing Sprite");
                }
            }

            private bool IsErrorMaterial(Material material)
            {
                // 1. Material이 null인 경우
                if (material == null)
                {
                    LogInfo($"IsErrorMaterial: Material is null");
                    return true;
                }

                // 2. Shader가 null인 경우
                if (material.shader == null)
                {
                    LogInfo($"IsErrorMaterial: Material '{material.name}' has null shader");
                    return true;
                }

                var shader = material.shader;
                var shaderName = shader.name;

                // 3. Unity Error Shader인 경우 (마젠타의 가장 흔한 원인)
                if (shaderName == "Hidden/InternalErrorShader" ||
                    shaderName == "Hidden/InternalError" ||
                    shaderName.Contains("InternalErrorShader") ||
                    shaderName.Contains("Internal-Error"))
                {
                    LogInfo($"IsErrorMaterial: Unity error shader detected: {shaderName}");
                    return true;
                }

                // 4. Shader가 현재 플랫폼/파이프라인에서 지원되지 않는 경우
                if (!shader.isSupported)
                {
                    LogInfo($"IsErrorMaterial: Shader not supported: {shaderName}");
                    return true;
                }

                // 5. Shader 컴파일 에러가 있는 경우
                if (UnityVersionHelper.ShaderHasError(shader))
                {
                    LogInfo($"IsErrorMaterial: Shader has compilation errors: {shaderName}");
                    return true;
                }

                // 6. Render Pipeline Mismatch 체크
                if (IsRenderPipelineMismatch(shader, shaderName))
                {
                    LogInfo($"IsErrorMaterial: Render pipeline mismatch: {shaderName}");
                    return true;
                }

                // 7. Shader의 실제 렌더 큐 확인 - 에러 셰이더는 보통 -1을 반환
                if (shader.renderQueue < 0)
                {
                    LogInfo($"IsErrorMaterial: Invalid render queue: {shader.renderQueue} for shader {shaderName}");
                    return true;
                }

                // 8. Valid Unity builtin shader면 OK
                if (IsUnityBuiltinShader(shaderName))
                {
                    return false;
                }

                return false;
            }

            private bool IsRenderPipelineMismatch(Shader shader, string shaderName)
            {
                // 현재 렌더 파이프라인 확인
                var currentRP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                bool isHDRP = currentRP != null && currentRP.GetType().Name.Contains("HDRenderPipelineAsset");
                bool isURP = currentRP != null && currentRP.GetType().Name.Contains("UniversalRenderPipelineAsset");
                bool isBuiltIn = currentRP == null;

                // HDRP를 사용 중인데 URP/Built-in 셰이더를 사용하는 경우
                if (isHDRP)
                {
                    if (shaderName.StartsWith("Universal Render Pipeline/") ||
                        shaderName.StartsWith("URP/") ||
                        shaderName == "Standard" ||
                        shaderName.StartsWith("Legacy Shaders/") ||
                        shaderName.StartsWith("Mobile/"))
                    {
                        return true;
                    }
                }

                // URP를 사용 중인데 HDRP/Built-in 셰이더를 사용하는 경우
                if (isURP)
                {
                    if (shaderName.StartsWith("HDRP/") ||
                        shaderName == "Standard" ||
                        shaderName.StartsWith("Legacy Shaders/") ||
                        shaderName.StartsWith("Mobile/"))
                    {
                        return true;
                    }
                }

                // Built-in을 사용 중인데 HDRP/URP 셰이더를 사용하는 경우
                if (isBuiltIn)
                {
                    if (shaderName.StartsWith("HDRP/") ||
                        shaderName.StartsWith("Universal Render Pipeline/") ||
                        shaderName.StartsWith("URP/"))
                    {
                        return true;
                    }
                }

                return false;
            }


            private bool HasShaderFunctionMismatch(string shaderContent)
            {
                try
                {
                    var vertexMatch = System.Text.RegularExpressions.Regex.Match(shaderContent, @"#pragma\s+vertex\s+(\w+)");
                    if (vertexMatch.Success)
                    {
                        string declaredVertexFunction = vertexMatch.Groups[1].Value;
                        
                        var vertexFunctionMatch = System.Text.RegularExpressions.Regex.Match(shaderContent, @"(\w+)\s+vert\s*\(");
                        if (vertexFunctionMatch.Success)
                        {
                            string actualVertexFunction = vertexFunctionMatch.Groups[1].Value;
                            
                            if (declaredVertexFunction != actualVertexFunction)
                            {
                                LogInfo($"Vertex function mismatch: declared '{declaredVertexFunction}' but found '{actualVertexFunction}'");
                                return true;
                            }
                        }
                        else
                        {
                            LogInfo($"No vertex function found for declared '{declaredVertexFunction}'");
                            return true;
                        }
                    }
                    
                    var fragmentMatch = System.Text.RegularExpressions.Regex.Match(shaderContent, @"#pragma\s+fragment\s+(\w+)");
                    if (fragmentMatch.Success)
                    {
                        string declaredFragmentFunction = fragmentMatch.Groups[1].Value;
                        
                        var fragmentFunctionMatch = System.Text.RegularExpressions.Regex.Match(shaderContent, @"(\w+)\s+frag\s*\(");
                        if (fragmentFunctionMatch.Success)
                        {
                            string actualFragmentFunction = fragmentFunctionMatch.Groups[1].Value;
                            
                            if (declaredFragmentFunction != actualFragmentFunction)
                            {
                                LogInfo($"Fragment function mismatch: declared '{declaredFragmentFunction}' but found '{actualFragmentFunction}'");
                                return true;
                            }
                        }
                        else
                        {
                            LogInfo($"No fragment function found for declared '{declaredFragmentFunction}'");
                            return true;
                        }
                    }
                    
                    return false;
                }
                catch (System.Exception e)
                {
                    LogInfo($"Error checking shader function mismatch: {e.Message}");
                    return true;
                }
            }
            
            private bool HasBasicShaderSyntaxErrors(string shaderContent)
            {
                try
                {
                    int cgProgramCount = System.Text.RegularExpressions.Regex.Matches(shaderContent, @"CGPROGRAM").Count;
                    int endCgCount = System.Text.RegularExpressions.Regex.Matches(shaderContent, @"ENDCG").Count;
                    
                    if (cgProgramCount != endCgCount)
                    {
                        LogInfo($"CGPROGRAM/ENDCG mismatch: {cgProgramCount} CGPROGRAM, {endCgCount} ENDCG");
                        return true;
                    }
                    
                    int passCount = System.Text.RegularExpressions.Regex.Matches(shaderContent, @"Pass\s*\{").Count;
                    if (passCount == 0)
                    {
                        LogInfo("No Pass blocks found in shader");
                        return true;
                    }
                    
                    if (!shaderContent.Contains("Shader") || !shaderContent.Contains("SubShader"))
                    {
                        LogInfo("Missing basic shader structure (Shader/SubShader)");
                        return true;
                    }
                    
                    return false;
                }
                catch (System.Exception e)
                {
                    LogInfo($"Error checking shader syntax: {e.Message}");
                    return true;
                }
            }
            
            private void AddMissingMaterialInfo(GameObject obj, Renderer renderer, int materialIndex, string sceneName, string assetPath, string errorReason)
            {
                var newInfo = new MissingMaterialInfo
                {
                    gameObject = obj,
                    component = renderer,
                    propertyPath = $"m_Materials.Array.data[{materialIndex}]",
                    sceneName = sceneName,
                    scenePath = assetPath,
                    assetPath = assetPath,
                    gameObjectName = obj.name,
                    instanceID = obj.GetInstanceID().ToString(),
                    componentTypeName = renderer.GetType().Name,
                    material = renderer.sharedMaterials[materialIndex],
                    materialName = renderer.sharedMaterials[materialIndex]?.name ?? "Missing",
                    shaderName = renderer.sharedMaterials[materialIndex]?.shader?.name ?? "Missing",
                    errorReason = errorReason,
                    materialIndex = materialIndex
                };
                
                missingMaterialResults.Add(newInfo);
                LogInfo($"Found missing material: {obj.name} - {errorReason}");
            }
            
            private void AddMissingSpriteInfo(GameObject obj, Component component, string sceneName, string assetPath, string errorReason)
            {
                var newInfo = new MissingMaterialInfo
                {
                    gameObject = obj,
                    component = component,
                    propertyPath = "m_Sprite",
                    sceneName = sceneName,
                    scenePath = assetPath, 
                    assetPath = assetPath,
                    gameObjectName = obj.name,
                    instanceID = obj.GetInstanceID().ToString(),
                    componentTypeName = component.GetType().Name,
                    material = null,
                    materialName = "N/A",
                    shaderName = "N/A",
                    errorReason = errorReason,
                    materialIndex = -1,
                    sprite = null,
                    spriteName = "Missing"
                };
                
                missingMaterialResults.Add(newInfo);
                LogInfo($"Found missing sprite: {obj.name} - {errorReason}");
            }
            
            private void AddMissingMaterialInfoForComponent(GameObject obj, Component component, int materialIndex, string sceneName, string assetPath, string errorReason, Material material)
            {
                var newInfo = new MissingMaterialInfo
                {
                    gameObject = obj,
                    component = component,
                    propertyPath = "m_Material",
                    sceneName = sceneName,
                    scenePath = assetPath, 
                    assetPath = assetPath,
                    gameObjectName = obj.name,
                    instanceID = obj.GetInstanceID().ToString(),
                    componentTypeName = component.GetType().Name,
                    material = material,
                    materialName = material?.name ?? "Missing",
                    shaderName = material?.shader?.name ?? "Missing",
                    errorReason = errorReason,
                    materialIndex = materialIndex
                };
                
                missingMaterialResults.Add(newInfo);
                LogInfo($"Found missing material: {obj.name} - {errorReason}");
            }
            
            private GameObject FindGameObjectInSceneInternal(Scene scene, string objectName)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject rootObj in rootObjects)
                {
                    GameObject found = FindGameObjectRecursiveInternal(rootObj, objectName);
                    if (found != null)
                        return found;
                }
                return null;
            }
            
            private GameObject FindGameObjectRecursiveInternal(GameObject obj, string objectName)
            {
                if (obj.name == objectName)
                    return obj;
                    
                foreach (Transform child in obj.transform)
                {
                    GameObject found = FindGameObjectRecursiveInternal(child.gameObject, objectName);
                    if (found != null)
                        return found;
                }
                
                return null;
            }
            
            private bool RemoveMissingMaterial(MissingMaterialInfo materialInfo)
            {
                LogInfo($"Attempting to remove missing material from {materialInfo.gameObjectName}");
                
                if (materialInfo.gameObject == null)
                {
                    LogInfo($"GameObject is null, attempting to find it in scene: {materialInfo.sceneName}");
                    
                    if (!string.IsNullOrEmpty(materialInfo.scenePath))
                    {
                        try
                        {
                            Scene scene = EditorSceneManager.OpenScene(materialInfo.scenePath, OpenSceneMode.Single);
                            if (scene.IsValid())
                            {
                                LogInfo($"Successfully opened scene: {scene.name}");
                                
                                GameObject foundObject = FindGameObjectInSceneInternal(scene, materialInfo.gameObjectName);
                                if (foundObject != null)
                                {
                                    LogInfo($"Found GameObject: {foundObject.name} in opened scene");
                                    
                                    materialInfo.gameObject = foundObject;
                                    
                                    if (materialInfo.component != null)
                                    {
                                        materialInfo.component = foundObject.GetComponent(materialInfo.component.GetType());
                                    }
                                }
                                else
                                {
                                    LogWarning($"Could not find GameObject {materialInfo.gameObjectName} in opened scene {scene.name}");
                                    return false;
                                }
                            }
                            else
                            {
                                LogError($"Failed to open scene: {materialInfo.scenePath}");
                                return false;
                            }
                        }
                        catch (System.Exception e)
                        {
                            LogError($"Error opening scene {materialInfo.scenePath}: {e.Message}");
                            return false;
                        }
                    }
                    else
                    {
                        LogError($"No scene path available for GameObject: {materialInfo.gameObjectName}");
                        return false;
                    }
                }
                
                if (materialInfo.gameObject != null)
                {
                    if (materialRemovalMode == RemovalMode.DeleteGameObjects)
                    {
                        return DeleteGameObjectWithMissingMaterial(materialInfo);
                    }
                    
                    if (PrefabUtility.IsPartOfPrefabAsset(materialInfo.gameObject))
                    {
                        return DeletePrefabFile(materialInfo);
                    }
                    
                    var renderer = materialInfo.component as Renderer;
                    if (renderer != null)
                    {
                        var materials = renderer.sharedMaterials.ToList();
                        materials[materialInfo.materialIndex] = null;
                        renderer.sharedMaterials = materials.ToArray();
                        
                        if (materialInfo.gameObject.scene.IsValid())
                        {
                            EditorSceneManager.MarkSceneDirty(materialInfo.gameObject.scene);
                        }
                    }

                    missingMaterialResults.Remove(materialInfo);
                    LogInfo($"Successfully removed missing material from {materialInfo.gameObject.name}");
                    return true;
                }
                
                LogWarning($"Cannot remove missing material: GameObject is still null after scene search");
                return false;
            }
            
            private bool DeleteGameObjectWithMissingMaterial(MissingMaterialInfo materialInfo)
            {
                if (PrefabUtility.IsPartOfPrefabAsset(materialInfo.gameObject))
                {
                    return DeletePrefabFile(materialInfo);
                }
                else
                {
                    if (materialInfo.gameObject.scene.IsValid())
                    {
                        EditorSceneManager.MarkSceneDirty(materialInfo.gameObject.scene);
                    }
                    GameObject.DestroyImmediate(materialInfo.gameObject);
                    missingMaterialResults.Remove(materialInfo);
                    LogInfo($"Deleted GameObject with missing material: {materialInfo.gameObjectName}");
                    return true;
                }
            }
            
            private bool DeletePrefabFile(MissingMaterialInfo materialInfo)
            {
                string assetPath = AssetDatabase.GetAssetPath(materialInfo.gameObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    missingMaterialResults.Remove(materialInfo);
                    LogInfo($"Deleted prefab file: {assetPath}");
                    return true;
                }
                return false;
            }

            private void RemoveAllMissingMaterials()
            {
                for (int i = missingMaterialResults.Count - 1; i >= 0; i--)
                {
                    RemoveMissingMaterial(missingMaterialResults[i]);
                }
                
                if (missingMaterialResults.Count == 0)
                {
                    showMaterialResults = false;
                    selectedMaterialResultIndex = -1;
                }
            }
        }
    }
}