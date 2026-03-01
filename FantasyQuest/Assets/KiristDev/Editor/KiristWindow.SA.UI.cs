using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Kirist.EditorTool
{
    public partial class KiristWindow
    {
        public partial class SceneAnalyzer
        {
            private void DrawGameObjectAnalysis()
            {
                EditorGUILayout.BeginVertical(UIStyles.GradientBackgroundStyle);
                
                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UIColors.ModernGreen }
                };
                EditorGUILayout.LabelField("🎯 GameObject Analysis", titleStyle);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Component Types:", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                var sortedComponents = currentAnalysisResult.componentTypes?.OrderByDescending(x => x.Value).Take(10) ?? new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>();
                foreach (var component in sortedComponents)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {component.Key}", EditorStyles.miniLabel, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"{component.Value}", EditorStyles.miniLabel, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("GameObjects:", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                objectListScrollPos = EditorGUILayout.BeginScrollView(objectListScrollPos, GUILayout.Height(250));
                
                for (int i = 0; i < (currentAnalysisResult.gameObjects?.Count ?? 0); i++)
                {
                    var objInfo = currentAnalysisResult.gameObjects[i];
                    bool isSelected = selectedObjectIndex == i;
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button(isSelected ? "✓" : "○", GUILayout.Width(20), GUILayout.Height(20)))
                    {
                        selectedObjectIndex = isSelected ? -1 : i;
                        selectedGameObject = isSelected ? null : objInfo.gameObject;
                    }
                    
                    var nameStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = objInfo.isActive ? Color.white : Color.gray }
                    };
                    
                    EditorGUILayout.LabelField(objInfo.name, nameStyle, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"Layer: {objInfo.layer}", EditorStyles.miniLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Tag: {objInfo.tag}", EditorStyles.miniLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Components: {objInfo.components.Count}", EditorStyles.miniLabel, GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Children: {objInfo.childCount}", EditorStyles.miniLabel, GUILayout.Width(80));
                    
                    if (GUILayout.Button("🔍", GUILayout.Width(30), GUILayout.Height(20)))
                    {
                        Selection.activeGameObject = objInfo.gameObject;
                        EditorGUIUtility.PingObject(objInfo.gameObject);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                
                if (selectedObjectIndex >= 0 && selectedObjectIndex < (currentAnalysisResult.gameObjects?.Count ?? 0))
                {
                    DrawSelectedObjectDetails();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawSelectedObjectDetails()
            {
                var objInfo = currentAnalysisResult.gameObjects[selectedObjectIndex];
                
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                EditorGUILayout.LabelField($"Selected: {objInfo.name}", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField($"Active: {objInfo.isActive}");
                EditorGUILayout.LabelField($"Layer: {objInfo.layer} ({LayerMask.LayerToName(objInfo.layer)})");
                EditorGUILayout.LabelField($"Tag: {objInfo.tag}");
                EditorGUILayout.LabelField($"Components: {objInfo.components.Count}");
                EditorGUILayout.LabelField($"Children: {objInfo.childCount}");
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Components:", EditorStyles.boldLabel);
                
                foreach (var compInfo in objInfo.components)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {compInfo.componentType.Name}", EditorStyles.miniLabel);
                    if (GUILayout.Button("🔍", GUILayout.Width(30), GUILayout.Height(15)))
                    {
                        Selection.activeObject = compInfo.component;
                        EditorGUIUtility.PingObject(compInfo.component);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawEnvironmentAnalysis()
            {
                EditorGUILayout.BeginVertical(UIStyles.GradientBackgroundStyle);
                
                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UIColors.ModernPurple }
                };
                EditorGUILayout.LabelField("🌍 Environment Analysis", titleStyle);
                EditorGUILayout.Space(5);
                
                environmentScrollPos = EditorGUILayout.BeginScrollView(environmentScrollPos, GUILayout.Height(300));
                
                DrawLightingInfo();
                EditorGUILayout.Space(10);
                
                DrawCameraInfo();
                EditorGUILayout.Space(10);
                
                DrawTerrainInfo();
                EditorGUILayout.Space(10);
                
                DrawPostProcessingInfo();
                
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            
            private void DrawLightingInfo()
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("💡 Lighting", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                if (currentAnalysisResult?.environmentInfo != null)
                {
                    EditorGUILayout.LabelField($"Total Lights: {currentAnalysisResult.environmentInfo.lightCount}");
                    EditorGUILayout.LabelField($"Lightmaps: {currentAnalysisResult.environmentInfo.lightmapCount}");
                    EditorGUILayout.LabelField($"Reflection Mode: {currentAnalysisResult.environmentInfo.reflectionMode}");
                    
                    if (currentAnalysisResult.environmentInfo.lights?.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Light Details:", EditorStyles.boldLabel);
                    
                    foreach (var lightInfo in currentAnalysisResult.environmentInfo.lights)
                    {
                        if (lightInfo.light == null)
                        {
                            continue;
                        }
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {lightInfo.light.name}", EditorStyles.miniLabel, GUILayout.Width(150));
                        EditorGUILayout.LabelField($"Type: {lightInfo.type}", EditorStyles.miniLabel, GUILayout.Width(100));
                        EditorGUILayout.LabelField($"Intensity: {lightInfo.intensity:F2}", EditorStyles.miniLabel, GUILayout.Width(100));
                        EditorGUILayout.LabelField($"Range: {lightInfo.range:F1}", EditorStyles.miniLabel, GUILayout.Width(80));
                        
                        var statusColor = lightInfo.isActive ? UIColors.Success : UIColors.Danger;
                        var statusStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = statusColor } };
                        EditorGUILayout.LabelField(lightInfo.isActive ? "Active" : "Inactive", statusStyle, GUILayout.Width(60));
                        
                        if (GUILayout.Button("🔍", GUILayout.Width(30), GUILayout.Height(15)))
                        {
                            if (lightInfo.light != null && lightInfo.light.gameObject != null)
                            {
                                Selection.activeGameObject = lightInfo.light.gameObject;
                                EditorGUIUtility.PingObject(lightInfo.light.gameObject);
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                }
                else
                {
                    EditorGUILayout.LabelField("No lighting information available", EditorStyles.centeredGreyMiniLabel);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawCameraInfo()
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📷 Cameras", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                if (currentAnalysisResult?.environmentInfo != null)
                {
                    EditorGUILayout.LabelField($"Total Cameras: {currentAnalysisResult.environmentInfo.cameraCount}");
                    
                    if (currentAnalysisResult.environmentInfo.cameras?.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Camera Details:", EditorStyles.boldLabel);
                    
                    foreach (var cameraInfo in currentAnalysisResult.environmentInfo.cameras)
                    {
                        if (cameraInfo.camera == null)
                        {
                            continue;
                        }
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {cameraInfo.camera.name}", EditorStyles.miniLabel, GUILayout.Width(150));
                        EditorGUILayout.LabelField($"FOV: {cameraInfo.fieldOfView:F1}°", EditorStyles.miniLabel, GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Near: {cameraInfo.nearClipPlane:F2}", EditorStyles.miniLabel, GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Far: {cameraInfo.farClipPlane:F0}", EditorStyles.miniLabel, GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Clear: {cameraInfo.clearFlags}", EditorStyles.miniLabel, GUILayout.Width(100));
                        
                        var statusColor = cameraInfo.isActive ? UIColors.Success : UIColors.Danger;
                        var statusStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = statusColor } };
                        EditorGUILayout.LabelField(cameraInfo.isActive ? "Active" : "Inactive", statusStyle, GUILayout.Width(60));
                        
                        if (GUILayout.Button("🔍", GUILayout.Width(30), GUILayout.Height(15)))
                        {
                            if (cameraInfo.camera != null && cameraInfo.camera.gameObject != null)
                            {
                                Selection.activeGameObject = cameraInfo.camera.gameObject;
                                EditorGUIUtility.PingObject(cameraInfo.camera.gameObject);
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                }
                else
                {
                    EditorGUILayout.LabelField("No camera information available", EditorStyles.centeredGreyMiniLabel);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawTerrainInfo()
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🏔️ Terrains", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                if (currentAnalysisResult?.environmentInfo != null)
                {
                    EditorGUILayout.LabelField($"Total Terrains: {currentAnalysisResult.environmentInfo.terrainCount}");
                    
                    if (currentAnalysisResult.environmentInfo.terrains?.Count > 0)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Terrain Details:", EditorStyles.boldLabel);
                    
                    foreach (var terrainInfo in currentAnalysisResult.environmentInfo.terrains)
                    {
                        if (terrainInfo.terrain == null)
                        {
                            continue;
                        }
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {terrainInfo.terrain.name}", EditorStyles.miniLabel, GUILayout.Width(150));
                        EditorGUILayout.LabelField($"Heightmap: {terrainInfo.heightmapResolution}", EditorStyles.miniLabel, GUILayout.Width(120));
                        EditorGUILayout.LabelField($"Detail: {terrainInfo.detailResolution}", EditorStyles.miniLabel, GUILayout.Width(100));
                        EditorGUILayout.LabelField($"Alpha: {terrainInfo.alphamapResolution}", EditorStyles.miniLabel, GUILayout.Width(100));
                        
                        var statusColor = terrainInfo.isActive ? UIColors.Success : UIColors.Danger;
                        var statusStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = statusColor } };
                        EditorGUILayout.LabelField(terrainInfo.isActive ? "Active" : "Inactive", statusStyle, GUILayout.Width(60));
                        
                        if (GUILayout.Button("🔍", GUILayout.Width(30), GUILayout.Height(15)))
                        {
                            if (terrainInfo.terrain != null && terrainInfo.terrain.gameObject != null)
                            {
                                Selection.activeGameObject = terrainInfo.terrain.gameObject;
                                EditorGUIUtility.PingObject(terrainInfo.terrain.gameObject);
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                }
                else
                {
                    EditorGUILayout.LabelField("No terrain information available", EditorStyles.centeredGreyMiniLabel);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawPostProcessingInfo()
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("🎨 Post Processing", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                if (currentAnalysisResult?.environmentInfo != null)
                {
                    EditorGUILayout.LabelField($"Total Volumes: {currentAnalysisResult.environmentInfo.postProcessingVolumeCount}");
                    
                    if (currentAnalysisResult.environmentInfo.postProcessingVolumes?.Count > 0)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Volume Details:", EditorStyles.boldLabel);
                        
                        foreach (var ppInfo in currentAnalysisResult.environmentInfo.postProcessingVolumes)
                        {
                            if (ppInfo.volume == null)
                            {
                                continue;
                            }
                            
                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"• {ppInfo.volume.name}", EditorStyles.boldLabel, GUILayout.Width(200));
                            
                            var statusColor = ppInfo.isActive ? UIColors.Success : UIColors.Danger;
                            var statusStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = statusColor } };
                            EditorGUILayout.LabelField(ppInfo.isActive ? "Active" : "Inactive", statusStyle, GUILayout.Width(60));
                            
                            if (GUILayout.Button("🔍", GUILayout.Width(30), GUILayout.Height(20)))
                            {
                                if (ppInfo.volume != null && ppInfo.volume.gameObject != null)
                                {
                                    Selection.activeGameObject = ppInfo.volume.gameObject;
                                    EditorGUIUtility.PingObject(ppInfo.volume.gameObject);
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"Global: {ppInfo.isGlobal}", EditorStyles.miniLabel, GUILayout.Width(80));
                            EditorGUILayout.LabelField($"Priority: {ppInfo.priority:F0}", EditorStyles.miniLabel, GUILayout.Width(80));
                            EditorGUILayout.LabelField($"Weight: {ppInfo.weight:F2}", EditorStyles.miniLabel, GUILayout.Width(80));
                            EditorGUILayout.LabelField($"Blend: {ppInfo.blendDistance:F1}", EditorStyles.miniLabel, GUILayout.Width(80));
                            EditorGUILayout.EndHorizontal();
                            
                            if (ppInfo.profile != null)
                            {
                                EditorGUILayout.Space(3);
                                EditorGUILayout.LabelField("📋 Profile Information:", EditorStyles.boldLabel);
                                
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField($"Profile Name: {ppInfo.profile.name}", EditorStyles.miniLabel, GUILayout.Width(200));
                                EditorGUILayout.LabelField($"Settings Count: {ppInfo.settingsCount}", EditorStyles.miniLabel, GUILayout.Width(100));
                                EditorGUILayout.EndHorizontal();
                                
                                if (ppInfo.activeSettings?.Count > 0)
                                {
                                    EditorGUILayout.Space(2);
                                    EditorGUILayout.LabelField("Active Settings:", EditorStyles.miniLabel);
                                    
                                    foreach (var setting in ppInfo.activeSettings)
                                    {
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.LabelField($"  • {setting}", EditorStyles.miniLabel, GUILayout.Width(250));
                                        EditorGUILayout.EndHorizontal();
                                    }
                                }
                                
                                if (ppInfo.inactiveSettings?.Count > 0)
                                {
                                    EditorGUILayout.Space(2);
                                    EditorGUILayout.LabelField("Inactive Settings:", EditorStyles.miniLabel);
                                    
                                    foreach (var setting in ppInfo.inactiveSettings)
                                    {
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.LabelField($"  • {setting}", EditorStyles.miniLabel, GUILayout.Width(250));
                                        EditorGUILayout.EndHorizontal();
                                    }
                                }
                                
                                EditorGUILayout.Space(3);
                                EditorGUILayout.BeginHorizontal();
                                if (GUILayout.Button("📋 Open Profile", UIStyles.ButtonStyle, GUILayout.Width(120), GUILayout.Height(20)))
                                {
                                    Selection.activeObject = ppInfo.profile;
                                    EditorGUIUtility.PingObject(ppInfo.profile);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            else
                            {
                                EditorGUILayout.Space(3);
                                EditorGUILayout.LabelField("⚠️ No Profile Assigned", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = UIColors.Warning } });
                            }
                            
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.Space(5);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No post processing information available", EditorStyles.centeredGreyMiniLabel);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            
            private void DrawPerformanceAnalysis()
            {
                EditorGUILayout.BeginVertical(UIStyles.GradientBackgroundStyle);
                
                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = UIColors.ModernOrange }
                };
                EditorGUILayout.LabelField("⚡ Performance Analysis", titleStyle);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Performance Metrics:", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField($"Total Objects: {currentAnalysisResult.totalObjects}");
                EditorGUILayout.LabelField($"Active Objects: {currentAnalysisResult.activeObjects}");
                EditorGUILayout.LabelField($"Total Components: {currentAnalysisResult.totalComponents}");
                EditorGUILayout.LabelField($"Renderers: {currentAnalysisResult.totalRenderers}");
                EditorGUILayout.LabelField($"Materials: {currentAnalysisResult.totalMaterials}");
                
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Recommendations:", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                if (currentAnalysisResult.totalObjects > 1000)
                {
                    EditorGUILayout.HelpBox("⚠️ High object count detected. Consider using object pooling or LOD systems.", MessageType.Warning);
                }
                
                if (currentAnalysisResult.totalMaterials > 100)
                {
                    EditorGUILayout.HelpBox("⚠️ High material count detected. Consider material atlasing.", MessageType.Warning);
                }
                
                if (currentAnalysisResult.totalRenderers > 500)
                {
                    EditorGUILayout.HelpBox("⚠️ High renderer count detected. Consider batching or culling.", MessageType.Warning);
                }
                
                EditorGUILayout.EndVertical();
            }
            
        }
    }
}
