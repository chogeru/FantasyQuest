using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

namespace Kirist.EditorTool
{
    public partial class KiristWindow
    {
        private string dragDropMessage = "";
        private float dragDropMessageTime = 0f;

        private float loadingAnimationTime = 0f;
        private bool isAnalyzing = false;

        private GameObject selectedPrefab = null;
        private PrefabAnalysisResult currentAnalysisResult = null;

        public class PrefabAnalyzer : BaseFinderBehaviour
        {
            private List<PrefabAnalysisResult> analysisResults = new List<PrefabAnalysisResult>();
            private bool showAnalysisResults = false;
            private AnalysisMode analysisMode = AnalysisMode.DependencyAnalysis;

            private List<GameObject> prefabsInFolder = new List<GameObject>();
            private List<bool> selectedPrefabs = new List<bool>();
            private string selectedPrefabFolder = "";
            
            private bool autoScanFolder = false;
            private bool includeSubfolders = true;
            private string scanFolderPath = "Assets";

            private Vector2 analysisResultsScrollPos = Vector2.zero;
            private int selectedAnalysisResultIndex = -1;
            private int visibleItemsCount = 50;
            private int scrollOffset = 0;

            private int totalPrefabsAnalyzed = 0;
            private int prefabsWithIssues = 0;
            private int totalComponents = 0;
            private int totalMaterials = 0;
            private int totalTextures = 0;

            public PrefabAnalyzer(KiristWindow parent) : base(parent)
            {
            }

            public override void ClearResults()
            {
                analysisResults.Clear();
                showAnalysisResults = false;
                selectedAnalysisResultIndex = -1;
                scrollOffset = 0;
                totalPrefabsAnalyzed = 0;
                prefabsWithIssues = 0;
                totalComponents = 0;
                totalMaterials = 0;
                totalTextures = 0;
                parentWindow.selectedPrefab = null;
                parentWindow.currentAnalysisResult = null;
            }
            
            private void ScanPrefabsInFolder()
            {
                LogInfo($"Scanning prefabs in folder: {scanFolderPath} (Include subfolders: {includeSubfolders})");
                
                prefabsInFolder.Clear();
                selectedPrefabs.Clear();
                
                try
                {
                    string searchPattern = includeSubfolders ? "t:Prefab" : "t:Prefab";
                    string[] guids = AssetDatabase.FindAssets(searchPattern, new[] { scanFolderPath });
                    
                    LogInfo($"Found {guids.Length} prefabs in folder");
                    
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (prefab != null)
                            {
                                prefabsInFolder.Add(prefab);
                                selectedPrefabs.Add(true);
                                LogInfo($"  - Found prefab: {prefab.name} at {path}");
                            }
                        }
                    }
                    
                    LogInfo($"Successfully loaded {prefabsInFolder.Count} prefabs from folder");
                }
                catch (System.Exception e)
                {
                    LogError($"Error scanning prefabs in folder: {e.Message}");
                }
            }
            
            private void ScanPrefabInstancesInScenes()
            {
                LogInfo("Scanning for prefab instances in all scenes...");
                
                prefabsInFolder.Clear();
                selectedPrefabs.Clear();
                
                try
                {
                    string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
                    HashSet<GameObject> uniquePrefabs = new HashSet<GameObject>();
                    
                    foreach (string sceneGuid in sceneGuids)
                    {
                        string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                        if (!string.IsNullOrEmpty(scenePath) && !IsPackageScene(scenePath))
                        {
                            LogInfo($"Checking scene: {scenePath}");
                            
                            try
                            {
                                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                                if (scene.IsValid())
                                {
                                    GameObject[] rootObjects = scene.GetRootGameObjects();
                                    foreach (GameObject rootObj in rootObjects)
                                    {
                                        FindPrefabInstancesRecursive(rootObj, uniquePrefabs);
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
                        prefabsInFolder.Add(prefab);
                        selectedPrefabs.Add(true);
                        LogInfo($"  - Found prefab instance: {prefab.name}");
                    }
                    
                    LogInfo($"Found {prefabsInFolder.Count} unique prefab instances across all scenes");
                }
                catch (System.Exception e)
                {
                    LogError($"Error scanning prefab instances: {e.Message}");
                }
            }
            
            private void FindPrefabInstancesRecursive(GameObject obj, HashSet<GameObject> uniquePrefabs)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabAsset != null && !uniquePrefabs.Contains(prefabAsset))
                    {
                        uniquePrefabs.Add(prefabAsset);
                        LogInfo($"    - Found prefab instance: {obj.name} -> {prefabAsset.name}");
                    }
                }
                
                foreach (Transform child in obj.transform)
                {
                    FindPrefabInstancesRecursive(child.gameObject, uniquePrefabs);
                }
            }
            
            private bool IsPackageScene(string scenePath)
            {
                return scenePath.Contains("Packages/") || scenePath.Contains("Library/");
            }
            
            private void AnalyzeMissingMaterials(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    LogInfo($"=== ANALYZING MISSING MATERIALS IN PREFAB: {prefab.name} ===");
                    
                    var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                    LogInfo($"Found {renderers.Length} renderers in prefab");
                    
                    int totalMaterialsChecked = 0;
                    int errorMaterialsFound = 0;
                    
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null) continue;
                        
                        LogInfo($"  - Checking renderer: {renderer.name} (Type: {renderer.GetType().Name})");
                        LogInfo($"  - SharedMaterials count: {renderer.sharedMaterials.Length}");
                        
                        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                        {
                            var material = renderer.sharedMaterials[i];
                            totalMaterialsChecked++;
                            
                            LogInfo($"    - Material[{i}]: {(material != null ? material.name : "NULL")}");
                            
                            if (material == null)
                            {
                                LogInfo($"    - MISSING MATERIAL at index {i}");
                                result.missingMaterials.Add($"Missing Material at index {i} in {renderer.name}");
                                errorMaterialsFound++;
                            }
                            else
                            {
                                LogInfo($"    - Material: {material.name}");
                                LogInfo($"    - Shader: {(material.shader != null ? material.shader.name : "NULL")}");
                                
                                bool isError = IsErrorMaterial(material);
                                LogInfo($"    - IsErrorMaterial result: {isError}");
                                
                                bool hasIssues = false;
                                
                                if (material.shader == null)
                                {
                                    LogInfo($"    - ERROR: Material {material.name} has NULL shader");
                                    result.missingMaterials.Add($"Material '{material.name}' has NULL shader in {renderer.name}");
                                    hasIssues = true;
                                }
                                else
                                {
                                    string shaderPath = AssetDatabase.GetAssetPath(material.shader);
                                    LogInfo($"    - Shader path: {shaderPath}");
                                    
                                    if (string.IsNullOrEmpty(shaderPath) || !System.IO.File.Exists(shaderPath))
                                    {
                                        LogInfo($"    - ERROR: Material {material.name} references missing shader: {material.shader.name}");
                                        result.missingMaterials.Add($"Material '{material.name}' references missing shader: {material.shader.name} in {renderer.name}");
                                        hasIssues = true;
                                    }
                                    else if (!material.shader.isSupported)
                                    {
                                        LogInfo($"    - ERROR: Material {material.name} shader not supported: {material.shader.name}");
                                        result.missingMaterials.Add($"Material '{material.name}' shader not supported: {material.shader.name} in {renderer.name}");
                                        hasIssues = true;
                                    }
                                    else if (material.shader.passCount == 0)
                                    {
                                        LogInfo($"    - ERROR: Material {material.name} shader has no passes (compilation failed): {material.shader.name}");
                                        result.missingMaterials.Add($"Material '{material.name}' shader compilation failed: {material.shader.name} in {renderer.name}");
                                        hasIssues = true;
                                    }
                                }
                                
                                if (isError)
                                {
                                    LogInfo($"    - ERROR: Material {material.name} detected as ERROR MATERIAL");
                                    result.missingMaterials.Add($"Error Material '{material.name}' in {renderer.name}");
                                    hasIssues = true;
                                }
                                
                                
                                if (hasIssues)
                                {
                                    errorMaterialsFound++;
                                }
                                else
                                {
                                    LogInfo($"    - Material {material.name} is OK");
                                }
                            }
                        }
                    }
                    
                    LogInfo($"=== MATERIAL ANALYSIS SUMMARY ===");
                    LogInfo($"Total materials checked: {totalMaterialsChecked}");
                    LogInfo($"Error materials found: {errorMaterialsFound}");
                    
                    var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
                    foreach (var ps in particleSystems)
                    {
                        if (ps == null) continue;
                        
                        var renderer = ps.GetComponent<ParticleSystemRenderer>();
                        if (renderer != null)
                        {
                            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                            {
                                var material = renderer.sharedMaterials[i];
                                
                                if (material == null)
                                {
                                    LogInfo($"    - Missing particle material at index {i} in {ps.name}");
                                    result.missingMaterials.Add($"Missing Particle Material at index {i} in {ps.name}");
                                }
                                else if (IsErrorMaterial(material))
                                {
                                    LogInfo($"    - Error particle material: {material.name} in {ps.name}");
                                    result.missingMaterials.Add($"Error Particle Material '{material.name}' in {ps.name}");
                                }
                            }
                        }
                    }
                    
                    var lineRenderers = prefab.GetComponentsInChildren<LineRenderer>(true);
                    foreach (var lr in lineRenderers)
                    {
                        if (lr == null) continue;
                        
                        var material = lr.material;
                        if (material == null)
                        {
                            LogInfo($"    - Missing line renderer material in {lr.name}");
                            result.missingMaterials.Add($"Missing Line Renderer Material in {lr.name}");
                        }
                        else if (IsErrorMaterial(material))
                        {
                            LogInfo($"    - Error line renderer material: {material.name} in {lr.name}");
                            result.missingMaterials.Add($"Error Line Renderer Material '{material.name}' in {lr.name}");
                        }
                    }
                    
                    var trailRenderers = prefab.GetComponentsInChildren<TrailRenderer>(true);
                    foreach (var tr in trailRenderers)
                    {
                        if (tr == null) continue;
                        
                        var material = tr.material;
                        if (material == null)
                        {
                            LogInfo($"    - Missing trail renderer material in {tr.name}");
                            result.missingMaterials.Add($"Missing Trail Renderer Material in {tr.name}");
                        }
                        else if (IsErrorMaterial(material))
                        {
                            LogInfo($"    - Error trail renderer material: {material.name} in {tr.name}");
                            result.missingMaterials.Add($"Error Trail Renderer Material '{material.name}' in {tr.name}");
                        }
                    }
                    
                    LogInfo($"Missing materials analysis completed: {result.missingMaterials.Count} issues found");
                }
                catch (System.Exception e)
                {
                    LogError($"Error in AnalyzeMissingMaterials: {e.Message}");
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
            
            private void AnalyzeSelectedPrefabsFromFolder()
            {
                try
                {
                    LogInfo($"Starting batch analysis of {selectedPrefabs.Count(s => s)} selected prefabs");
                    
                    parentWindow.isAnalyzing = true;
                    parentWindow.loadingAnimationTime = 0f;
                    
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            analysisResults.Clear();
                            totalPrefabsAnalyzed = 0;
                            prefabsWithIssues = 0;
                            totalComponents = 0;
                            totalMaterials = 0;
                            totalTextures = 0;
                            
                            for (int i = 0; i < prefabsInFolder.Count; i++)
                            {
                                if (selectedPrefabs[i])
                                {
                                    LogInfo($"Analyzing prefab: {prefabsInFolder[i].name}");
                                    
                                    var result = AnalyzePrefabDetailed(prefabsInFolder[i]);
                                    if (result != null)
                                    {
                                        analysisResults.Add(result);
                                        totalPrefabsAnalyzed++;
                                        
                                        if (result.boneIssues?.Count > 0)
                                        {
                                            prefabsWithIssues++;
                                        }
                                        
                                        totalComponents += result.componentCount;
                                        totalMaterials += result.materialCount;
                                        totalTextures += result.textureCount;
                                        
                                        LogInfo($"  - Analysis completed: {result.prefabName}");
                                    }
                                }
                            }
                            
                            showAnalysisResults = true;
                            parentWindow.isAnalyzing = false;
                            
                            LogInfo($"Batch analysis completed: {totalPrefabsAnalyzed} prefabs analyzed, {prefabsWithIssues} with issues");
                        }
                        catch (System.Exception e)
                        {
                            LogError($"Error in batch analysis: {e.Message}");
                            parentWindow.isAnalyzing = false;
                        }
                    };
                }
                catch (System.Exception e)
                {
                    LogError($"Error in AnalyzeSelectedPrefabsFromFolder: {e.Message}");
                }
            }

            public override void DrawUI()
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUILayout.Width(parentWindow.position.width * 0.4f));
                DrawLeftPanel();
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical();
                DrawRightPanel();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }

            private void DrawLeftPanel()
            {
                try
                {
                    var leftPanelStyle = new GUIStyle(GUI.skin.box)
                    {
                        normal = { background = parentWindow.CreateGradientTexture(new Color(0.15f, 0.2f, 0.3f, 1f), new Color(0.2f, 0.25f, 0.35f, 1f)) },
                        border = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(15, 15, 15, 15)
                    };

                    EditorGUILayout.BeginVertical(leftPanelStyle);

                    var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 16,
                        normal = { textColor = UIColors.ModernBlue },
                        alignment = TextAnchor.MiddleCenter
                    };
                    EditorGUILayout.LabelField("🔍 PREFAB ANALYZER", titleStyle);

                    EditorGUILayout.Space(10);

                    DrawPrefabDropArea();

                    EditorGUILayout.Space(10);

                    DrawSelectedPrefabInfo();

                    EditorGUILayout.Space(10);

                    DrawFolderScanOptions();

                    EditorGUILayout.Space(10);

                    DrawAnalysisButtons();

                    EditorGUILayout.EndVertical();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawLeftPanel: {e.Message}");
                    EditorGUILayout.EndVertical();
                }
            }

            private void DrawRightPanel()
            {
                try
                {
                    var rightPanelStyle = new GUIStyle(GUI.skin.box)
                    {
                        normal = { background = parentWindow.CreateGradientTexture(new Color(0.1f, 0.15f, 0.25f, 1f), new Color(0.15f, 0.2f, 0.3f, 1f)) },
                        border = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(15, 15, 15, 15)
                    };

                    EditorGUILayout.BeginVertical(rightPanelStyle);

                    if (parentWindow.isAnalyzing)
                    {
                        DrawAnalyzingIndicator();
                    }
                    else if (parentWindow.currentAnalysisResult != null)
                    {
                        DrawDetailedAnalysis();
                    }
                    else
                    {
                        DrawEmptyState();
                    }

                    EditorGUILayout.EndVertical();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawRightPanel: {e.Message}");
                    EditorGUILayout.EndVertical();
                }
            }

            private void DrawPrefabDropArea()
            {
                try
                {
                    var dropAreaHeight = 120f;
                    var dropArea = GUILayoutUtility.GetRect(0, dropAreaHeight, GUILayout.ExpandWidth(true));

                    bool isDragOver = Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform;
                    bool isValidDrag = false;

                    if (isDragOver && dropArea.Contains(Event.current.mousePosition))
                    {
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            var obj = DragAndDrop.objectReferences[0];
                            if (obj is GameObject gameObject)
                            {
                                bool isPrefab = UnityVersionHelper.IsPrefab(gameObject);
                                bool isModelPrefab = UnityVersionHelper.IsModelPrefab(gameObject);
                                isValidDrag = isPrefab && !isModelPrefab;

                                if (isValidDrag)
                                {
                                    var assetPath = AssetDatabase.GetAssetPath(gameObject);
                                    isValidDrag = !string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab");
                                }
                            }
                        }
                    }

                    var dropAreaStyle = new GUIStyle(GUI.skin.box)
                    {
                        normal = {
                        background = isValidDrag ?
                            parentWindow.CreateGradientTexture(new Color(0.2f, 0.4f, 0.2f, 1f), new Color(0.3f, 0.5f, 0.3f, 1f)) :
                            parentWindow.CreateGradientTexture(new Color(0.1f, 0.15f, 0.2f, 1f), new Color(0.15f, 0.2f, 0.25f, 1f))
                    },
                        border = new RectOffset(3, 3, 3, 3),
                        padding = new RectOffset(20, 20, 20, 20),
                        alignment = TextAnchor.MiddleCenter
                    };

                    GUI.Box(dropArea, "", dropAreaStyle);

                    var contentRect = new Rect(dropArea.x + 20, dropArea.y + 20, dropArea.width - 40, dropArea.height - 40);
                    GUI.BeginGroup(contentRect);

                    var iconStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 32,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = isValidDrag ? UIColors.Success : UIColors.ModernBlue }
                    };

                    var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        normal = { textColor = isValidDrag ? UIColors.Success : UIColors.ModernBlue },
                        fontSize = 16,
                        alignment = TextAnchor.MiddleCenter
                    };

                    var descStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        normal = { textColor = isValidDrag ? UIColors.Success : UIColors.Info },
                        fontSize = 12,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true
                    };

                    GUI.Label(new Rect(0, 10, contentRect.width, 40), isValidDrag ? "📦✓" : "📦", iconStyle);

                    GUI.Label(new Rect(0, 50, contentRect.width, 25),
                        isValidDrag ? "Drop Prefab Here!" : "Drag & Drop Prefab Here", titleStyle);

                    GUI.Label(new Rect(0, 75, contentRect.width, 20),
                        isValidDrag ? "Release to load prefab" : "Only .prefab files are supported", descStyle);

                    GUI.EndGroup();

                    if (isDragOver && dropArea.Contains(Event.current.mousePosition))
                    {
                        var borderColor = isValidDrag ? UIColors.Success : UIColors.Danger;
                        DrawDashedBorder(dropArea, borderColor, 2f);
                    }

                    HandleDragAndDrop(dropArea);

                    DrawDragDropMessage();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawPrefabDropArea: {e.Message}");
                }
            }

            private void DrawDashedBorder(Rect rect, Color color, float thickness)
            {
                try
                {
                    var originalColor = Handles.color;
                    Handles.color = color;

                    Handles.DrawLine(new Vector3(rect.xMin, rect.yMin, 0), new Vector3(rect.xMax, rect.yMin, 0));
                    Handles.DrawLine(new Vector3(rect.xMin, rect.yMax, 0), new Vector3(rect.xMax, rect.yMax, 0));
                    Handles.DrawLine(new Vector3(rect.xMin, rect.yMin, 0), new Vector3(rect.xMin, rect.yMax, 0));
                    Handles.DrawLine(new Vector3(rect.xMax, rect.yMin, 0), new Vector3(rect.xMax, rect.yMax, 0));

                    Handles.color = originalColor;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawDashedBorder: {e.Message}");
                }
            }

            private void DrawSelectedPrefabInfo()
            {
                EditorGUILayout.LabelField("Selected Prefab:", EditorStyles.boldLabel);

                if (parentWindow.selectedPrefab != null)
                {
                    var previewTexture = AssetPreview.GetAssetPreview(parentWindow.selectedPrefab);
                    if (previewTexture != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        var previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
                        GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox);
                        GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit);

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space(5);
                    }
                    else
                    {
                        if (AssetPreview.IsLoadingAssetPreview(parentWindow.selectedPrefab.GetInstanceID()))
                        {
                            parentWindow.Repaint();
                        }
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
                    EditorGUILayout.LabelField(parentWindow.selectedPrefab.name, EditorStyles.helpBox);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Path:", GUILayout.Width(50));
                    EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(parentWindow.selectedPrefab), EditorStyles.helpBox);

                    if (GUILayout.Button("X Clear", GUILayout.Width(60)))
                    {
                        parentWindow.selectedPrefab = null;
                        parentWindow.currentAnalysisResult = null;
                        showAnalysisResults = false;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("No prefab selected", EditorStyles.helpBox);
                }
            }

            private void DrawFolderScanOptions()
            {
                try
                {
                    var headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        normal = { textColor = UIColors.ModernBlue }
                    };
                    EditorGUILayout.LabelField("📁 FOLDER SCAN OPTIONS", headerStyle);
                    
                    EditorGUILayout.Space(5);
                    
                    autoScanFolder = EditorGUILayout.Toggle("Auto Scan Folder", autoScanFolder);
                    
                    if (autoScanFolder)
                    {
                        EditorGUI.indentLevel++;
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Scan Folder:", GUILayout.Width(80));
                        scanFolderPath = EditorGUILayout.TextField(scanFolderPath);
                        if (GUILayout.Button("📁", GUILayout.Width(30)))
                        {
                            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder to Scan", "Assets", "");
                            if (!string.IsNullOrEmpty(selectedPath))
                            {
                                scanFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
                        
                        EditorGUI.indentLevel--;
                        
                        EditorGUILayout.Space(5);
                        
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("🔍 Scan Folder", GUILayout.Height(25)))
                        {
                            ScanPrefabsInFolder();
                        }
                        if (GUILayout.Button("🎯 Scan Scene Instances", GUILayout.Height(25)))
                        {
                            ScanPrefabInstancesInScenes();
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        if (prefabsInFolder.Count > 0)
                        {
                            EditorGUILayout.Space(5);
                            EditorGUILayout.LabelField($"Found {prefabsInFolder.Count} prefabs:", EditorStyles.miniLabel);
                            
                            var scrollStyle = new GUIStyle(GUI.skin.box)
                            {
                                normal = { background = parentWindow.CreateGradientTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f), new Color(0.15f, 0.15f, 0.15f, 0.5f)) }
                            };
                            
                            EditorGUILayout.BeginVertical(scrollStyle);
                            for (int i = 0; i < prefabsInFolder.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                selectedPrefabs[i] = EditorGUILayout.Toggle(selectedPrefabs[i], GUILayout.Width(20));
                                EditorGUILayout.LabelField(prefabsInFolder[i].name, EditorStyles.miniLabel);
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUILayout.EndVertical();
                            
                            EditorGUILayout.Space(5);
                            
                            int selectedCount = selectedPrefabs.Count(s => s);
                            if (selectedCount > 0)
                            {
                                if (GUILayout.Button($"🔍 Analyze {selectedCount} Selected Prefabs", GUILayout.Height(30)))
                                {
                                    AnalyzeSelectedPrefabsFromFolder();
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawFolderScanOptions: {e.Message}");
                }
            }

            private void DrawAnalysisButtons()
            {
                EditorGUILayout.BeginHorizontal();

                if (parentWindow.selectedPrefab == null)
                {
                    GUI.enabled = false;
                }

                if (GUILayout.Button("🔍 Analyze Prefab", UIStyles.ButtonStyle, GUILayout.Height(30)))
                {
                    AnalyzeSelectedPrefab();
                }

                GUI.enabled = true;

                if (showAnalysisResults && parentWindow.currentAnalysisResult != null)
                {
                    if (GUILayout.Button("📊 Hide Analysis", UIStyles.ButtonStyle, GUILayout.Height(30)))
                    {
                        showAnalysisResults = false;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            private void DrawEmptyState()
            {
                try
                {
                    var emptyStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        normal = { textColor = UIColors.Info },
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 14
                    };

                    EditorGUILayout.Space(50);
                    EditorGUILayout.LabelField("Select a prefab to analyze", emptyStyle);
                    EditorGUILayout.Space(50);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawEmptyState: {e.Message}");
                }
            }

            private void DrawAnalyzingIndicator()
            {
                try
                {
                    parentWindow.loadingAnimationTime += Time.deltaTime;

                    var centerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 18,
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        wordWrap = true,
                        clipping = TextClipping.Overflow
                    };

                    EditorGUILayout.Space(30);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("🔍 Analyzing Prefab...", centerStyle, GUILayout.ExpandWidth(true));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(10);

                    var descStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        normal = { textColor = UIColors.Info },
                        fontSize = 12,
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true,
                        clipping = TextClipping.Overflow
                    };

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("This may take up to 3 minutes for complex prefabs...", descStyle, GUILayout.ExpandWidth(true));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(20);

                    DrawLoadingAnimation();

                    EditorGUILayout.Space(30);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawAnalyzingIndicator: {e.Message}");
                }
            }

            private void DrawLoadingAnimation()
            {
                try
                {
                    var animationStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 24,
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold
                    };

                    var animationFrames = new string[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
                    var frameIndex = Mathf.FloorToInt(parentWindow.loadingAnimationTime * 10) % animationFrames.Length;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(animationFrames[frameIndex], animationStyle);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawLoadingAnimation: {e.Message}");
                }
            }

            private void DrawDetailedAnalysis()
            {
                try
                {
                    if (parentWindow.currentAnalysisResult == null) return;

                    var result = parentWindow.currentAnalysisResult;
                    if (result == null) return;

                    analysisResultsScrollPos = EditorGUILayout.BeginScrollView(analysisResultsScrollPos);

                    var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 16,
                        normal = { textColor = UIColors.ModernBlue },
                        alignment = TextAnchor.MiddleCenter
                    };
                    EditorGUILayout.LabelField($"Detailed Analysis for {result?.prefabName ?? "Unknown Prefab"}", titleStyle);

                    EditorGUILayout.Space(10);

                    DrawObjectTypeAnalysis(result);

                    EditorGUILayout.Space(10);

                    DrawAnimatorAnalysis(result);

                    EditorGUILayout.Space(10);

                    DrawComponentAnalysis(result);

                    EditorGUILayout.Space(10);

                    DrawReferenceAnalysis(result);

                    EditorGUILayout.Space(10);

                    if (result?.boneMap != null && result.boneMap.Count > 0)
                    {
                        DrawAvatarBoneMappingAnalysis(result);
                    }

                    EditorGUILayout.Space(10);

                    DrawTransformSymmetryAnalysis(result);

                    EditorGUILayout.Space(10);

                    var endStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = UIColors.Info },
                        alignment = TextAnchor.MiddleCenter
                    };
                    EditorGUILayout.LabelField("📄 CONTENTS END", endStyle);

                    EditorGUILayout.EndScrollView();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawDetailedAnalysis: {e.Message}");
                    EditorGUILayout.EndScrollView();
                }
            }

            private void DrawObjectTypeAnalysis(PrefabAnalysisResult result)
            {
                try
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("🎯 Object Type Analysis", EditorStyles.boldLabel);

                    var objectType = "Unknown";
                    if (result?.prefab != null)
                    {
                        objectType = DetermineObjectType(result.prefab);
                    }
                    else
                    {
                        objectType = "Invalid Prefab";
                    }
                    EditorGUILayout.LabelField($"Object Type: {objectType}", EditorStyles.helpBox);

                    if (objectType.Contains("Rigging") && result?.prefab != null)
                    {
                        var animator = result.prefab.GetComponentInChildren<Animator>();
                        if (animator != null && animator.avatar != null)
                        {
                            var avatarType = animator.avatar.isHuman ? "Humanoid" : "Generic";
                            EditorGUILayout.LabelField($"Avatar Type: {avatarType}", EditorStyles.helpBox);
                        }
                    }

                    EditorGUILayout.LabelField($"Components: {result?.componentCount ?? 0}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Materials: {result?.materialCount ?? 0}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Textures: {result?.textureCount ?? 0}", EditorStyles.miniLabel);

                    EditorGUILayout.EndVertical();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawObjectTypeAnalysis: {e.Message}");
                    EditorGUILayout.EndVertical();
                }
            }

            private void DrawAnimatorAnalysis(PrefabAnalysisResult result)
            {
                try
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("🎭 Animator Analysis", EditorStyles.boldLabel);

                    if (result == null)
                    {
                        EditorGUILayout.LabelField("⚠️ No analysis result available", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                        EditorGUILayout.EndVertical();
                        return;
                    }

                    if (result.prefab == null)
                    {
                        EditorGUILayout.LabelField("⚠️ No prefab available", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                        EditorGUILayout.EndVertical();
                        return;
                    }

                    var animator = result.prefab.GetComponentInChildren<Animator>();
                    if (animator != null)
                    {
                        EditorGUILayout.LabelField("✓ Animator Component Found", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Success } });

                        if (animator.runtimeAnimatorController != null)
                        {
                            EditorGUILayout.LabelField($"Controller: {animator.runtimeAnimatorController.name}", EditorStyles.miniLabel);

                            var clips = animator.runtimeAnimatorController.animationClips;
                            if (clips != null)
                            {
                                EditorGUILayout.LabelField($"Animation Clips: {clips.Length}", EditorStyles.miniLabel);

                                foreach (var clip in clips)
                                {
                                    if (clip != null)
                                    {
                                        EditorGUILayout.LabelField($"  • {clip.name} ({clip.length:F2}s)", EditorStyles.miniLabel);
                                    }
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("Animation Clips: 0", EditorStyles.miniLabel);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField("⚠️ No Animation Controller", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                        }

                        if (animator.avatar != null)
                        {
                            EditorGUILayout.LabelField($"Avatar: {animator.avatar.name}", EditorStyles.miniLabel);
                            EditorGUILayout.LabelField($"Is Human: {animator.avatar.isHuman}", EditorStyles.miniLabel);

                            if (animator.avatar.isHuman)
                            {
                                var humanDescription = animator.avatar.humanDescription;
                                if (humanDescription.human != null)
                                {
                                    var mappedBones = humanDescription.human.Length;
                                    EditorGUILayout.LabelField($"Mapped Bones: {mappedBones}", EditorStyles.miniLabel);

                                    if (result.boneIssues != null && result.boneIssues.Count > 0)
                                    {
                                        EditorGUILayout.LabelField($"⚠️ Bone Issues: {result.boneIssues.Count}", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                                    }
                                }
                                else
                                {
                                    EditorGUILayout.LabelField("Mapped Bones: 0", EditorStyles.miniLabel);
                                }
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField("⚠️ No Avatar", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("ℹ️ No Animator Component", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Info } });
                    }

                    EditorGUILayout.EndVertical();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawAnimatorAnalysis: {e.Message}");
                    EditorGUILayout.EndVertical();
                }
            }

            private void DrawComponentAnalysis(PrefabAnalysisResult result)
            {
                try
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("🔧 Component Analysis", EditorStyles.boldLabel);

                    var missingComponents = result?.missingComponents ?? new List<string>();
                    if (missingComponents.Count > 0)
                    {
                        EditorGUILayout.LabelField($"⚠️ Missing Components: {missingComponents.Count}", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                        foreach (var missing in missingComponents)
                        {
                            EditorGUILayout.LabelField($"  • {missing}", EditorStyles.miniLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("✓ No Missing Components", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Success } });
                    }

                    var missingMaterials = result?.missingMaterials ?? new List<string>();
                    if (missingMaterials.Count > 0)
                    {
                        EditorGUILayout.LabelField($"⚠️ Missing Materials: {missingMaterials.Count}", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                        foreach (var missing in missingMaterials)
                        {
                            EditorGUILayout.LabelField($"  • {missing}", EditorStyles.miniLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("✓ No Missing Materials", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Success } });
                    }

                    var missingScripts = result?.missingScripts ?? new List<string>();
                    if (missingScripts.Count > 0)
                    {
                        EditorGUILayout.LabelField($"⚠️ Missing Scripts: {missingScripts.Count}", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                        foreach (var missing in missingScripts)
                        {
                            EditorGUILayout.LabelField($"  • {missing}", EditorStyles.miniLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("✓ No Missing Scripts", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Success } });
                    }

                    var missingPrefabs = result?.missingPrefabs ?? new List<string>();
                    if (missingPrefabs.Count > 0)
                    {
                        EditorGUILayout.LabelField($"⚠️ Missing Prefabs: {missingPrefabs.Count}", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                        foreach (var missing in missingPrefabs)
                        {
                            EditorGUILayout.LabelField($"  • {missing}", EditorStyles.miniLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("✓ No Missing Prefabs", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Success } });
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Component List:", EditorStyles.boldLabel);

                    var uniqueComponents = (result?.componentInfo ?? new List<ComponentInfo>())
                        .Where(c => c.componentType != null) 
                        .GroupBy(c => c.componentType.Name)
                        .Select(g => new { Type = g.Key, Count = g.Count(), IsMissing = g.Any(c => c.isMissing) })
                        .OrderBy(c => c.Type);

                    foreach (var comp in uniqueComponents)
                    {
                        var status = comp.IsMissing ? "❌" : "✓";
                        var color = comp.IsMissing ? UIColors.Danger : UIColors.Success;
                        var countText = comp.Count > 1 ? $" ({comp.Count})" : "";

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{status} {comp.Type}{countText}", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } });

                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            SelectComponentsOfType(result, comp.Type);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Quick Select:", EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Scripts", GUILayout.Width(80)))
                    {
                        SelectComponentsOfType(result, "MonoBehaviour");
                    }
                    if (GUILayout.Button("Rigidbody", GUILayout.Width(80)))
                    {
                        SelectComponentsOfType(result, "Rigidbody");
                    }
                    if (GUILayout.Button("MeshRenderer", GUILayout.Width(80)))
                    {
                        SelectComponentsOfType(result, "MeshRenderer");
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Materials", GUILayout.Width(80)))
                    {
                        SelectMaterials(result);
                    }
                    if (GUILayout.Button("Colliders", GUILayout.Width(80)))
                    {
                        SelectComponentsOfType(result, "Collider");
                    }
                    if (GUILayout.Button("Animators", GUILayout.Width(80)))
                    {
                        SelectComponentsOfType(result, "Animator");
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawComponentAnalysis: {e.Message}");
                    EditorGUILayout.EndVertical();
                }
            }

            private void DrawReferenceAnalysis(PrefabAnalysisResult result)
            {
                try
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("🔗 Usage Analysis", EditorStyles.boldLabel);

                    if (result?.referencedByPaths?.Count > 0)
                    {
                        EditorGUILayout.LabelField("Used In:", EditorStyles.boldLabel);
                        foreach (var usage in result.referencedByPaths)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"  • {usage}", EditorStyles.miniLabel);

                            if (GUILayout.Button("Go", GUILayout.Width(40)))
                            {
                                GoToUsageLocation(usage);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("ℹ️ Not used in any scene or prefab", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Info } });
                    }

                    if (result?.referencedObjects?.Count > 0)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("References:", EditorStyles.boldLabel);
                        foreach (var refObj in result.referencedObjects)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"  • {refObj.referenceType}: {refObj.referencePath}", EditorStyles.miniLabel);

                            if (GUILayout.Button("Go", GUILayout.Width(40)))
                            {
                                GoToReferencedObject(refObj);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawReferenceAnalysis: {e.Message}");
                    EditorGUILayout.EndVertical();
                }
            }

            private void DrawAvatarBoneMappingAnalysis(PrefabAnalysisResult result)
            {
                try
                {
                    EditorGUILayout.BeginVertical("box");

                    var animator = result?.prefab?.GetComponentInChildren<Animator>();
                    var avatarName = animator?.avatar?.name ?? "Unknown Avatar";

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"🦴 {avatarName} Bone Mapping Analysis", EditorStyles.boldLabel);

                    if (animator != null && animator.avatar != null)
                    {
                        if (GUILayout.Button("Configure Avatar", GUILayout.Width(120)))
                        {
                            OpenAvatarConfigureWindow(animator);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    var boneGroups = new Dictionary<string, string[]>
                {
                    {"Head & Neck", new[] {"Head", "Neck"}},
                    {"Upper Body", new[] {"Shoulders", "Arms", "Hands", "Chest"}},
                    {"Spine", new[] {"Spine"}},
                    {"Lower Body", new[] {"Hips", "Legs", "Feet"}}
                };

                    foreach (var group in boneGroups)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField($"📍 {group.Key}", EditorStyles.boldLabel);

                        foreach (var boneKey in group.Value)
                        {
                            if (result.boneMap.ContainsKey(boneKey))
                            {
                                var bones = result.boneMap[boneKey];
                                foreach (var bone in bones)
                                {
                                    bool hasIssue = result.boneIssues != null && result.boneIssues.Any(issue => issue.Contains(bone));

                                    var style = new GUIStyle(EditorStyles.miniLabel)
                                    {
                                        normal = { textColor = hasIssue ? UIColors.Danger : UIColors.Success },
                                        fontStyle = FontStyle.Bold
                                    };

                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField($"  • {bone}", style, GUILayout.Width(120));

                                    if (hasIssue)
                                    {
                                        var issueText = result.boneIssues?.FirstOrDefault(issue => issue.Contains(bone));
                                        if (!string.IsNullOrEmpty(issueText))
                                        {
                                            EditorGUILayout.LabelField(issueText, new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Danger } });
                                        }
                                    }
                                    else
                                    {
                                        EditorGUILayout.LabelField("✓ OK", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Success } });
                                    }

                                    if (GUILayout.Button("Select", GUILayout.Width(50)))
                                    {
                                        SelectBone(result, bone);
                                    }

                                    EditorGUILayout.EndHorizontal();
                                }
                            }
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawAvatarBoneMappingAnalysis: {e.Message}");
                    EditorGUILayout.EndVertical();
                }
            }

            private void DrawTransformSymmetryAnalysis(PrefabAnalysisResult result)
            {
                try
                {
                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.LabelField("🔄 Transform Symmetry Analysis", EditorStyles.boldLabel);

                    var transformIssues = AnalyzeTransformSymmetry(result?.prefab);
                    if (transformIssues.Count > 0)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Transform Symmetry Issues:", EditorStyles.boldLabel);
                        foreach (var issue in transformIssues)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"• {issue}", new GUIStyle(EditorStyles.helpBox) { normal = { textColor = UIColors.Danger } });

                            if (GUILayout.Button("Select", GUILayout.Width(60)))
                            {
                                SelectTransformIssueObject(result, issue);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("✓ No Transform Symmetry Issues Found", new GUIStyle(EditorStyles.helpBox) { normal = { textColor = UIColors.Success } });
                    }

                    EditorGUILayout.EndVertical();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in DrawTransformSymmetryAnalysis: {e.Message}");
                    EditorGUILayout.EndVertical();
                }
            }

            private List<string> AnalyzeTransformSymmetry(GameObject prefab)
            {
                var issues = new List<string>();
                if (prefab == null) return issues;

                var allTransforms = prefab.GetComponentsInChildren<Transform>();

                var leftRightPairs = new Dictionary<string, string>
                {
                    {"LeftHand", "RightHand"},
                    {"LeftArm", "RightArm"},
                    {"LeftShoulder", "RightShoulder"},
                    {"LeftFoot", "RightFoot"},
                    {"LeftLeg", "RightLeg"},
                    {"LeftThigh", "RightThigh"},
                    {"LeftKnee", "RightKnee"},
                    {"LeftAnkle", "RightAnkle"},
                    {"LeftToe", "RightToe"},
                    {"LeftEye", "RightEye"},
                    {"LeftEar", "RightEar"},
                    {"LeftFinger", "RightFinger"},
                    {"LeftThumb", "RightThumb"},
                    {"LeftIndex", "RightIndex"},
                    {"LeftMiddle", "RightMiddle"},
                    {"LeftRing", "RightRing"},
                    {"LeftPinky", "RightPinky"}
                };

                foreach (var pair in leftRightPairs)
                {
                    var leftTransform = FindTransformByName(allTransforms, pair.Key);
                    var rightTransform = FindTransformByName(allTransforms, pair.Value);

                    if (leftTransform != null && rightTransform != null)
                    {
                        var leftScale = leftTransform.localScale;
                        var rightScale = rightTransform.localScale;

                        var scaleDifference = Vector3.Distance(leftScale, rightScale);
                        if (scaleDifference > 0.1f)
                        {
                            issues.Add($"{pair.Key} vs {pair.Value}: Scale asymmetry (Left: {leftScale}, Right: {rightScale}, Diff: {scaleDifference:F2})");
                        }

                        if (leftScale.x < 0.5f || leftScale.x > 2.0f || leftScale.y < 0.5f || leftScale.y > 2.0f || leftScale.z < 0.5f || leftScale.z > 2.0f)
                        {
                            issues.Add($"{pair.Key}: Scale out of ideal range (0.5~2.0): {leftScale}");
                        }
                        if (rightScale.x < 0.5f || rightScale.x > 2.0f || rightScale.y < 0.5f || rightScale.y > 2.0f || rightScale.z < 0.5f || rightScale.z > 2.0f)
                        {
                            issues.Add($"{pair.Value}: Scale out of ideal range (0.5~2.0): {rightScale}");
                        }
                    }
                    else if (leftTransform != null || rightTransform != null)
                    {
                        var missing = leftTransform == null ? pair.Key : pair.Value;
                        var existing = leftTransform == null ? pair.Value : pair.Key;
                        issues.Add($"Missing counterpart: {missing} (found {existing})");
                    }
                }

                return issues;
            }

            private Transform FindTransformByName(Transform[] transforms, string name)
            {
                return transforms.FirstOrDefault(t => t.name.Contains(name));
            }

            private void SelectTransformIssueObject(PrefabAnalysisResult result, string issue)
            {
                var transformNames = ExtractTransformNamesFromIssue(issue);

                if (transformNames.Count > 0)
                {
                    var allTransforms = result.prefab.GetComponentsInChildren<Transform>();
                    var foundTransforms = new List<Transform>();

                    foreach (var name in transformNames)
                    {
                        var transform = FindTransformByName(allTransforms, name);
                        if (transform != null)
                        {
                            foundTransforms.Add(transform);
                        }
                    }

                    if (foundTransforms.Count > 0)
                    {
                        Selection.activeObject = foundTransforms[0].gameObject;
                        EditorGUIUtility.PingObject(foundTransforms[0].gameObject);

                        OpenPrefabAndHighlightObject(foundTransforms[0].gameObject);

                        if (foundTransforms.Count > 1)
                        {
                            Selection.objects = foundTransforms.Select(t => t.gameObject).ToArray();
                        }
                    }
                }
            }

            private List<string> ExtractTransformNamesFromIssue(string issue)
            {
                var names = new List<string>();

                if (issue.Contains(" vs "))
                {
                    var parts = issue.Split(new[] { " vs " }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                    {
                        var leftName = parts[0].Split(':')[0].Trim();
                        var rightName = parts[1].Split(':')[0].Trim();
                        names.Add(leftName);
                        names.Add(rightName);
                    }
                }
                else if (issue.Contains(": "))
                {
                    var name = issue.Split(':')[0].Trim();
                    names.Add(name);
                }

                return names;
            }

            private void OpenPrefabAndHighlightObject(GameObject targetObject)
            {
                try
                {
                    if (parentWindow.currentAnalysisResult != null && parentWindow.currentAnalysisResult.prefab != null)
                    {
                        var prefabAsset = parentWindow.currentAnalysisResult.prefab;

                        AssetDatabase.OpenAsset(prefabAsset);

                        var allObjects = prefabAsset.GetComponentsInChildren<Transform>();
                        var matchingObject = allObjects.FirstOrDefault(t => t.name == targetObject.name);

                        if (matchingObject != null)
                        {
                            Selection.activeObject = matchingObject.gameObject;
                            EditorGUIUtility.PingObject(matchingObject.gameObject);

                            FocusOnObjectInSceneView(matchingObject.gameObject);
                        }
                        else
                        {
                            var targetPath = GetGameObjectPath(targetObject);
                            matchingObject = FindObjectByPath(allObjects, targetPath);

                            if (matchingObject != null)
                            {
                                Selection.activeObject = matchingObject.gameObject;
                                EditorGUIUtility.PingObject(matchingObject.gameObject);

                                FocusOnObjectInSceneView(matchingObject.gameObject);
                            }
                        }
                    }
                    else
                    {
                        Selection.activeObject = targetObject;
                        EditorGUIUtility.PingObject(targetObject);

                        FocusOnObjectInSceneView(targetObject);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to open prefab and highlight object: {e.Message}");

                    Selection.activeObject = targetObject;
                    EditorGUIUtility.PingObject(targetObject);
                    FocusOnObjectInSceneView(targetObject);
                }
            }

            private void FocusOnObjectInSceneView(GameObject targetObject)
            {
                try
                {
                    if (targetObject == null) return;

                    Selection.activeObject = targetObject;

                    var hierarchyWindow = EditorWindow.GetWindow<EditorWindow>("Hierarchy");
                    if (hierarchyWindow == null)
                    {
                        EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
                    }
                    else
                    {
                        hierarchyWindow.Focus();
                    }

                    EditorGUIUtility.PingObject(targetObject);

                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            Selection.activeObject = targetObject;
                            EditorGUIUtility.PingObject(targetObject);

                            var hierarchy = EditorWindow.GetWindow<EditorWindow>("Hierarchy");
                            if (hierarchy != null)
                            {
                                hierarchy.Focus();
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error in FocusOnObjectInSceneView delayCall: {e.Message}");
                        }
                    };
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FocusOnObjectInSceneView: {e.Message}");
                }
            }



            private string GetGameObjectPath(GameObject obj)
            {
                try
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
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in GetGameObjectPath: {e.Message}");
                    return "Error";
                }
            }

            private Transform FindObjectByPath(Transform[] transforms, string path)
            {
                try
                {
                    if (transforms == null || string.IsNullOrEmpty(path)) return null;

                    var pathParts = path.Split('/');
                    var current = transforms.FirstOrDefault(t => t.name == pathParts[0]);

                    for (int i = 1; i < pathParts.Length && current != null; i++)
                    {
                        current = current.GetComponentsInChildren<Transform>()
                            .FirstOrDefault(t => t.name == pathParts[i] && t.IsChildOf(current));
                    }

                    return current;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindObjectByPath: {e.Message}");
                    return null;
                }
            }

            private string DetermineObjectType(GameObject obj)
            {
                if (obj == null)
                {
                    return "Invalid GameObject";
                }

                var renderer = obj.GetComponent<Renderer>();
                var animator = obj.GetComponent<Animator>();
                var rigidbody = obj.GetComponent<Rigidbody>();
                var rigidbody2D = obj.GetComponent<Rigidbody2D>();

                if (rigidbody2D != null || obj.GetComponent<SpriteRenderer>() != null)
                {
                    return "2D Object";
                }
                else if (animator != null)
                {
                    return "Rigging Object";
                }
                else if (renderer != null)
                {
                    return "Static 3D Object";
                }
                else
                {
                    return "Empty GameObject";
                }
            }

            private void HandleDragAndDrop(Rect dropArea)
            {
                Event evt = Event.current;
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    if (dropArea.Contains(evt.mousePosition))
                    {
                        bool isValidPrefab = false;
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            var obj = DragAndDrop.objectReferences[0];
                            if (obj is GameObject gameObject)
                            {
                                bool isPrefab = UnityVersionHelper.IsPrefab(gameObject);
                                bool isModelPrefab = UnityVersionHelper.IsModelPrefab(gameObject);
                                isValidPrefab = isPrefab && !isModelPrefab;

                                if (isValidPrefab)
                                {
                                    var assetPath = AssetDatabase.GetAssetPath(gameObject);
                                    if (!string.IsNullOrEmpty(assetPath) && !assetPath.EndsWith(".prefab"))
                                    {
                                        isValidPrefab = false;
                                    }
                                }
                            }
                        }

                        if (isValidPrefab)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                            if (evt.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();
                                if (DragAndDrop.objectReferences.Length > 0)
                                {
                                    var prefab = DragAndDrop.objectReferences[0] as GameObject;
                                    if (prefab != null)
                                    {
                                        SetPrefab(prefab);
                                        parentWindow.dragDropMessage = "✅ Prefab loaded successfully!";
                                        parentWindow.dragDropMessageTime = 2f;
                                    }
                                }
                            }
                        }
                        else
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

                            if (evt.type == EventType.DragPerform)
                            {
                                if (DragAndDrop.objectReferences.Length > 0)
                                {
                                    var obj = DragAndDrop.objectReferences[0];
                                    if (obj is GameObject gameObject)
                                    {
                                        bool isModelPrefab = UnityVersionHelper.IsModelPrefab(gameObject);
                                        if (isModelPrefab)
                                        {
                                            parentWindow.dragDropMessage = "❌ FBX/Model files are not supported! Use .prefab files only.";
                                        }
                                        else
                                        {
                                            parentWindow.dragDropMessage = "❌ Only .prefab files are supported!";
                                        }
                                    }
                                    else
                                    {
                                        parentWindow.dragDropMessage = "❌ Only .prefab files are supported!";
                                    }
                                }
                                else
                                {
                                    parentWindow.dragDropMessage = "❌ Only .prefab files are supported!";
                                }
                                parentWindow.dragDropMessageTime = 3f;
                            }
                        }
                    }
                }
            }

            private void DrawDragDropMessage()
            {
                if (parentWindow.dragDropMessageTime > 0)
                {
                    parentWindow.dragDropMessageTime -= Time.deltaTime;
                    var messageStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        wordWrap = true,
                        clipping = TextClipping.Overflow
                    };

                    EditorGUILayout.Space(10);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(parentWindow.dragDropMessage, messageStyle, GUILayout.ExpandWidth(true));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }

            private void SetPrefab(GameObject prefab)
            {
                try
                {
                    parentWindow.selectedPrefab = prefab;
                    showAnalysisResults = false;
                    parentWindow.currentAnalysisResult = null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in SetPrefab: {e.Message}");
                }
            }

            private void AnalyzeSelectedPrefab()
            {
                try
                {
                    if (parentWindow.selectedPrefab == null) return;

                    parentWindow.isAnalyzing = true;
                    parentWindow.loadingAnimationTime = 0f;

                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            var analysisResult = AnalyzePrefabDetailed(parentWindow.selectedPrefab);
                            parentWindow.currentAnalysisResult = analysisResult;
                            showAnalysisResults = true;
                            totalPrefabsAnalyzed = 1;
                            prefabsWithIssues = analysisResult?.boneIssues?.Count > 0 ? 1 : 0;
                            totalComponents = analysisResult?.componentCount ?? 0;
                            totalMaterials = analysisResult?.materialCount ?? 0;
                            totalTextures = analysisResult?.textureCount ?? 0;
                            parentWindow.isAnalyzing = false;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error in AnalyzeSelectedPrefab delayCall: {e.Message}");
                            parentWindow.isAnalyzing = false;
                        }
                    };
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzeSelectedPrefab: {e.Message}");
                }
            }

            private PrefabAnalysisResult AnalyzePrefabDetailed(GameObject prefab)
            {
                try
                {
                    if (prefab == null)
                    {
                        Debug.LogError("AnalyzePrefabDetailed: prefab is null");
                        return new PrefabAnalysisResult
                        {
                            prefab = null,
                            prefabName = "Invalid Prefab",
                            prefabPath = "",
                            objectType = "Invalid",
                            componentCount = 0,
                            materialCount = 0,
                            shaderCount = 0,
                            uniqueShaders = new List<Shader>(),
                            scriptCount = 0,
                            uniqueScriptTypes = new List<System.Type>(),
                            animationCount = 0,
                            referenceCount = 0,
                            hasRenderer = false,
                            textureCount = 0,
                            meshInfo = new MeshInfo(),
                            boneIssues = new List<string>(),
                            bonePositions = new Dictionary<string, Vector3>(),
                            boneMap = new Dictionary<string, string[]>(),
                            materialInfo = new List<MaterialInfo>(),
                            scriptInfo = new List<ScriptInfo>(),
                            animationInfo = new List<AnimationInfo>(),
                            referenceInfo = new List<ReferenceInfo>(),
                            missingComponents = new List<string>(),
                            missingMaterials = new List<string>(),
                            missingScripts = new List<string>(),
                            missingPrefabs = new List<string>(),
                            componentInfo = new List<ComponentInfo>(),
                            referencedByPaths = new List<string>(),
                            referencedObjects = new List<ReferenceInfo>()
                        };
                    }

                    var result = new PrefabAnalysisResult
                    {
                        prefab = prefab,
                        prefabName = prefab.name,
                        prefabPath = AssetDatabase.GetAssetPath(prefab),
                        componentCount = 0,
                        materialCount = 0,
                        shaderCount = 0,
                        uniqueShaders = new List<Shader>(),
                        scriptCount = 0,
                        uniqueScriptTypes = new List<System.Type>(),
                        animationCount = 0,
                        referenceCount = 0,
                        hasRenderer = false,
                        textureCount = 0,
                        meshInfo = new MeshInfo(),
                        boneIssues = new List<string>(),
                        bonePositions = new Dictionary<string, Vector3>(),
                        boneMap = new Dictionary<string, string[]>(),
                        materialInfo = new List<MaterialInfo>(),
                        scriptInfo = new List<ScriptInfo>(),
                        animationInfo = new List<AnimationInfo>(),
                        referenceInfo = new List<ReferenceInfo>(),
                        missingComponents = new List<string>(),
                        missingMaterials = new List<string>(),
                        missingScripts = new List<string>(),
                        missingPrefabs = new List<string>(),
                        componentInfo = new List<ComponentInfo>(),
                        referencedByPaths = new List<string>(),
                        referencedObjects = new List<ReferenceInfo>()
                    };

                    result.objectType = DetermineObjectType(prefab);

                    AnalyzeAllComponents(prefab, result);

                    AnalyzeMissingPrefabs(prefab, result);

                    AnalyzeMissingMaterials(prefab, result);

                    AnalyzeAnimatorDetails(prefab, result);

                    AnalyzeReferences(prefab, result);

                    return result;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzePrefabDetailed: {e.Message}");
                    return new PrefabAnalysisResult
                    {
                        prefab = prefab,
                        prefabName = prefab?.name ?? "Error",
                        prefabPath = prefab != null ? AssetDatabase.GetAssetPath(prefab) : "",
                        objectType = "Error",
                        componentCount = 0,
                        materialCount = 0,
                        shaderCount = 0,
                        uniqueShaders = new List<Shader>(),
                        scriptCount = 0,
                        uniqueScriptTypes = new List<System.Type>(),
                        animationCount = 0,
                        referenceCount = 0,
                        hasRenderer = false,
                        textureCount = 0,
                        meshInfo = new MeshInfo(),
                        boneIssues = new List<string>(),
                        bonePositions = new Dictionary<string, Vector3>(),
                        boneMap = new Dictionary<string, string[]>(),
                        materialInfo = new List<MaterialInfo>(),
                        scriptInfo = new List<ScriptInfo>(),
                        animationInfo = new List<AnimationInfo>(),
                        referenceInfo = new List<ReferenceInfo>(),
                        missingComponents = new List<string>(),
                        missingMaterials = new List<string>(),
                        missingScripts = new List<string>(),
                        missingPrefabs = new List<string>(),
                        componentInfo = new List<ComponentInfo>(),
                        referencedByPaths = new List<string>(),
                        referencedObjects = new List<ReferenceInfo>()
                    };
                }
            }

            private void AnalyzeAllComponents(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;

                    var allComponents = prefab.GetComponentsInChildren<Component>();
                    result.componentCount = allComponents.Length;

                    var materials = new HashSet<Material>();
                    var shaders = new HashSet<Shader>();
                    var textures = new HashSet<Texture>();

                    AnalyzeGameObjectComponentsRecursive(prefab, result, materials, shaders, textures);

                    foreach (var component in allComponents)
                    {
                        if (component == null)
                        {

                            continue;
                        }

                        var componentInfo = new ComponentInfo
                        {
                            component = component,
                            componentType = component.GetType(),
                            isMissing = false
                        };
                        result.componentInfo.Add(componentInfo);

                        if (component is Renderer renderer)
                        {
                            result.hasRenderer = true;
                            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                            {
                                var material = renderer.sharedMaterials[i];
                                if (material == null)
                                {
                                    result.missingMaterials.Add($"Missing Material on {renderer.name} (Slot {i})");
                                }
                                else if (IsErrorMaterial(material))
                                {
                                    result.missingMaterials.Add($"Error Shader Material on {renderer.name} (Slot {i}): {material.name}");
                                }
                                else
                                {
                                    materials.Add(material);
                                    if (material.shader != null)
                                    {
                                        shaders.Add(material.shader);

                                        var shader = material.shader;
                                        for (int j = 0; j < ShaderUtil.GetPropertyCount(shader); j++)
                                        {
                                            if (ShaderUtil.GetPropertyType(shader, j) == ShaderUtil.ShaderPropertyType.TexEnv)
                                            {
                                                var texture = material.GetTexture(ShaderUtil.GetPropertyName(shader, j));
                                                if (texture != null)
                                                {
                                                    textures.Add(texture);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (component is MeshFilter meshFilter && meshFilter.sharedMesh != null)
                        {
                            var mesh = meshFilter.sharedMesh;
                            result.meshInfo = new MeshInfo
                            {
                                vertexCount = mesh.vertexCount,
                                triangleCount = mesh.triangles.Length / 3,
                                hasNormals = mesh.normals != null && mesh.normals.Length > 0,
                                hasUVs = mesh.uv != null && mesh.uv.Length > 0,
                                hasColors = mesh.colors != null && mesh.colors.Length > 0
                            };
                            LogInfo($"  - Mesh: {mesh.name} (Verts: {mesh.vertexCount}, Tris: {mesh.triangles.Length / 3})");
                        }
                        else if (component is Animator animator && animator.runtimeAnimatorController != null)
                        {
                            var animInfo = new AnimationInfo
                            {
                                animator = animator,
                                controller = animator.runtimeAnimatorController,
                                animationCount = animator.runtimeAnimatorController.animationClips.Length
                            };
                            result.animationInfo.Add(animInfo);
                            result.animationCount += animInfo.animationCount;
                            LogInfo($"  - Animator: {animator.runtimeAnimatorController.name} ({animInfo.animationCount} clips)");
                        }
                        else if (component is Animation animation)
                        {
                            int clipCount = 0;
                            foreach (AnimationState state in animation)
                            {
                                clipCount++;
                            }
                            var animInfo = new AnimationInfo
                            {
                                animation = animation,
                                animationCount = clipCount
                            };
                            result.animationInfo.Add(animInfo);
                            result.animationCount += clipCount;
                            LogInfo($"  - Animation: {clipCount} clips");
                        }
                        else if (component is AudioSource audioSource)
                        {
                            if (audioSource.clip == null)
                            {
                                LogInfo($"  - AudioSource: Missing AudioClip on {audioSource.name}");
                                result.missingComponents.Add($"Missing AudioClip on AudioSource '{audioSource.name}'");
                            }
                            else
                            {
                                LogInfo($"  - AudioSource: {audioSource.clip.name}");
                            }
                        }
                        else if (component is Light light)
                        {
                            LogInfo($"  - Light: Type={light.type}, Intensity={light.intensity}, Range={light.range}");
                        }
                        else if (component is ParticleSystem particleSystem)
                        {
                            var psRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                            if (psRenderer != null)
                            {
                                for (int i = 0; i < psRenderer.sharedMaterials.Length; i++)
                                {
                                    var material = psRenderer.sharedMaterials[i];
                                    if (material != null && !IsErrorMaterial(material))
                                    {
                                        materials.Add(material);
                                        if (material.shader != null)
                                        {
                                            shaders.Add(material.shader);
                                        }
                                    }
                                }
                            }
                            LogInfo($"  - ParticleSystem: MaxParticles={particleSystem.main.maxParticles}");
                        }
                    
                        else if (component.GetType().Namespace == "UnityEngine.UI")
                        {
                            AnalyzeUIComponent(component, result, materials, shaders, textures);
                        }
                        else if (component is Canvas canvas)
                        {
                            LogInfo($"  - Canvas: RenderMode={canvas.renderMode}");
                        }
                        else if (component is Collider collider)
                        {
                            LogInfo($"  - Collider: Type={collider.GetType().Name}, IsTrigger={collider.isTrigger}");
                        }
                        else if (component is Rigidbody rigidbody)
                        {
                            LogInfo($"  - Rigidbody: Mass={rigidbody.mass}, UseGravity={rigidbody.useGravity}, IsKinematic={rigidbody.isKinematic}");
                        }

                        if (component is MonoBehaviour monoBehaviour)
                        {
                            if (monoBehaviour != null)
                            {
                                var scriptType = monoBehaviour.GetType();
                                if (!result.uniqueScriptTypes.Contains(scriptType))
                                {
                                    result.uniqueScriptTypes.Add(scriptType);
                                }
                            }
                        }
                    }

                    result.materialCount = materials.Count;
                    result.shaderCount = shaders.Count;
                    result.uniqueShaders = shaders.ToList();
                    result.textureCount = textures.Count;
                    result.scriptCount = result.uniqueScriptTypes.Count;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzeAllComponents: {e.Message}");
                }
            }

            private void AnalyzeGameObjectComponentsRecursive(GameObject obj, PrefabAnalysisResult result, HashSet<Material> materials, HashSet<Shader> shaders, HashSet<Texture> textures)
            {
                if (obj == null) return;

                Component[] components = obj.GetComponents<Component>();

                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        LogInfo($"  [PA] MISSING SCRIPT at index {i} on GameObject '{obj.name}'");
                        result.missingScripts.Add($"Missing Script on '{obj.name}' (Component index {i})");
                    }
                }

                foreach (Transform child in obj.transform)
                {
                    AnalyzeGameObjectComponentsRecursive(child.gameObject, result, materials, shaders, textures);
                }
            }

            private void AnalyzeUIComponent(Component component, PrefabAnalysisResult result, HashSet<Material> materials, HashSet<Shader> shaders, HashSet<Texture> textures)
            {
                try
                {
                    var componentType = component.GetType();
                    LogInfo($"  - UI Component: {componentType.Name}");

                    if (componentType.Name == "Image")
                    {
                        var spriteProperty = componentType.GetProperty("sprite");
                        var materialProperty = componentType.GetProperty("material");

                        if (spriteProperty != null)
                        {
                            var sprite = spriteProperty.GetValue(component) as UnityEngine.Sprite;
                            if (sprite == null)
                            {
                                result.missingComponents.Add($"Missing Sprite on UI.Image '{component.name}'");
                                LogInfo($"    - Missing Sprite");
                            }
                            else if (sprite.texture != null)
                            {
                                textures.Add(sprite.texture);
                                LogInfo($"    - Sprite: {sprite.name}");
                            }
                        }

                        if (materialProperty != null)
                        {
                            var material = materialProperty.GetValue(component) as Material;
                            if (material != null && !IsErrorMaterial(material))
                            {
                                materials.Add(material);
                                if (material.shader != null)
                                {
                                    shaders.Add(material.shader);
                                }
                            }
                        }
                    }
                    else if (componentType.Name == "RawImage")
                    {
                        var textureProperty = componentType.GetProperty("texture");
                        var materialProperty = componentType.GetProperty("material");

                        if (textureProperty != null)
                        {
                            var texture = textureProperty.GetValue(component) as UnityEngine.Texture;
                            if (texture == null)
                            {
                                result.missingComponents.Add($"Missing Texture on UI.RawImage '{component.name}'");
                                LogInfo($"    - Missing Texture");
                            }
                            else
                            {
                                textures.Add(texture);
                                LogInfo($"    - Texture: {texture.name}");
                            }
                        }

                        if (materialProperty != null)
                        {
                            var material = materialProperty.GetValue(component) as Material;
                            if (material != null && !IsErrorMaterial(material))
                            {
                                materials.Add(material);
                                if (material.shader != null)
                                {
                                    shaders.Add(material.shader);
                                }
                            }
                        }
                    }
                    else if (componentType.Name == "Text")
                    {
                        var fontProperty = componentType.GetProperty("font");
                        var textProperty = componentType.GetProperty("text");

                        if (fontProperty != null)
                        {
                            var font = fontProperty.GetValue(component) as UnityEngine.Font;
                            if (font == null)
                            {
                                result.missingComponents.Add($"Missing Font on UI.Text '{component.name}'");
                                LogInfo($"    - Missing Font");
                            }
                            else
                            {
                                LogInfo($"    - Font: {font.name}");
                            }
                        }

                        if (textProperty != null)
                        {
                            var text = textProperty.GetValue(component) as string;
                            if (string.IsNullOrEmpty(text))
                            {
                                LogInfo($"    - Empty Text");
                            }
                        }
                    }
                    else if (componentType.Name == "Button")
                    {
                        LogInfo($"    - Button component");
                    }
                }
                catch (System.Exception e)
                {
                    LogWarning($"Error analyzing UI component: {e.Message}");
                }
            }

            private void AnalyzeMissingPrefabs(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;

                    LogInfo($"=== ANALYZING MISSING PREFABS IN: {prefab.name} ===");

                    // 프리팹 자체와 모든 자식을 재귀적으로 검사
                    CheckGameObjectForMissingPrefabsRecursive(prefab, result);

                    LogInfo($"Missing Prefabs found: {result.missingPrefabs.Count}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzeMissingPrefabs: {e.Message}");
                }
            }

            private void CheckGameObjectForMissingPrefabsRecursive(GameObject obj, PrefabAnalysisResult result)
            {
                if (obj == null) return;

                if (PrefabUtility.IsPrefabAssetMissing(obj))
                {
                    LogInfo($"  [PA] MISSING PREFAB ASSET: {obj.name}");
                    result.missingPrefabs.Add($"Missing Prefab Asset on '{obj.name}'");
                }

                if (PrefabUtility.IsPartOfPrefabInstance(obj))
                {
                    var prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabSource == null)
                    {
                        LogInfo($"  [PA] BROKEN PREFAB INSTANCE: {obj.name}");
                        result.missingPrefabs.Add($"Broken Prefab Instance on '{obj.name}'");
                    }
                }

                if (PrefabUtility.IsPartOfPrefabInstance(obj) && PrefabUtility.IsOutermostPrefabInstanceRoot(obj))
                {
                    var outerPrefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (outerPrefab == null)
                    {
                        LogInfo($"  [PA] MISSING NESTED PREFAB: {obj.name}");
                        result.missingPrefabs.Add($"Missing Nested Prefab on '{obj.name}'");
                    }
                }

                foreach (Transform child in obj.transform)
                {
                    CheckGameObjectForMissingPrefabsRecursive(child.gameObject, result);
                }
            }

            private void AnalyzeAnimatorDetails(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;

                    var animator = prefab.GetComponentInChildren<Animator>();
                    if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                    {
                        return;
                    }

                    var avatar = animator.avatar;
                    var humanDescription = avatar.humanDescription;
                    var humanBones = humanDescription.human;

                    var boneMap = new Dictionary<string, string[]>
                {
                    {"Head", new[] {"Head"}},
                    {"Neck", new[] {"Neck"}},
                    {"Shoulders", new[] {"LeftShoulder", "RightShoulder"}},
                    {"Arms", new[] {"LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm"}},
                    {"Hands", new[] {"LeftHand", "RightHand"}},
                    {"Chest", new[] {"Chest"}},
                    {"Spine", new[] {"Spine"}},
                    {"Hips", new[] {"Hips"}},
                    {"Legs", new[] {"LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg"}},
                    {"Feet", new[] {"LeftFoot", "RightFoot"}}
                };

                    result.boneMap = boneMap;

                    var bonePositions = new Dictionary<string, Vector3>();
                    var boneIssues = new List<string>();

                    foreach (var humanBone in humanBones)
                    {
                        var boneName = humanBone.humanName;

                        if (System.Enum.TryParse<HumanBodyBones>(boneName, out HumanBodyBones bodyBone))
                        {
                            var boneTransform = animator.GetBoneTransform(bodyBone);

                            if (boneTransform != null)
                            {
                                var position = boneTransform.localPosition;
                                var rotation = boneTransform.localRotation;
                                var scale = boneTransform.localScale;

                                bonePositions[boneName] = position;

                                var issue = CheckBonePositionIssues(boneName, position, rotation, scale, boneMap);
                                if (!string.IsNullOrEmpty(issue))
                                {
                                    boneIssues.Add($"{boneName}: {issue}");
                                }
                            }
                        }
                    }

                    result.bonePositions = bonePositions;
                    result.boneIssues = boneIssues;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzeAnimatorDetails: {e.Message}");
                }
            }

            private void AnalyzeReferences(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;

                    var prefabPath = AssetDatabase.GetAssetPath(prefab);

                    FindUsageInScenes(prefab, result);

                    FindUsageInPrefabs(prefab, prefabPath, result);

                    var allComponents = prefab.GetComponentsInChildren<Component>();
                    foreach (var component in allComponents)
                    {
                        if (component == null) continue;

                        var serializedObject = new SerializedObject(component);
                        var iterator = serializedObject.GetIterator();

                        while (iterator.NextVisible(true))
                        {
                            if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue != null)
                            {
                                var refInfo = new ReferenceInfo
                                {
                                    reference = iterator.objectReferenceValue,
                                    referenceType = iterator.objectReferenceValue.GetType().Name,
                                    referencePath = AssetDatabase.GetAssetPath(iterator.objectReferenceValue)
                                };
                                result.referencedObjects.Add(refInfo);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in AnalyzeReferences: {e.Message}");
                }
            }

            private void FindUsageInScenes(GameObject prefab, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;
                    var prefabPath = AssetDatabase.GetAssetPath(prefab);

                    var sceneGuids = AssetDatabase.FindAssets("t:Scene");

                    foreach (var guid in sceneGuids)
                    {
                        var scenePath = AssetDatabase.GUIDToAssetPath(guid);

                        if (scenePath.StartsWith("Packages/"))
                        {
                            continue;
                        }

                        try
                        {
                            var dependencies = AssetDatabase.GetDependencies(scenePath, true);
                            if (dependencies.Contains(prefabPath))
                            {
                                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                                result.referencedByPaths.Add($"Scene: {sceneName} ({scenePath})");
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Could not check scene dependencies {scenePath}: {e.Message}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindUsageInScenes: {e.Message}");
                }
            }

            private void FindUsageInPrefabs(GameObject prefab, string prefabPath, PrefabAnalysisResult result)
            {
                try
                {
                    if (prefab == null || result == null) return;

                    var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

                    foreach (var guid in prefabGuids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (path == prefabPath) continue;

                        var otherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (otherPrefab != null)
                        {
                            CheckGameObjectForPrefabUsage(otherPrefab, prefab, "Prefab", path, result);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindUsageInPrefabs: {e.Message}");
                }
            }

            private void CheckGameObjectForPrefabUsage(GameObject obj, GameObject targetPrefab, string locationName, string locationPath, PrefabAnalysisResult result)
            {
                try
                {
                    if (obj == null || targetPrefab == null || result == null) return;

                    var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    if (prefabAsset == targetPrefab)
                    {
                        result.referencedByPaths.Add($"{locationName}: {obj.name} ({locationPath})");
                    }

                    foreach (Transform child in obj.transform)
                    {
                        CheckGameObjectForPrefabUsage(child.gameObject, targetPrefab, locationName, locationPath, result);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in CheckGameObjectForPrefabUsage: {e.Message}");
                }
            }

            private void SelectComponentsOfType(PrefabAnalysisResult result, string componentTypeName)
            {
                try
                {
                    if (result?.componentInfo == null) return;

                    var components = result.componentInfo
                        .Where(c => c.componentType.Name == componentTypeName && !c.isMissing)
                        .Select(c => c.component)
                        .Where(c => c != null)
                        .ToArray();

                    if (components.Length > 0)
                    {
                        Selection.objects = components;
                        EditorGUIUtility.PingObject(components[0]);

                        OpenPrefabAndHighlightObject(components[0].gameObject);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in SelectComponentsOfType: {e.Message}");
                }
            }

            private void SelectMaterials(PrefabAnalysisResult result)
            {
                try
                {
                    if (result?.materialInfo == null) return;

                    var materials = result.materialInfo
                        .Where(m => m.material != null)
                        .Select(m => m.material)
                        .ToArray();

                    if (materials.Length > 0)
                    {
                        Selection.objects = materials;
                        EditorGUIUtility.PingObject(materials[0]);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in SelectMaterials: {e.Message}");
                }
            }

            private void SelectBone(PrefabAnalysisResult result, string boneName)
            {
                try
                {
                    if (result?.prefab == null) return;

                    var animator = result.prefab.GetComponentInChildren<Animator>();
                    if (animator != null && animator.avatar != null && animator.avatar.isHuman)
                    {
                        if (System.Enum.TryParse<HumanBodyBones>(boneName, out HumanBodyBones bodyBone))
                        {
                            var boneTransform = animator.GetBoneTransform(bodyBone);
                            if (boneTransform != null)
                            {
                                Selection.activeObject = boneTransform.gameObject;
                                EditorGUIUtility.PingObject(boneTransform.gameObject);

                                OpenPrefabAndHighlightObject(boneTransform.gameObject);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in SelectBone: {e.Message}");
                }
            }

            private void GoToUsageLocation(string usageInfo)
            {
                try
                {
                    if (string.IsNullOrEmpty(usageInfo)) return;

                    var pathStart = usageInfo.LastIndexOf('(');
                    var pathEnd = usageInfo.LastIndexOf(')');

                    if (pathStart > 0 && pathEnd > pathStart)
                    {
                        var path = usageInfo.Substring(pathStart + 1, pathEnd - pathStart - 1);

                        if (path.EndsWith(".unity"))
                        {
                            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                            if (scene.IsValid())
                            {
                                Debug.Log($"Opened scene: {scene.name}");
                            }
                        }
                        else if (path.EndsWith(".prefab"))
                        {
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (prefab != null)
                            {
                                Selection.activeObject = prefab;
                                EditorGUIUtility.PingObject(prefab);
                                AssetDatabase.OpenAsset(prefab);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to go to usage location: {e.Message}");
                }
            }

            private GameObject FindGameObjectByName(GameObject[] rootObjects, string name)
            {
                try
                {
                    if (rootObjects == null || string.IsNullOrEmpty(name)) return null;

                    foreach (var rootObj in rootObjects)
                    {
                        var found = FindGameObjectByNameRecursive(rootObj, name);
                        if (found != null) return found;
                    }
                    return null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindGameObjectByName: {e.Message}");
                    return null;
                }
            }

            private GameObject FindGameObjectByNameRecursive(GameObject obj, string name)
            {
                try
                {
                    if (obj == null || string.IsNullOrEmpty(name)) return null;

                    if (obj.name == name) return obj;

                    foreach (Transform child in obj.transform)
                    {
                        var found = FindGameObjectByNameRecursive(child.gameObject, name);
                        if (found != null) return found;
                    }
                    return null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in FindGameObjectByNameRecursive: {e.Message}");
                    return null;
                }
            }

            private void GoToReferencedPrefab(string prefabPath)
            {
                try
                {
                    if (string.IsNullOrEmpty(prefabPath)) return;

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        Selection.activeObject = prefab;
                        EditorGUIUtility.PingObject(prefab);

                        AssetDatabase.OpenAsset(prefab);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not load prefab at path: {prefabPath}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to go to referenced prefab: {e.Message}");
                }
            }

            private void GoToReferencedObject(ReferenceInfo refInfo)
            {
                try
                {
                    if (refInfo == null || refInfo.reference == null) return;

                    if (refInfo.reference != null)
                    {
                        Selection.activeObject = refInfo.reference;
                        EditorGUIUtility.PingObject(refInfo.reference);

                        if (refInfo.reference is GameObject gameObj)
                        {
                            FocusOnObjectInSceneView(gameObj);
                        }
                        else
                        {
                            var assetPath = AssetDatabase.GetAssetPath(refInfo.reference);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                                if (asset != null)
                                {
                                    Selection.activeObject = asset;
                                    EditorGUIUtility.PingObject(asset);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Referenced object is null: {refInfo.referenceType}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to go to referenced object: {e.Message}");
                }
            }

            private void OpenAvatarConfigureWindow(Animator animator)
            {
                try
                {
                    if (animator != null && animator.avatar != null)
                    {
                        Selection.activeObject = animator.avatar;
                        EditorGUIUtility.PingObject(animator.avatar);

                        EditorApplication.ExecuteMenuItem("Window/Animation/Avatar Setup");
                        Selection.activeObject = animator.gameObject;
                        EditorGUIUtility.PingObject(animator.gameObject);

                        EditorApplication.delayCall += () =>
                        {
                            try
                            {
                                var avatarSetupWindow = EditorWindow.focusedWindow;
                                if (avatarSetupWindow != null && avatarSetupWindow.GetType().Name.Contains("Avatar"))
                                {
                                    Debug.Log("Avatar Setup 창이 열렸습니다. 뼈 매핑을 확인하세요.");
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning($"Avatar Setup 창에서 뼈 매핑 표시 실패: {ex.Message}");
                            }
                        };
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to open Avatar Configure window: {e.Message}");

                    if (animator != null && animator.avatar != null)
                    {
                        Selection.activeObject = animator.avatar;
                        EditorGUIUtility.PingObject(animator.avatar);
                    }
                }
            }


            private string CheckBonePositionIssues(string humanBoneName, Vector3 position, Quaternion rotation, Vector3 scale, Dictionary<string, string[]> boneMap)
            {
                try
                {
                    if (string.IsNullOrEmpty(humanBoneName) || boneMap == null) return null;

                    var issues = new List<string>();

                    var centerBones = new HashSet<string> { "Head", "Neck", "Chest", "Spine", "Hips" };

                    if (centerBones.Contains(humanBoneName))
                    {
                        if (Mathf.Abs(position.x) > 0.1f)
                        {
                            issues.Add($"Center bone X offset: {position.x:F3}");
                        }
                    }

                    if (Mathf.Abs(position.x) > 10f || Mathf.Abs(position.y) > 10f || Mathf.Abs(position.z) > 10f)
                    {
                        issues.Add($"Extreme position: {position}");
                    }

                    if (Mathf.Abs(scale.x - 1f) > 0.5f || Mathf.Abs(scale.y - 1f) > 0.5f || Mathf.Abs(scale.z - 1f) > 0.5f)
                    {
                        issues.Add($"Extreme scale: {scale}");
                    }

                    var eulerAngles = rotation.eulerAngles;
                    if (Mathf.Abs(eulerAngles.y) > 180f)
                    {
                        issues.Add($"Extreme Y rotation: {eulerAngles.y:F1}°");
                    }

                    return issues.Count > 0 ? string.Join(", ", issues) : null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in CheckBonePositionIssues: {e.Message}");
                    return null;
                }
            }
        }

        public enum AnalysisMode
        {
            DependencyAnalysis,
            PerformanceAnalysis,
            UsageAnalysis
        }

        [System.Serializable]
        public class PrefabAnalysisResult
        {
            public GameObject prefab;
            public string prefabName;
            public string prefabPath;
            public string objectType;
            public int componentCount;
            public int materialCount;
            public int shaderCount;
            public List<Shader> uniqueShaders;
            public int scriptCount;
            public List<System.Type> uniqueScriptTypes;
            public int animationCount;
            public int referenceCount;
            public bool hasRenderer;
            public int textureCount;
            public MeshInfo meshInfo;
            public List<string> boneIssues;
            public Dictionary<string, Vector3> bonePositions;
            public Dictionary<string, string[]> boneMap;
            public List<MaterialInfo> materialInfo;
            public List<ScriptInfo> scriptInfo;
            public List<AnimationInfo> animationInfo;
            public List<ReferenceInfo> referenceInfo;
            public List<string> missingComponents;
            public List<string> missingMaterials;
            public List<string> missingScripts;
            public List<string> missingPrefabs;
            public List<ComponentInfo> componentInfo;
            public List<string> referencedByPaths;
            public List<ReferenceInfo> referencedObjects;
        }

        [System.Serializable]
        public class MeshInfo
        {
            public int vertexCount;
            public int triangleCount;
            public bool hasNormals;
            public bool hasUVs;
            public bool hasColors;
        }

        [System.Serializable]
        public class MaterialInfo
        {
            public Material material;
            public Shader shader;
            public int textureCount;
        }

        [System.Serializable]
        public class ScriptInfo
        {
            public MonoBehaviour script;
            public System.Type scriptType;
            public bool isEnabled;
        }

        [System.Serializable]
        public class AnimationInfo
        {
            public Animator animator;
            public Animation animation;
            public RuntimeAnimatorController controller;
            public int animationCount;
        }

        [System.Serializable]
        public class ReferenceInfo
        {
            public UnityEngine.Object reference;
            public string referenceType;
            public string referencePath;
        }

        [System.Serializable]
        public class ComponentInfo
        {
            public Component component;
            public System.Type componentType;
            public bool isMissing;
        }
    }
}