using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TerrainTools;
using UnityEngine.TerrainUtils;

namespace Kirist.EditorTool
{
    public partial class KiristWindow
    {
        public partial class SceneAnalyzer : BaseFinderBehaviour
        {
            private SceneAnalysisResult currentAnalysisResult = null;
            private bool isAnalyzing = false;
            private float analysisProgress = 0f;
            private string analysisStatus = "";
            
            private Vector2 sceneAnalysisScrollPos = Vector2.zero;
            private Vector2 objectListScrollPos = Vector2.zero;
            private Vector2 environmentScrollPos = Vector2.zero;
            private Vector2 errorScrollPos = Vector2.zero;
            
            private bool analyzeGameObjects = true;
            private bool analyzeEnvironment = true;
            private bool analyzeErrors = true;
            private bool analyzePerformance = true;
            private bool includeInactiveObjects = false;
            
            private bool checkMissingScripts = true;
            private bool checkMissingMaterials = true;
            private bool checkMissingPrefabs = true;
            private bool autoFixErrors = false;
            
            private SceneAnalysisMode analysisMode = SceneAnalysisMode.CurrentScene;
            private SceneAsset selectedSceneAsset = null;
            
            private int selectedObjectIndex = -1;
            private GameObject selectedGameObject = null;
            
            private string originalScenePath = null;
            private bool wasAnalyzingSpecificScene = false;
            
            public SceneAnalyzer(KiristWindow parent) : base(parent)
            {
            }
            
            public override void DrawUI()
            {
                var tealBackgroundStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = CreateGradientTexture(new Color(0.15f, 0.25f, 0.25f, 1f), new Color(0.20f, 0.30f, 0.30f, 1f)) },
                    border = new RectOffset(3, 3, 3, 3),
                    padding = new RectOffset(15, 15, 15, 15)
                };
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginVertical(tealBackgroundStyle);
                
                DrawTitle("🌍 SCENE ANALYZER");
                EditorGUILayout.Space(5);
                
                var tealSectionStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = CreateGradientTexture(new Color(0.25f, 0.35f, 0.35f, 1f), new Color(0.30f, 0.40f, 0.40f, 1f)) },
                    border = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(8, 8, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2)
                };
                
                EditorGUILayout.BeginVertical(tealSectionStyle);
                DrawAnalysisOptions();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(3);
                
                EditorGUILayout.BeginVertical(tealSectionStyle);
                DrawAnalysisButton();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(3);
                if (currentAnalysisResult != null)
                {
                    DrawAnalysisResults();
                }
                else if (isAnalyzing)
                {
                    DrawAnalyzingIndicator();
                }
                else
                {
                    DrawEmptyState();
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
            
            private void DrawAnalysisOptions()
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical(GUILayout.Width(200));
                if (currentAnalysisResult?.sceneSnapshot != null)
                {
                    var snapshotLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter
                    };
                    EditorGUILayout.LabelField("📸 Scene Snapshot", snapshotLabelStyle);
                    EditorGUILayout.Space(2);

                    var snapshotRect = GUILayoutUtility.GetRect(180, 180, GUILayout.Width(180), GUILayout.Height(180));
                    GUI.Box(snapshotRect, GUIContent.none, EditorStyles.helpBox);
                    GUI.DrawTexture(snapshotRect, currentAnalysisResult.sceneSnapshot, ScaleMode.ScaleToFit);
                }
                else
                {
                    var noSnapshotRect = GUILayoutUtility.GetRect(180, 180, GUILayout.Width(180), GUILayout.Height(180));
                    GUI.Box(noSnapshotRect, GUIContent.none, EditorStyles.helpBox);

                    var emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };
                    GUI.Label(noSnapshotRect, "No snapshot\navailable", emptyStyle);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("🔧 Analysis Options", EditorStyles.boldLabel);
                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Mode:", GUILayout.Width(45));
                analysisMode = (SceneAnalysisMode)EditorGUILayout.EnumPopup(analysisMode, GUILayout.Width(140));
                EditorGUILayout.EndHorizontal();

                if (analysisMode == SceneAnalysisMode.SpecificScene)
                {
                    EditorGUILayout.Space(2);
                    DrawSceneDropArea();
                }

                EditorGUILayout.Space(2);

                var miniToggleStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10 };
                EditorGUILayout.BeginHorizontal();
                analyzeGameObjects = EditorGUILayout.ToggleLeft("GameObjects", analyzeGameObjects, miniToggleStyle, GUILayout.Width(90));
                analyzeEnvironment = EditorGUILayout.ToggleLeft("Environment", analyzeEnvironment, miniToggleStyle, GUILayout.Width(90));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                analyzeErrors = EditorGUILayout.ToggleLeft("Errors", analyzeErrors, miniToggleStyle, GUILayout.Width(90));
                analyzePerformance = EditorGUILayout.ToggleLeft("Performance", analyzePerformance, miniToggleStyle, GUILayout.Width(90));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                includeInactiveObjects = EditorGUILayout.ToggleLeft("Inactive Objects", includeInactiveObjects, miniToggleStyle, GUILayout.Width(110));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();
                checkMissingScripts = EditorGUILayout.ToggleLeft("Scripts", checkMissingScripts, miniToggleStyle, GUILayout.Width(60));
                checkMissingMaterials = EditorGUILayout.ToggleLeft("Materials", checkMissingMaterials, miniToggleStyle, GUILayout.Width(70));
                checkMissingPrefabs = EditorGUILayout.ToggleLeft("Prefabs", checkMissingPrefabs, miniToggleStyle, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                autoFixErrors = EditorGUILayout.ToggleLeft("Auto Fix", autoFixErrors, miniToggleStyle, GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }
            
            private void DrawSceneDropArea()
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                var dropArea = EditorGUILayout.GetControlRect(GUILayout.Height(50));
                
                if (selectedSceneAsset != null)
                {
                    EditorGUI.DrawRect(dropArea, new Color(0.2f, 0.8f, 0.2f, 0.3f));
                    
                    var iconRect = new Rect(dropArea.x + 5, dropArea.y + 5, 15, 15);
                    var textRect = new Rect(dropArea.x + 25, dropArea.y + 8, dropArea.width - 70, 15);
                    var buttonRect = new Rect(dropArea.x + dropArea.width - 60, dropArea.y + 8, 55, 15);
                    
                    GUI.Label(iconRect, "🌍", new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
                    EditorGUI.LabelField(textRect, selectedSceneAsset.name, EditorStyles.miniLabel);
                    
                    if (GUI.Button(buttonRect, "Remove"))
                    {
                        selectedSceneAsset = null;
                    }
                }
                else
                {
                    EditorGUI.DrawRect(dropArea, new Color(0.3f, 0.3f, 0.3f, 0.3f));
                    
                    var iconRect = new Rect(dropArea.x + dropArea.width/2 - 10, dropArea.y + 5, 20, 20);
                    var textRect = new Rect(dropArea.x, dropArea.y + 30, dropArea.width, 15);
                    
                    GUI.Label(iconRect, "🌍", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter });
                    EditorGUI.LabelField(textRect, "Drop Scene Here", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 10 });
                }
                var evt = Event.current;
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    if (dropArea.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            
                            foreach (var draggedObject in DragAndDrop.objectReferences)
                            {
                                if (draggedObject is SceneAsset sceneAsset)
                                {
                                    selectedSceneAsset = sceneAsset;
                                    break;
                                }
                            }
                        }
                        
                        evt.Use();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawAnalysisButton()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                string buttonText = "🔍 ANALYZE";
                
                bool canAnalyze = analysisMode == SceneAnalysisMode.CurrentScene || selectedSceneAsset != null;
                
                GUI.enabled = canAnalyze;
                
                if (GUILayout.Button(buttonText, UIStyles.SearchButtonStyle, GUILayout.Width(150), GUILayout.Height(25)))
                {
                    if (analysisMode == SceneAnalysisMode.CurrentScene)
                    {
                        AnalyzeCurrentScene();
                    }
                    else
                    {
                        AnalyzeSpecificScene(selectedSceneAsset);
                    }
                }
                
                GUI.enabled = true;
                
                if (currentAnalysisResult != null)
                {
                    if (GUILayout.Button("🗑️ Clear", UIStyles.ButtonStyle, GUILayout.Width(80), GUILayout.Height(25)))
                    {
                        currentAnalysisResult = null;
                        isAnalyzing = false;
                        analysisProgress = 0f;
                        analysisStatus = "";
                        selectedObjectIndex = -1;
                        selectedGameObject = null;
                        selectedSceneAsset = null;
                        
                        AssetDatabase.Refresh();
                        
                        LogInfo("Analysis results cleared. Please run analysis again to apply new shader detection logic.");
                    }
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            private void DrawAnalyzingIndicator()
            {
                EditorGUILayout.BeginVertical(UIStyles.GradientBackgroundStyle);
                
                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UIColors.ModernTeal }
                };
                EditorGUILayout.LabelField("🔍 Analyzing Scene...", titleStyle);
                EditorGUILayout.Space(10);
                
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), analysisProgress, $"{analysisProgress * 100:F0}%");
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField(analysisStatus, EditorStyles.centeredGreyMiniLabel);
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawEmptyState()
            {
                EditorGUILayout.BeginVertical(UIStyles.GradientBackgroundStyle);
                
                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UIColors.ModernTeal }
                };
                EditorGUILayout.LabelField("🌍 Scene Analyzer", titleStyle);
                EditorGUILayout.Space(10);
                
                var helpStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    fontSize = 12,
                    wordWrap = true,
                    normal = { textColor = UIColors.Info }
                };
                EditorGUILayout.HelpBox("Click 'ANALYZE CURRENT SCENE' to start analyzing the current scene. This will provide comprehensive information about GameObjects, Environment settings, and potential issues.", MessageType.Info);
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawAnalysisResults()
            {
                sceneAnalysisScrollPos = EditorGUILayout.BeginScrollView(sceneAnalysisScrollPos);

                if (currentAnalysisResult.sceneSnapshot != null)
                {
                    DrawSceneSnapshot();
                    EditorGUILayout.Space(10);
                }

                DrawSceneOverview();
                EditorGUILayout.Space(10);
                
                if (analyzeErrors)
                {
                    DrawErrorAnalysis();
                    EditorGUILayout.Space(10);
                }
                
                if (analyzeGameObjects)
                {
                    DrawGameObjectAnalysis();
                    EditorGUILayout.Space(10);
                }
                
                if (analyzeEnvironment)
                {
                    DrawEnvironmentAnalysis();
                    EditorGUILayout.Space(10);
                }
                
                if (analyzePerformance)
                {
                    DrawPerformanceAnalysis();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            private void DrawSceneSnapshot()
            {
                EditorGUILayout.BeginVertical(UIStyles.GradientBackgroundStyle);

                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UIColors.ModernBlue }
                };
                EditorGUILayout.LabelField("📸 Scene Snapshot", titleStyle);
                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                var snapshotRect = GUILayoutUtility.GetRect(256, 256, GUILayout.Width(256), GUILayout.Height(256));
                GUI.Box(snapshotRect, GUIContent.none, EditorStyles.helpBox);
                GUI.DrawTexture(snapshotRect, currentAnalysisResult.sceneSnapshot, ScaleMode.ScaleToFit);

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.EndVertical();
            }

            private void DrawSceneOverview()
            {
                EditorGUILayout.BeginVertical(UIStyles.GradientBackgroundStyle);

                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UIColors.ModernGreen }
                };
                EditorGUILayout.LabelField($"📊 Scene Overview: {currentAnalysisResult.sceneName}", titleStyle);
                EditorGUILayout.Space(10);
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📋 Scene Information", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, normal = { textColor = UIColors.ModernBlue } });
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Scene Path: {currentAnalysisResult.scenePath}", EditorStyles.miniLabel, GUILayout.Width(300));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Analysis Time: {currentAnalysisResult.analysisTime:yyyy-MM-dd HH:mm:ss}", EditorStyles.miniLabel, GUILayout.Width(300));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🎯 Object Statistics", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, normal = { textColor = UIColors.ModernBlue } });
                EditorGUILayout.Space(5);
                
                var statsStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = UIColors.ModernBlue }
                };
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🎯 Total Objects: {currentAnalysisResult.totalObjects}", statsStyle, GUILayout.Width(200));
                EditorGUILayout.LabelField($"✅ Active Objects: {currentAnalysisResult.activeObjects}", statsStyle, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🔧 Components: {currentAnalysisResult.totalComponents}", statsStyle, GUILayout.Width(200));
                EditorGUILayout.LabelField($"🎨 Materials: {currentAnalysisResult.totalMaterials}", statsStyle, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📜 Scripts: {currentAnalysisResult.totalScripts}", statsStyle, GUILayout.Width(200));
                EditorGUILayout.LabelField($"🖼️ Renderers: {currentAnalysisResult.totalRenderers}", statsStyle, GUILayout.Width(200));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(10);
                
                if (currentAnalysisResult.componentTypes != null && currentAnalysisResult.componentTypes.Count > 0)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField("🔧 Component Types", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, normal = { textColor = UIColors.ModernBlue } });
                    EditorGUILayout.Space(5);
                    
                    var sortedComponents = currentAnalysisResult.componentTypes.OrderByDescending(x => x.Value).Take(8);
                    foreach (var component in sortedComponents)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {component.Key}", EditorStyles.miniLabel, GUILayout.Width(200));
                        EditorGUILayout.LabelField($"{component.Value}", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.ModernGreen } }, GUILayout.Width(50));
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.Space(10);
                }
                
                if (currentAnalysisResult.errorCount > 0)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField("⚠️ Error Summary", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, normal = { textColor = UIColors.Danger } });
                    EditorGUILayout.Space(5);
                    
                    var errorStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 14,
                        normal = { textColor = UIColors.Danger }
                    };
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"⚠️ Total Errors: {currentAnalysisResult.errorCount}", errorStyle, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"🔴 High: {currentAnalysisResult.highSeverityErrors}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, normal = { textColor = UIColors.Danger } }, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"🟡 Medium: {currentAnalysisResult.mediumSeverityErrors}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, normal = { textColor = UIColors.Warning } }, GUILayout.Width(110));
                    EditorGUILayout.LabelField($"🔵 Low: {currentAnalysisResult.lowSeverityErrors}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, normal = { textColor = UIColors.Info } }, GUILayout.Width(80));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.LabelField("✅ No Errors Found", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, normal = { textColor = UIColors.Success } });
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            public override void ClearResults()
            {
                currentAnalysisResult = null;
                isAnalyzing = false;
                analysisProgress = 0f;
                analysisStatus = "";
                selectedObjectIndex = -1;
                selectedGameObject = null;
                selectedSceneAsset = null;
                
                System.GC.Collect();
                AssetDatabase.Refresh();
                
                LogInfo("=== CACHE CLEARED - READY FOR NEW ANALYSIS ===");
            }
            
            private void AnalyzeCurrentScene()
            {
                if (isAnalyzing) return;
                
                currentAnalysisResult = null;
                System.GC.Collect();
                AssetDatabase.Refresh();
                
                isAnalyzing = true;
                analysisProgress = 0f;
                analysisStatus = "Starting analysis...";
                
                try
                {
                    currentAnalysisResult = new SceneAnalysisResult();
                    currentAnalysisResult.sceneName = SceneManager.GetActiveScene().name;
                    currentAnalysisResult.scenePath = SceneManager.GetActiveScene().path;
                    
                    EditorApplication.update += UpdateAnalysis;
                }
                catch (System.Exception e)
                {
                    LogError($"Error starting scene analysis: {e.Message}");
                    isAnalyzing = false;
                }
            }
            
            private void AnalyzeSpecificScene(SceneAsset sceneAsset)
            {
                if (isAnalyzing) return;
                
                isAnalyzing = true;
                analysisProgress = 0f;
                analysisStatus = "Starting analysis...";
                
                try
                {
                    var currentScene = SceneManager.GetActiveScene();
                    originalScenePath = currentScene.path;
                    wasAnalyzingSpecificScene = true;
                    
                    currentAnalysisResult = new SceneAnalysisResult();
                    currentAnalysisResult.sceneName = sceneAsset.name;
                    currentAnalysisResult.scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                    
                    var scene = EditorSceneManager.OpenScene(currentAnalysisResult.scenePath, OpenSceneMode.Single);
                    
                    LogInfo($"Analyzing specific scene: {sceneAsset.name} (Single mode)");
                    LogInfo($"Original scene saved: {originalScenePath}");
                    
                    EditorApplication.update += UpdateAnalysis;
                }
                catch (System.Exception e)
                {
                    LogError($"Error starting scene analysis: {e.Message}");
                    isAnalyzing = false;
                    wasAnalyzingSpecificScene = false;
                    originalScenePath = null;
                }
            }
            
            private void UpdateAnalysis()
            {
                try
                {
                    if (!isAnalyzing) return;
                    
                    if (currentAnalysisResult == null)
                    {
                        isAnalyzing = false;
                        EditorApplication.update -= UpdateAnalysis;
                        return;
                    }
                    
                    if (analysisProgress < 0.2f)
                    {
                        analysisStatus = "Analyzing GameObjects...";
                        AnalyzeGameObjects();
                        analysisProgress = 0.2f;
                    }
                    else if (analysisProgress < 0.4f)
                    {
                        analysisStatus = "Analyzing Components...";
                        AnalyzeComponents();
                        analysisProgress = 0.4f;
                    }
                    else if (analysisProgress < 0.6f)
                    {
                        analysisStatus = "Analyzing Environment...";
                        AnalyzeEnvironment();
                        analysisProgress = 0.6f;
                    }
                    else if (analysisProgress < 0.8f)
                    {
                        analysisStatus = "Detecting Errors...";
                        AnalyzeErrors();
                        analysisProgress = 0.8f;
                    }
                    else if (analysisProgress < 0.9f)
                    {
                        analysisStatus = "Capturing Scene Snapshot...";
                        CaptureSceneSnapshot();
                        analysisProgress = 0.9f;
                    }
                    else if (analysisProgress < 1.0f)
                    {
                        analysisStatus = "Finalizing Analysis...";
                        FinalizeAnalysis();
                        analysisProgress = 1.0f;
                    }
                    else
                    {
                        analysisStatus = "Analysis Complete!";
                        isAnalyzing = false;
                        EditorApplication.update -= UpdateAnalysis;
                        LogInfo($"Scene analysis completed: {currentAnalysisResult.sceneName}");
                        
                        if (wasAnalyzingSpecificScene && !string.IsNullOrEmpty(originalScenePath))
                        {
                            try
                            {
                                LogInfo($"Returning to original scene: {originalScenePath}");
                                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                            }
                            catch (System.Exception e)
                            {
                                LogError($"Error returning to original scene: {e.Message}");
                            }
                            finally
                            {
                                wasAnalyzingSpecificScene = false;
                                originalScenePath = null;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"Error during analysis: {e.Message}");
                    isAnalyzing = false;
                    EditorApplication.update -= UpdateAnalysis;
                    
                    if (wasAnalyzingSpecificScene && !string.IsNullOrEmpty(originalScenePath))
                    {
                        try
                        {
                            LogInfo($"Error occurred, returning to original scene: {originalScenePath}");
                            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
                        }
                        catch (System.Exception returnError)
                        {
                            LogError($"Error returning to original scene: {returnError.Message}");
                        }
                        finally
                        {
                            wasAnalyzingSpecificScene = false;
                            originalScenePath = null;
                        }
                    }
                }
            }
            
            private void AnalyzeGameObjects()
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();
                
                currentAnalysisResult.totalObjects = 0;
                currentAnalysisResult.activeObjects = 0;
                currentAnalysisResult.gameObjects = new List<GameObjectInfo>();
                
                foreach (var rootObj in rootObjects)
                {
                    AnalyzeGameObjectRecursive(rootObj);
                }
                
            }
            
            private void AnalyzeGameObjectRecursive(GameObject obj)
            {
                if (!includeInactiveObjects && !obj.activeInHierarchy) return;
                
                currentAnalysisResult.totalObjects++;
                if (obj.activeInHierarchy) currentAnalysisResult.activeObjects++;
                
                bool isPrefab = PrefabUtility.IsPartOfPrefabInstance(obj);
                bool isMissingPrefab = PrefabUtility.IsPrefabAssetMissing(obj);
                var prefabType = PrefabUtility.GetPrefabAssetType(obj);
                var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
                
                LogInfo($"Collecting GameObject: {obj.name} (Active: {obj.activeInHierarchy}, IsPrefab: {isPrefab}, IsMissing: {isMissingPrefab}, Type: {prefabType}, Status: {prefabStatus})");
                
                var objInfo = new GameObjectInfo
                {
                    gameObject = obj,
                    name = obj.name,
                    isActive = obj.activeInHierarchy,
                    layer = obj.layer,
                    tag = obj.tag,
                    components = new List<ComponentInfo>(),
                    childCount = obj.transform.childCount
                };
                
                var components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    var compInfo = new ComponentInfo
                    {
                        component = component,
                        componentType = component.GetType(),
                        isMissing = false
                    };
                    
                    objInfo.components.Add(compInfo);
                }
                
                currentAnalysisResult.gameObjects.Add(objInfo);
                
                foreach (Transform child in obj.transform)
                {
                    AnalyzeGameObjectRecursive(child.gameObject);
                }
            }
            
            private void AnalyzeComponents()
            {
                currentAnalysisResult.totalComponents = 0;
                currentAnalysisResult.totalScripts = 0;
                currentAnalysisResult.totalRenderers = 0;
                currentAnalysisResult.totalMaterials = 0;
                currentAnalysisResult.componentTypes = new Dictionary<string, int>();
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    foreach (var compInfo in objInfo.components)
                    {
                        currentAnalysisResult.totalComponents++;
                        
                        var typeName = compInfo.componentType.Name;
                        if (currentAnalysisResult.componentTypes.ContainsKey(typeName))
                            currentAnalysisResult.componentTypes[typeName]++;
                        else
                            currentAnalysisResult.componentTypes[typeName] = 1;
                        
                        if (compInfo.componentType == typeof(MonoBehaviour))
                            currentAnalysisResult.totalScripts++;
                        else if (compInfo.component is Renderer)
                            currentAnalysisResult.totalRenderers++;
                    }
                }
            }
            
            private void AnalyzeEnvironment()
            {
                currentAnalysisResult.environmentInfo = new EnvironmentInfo();
                
                AnalyzeLighting();
                
                AnalyzeCameras();
                
                AnalyzeTerrains();
                
                AnalyzePostProcessing();
            }
            
            private void AnalyzeLighting()
            {
                var lights = FindObjectsOfType<Light>();
                currentAnalysisResult.environmentInfo.lightCount = lights.Length;
                currentAnalysisResult.environmentInfo.lights = new List<LightInfo>();
                
                foreach (var light in lights)
                {
                    var lightInfo = new LightInfo
                    {
                        light = light,
                        type = light.type,
                        intensity = light.intensity,
                        range = light.range,
                        color = light.color,
                        isActive = light.gameObject.activeInHierarchy
                    };
                    
                    currentAnalysisResult.environmentInfo.lights.Add(lightInfo);
                }
                
                var lightingSettings = LightmapSettings.lightmaps;
                currentAnalysisResult.environmentInfo.lightmapCount = lightingSettings.Length;
                
                var renderSettings = RenderSettings.defaultReflectionMode;
                currentAnalysisResult.environmentInfo.reflectionMode = renderSettings.ToString();
            }
            
            private void AnalyzeCameras()
            {
                var cameras = FindObjectsOfType<Camera>();
                currentAnalysisResult.environmentInfo.cameraCount = cameras.Length;
                currentAnalysisResult.environmentInfo.cameras = new List<CameraInfo>();
                
                foreach (var camera in cameras)
                {
                    var cameraInfo = new CameraInfo
                    {
                        camera = camera,
                        fieldOfView = camera.fieldOfView,
                        nearClipPlane = camera.nearClipPlane,
                        farClipPlane = camera.farClipPlane,
                        clearFlags = camera.clearFlags,
                        backgroundColor = camera.backgroundColor,
                        isActive = camera.gameObject.activeInHierarchy
                    };
                    
                    currentAnalysisResult.environmentInfo.cameras.Add(cameraInfo);
                }
            }
            
            private void AnalyzeTerrains()
            {
                var terrains = FindObjectsOfType<Terrain>();
                currentAnalysisResult.environmentInfo.terrainCount = terrains.Length;
                currentAnalysisResult.environmentInfo.terrains = new List<TerrainInfo>();
                
                foreach (var terrain in terrains)
                {
                    var terrainInfo = new TerrainInfo
                    {
                        terrain = terrain,
                        terrainData = terrain.terrainData,
                        heightmapResolution = terrain.terrainData?.heightmapResolution ?? 0,
                        detailResolution = terrain.terrainData?.detailResolution ?? 0,
                        alphamapResolution = terrain.terrainData?.alphamapResolution ?? 0,
                        isActive = terrain.gameObject.activeInHierarchy
                    };
                    
                    currentAnalysisResult.environmentInfo.terrains.Add(terrainInfo);
                }
            }
            
            private void AnalyzePostProcessing()
            {
                
                var volumes = FindObjectsOfType<Volume>();
                currentAnalysisResult.environmentInfo.postProcessingVolumeCount = volumes.Length;
                currentAnalysisResult.environmentInfo.postProcessingVolumes = new List<PostProcessingInfo>();
                
                foreach (var volume in volumes)
                {
                    var ppInfo = new PostProcessingInfo
                    {
                        volume = volume,
                        isGlobal = volume.isGlobal,
                        priority = volume.priority,
                        blendDistance = volume.blendDistance,
                        weight = volume.weight,
                        isActive = volume.gameObject.activeInHierarchy,
                        profile = null, 
                        settingsCount = 0,
                        activeSettings = new List<string>(),
                        inactiveSettings = new List<string>()
                    };
                    
                
                    try
                    {
                    
                        var profileProperty = volume.GetType().GetProperty("profile");
                        if (profileProperty != null)
                        {
                            ppInfo.profile = profileProperty.GetValue(volume) as ScriptableObject;
                            
                            if (ppInfo.profile != null)
                            {
                                AnalyzeVolumeProfile(ppInfo);
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogWarning($"Post Processing package not available: {e.Message}");
                    }
                    
                    currentAnalysisResult.environmentInfo.postProcessingVolumes.Add(ppInfo);
                }
            }
            
            private void AnalyzeVolumeProfile(PostProcessingInfo ppInfo)
            {
                if (ppInfo.profile == null) return;
                
                try
                {
                    
                    var componentsProperty = ppInfo.profile.GetType().GetProperty("components");
                    if (componentsProperty != null)
                    {
                        var settings = componentsProperty.GetValue(ppInfo.profile) as System.Collections.IList;
                        if (settings != null)
                        {
                            ppInfo.settingsCount = settings.Count;
                            
                            foreach (var setting in settings)
                            {
                                if (setting == null) continue;
                                
                                var settingName = setting.GetType().Name;
                                
                               
                                var isActive = IsPostProcessingSettingActive(setting);
                                
                                if (isActive)
                                {
                                    ppInfo.activeSettings.Add(settingName);
                                }
                                else
                                {
                                    ppInfo.inactiveSettings.Add(settingName);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    LogWarning($"Error analyzing volume profile: {e.Message}");
                }
            }
            
            private bool IsPostProcessingSettingActive(object setting)
            {
                try
                {
                 
                    var type = setting.GetType();
                    var enabledField = type.GetField("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (enabledField != null)
                    {
                        return (bool)enabledField.GetValue(setting);
                    }
                    
                
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            
            private void AnalyzeErrors()
            {
                currentAnalysisResult.errors = new List<SceneError>();
                
                LogInfo("Starting error analysis...");
                
                AnalyzeMissingScripts();
                LogInfo($"After AnalyzeMissingScripts: {currentAnalysisResult.errors.Count} errors");
                
                AnalyzeMissingMaterials();
                LogInfo($"After AnalyzeMissingMaterials: {currentAnalysisResult.errors.Count} errors");
                
                AnalyzeErrorShaders();
                LogInfo($"After AnalyzeErrorShaders: {currentAnalysisResult.errors.Count} errors");
                
                CheckForMissingPrefabs();
                LogInfo($"After CheckForMissingPrefabs: {currentAnalysisResult.errors.Count} errors");
                
                AnalyzeMissingPrefabs();
                LogInfo($"After AnalyzeMissingPrefabs: {currentAnalysisResult.errors.Count} errors");
                
                AnalyzePrefabReferences();
                LogInfo($"After AnalyzePrefabReferences: {currentAnalysisResult.errors.Count} errors");
                
                if (analyzePerformance)
                {
                    AnalyzePerformanceIssues();
                    LogInfo($"After AnalyzePerformanceIssues: {currentAnalysisResult.errors.Count} errors");
                }
                
                LogInfo($"Error analysis completed. Total errors: {currentAnalysisResult.errors.Count}");
            }
            
            private void AnalyzeMissingScripts()
            {
                LogInfo("Analyzing missing scripts...");
                int missingScriptCount = 0;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var components = objInfo.gameObject.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            missingScriptCount++;
                            LogInfo($"Found missing script: {objInfo.gameObject.name} at index {i}");
                            
                            var error = new SceneError
                            {
                                type = SceneErrorType.MissingScript,
                                severity = SceneErrorSeverity.High,
                                gameObject = objInfo.gameObject,
                                message = $"Missing Script at component index {i}",
                                componentIndex = i,
                                gameObjectName = objInfo.gameObject.name,
                                gameObjectPath = GetGameObjectPath(objInfo.gameObject),
                                componentTypeName = "Missing Script"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                        }
                    }
                }
                
                LogInfo($"Missing scripts analysis completed. Found {missingScriptCount} missing scripts.");
            }
            
            private void AnalyzeMissingMaterials()
            {
                LogInfo("Analyzing missing materials...");
                int missingMaterialCount = 0;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var renderers = objInfo.gameObject.GetComponents<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (materials[i] == null)
                            {
                                missingMaterialCount++;
                                LogInfo($"Found missing material: {objInfo.gameObject.name} at index {i}");
                                
                                var error = new SceneError
                                {
                                    type = SceneErrorType.MissingMaterial,
                                    severity = SceneErrorSeverity.Medium,
                                    gameObject = objInfo.gameObject,
                                    message = $"Missing Material at index {i}",
                                    component = renderer,
                                    materialIndex = i,
                                    gameObjectName = objInfo.gameObject.name,
                                    gameObjectPath = GetGameObjectPath(objInfo.gameObject),
                                    componentTypeName = renderer.GetType().Name
                                };
                                
                                currentAnalysisResult.errors.Add(error);
                            }
                        }
                    }
                }
                
                LogInfo($"Missing materials analysis completed. Found {missingMaterialCount} missing materials.");
            }
            
            private void AnalyzeErrorShaders()
            {
                LogInfo("=== ANALYZING ERROR SHADERS ===");
                int errorShaderCount = 0;
                int totalMaterials = 0;
                
                LogInfo($"Checking {currentAnalysisResult.gameObjects.Count} game objects...");

                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var renderers = objInfo.gameObject.GetComponents<Renderer>();
                    LogInfo($"Object '{objInfo.gameObject.name}' has {renderers.Length} renderers");
                    
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        var materials = renderer.sharedMaterials;
                        LogInfo($"  Renderer '{renderer.GetType().Name}' has {materials.Length} materials");
                        
                        for (int i = 0; i < materials.Length; i++)
                        {
                            var material = materials[i];
                            if (material == null) continue;
                            
                            totalMaterials++;
                            LogInfo($"  Checking material {i}: {material.name}");
                            
                            if (IsErrorMaterial(material))
                            {
                                errorShaderCount++;
                                LogInfo($"*** FOUND ERROR SHADER: {objInfo.gameObject.name} - {material.shader?.name ?? "Unknown"} ***");
                                
                                var error = new SceneError
                                {
                                    type = SceneErrorType.ErrorShader,
                                    severity = SceneErrorSeverity.Medium,
                                    gameObject = objInfo.gameObject,
                                    message = $"Error Shader detected: {material.shader?.name ?? "Unknown"}",
                                    component = renderer,
                                    materialIndex = i,
                                    gameObjectName = objInfo.gameObject.name,
                                    gameObjectPath = GetGameObjectPath(objInfo.gameObject),
                                    componentTypeName = renderer.GetType().Name
                                };
                                
                                currentAnalysisResult.errors.Add(error);
                            }
                        }
                    }
                }
                
                LogInfo($"=== ERROR SHADERS ANALYSIS COMPLETED ===");
                LogInfo($"Total materials checked: {totalMaterials}");
                LogInfo($"Error shaders found: {errorShaderCount}");
                LogInfo($"Total errors in result: {currentAnalysisResult.errors.Count}");
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

                // 7. Shader가 "Hidden/InternalErrorShader"로 폴백된 경우 체크
                // Material의 shader.name이 변경되었는지 확인
                try
                {
                    var renderType = material.GetTag("RenderType", false);
                    if (string.IsNullOrEmpty(renderType) && !IsUnityBuiltinShader(shaderName))
                    {
                        // RenderType이 없는 것은 문제가 있을 수 있음
                        // 하지만 일부 커스텀 셰이더는 RenderType이 없을 수 있으므로 경고만
                    }
                }
                catch (System.Exception e)
                {
                    LogInfo($"IsErrorMaterial: Error checking material tags: {e.Message}");
                }

                // 8. Shader의 실제 렌더 큐 확인 - 에러 셰이더는 보통 -1을 반환
                if (shader.renderQueue < 0)
                {
                    LogInfo($"IsErrorMaterial: Invalid render queue: {shader.renderQueue} for shader {shaderName}");
                    return true;
                }

                // 9. Valid Unity builtin shader면 OK
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

            private void AnalyzeMissingPrefabs()
            {
                LogInfo("Starting Missing Prefab Analysis...");
                int totalObjects = currentAnalysisResult.gameObjects.Count;
                LogInfo($"Total GameObjects to analyze: {totalObjects}");
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var gameObject = objInfo.gameObject;
                    LogInfo($"Analyzing GameObject: {gameObject.name}");
                    
                    bool isMissing = PrefabUtility.IsPrefabAssetMissing(gameObject);
                    LogInfo($"  - IsPrefabAssetMissing: {isMissing}");
                    
                    if (isMissing)
                    {
                        LogInfo($"  - FOUND MISSING PREFAB: {gameObject.name}");
                        var error = new SceneError
                        {
                            type = SceneErrorType.MissingPrefabAsset,
                            severity = SceneErrorSeverity.Critical,
                            gameObject = gameObject,
                            message = "Missing Prefab Asset - The prefab file cannot be found"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                        continue;
                    }
                    
                    var prefabType = PrefabUtility.GetPrefabAssetType(gameObject);
                    var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
                    var isPartOfPrefab = PrefabUtility.IsPartOfPrefabInstance(gameObject);
                    
                    LogInfo($"  - PrefabAssetType: {prefabType}");
                    LogInfo($"  - PrefabInstanceStatus: {prefabInstanceStatus}");
                    LogInfo($"  - IsPartOfPrefabInstance: {isPartOfPrefab}");
                    
                    if (prefabType != PrefabAssetType.NotAPrefab && 
                        prefabInstanceStatus == PrefabInstanceStatus.MissingAsset)
                    {
                        LogInfo($"  - FOUND BROKEN PREFAB CONNECTION: {gameObject.name}");
                        var error = new SceneError
                        {
                            type = SceneErrorType.BrokenPrefabConnection,
                            severity = SceneErrorSeverity.High,
                            gameObject = gameObject,
                            message = "Broken Prefab Connection - Prefab asset reference is broken"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                    }
                    
                    if (prefabType != PrefabAssetType.NotAPrefab)
                    {
                        if (prefabInstanceStatus == PrefabInstanceStatus.Disconnected)
                        {
                            LogInfo($"  - FOUND DISCONNECTED PREFAB: {gameObject.name}");
                            var error = new SceneError
                            {
                                type = SceneErrorType.PrefabInstanceIssue,
                                severity = SceneErrorSeverity.Medium,
                                gameObject = gameObject,
                                message = "Disconnected Prefab Instance - Prefab connection is lost"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                        }
                        
                        if (prefabInstanceStatus == PrefabInstanceStatus.Connected && 
                            PrefabUtility.IsPartOfPrefabInstance(gameObject) && 
                            PrefabUtility.GetCorrespondingObjectFromSource(gameObject) == null)
                        {
                            LogInfo($"  - FOUND INVALID PREFAB INSTANCE: {gameObject.name}");
                            var error = new SceneError
                            {
                                type = SceneErrorType.PrefabInstanceIssue,
                                severity = SceneErrorSeverity.Medium,
                                gameObject = gameObject,
                                message = "Invalid Prefab Instance - Corrupted prefab hierarchy"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                        }
                    }
                    
                    if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
                    {
                        var rootPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                        LogInfo($"  - Root Prefab: {(rootPrefab != null ? rootPrefab.name : "null")}");
                        
                        if (rootPrefab != null && PrefabUtility.IsPrefabAssetMissing(rootPrefab))
                        {
                            LogInfo($"  - FOUND NESTED PREFAB ISSUE: {gameObject.name} (Root: {rootPrefab.name})");
                        var error = new SceneError
                        {
                            type = SceneErrorType.MissingPrefab,
                            severity = SceneErrorSeverity.High,
                                gameObject = gameObject,
                                message = $"Nested Prefab Issue - Root prefab '{rootPrefab.name}' is missing"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                    }
                }
                }
                
                LogInfo($"Missing Prefab Analysis completed. Found {currentAnalysisResult.errors.Count(e => e.type == SceneErrorType.MissingPrefabAsset || e.type == SceneErrorType.BrokenPrefabConnection || e.type == SceneErrorType.PrefabInstanceIssue || e.type == SceneErrorType.MissingPrefab)} prefab-related errors.");
            }
            
            private void AnalyzePrefabReferences()
            {
                LogInfo("Starting Prefab References Analysis...");
                int prefabRefErrors = 0;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var gameObject = objInfo.gameObject;
                    var components = gameObject.GetComponents<Component>();
                    
                    foreach (var component in components)
                    {
                        if (component == null) continue;
                        
                        try
                        {
                            var serializedObject = new UnityEditor.SerializedObject(component);
                            var iterator = serializedObject.GetIterator();
                            
                            while (iterator.NextVisible(true))
                            {
                                if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                                {
                                    var referencedObject = iterator.objectReferenceValue;
                                    
                                    if (referencedObject != null && PrefabUtility.IsPartOfPrefabAsset(referencedObject))
                                    {
                                        LogInfo($"  - Found Prefab Reference: {gameObject.name}.{component.GetType().Name}.{iterator.name} -> {referencedObject.name}");
                                        
                                        var prefabPath = AssetDatabase.GetAssetPath(referencedObject);
                                        LogInfo($"    - Prefab Path: {prefabPath}");
                                        LogInfo($"    - File Exists: {System.IO.File.Exists(prefabPath)}");
                                        
                                        if (string.IsNullOrEmpty(prefabPath) || !System.IO.File.Exists(prefabPath))
                                        {
                                            LogInfo($"    - FOUND MISSING PREFAB REFERENCE: {referencedObject.name}");
                                            var error = new SceneError
                                            {
                                                type = SceneErrorType.MissingPrefabAsset,
                                                severity = SceneErrorSeverity.High,
                                                gameObject = gameObject,
                                                component = component,
                                                message = $"Missing Prefab Reference in {component.GetType().Name}.{iterator.name} - Referenced prefab '{referencedObject.name}' not found"
                                            };
                                            
                                            currentAnalysisResult.errors.Add(error);
                                            prefabRefErrors++;
                                        }
                                    }
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            LogInfo($"  - SerializedObject creation failed for {component.GetType().Name}: {e.Message}");
                            continue;
                        }
                    }
                }
                
                LogInfo($"Prefab References Analysis completed. Found {prefabRefErrors} missing prefab references.");
            }
            
            private void AnalyzePerformanceIssues()
            {
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var meshFilter = objInfo.gameObject.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        var vertexCount = meshFilter.sharedMesh.vertexCount;
                        if (vertexCount > 10000) 
                        {
                            var error = new SceneError
                            {
                                type = SceneErrorType.PerformanceIssue,
                                severity = SceneErrorSeverity.Medium,
                                gameObject = objInfo.gameObject,
                                message = $"High polygon mesh: {vertexCount} vertices"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                        }
                    }
                }
            }
            
            private void CaptureSceneSnapshot()
            {
                try
                {
                    LogInfo("Capturing scene snapshot...");

                    Camera mainCamera = Camera.main;
                    if (mainCamera == null)
                    {
                        Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                        if (cameras.Length > 0)
                        {
                            mainCamera = cameras[0];
                        }
                    }

                    if (mainCamera == null)
                    {
                        GameObject tempCameraObj = new GameObject("TempSnapshotCamera");
                        mainCamera = tempCameraObj.AddComponent<Camera>();

                        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                        if (rootObjects.Length > 0)
                        {
                            Bounds sceneBounds = new Bounds(Vector3.zero, Vector3.zero);
                            bool boundsInitialized = false;

                            foreach (var obj in rootObjects)
                            {
                                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                                foreach (var renderer in renderers)
                                {
                                    if (!boundsInitialized)
                                    {
                                        sceneBounds = renderer.bounds;
                                        boundsInitialized = true;
                                    }
                                    else
                                    {
                                        sceneBounds.Encapsulate(renderer.bounds);
                                    }
                                }
                            }

                            if (boundsInitialized)
                            {
                                Vector3 center = sceneBounds.center;
                                float distance = sceneBounds.size.magnitude * 1.5f;
                                tempCameraObj.transform.position = center + new Vector3(distance, distance * 0.5f, -distance);
                                tempCameraObj.transform.LookAt(center);
                            }
                        }
                    }

                    int width = 512;
                    int height = 512;
                    RenderTexture renderTexture = new RenderTexture(width, height, 24);
                    RenderTexture previousRT = mainCamera.targetTexture;

                    mainCamera.targetTexture = renderTexture;
                    mainCamera.Render();

                    RenderTexture.active = renderTexture;
                    Texture2D snapshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                    snapshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    snapshot.Apply();

                    mainCamera.targetTexture = previousRT;
                    RenderTexture.active = null;
                    UnityEngine.Object.DestroyImmediate(renderTexture);

                    if (mainCamera.gameObject.name == "TempSnapshotCamera")
                    {
                        UnityEngine.Object.DestroyImmediate(mainCamera.gameObject);
                    }

                    currentAnalysisResult.sceneSnapshot = snapshot;
                    LogInfo("Scene snapshot captured successfully");
                }
                catch (System.Exception e)
                {
                    LogError($"Error capturing scene snapshot: {e.Message}");
                }
            }

            private void FinalizeAnalysis()
            {
                LogInfo("Finalizing Analysis...");
                LogInfo($"Total errors found: {currentAnalysisResult.errors.Count}");

                var validErrors = currentAnalysisResult.errors.Where(e => e.gameObject != null).ToList();
                var removedCount = currentAnalysisResult.errors.Count - validErrors.Count;
                if (removedCount > 0)
                {
                    LogInfo($"Removed {removedCount} errors with null GameObjects");
                    currentAnalysisResult.errors = validErrors;
                }
            
                currentAnalysisResult.totalMaterials = CalculateTotalMaterials();
                currentAnalysisResult.errorCount = currentAnalysisResult.errors.Count;
                
                currentAnalysisResult.highSeverityErrors = currentAnalysisResult.errors.Count(e => e.severity == SceneErrorSeverity.High || e.severity == SceneErrorSeverity.Critical);
                currentAnalysisResult.mediumSeverityErrors = currentAnalysisResult.errors.Count(e => e.severity == SceneErrorSeverity.Medium);
                currentAnalysisResult.lowSeverityErrors = currentAnalysisResult.errors.Count(e => e.severity == SceneErrorSeverity.Low);
                
                LogInfo($"Error counts - High/Critical: {currentAnalysisResult.highSeverityErrors}, Medium: {currentAnalysisResult.mediumSeverityErrors}, Low: {currentAnalysisResult.lowSeverityErrors}");
                
                foreach (var errorType in System.Enum.GetValues(typeof(SceneErrorType)))
                {
                    var count = currentAnalysisResult.errors.Count(e => e.type == (SceneErrorType)errorType);
                    if (count > 0)
                    {
                        LogInfo($"  - {errorType}: {count}");
                    }
                }
            }
            
            private int CalculateTotalMaterials()
            {
                var materialSet = new HashSet<Material>();
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var renderers = objInfo.gameObject.GetComponents<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        var materials = renderer.sharedMaterials;
                        foreach (var material in materials)
                        {
                            if (material != null)
                                materialSet.Add(material);
                        }
                    }
                }
                
                return materialSet.Count;
            }
            

            private void CheckForMissingScripts()
            {
                if (!checkMissingScripts) return;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var components = objInfo.gameObject.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            var error = new SceneError
                            {
                                type = SceneErrorType.MissingScript,
                                severity = SceneErrorSeverity.High,
                                gameObject = objInfo.gameObject,
                                message = "Missing Script Component"
                            };
                            
                            currentAnalysisResult.errors.Add(error);
                            
                            if (autoFixErrors)
                            {
                                var serializedObject = new SerializedObject(objInfo.gameObject);
                                var componentsProperty = serializedObject.FindProperty("m_Component");
                                
                                for (int i = componentsProperty.arraySize - 1; i >= 0; i--)
                                {
                                    var componentProperty = componentsProperty.GetArrayElementAtIndex(i);
                                    var componentRef = componentProperty.FindPropertyRelative("component");
                                    
                                    if (componentRef.objectReferenceValue == null)
                                    {
                                        componentsProperty.DeleteArrayElementAtIndex(i);
                                    }
                                }
                                
                                serializedObject.ApplyModifiedProperties();
                            }
                        }
                    }
                }
            }
            
            private void CheckForMissingMaterials()
            {
                if (!checkMissingMaterials) return;
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var renderers = objInfo.gameObject.GetComponents<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        CheckRendererMaterials(renderer, objInfo.gameObject);
                    }
                    
                    var particleSystems = objInfo.gameObject.GetComponents<ParticleSystem>();
                    foreach (var ps in particleSystems)
                    {
                        if (ps == null) continue;
                        
                        CheckParticleSystemMaterials(ps, objInfo.gameObject);
                    }
                    
                    var lineRenderer = objInfo.gameObject.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        CheckLineRendererMaterials(lineRenderer, objInfo.gameObject);
                    }
                    
                    var trailRenderer = objInfo.gameObject.GetComponent<TrailRenderer>();
                    if (trailRenderer != null)
                    {
                        CheckTrailRendererMaterials(trailRenderer, objInfo.gameObject);
                    }
                }
            }
            
            private void CheckRendererMaterials(Renderer renderer, GameObject gameObject)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                    {
                        var error = new SceneError
                        {
                            type = SceneErrorType.MissingMaterial,
                            severity = SceneErrorSeverity.Medium,
                            gameObject = gameObject,
                            message = $"Missing Material at index {i} in {renderer.GetType().Name}"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                        
                        if (autoFixErrors)
                        {
                            var newMaterials = new Material[materials.Length];
                            for (int j = 0; j < materials.Length; j++)
                            {
                                newMaterials[j] = materials[j] ?? GetDefaultMaterial();
                            }
                            renderer.sharedMaterials = newMaterials;
                        }
                    }
                }
            }
            
            private void CheckParticleSystemMaterials(ParticleSystem ps, GameObject gameObject)
            {
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    CheckRendererMaterials(renderer, gameObject);
                }
            }
            
            private void CheckLineRendererMaterials(LineRenderer lineRenderer, GameObject gameObject)
            {
                var material = lineRenderer.material;
                if (material == null)
                {
                    var error = new SceneError
                    {
                        type = SceneErrorType.MissingMaterial,
                        severity = SceneErrorSeverity.Medium,
                        gameObject = gameObject,
                        message = "Missing Material in LineRenderer"
                    };
                    
                    currentAnalysisResult.errors.Add(error);
                    
                    if (autoFixErrors)
                    {
                        lineRenderer.material = GetDefaultMaterial();
                    }
                }
            }
            
            private void CheckTrailRendererMaterials(TrailRenderer trailRenderer, GameObject gameObject)
            {
                var material = trailRenderer.material;
                if (material == null)
                {
                    var error = new SceneError
                    {
                        type = SceneErrorType.MissingMaterial,
                        severity = SceneErrorSeverity.Medium,
                        gameObject = gameObject,
                        message = "Missing Material in TrailRenderer"
                    };
                    
                    currentAnalysisResult.errors.Add(error);
                    
                    if (autoFixErrors)
                    {
                        trailRenderer.material = GetDefaultMaterial();
                    }
                }
            }
            
            private void CheckForMissingPrefabs()
            {
                if (!checkMissingPrefabs) return;
                
                LogInfo("Starting CheckForMissingPrefabs (Legacy method)...");
                
                foreach (var objInfo in currentAnalysisResult.gameObjects)
                {
                    var gameObject = objInfo.gameObject;
                    
                    bool isMissing = PrefabUtility.IsPrefabAssetMissing(gameObject);
                    var prefabType = PrefabUtility.GetPrefabAssetType(gameObject);
                    var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
                    
                    LogInfo($"  - Checking {gameObject.name}: IsMissing={isMissing}, Type={prefabType}, Status={prefabStatus}");
                    
                    if (isMissing)
                    {
                        LogInfo($"  - FOUND MISSING PREFAB: {gameObject.name}");
                        var error = new SceneError
                        {
                            type = SceneErrorType.MissingPrefabAsset,
                            severity = SceneErrorSeverity.Critical,
                            gameObject = gameObject,
                            message = "Missing Prefab Asset - The prefab file cannot be found"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                        
                        if (autoFixErrors)
                        {
                            PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                        }
                    }
                    else if (prefabType != PrefabAssetType.NotAPrefab && 
                             prefabStatus == PrefabInstanceStatus.MissingAsset)
                    {
                        LogInfo($"  - FOUND BROKEN PREFAB CONNECTION: {gameObject.name}");
                        var error = new SceneError
                        {
                            type = SceneErrorType.BrokenPrefabConnection,
                            severity = SceneErrorSeverity.High,
                            gameObject = gameObject,
                            message = "Broken Prefab Connection - Prefab asset reference is broken"
                        };
                        
                        currentAnalysisResult.errors.Add(error);
                    }
                }
                
                LogInfo($"CheckForMissingPrefabs completed. Found {currentAnalysisResult.errors.Count(e => e.type == SceneErrorType.MissingPrefabAsset || e.type == SceneErrorType.BrokenPrefabConnection)} prefab errors.");
            }
            
            private Material GetDefaultMaterial()
            {
                var defaultMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                if (defaultMaterial == null)
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.name = "Default Material";
                    return material;
                }
                return defaultMaterial;
            }
            
            private void DrawErrorAnalysis()
            {
                EditorGUILayout.BeginVertical(UIStyles.GradientBackgroundStyle);
                
                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UIColors.Danger }
                };
                EditorGUILayout.LabelField("⚠️ Error Analysis", titleStyle);
                EditorGUILayout.Space(5);
                
                LogInfo($"=== UI ERROR DISPLAY DEBUG ===");
                LogInfo($"currentAnalysisResult is null: {currentAnalysisResult == null}");
                LogInfo($"errors is null: {currentAnalysisResult?.errors == null}");
                LogInfo($"errors count: {currentAnalysisResult?.errors?.Count ?? -1}");
                LogInfo($"errorCount: {currentAnalysisResult?.errorCount ?? -1}");
                
                if (currentAnalysisResult?.errors != null && currentAnalysisResult.errors.Count > 0)
                {
                    LogInfo("=== DISPLAYING ERRORS IN UI ===");
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Total: {currentAnalysisResult.errorCount}", EditorStyles.boldLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"🔴 {currentAnalysisResult.highSeverityErrors}", new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = UIColors.Danger } }, GUILayout.Width(40));
                    EditorGUILayout.LabelField($"🟡 {currentAnalysisResult.mediumSeverityErrors}", new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = UIColors.Warning } }, GUILayout.Width(60));
                    EditorGUILayout.LabelField($"🔵 {currentAnalysisResult.lowSeverityErrors}", new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = UIColors.Info } }, GUILayout.Width(40));
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(5);
                    
                    errorScrollPos = EditorGUILayout.BeginScrollView(errorScrollPos, GUILayout.Height(200));
                    
                    LogInfo($"Error display - Total: {currentAnalysisResult.errors.Count}");
                    
                    foreach (var error in currentAnalysisResult.errors)
                    {
                        LogInfo($"Drawing error: {error.type} - {error.message}");
                        DrawErrorItem(error);
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("✅ No errors found!", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawErrorItem(SceneError error)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                Color severityColor = error.severity switch
                {
                    SceneErrorSeverity.Critical => Color.red,
                    SceneErrorSeverity.High => UIColors.Danger,
                    SceneErrorSeverity.Medium => UIColors.Warning,
                    SceneErrorSeverity.Low => UIColors.Info,
                    _ => Color.white
                };
                
                var severityStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = severityColor } };
                var errorTypeStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, normal = { textColor = severityColor } };
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"⚠️ {GetErrorTypeDisplayName(error.type)}", errorTypeStyle, GUILayout.Width(150));
                EditorGUILayout.LabelField($"{error.severity}", severityStyle, GUILayout.Width(60));
                
                bool isGameObjectValid = false;
                try
                {
                    isGameObjectValid = error.gameObject != null;
                }
                catch (System.Exception)
                {
                    isGameObjectValid = false;
                }
                
                if (isGameObjectValid)
                {
                if (GUILayout.Button("🔍", UIStyles.ButtonStyle, GUILayout.Width(30), GUILayout.Height(18)))
                {
                        try
                    {
                        Selection.activeGameObject = error.gameObject;
                        EditorGUIUtility.PingObject(error.gameObject);
                    }
                        catch (System.Exception e)
                        {
                            LogError($"Error selecting GameObject: {e.Message}");
                        }
                    }
                }
                else
                {
                    GUI.enabled = false;
                    GUILayout.Button("🔍", UIStyles.ButtonStyle, GUILayout.Width(30), GUILayout.Height(18));
                    GUI.enabled = true;
                }
                
                if (error.type == SceneErrorType.MissingScript && isGameObjectValid && GUILayout.Button("🗑️", UIStyles.ButtonStyle, GUILayout.Width(30), GUILayout.Height(18)))
                {
                    RemoveMissingScript(error);
                }
                EditorGUILayout.EndHorizontal();
           
                EditorGUILayout.BeginHorizontal();
                string objectName = "Unknown";
                try
                {
                    objectName = error.gameObject?.name ?? error.gameObjectName ?? "Unknown";
                }
                catch (System.Exception)
                {
                    objectName = error.gameObjectName ?? "Unknown";
                }
                
                string objectPath = error.gameObjectPath ?? "Unknown Path";
                
                EditorGUILayout.LabelField($"Object: {objectName}", EditorStyles.miniLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField($"Message: {error.message}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                
                if (error.gameObject == null && !string.IsNullOrEmpty(error.gameObjectPath))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Path: {objectPath}", EditorStyles.miniLabel, GUILayout.Width(400));
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            
            private string GetGameObjectPath(GameObject obj)
            {
                if (obj == null) return "Unknown";
                
                var path = obj.name;
                var parent = obj.transform.parent;
                
                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
                
                return path;
            }
            
            private string GetErrorTypeDisplayName(SceneErrorType errorType)
            {
                return errorType switch
                {
                    SceneErrorType.MissingScript => "Missing Script",
                    SceneErrorType.MissingMaterial => "Missing Material",
                    SceneErrorType.MissingPrefab => "Missing Prefab",
                    SceneErrorType.MissingPrefabAsset => "Missing Prefab Asset",
                    SceneErrorType.BrokenPrefabConnection => "Broken Prefab Connection",
                    SceneErrorType.PrefabInstanceIssue => "Prefab Instance Issue",
                    SceneErrorType.ErrorShader => "Error Shader",
                    SceneErrorType.PerformanceIssue => "Performance Issue",
                    SceneErrorType.LightingIssue => "Lighting Issue",
                    SceneErrorType.CameraIssue => "Camera Issue",
                    SceneErrorType.TerrainIssue => "Terrain Issue",
                    SceneErrorType.PostProcessingIssue => "Post Processing Issue",
                    _ => errorType.ToString()
                };
            }
            
            private void RemoveMissingScript(SceneError error)
            {
                try
            {
                if (error.gameObject != null)
                {
                    var serializedObject = new SerializedObject(error.gameObject);
                    var componentsProperty = serializedObject.FindProperty("m_Component");
                    
                    for (int i = componentsProperty.arraySize - 1; i >= 0; i--)
                    {
                        var componentProperty = componentsProperty.GetArrayElementAtIndex(i);
                        var componentRef = componentProperty.FindPropertyRelative("component");
                        
                        if (componentRef.objectReferenceValue == null)
                        {
                            componentsProperty.DeleteArrayElementAtIndex(i);
                        }
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                        LogInfo($"Removed missing script from {error.gameObject.name}");
             
                    currentAnalysisResult.errors.Remove(error);
                    currentAnalysisResult.errorCount = currentAnalysisResult.errors.Count;
                }
                    else
                    {
                        LogInfo($"Cannot remove missing script - GameObject is null for error: {error.message}");
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"Error removing missing script: {e.Message}");
                }
            }
        }
    }
}
