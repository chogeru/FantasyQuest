using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Kirist.EditorTool
{
    public class AddressableHelper
    {
            private KiristWindow parentWindow;
            private AddressableAnalysisResult currentAnalysisResult;
            private GroupCreationInfo groupCreationInfo;
            private Vector2 analysisScrollPos;
            private Vector2 assetSelectionScrollPos;
            private Vector2 groupCreationScrollPos;
            private bool isAnalyzing;
            private bool showAssetSelection;
            private bool showGroupCreation;
            private string searchFilter = "";
            private string newGroupName = "New Group";
            private string newLabelName = "";
            private List<string> availableLabels = new List<string>();
            private List<string> selectedLabels = new List<string>();
            private List<UnityEngine.Object> selectedAssets = new List<UnityEngine.Object>();
            
            private UnityEngine.Object selectedFolder;
            private List<UnityEngine.Object> selectedPrefabs = new List<UnityEngine.Object>();
            private bool showPrefabList = false;
            private AddressableAssetGroup selectedGroup;
            private List<AddressableAssetGroup> availableGroups = new List<AddressableAssetGroup>();
            
            private GUIStyle headerStyle;
            private GUIStyle boxStyle;
            private GUIStyle buttonStyle;
            private GUIStyle errorStyle;
            private GUIStyle warningStyle;
            private GUIStyle infoStyle;
            
            public AddressableHelper(KiristWindow parent)
            {
                parentWindow = parent;
                currentAnalysisResult = new AddressableAnalysisResult();
                groupCreationInfo = new GroupCreationInfo();
                RefreshAddressableData();
            }
            
            private void InitializeStyles()
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = Color.white }
                };
                
                boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
                
                buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
                
                errorStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.red }
                };
                
                warningStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.yellow }
                };
                
                infoStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.cyan }
                };
            }
            
            public void DrawUI()
            {
                if (headerStyle == null)
                {
                    try
                    {
                        InitializeStyles();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error initializing styles: {e.Message}");
                        return;
                    }
                }
                
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("📦 Addressable Helper", headerStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🔍 Analysis", showAssetSelection || showGroupCreation ? GUI.skin.button : buttonStyle))
                {
                    showAssetSelection = false;
                    showGroupCreation = false;
                }
                if (GUILayout.Button("📁 Asset Manager", !showAssetSelection && !showGroupCreation ? GUI.skin.button : buttonStyle))
                {
                    showAssetSelection = true;
                    showGroupCreation = false;
                }
                if (GUILayout.Button("⚙️ Group Manager", !showAssetSelection && !showGroupCreation ? GUI.skin.button : buttonStyle))
                {
                    showAssetSelection = false;
                    showGroupCreation = true;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
                
                if (!showAssetSelection && !showGroupCreation)
                {
                    DrawAnalysisTab();
                }
                else if (showAssetSelection)
                {
                    DrawAssetManagerTab();
                }
                else if (showGroupCreation)
                {
                    DrawGroupCreatorTab();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawAnalysisTab()
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                EditorGUILayout.LabelField("🔍 Group Analysis", headerStyle);
                EditorGUILayout.Space(5);
                
                if (availableGroups.Count > 0)
                {
                    EditorGUILayout.LabelField("Select Group to Analyze:", EditorStyles.boldLabel);
                    int selectedIndex = availableGroups.IndexOf(selectedGroup);
                    var groupNames = availableGroups.Select(g => GetGroupName(g)).ToArray();
                    int newIndex = EditorGUILayout.Popup("Group", selectedIndex, groupNames);
                    
                    if (newIndex != selectedIndex && newIndex >= 0 && newIndex < availableGroups.Count)
                    {
                        selectedGroup = availableGroups[newIndex];
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    EditorGUI.BeginDisabledGroup(selectedGroup == null);
                    if (GUILayout.Button("🔍 Analyze Selected Group", buttonStyle, GUILayout.Height(30)))
                    {
                        AnalyzeSelectedGroup();
                    }
                    EditorGUI.EndDisabledGroup();
                    
                    if (selectedGroup == null)
                    {
                        EditorGUILayout.HelpBox("Please select a group to analyze.", MessageType.Info);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No Addressable Groups available.", MessageType.Warning);
                }
                
                EditorGUILayout.Space(10);
                
                if (currentAnalysisResult != null && currentAnalysisResult.groups.Count > 0)
                {
                    DrawAnalysisResults();
                }
                else if (currentAnalysisResult != null && currentAnalysisResult.globalIssues.Count > 0)
                {
                    DrawGlobalIssues();
                }
                else
                {
                    EditorGUILayout.LabelField("Select a group and click analyze button.", EditorStyles.centeredGreyMiniLabel);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawAssetManagerTab()
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                EditorGUILayout.LabelField("📁 Asset Manager", headerStyle);
                EditorGUILayout.Space(5);
                
                var settings = GetAddressableSettings();
                if (settings == null)
                {
                    EditorGUILayout.HelpBox("Addressable settings not found.", MessageType.Error);
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.LabelField("Solution:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("1. Open Window > Asset Management > Addressables > Groups");
                    EditorGUILayout.LabelField("2. Click 'Create Addressables Settings' button");
                    EditorGUILayout.LabelField("3. Refresh this window after setup");
                    EditorGUILayout.Space(5);
                    
                    if (GUILayout.Button("Open Addressable Groups Window", buttonStyle))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
                    }
                    
                    EditorGUILayout.EndVertical();
                    return;
                }
                
                if (GUILayout.Button("🔄 Refresh Data", buttonStyle))
                {
                    RefreshAddressableData();
                }
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("Folder Selection", EditorStyles.boldLabel);
                selectedFolder = EditorGUILayout.ObjectField("Folder", selectedFolder, typeof(UnityEngine.Object), false);
                
                if (selectedFolder != null && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(selectedFolder)))
                {
                    EditorGUILayout.HelpBox("Please select a folder.", MessageType.Warning);
                    selectedFolder = null;
                }
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Asset Drag & Drop", EditorStyles.boldLabel);
                
                Rect dropArea = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
                GUI.Box(dropArea, "Drag & Drop Assets Here\n(Supports all assets: folders, prefabs, scenes, materials, textures, audio, etc.)", EditorStyles.helpBox);
                
                HandleAssetDrop(dropArea);
                
                EditorGUILayout.Space(5);
                
                if (selectedPrefabs.Count > 0)
                {
                    EditorGUILayout.LabelField($"Selected Assets ({selectedPrefabs.Count}):", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    for (int i = selectedPrefabs.Count - 1; i >= 0; i--)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(selectedPrefabs[i], typeof(UnityEngine.Object), false);
                        if (GUILayout.Button("❌", GUILayout.Width(25)))
                        {
                            selectedPrefabs.RemoveAt(i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    
                    if (GUILayout.Button("Remove All Assets", GUILayout.Height(25)))
                    {
                        selectedPrefabs.Clear();
                    }
                }
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("Addressable Group Selection", EditorStyles.boldLabel);
                
                if (availableGroups.Count == 0)
                {
                    EditorGUILayout.HelpBox("No Addressable Groups available.", MessageType.Warning);
                }
                else
                {
                    int selectedIndex = availableGroups.IndexOf(selectedGroup);
                    var groupNames = availableGroups.Select(g => GetGroupName(g)).ToArray();
                    int newIndex = EditorGUILayout.Popup("Group", selectedIndex, groupNames);
                    
                    if (newIndex != selectedIndex && newIndex >= 0 && newIndex < availableGroups.Count)
                    {
                        selectedGroup = availableGroups[newIndex];
                    }
                }
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("Label Selection", EditorStyles.boldLabel);
                
                if (availableLabels.Count == 0)
                {
                    EditorGUILayout.HelpBox("No Labels available.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("Selected Labels:", EditorStyles.miniLabel);
                    
                    for (int i = selectedLabels.Count - 1; i >= 0; i--)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {selectedLabels[i]}");
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            selectedLabels.RemoveAt(i);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.Space(5);
                    
                    List<string> unselectedLabels = availableLabels.Where(label => !selectedLabels.Contains(label)).ToList();
                    if (unselectedLabels.Count > 0)
                    {
                        int labelIndex = EditorGUILayout.Popup("Add Label", -1, unselectedLabels.ToArray());
                        if (labelIndex >= 0 && labelIndex < unselectedLabels.Count)
                        {
                            selectedLabels.Add(unselectedLabels[labelIndex]);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("All labels are selected.");
                    }
                }
                
                EditorGUILayout.Space(20);
                
                bool canAdd = (selectedFolder != null || selectedPrefabs.Count > 0) && selectedGroup != null;
                
                EditorGUI.BeginDisabledGroup(!canAdd);
                if (GUILayout.Button("Add to Addressable", GUILayout.Height(30)))
                {
                    AddToAddressable();
                }
                EditorGUI.EndDisabledGroup();
                
                if (!canAdd)
                {
                    EditorGUILayout.HelpBox("Please select folder/assets and choose an Addressable Group.", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawGroupCreatorTab()
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                EditorGUILayout.LabelField("⚙️ Group Manager", headerStyle);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Existing Group Management", EditorStyles.boldLabel);
                
                if (availableGroups.Count > 0)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    for (int i = availableGroups.Count - 1; i >= 0; i--)
                    {
                        var group = availableGroups[i];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"📁 {GetGroupName(group)}");
                        EditorGUILayout.LabelField($"({GetGroupAssetCount(group)} assets)", EditorStyles.miniLabel);
                        
                        if (GUILayout.Button("🗑️", GUILayout.Width(25)))
                        {
                            if (EditorUtility.DisplayDialog("Delete Group", $"Delete group '{GetGroupName(group)}'?", "Delete", "Cancel"))
                            {
                                DeleteGroup(group);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("No groups created.", MessageType.Info);
                }
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("Create New Group", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Group Name:", GUILayout.Width(80));
                newGroupName = EditorGUILayout.TextField(newGroupName);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newGroupName.Trim()));
                if (GUILayout.Button("➕ Create New Group", buttonStyle, GUILayout.Height(30)))
                {
                    CreateNewGroup();
                }
                EditorGUI.EndDisabledGroup();
                
                if (string.IsNullOrEmpty(newGroupName.Trim()))
                {
                    EditorGUILayout.HelpBox("Enter a group name.", MessageType.Info);
                }
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.LabelField("Label Management", EditorStyles.boldLabel);
                
                if (availableLabels.Count > 0)
                {
                    EditorGUILayout.LabelField("Existing Labels:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    for (int i = availableLabels.Count - 1; i >= 0; i--)
                    {
                        string label = availableLabels[i];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"🏷️ {label}");
                        if (GUILayout.Button("🗑️", GUILayout.Width(25)))
                        {
                            if (EditorUtility.DisplayDialog("Delete Label", $"Delete label '{label}'?", "Delete", "Cancel"))
                            {
                                DeleteLabel(label);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("No labels available.", MessageType.Info);
                }
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("New Label:", GUILayout.Width(80));
                newLabelName = EditorGUILayout.TextField(newLabelName);
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    if (!string.IsNullOrEmpty(newLabelName.Trim()) && !availableLabels.Contains(newLabelName.Trim()))
                    {
                        AddNewLabel(newLabelName.Trim());
                        newLabelName = "";
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                if (string.IsNullOrEmpty(newLabelName.Trim()))
                {
                    EditorGUILayout.HelpBox("Enter a new label name.", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            private void AnalyzeAddressables()
            {
                isAnalyzing = true;
                currentAnalysisResult = new AddressableAnalysisResult();
                
                try
                {
                    if (!IsAddressablePackageAvailable())
                    {
                        LogError("Addressable package is not installed! Please install it from Package Manager.");
                        currentAnalysisResult.globalIssues.Add("Addressable package not installed");
                        return;
                    }
                    
                    var settings = GetAddressableSettings();
                    if (settings == null)
                    {
                        LogError("Addressable Settings not found!");
                        currentAnalysisResult.globalIssues.Add("Addressable Settings not found");
                        return;
                    }
                    
                    var groups = GetAddressableGroups(settings);
                    foreach (var group in groups)
                    {
                        if (group == null) continue;
                        
                        var groupInfo = new AddressableGroupInfo
                        {
                            groupName = GetGroupName(group),
                            groupGUID = GetGroupGUID(group),
                            lastModified = DateTime.Now
                        };
                        
                        var entries = GetGroupEntries(group);
                        foreach (var entry in entries)
                        {
                            if (entry == null) continue;
                            
                            var assetInfo = new AssetInfo
                            {
                                assetPath = GetEntryAssetPath(entry),
                                assetName = Path.GetFileNameWithoutExtension(GetEntryAssetPath(entry)),
                                assetGUID = GetEntryGUID(entry),
                                groupName = groupInfo.groupName,
                                isAddressable = true,
                                lastModified = DateTime.Now
                            };
                            
                            var labels = GetEntryLabels(entry);
                            assetInfo.labels.AddRange(labels);
                            
                            if (File.Exists(assetInfo.assetPath))
                            {
                                var fileInfo = new FileInfo(assetInfo.assetPath);
                                assetInfo.fileSize = fileInfo.Length;
                            }
                            
                            CheckAssetIssues(assetInfo);
                            groupInfo.assets.Add(assetInfo);
                        }
                        
                        groupInfo.assetCount = groupInfo.assets.Count;
                        CheckGroupIssues(groupInfo);
                        currentAnalysisResult.groups.Add(groupInfo);
                    }
                    
                    CollectAllLabels();
                    UpdateStatistics();
                    LogInfo($"Analysis completed! Found {currentAnalysisResult.groups.Count} groups with {currentAnalysisResult.totalAssets} assets.");
                }
                catch (Exception e)
                {
                    LogError($"Error during analysis: {e.Message}");
                }
                finally
                {
                    isAnalyzing = false;
                }
            }
            
            private void CheckAssetIssues(AssetInfo assetInfo)
            {
                if (!File.Exists(assetInfo.assetPath))
                {
                    assetInfo.issues.Add("Asset file not found");
                    assetInfo.hasIssues = true;
                }
                
                var duplicates = currentAnalysisResult.groups
                    .SelectMany(g => g.assets)
                    .Where(a => a.assetGUID == assetInfo.assetGUID && a != assetInfo)
                    .Count();
                
                if (duplicates > 0)
                {
                    assetInfo.issues.Add($"Asset is duplicated in {duplicates} other groups");
                    assetInfo.hasIssues = true;
                }
                
                if (assetInfo.fileSize > 10 * 1024 * 1024)
                {
                    assetInfo.issues.Add($"Large file: {assetInfo.fileSize / (1024 * 1024)}MB");
                    assetInfo.hasIssues = true;
                }
            }
            
            private void CheckGroupIssues(AddressableGroupInfo groupInfo)
            {
                if (groupInfo.assetCount == 0)
                {
                    groupInfo.issues.Add("Empty group");
                    groupInfo.hasIssues = true;
                }
                
                var duplicateAssets = groupInfo.assets
                    .GroupBy(a => a.assetGUID)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);
                
                foreach (var duplicate in duplicateAssets)
                {
                    groupInfo.issues.Add($"Duplicate asset: {duplicate}");
                    groupInfo.hasIssues = true;
                }
                
                if (groupInfo.hasIssues)
                {
                    currentAnalysisResult.groupsWithIssues++;
                }
            }
            
            private void CollectAllLabels()
            {
                var allLabels = new HashSet<string>();
                
                foreach (var group in currentAnalysisResult.groups)
                {
                    foreach (var asset in group.assets)
                    {
                        foreach (var label in asset.labels)
                        {
                            allLabels.Add(label);
                        }
                    }
                }
                
                availableLabels = allLabels.ToList();
                availableLabels.Sort();
            }
            
            private void UpdateStatistics()
            {
                currentAnalysisResult.totalGroups = currentAnalysisResult.groups.Count;
                currentAnalysisResult.totalAssets = currentAnalysisResult.groups.Sum(g => g.assetCount);
                currentAnalysisResult.totalLabels = availableLabels.Count;
                currentAnalysisResult.groupsWithIssues = currentAnalysisResult.groups.Count(g => g.hasIssues);
                currentAnalysisResult.assetsWithIssues = currentAnalysisResult.groups.Sum(g => g.assets.Count(a => a.hasIssues));
            }
            
            private void DrawAnalysisResults()
            {
                analysisScrollPos = EditorGUILayout.BeginScrollView(analysisScrollPos);
                
                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.LabelField("📊 Analysis Summary", headerStyle);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Groups: {currentAnalysisResult.totalGroups}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Assets: {currentAnalysisResult.totalAssets}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Labels: {currentAnalysisResult.totalLabels}", GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Groups with Issues: {currentAnalysisResult.groupsWithIssues}", errorStyle, GUILayout.Width(150));
                EditorGUILayout.LabelField($"Assets with Issues: {currentAnalysisResult.assetsWithIssues}", errorStyle, GUILayout.Width(150));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
                
                foreach (var group in currentAnalysisResult.groups)
                {
                    DrawGroupInfo(group);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            private void DrawGlobalIssues()
            {
                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.LabelField("⚠️ Global Issues", headerStyle);
                EditorGUILayout.Space(3);
                
                foreach (var issue in currentAnalysisResult.globalIssues)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("•", errorStyle, GUILayout.Width(15));
                    EditorGUILayout.LabelField(issue, errorStyle);
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Please install the Addressable package from Package Manager to use this feature.", EditorStyles.centeredGreyMiniLabel);
                
                EditorGUILayout.EndVertical();
            }
            
            private void DrawGroupInfo(AddressableGroupInfo groupInfo)
            {
                EditorGUILayout.BeginVertical(boxStyle);
                
                EditorGUILayout.BeginHorizontal();
                var groupStyle = groupInfo.hasIssues ? errorStyle : EditorStyles.boldLabel;
                EditorGUILayout.LabelField($"📁 {groupInfo.groupName}", groupStyle);
                EditorGUILayout.LabelField($"({groupInfo.assetCount} assets)", EditorStyles.miniLabel);
                
                if (groupInfo.hasIssues)
                {
                    EditorGUILayout.LabelField("⚠️", errorStyle, GUILayout.Width(20));
                }
                EditorGUILayout.EndHorizontal();
                
                if (groupInfo.issues.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("Issues:", EditorStyles.boldLabel);
                    foreach (var issue in groupInfo.issues)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("•", errorStyle, GUILayout.Width(15));
                        EditorGUILayout.LabelField(issue, errorStyle);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                if (groupInfo.labels.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("Labels:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    foreach (var label in groupInfo.labels)
                    {
                        EditorGUILayout.LabelField($"[{label}]", infoStyle, GUILayout.Width(60));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
            
            private void HandleAssetDrop(Rect dropArea)
            {
                Event evt = Event.current;
                
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!dropArea.Contains(evt.mousePosition))
                            return;
                        
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            
                            foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                            {
                                if (draggedObject != null)
                                {
                                    if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(draggedObject)))
                                    {
                                        AddAssetsFromFolder(draggedObject);
                                    }
                                    else if (!selectedPrefabs.Contains(draggedObject))
                                    {
                                        selectedPrefabs.Add(draggedObject);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            
            private void AddAssetsFromFolder(UnityEngine.Object folder)
            {
                string folderPath = AssetDatabase.GetAssetPath(folder);
                string[] assetGuids = AssetDatabase.FindAssets("", new[] { folderPath });
                
                foreach (string guid in assetGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!assetPath.EndsWith(".meta") && !AssetDatabase.IsValidFolder(assetPath))
                    {
                        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        if (asset != null && !selectedPrefabs.Contains(asset))
                        {
                            selectedPrefabs.Add(asset);
                        }
                    }
                }
            }
            
            private void DrawSelectedAssets()
            {
                EditorGUILayout.LabelField("Selected Assets:", EditorStyles.boldLabel);
                
                assetSelectionScrollPos = EditorGUILayout.BeginScrollView(assetSelectionScrollPos, GUILayout.Height(200));
                
                for (int i = selectedAssets.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.ObjectField(selectedAssets[i], typeof(UnityEngine.Object), false);
                    
                    if (GUILayout.Button("❌", GUILayout.Width(25)))
                    {
                        selectedAssets.RemoveAt(i);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Target Group:", GUILayout.Width(80));
                
                if (currentAnalysisResult.groups.Count > 0)
                {
                    var groupNames = currentAnalysisResult.groups.Select(g => g.groupName).ToArray();
                    int selectedIndex = EditorGUILayout.Popup(0, groupNames);
                }
                else
                {
                    EditorGUILayout.LabelField("No groups available", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndHorizontal();
                
                DrawLabelSelection();
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Add Selected Assets to Group", buttonStyle, GUILayout.Height(25)))
                {
                    AddAssetsToGroup();
                }
            }
            
            private void DrawLabelSelection()
            {
                EditorGUILayout.LabelField("Labels:", EditorStyles.boldLabel);
                
                if (availableLabels.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Available:", GUILayout.Width(70));
                    
                    for (int i = 0; i < availableLabels.Count; i++)
                    {
                        bool isSelected = selectedLabels.Contains(availableLabels[i]);
                        bool newSelection = EditorGUILayout.Toggle(availableLabels[i], isSelected, GUILayout.Width(80));
                        
                        if (newSelection != isSelected)
                        {
                            if (newSelection)
                            {
                                selectedLabels.Add(availableLabels[i]);
                            }
                            else
                            {
                                selectedLabels.Remove(availableLabels[i]);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("No labels available", EditorStyles.miniLabel);
                }
            }
            
            private void AddAssetsToGroup()
            {
                if (selectedAssets.Count == 0)
                {
                    LogWarning("No assets selected!");
                    return;
                }
                
                LogInfo($"Adding {selectedAssets.Count} assets to group with labels: {string.Join(", ", selectedLabels)}");
            }
            
            private void CreateNewGroup()
            {
                if (string.IsNullOrEmpty(newGroupName.Trim()))
                {
                    LogWarning("Please enter a group name!");
                    return;
                }
                
                try
                {
                    var settings = GetAddressableSettings();
                    if (settings == null)
                    {
                        LogError("Addressable settings not found.");
                        return;
                    }
                    
                    var newGroup = settings.CreateGroup(newGroupName.Trim(), false, false, true, null);
                    
                    if (newGroup != null)
                    {
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                        
                        RefreshAddressableData();
                        newGroupName = "";
                        LogInfo($"New group '{newGroup.name}' created successfully.");
                    }
                    else
                    {
                        LogError("Failed to create group.");
                    }
                }
                catch (Exception e)
                {
                    LogError($"Error creating group: {e.Message}");
                }
            }
            
            private void LogInfo(string message)
            {
                Debug.Log($"[Addressable Helper] {message}");
            }
            
            private void LogWarning(string message)
            {
                Debug.LogWarning($"[Addressable Helper] {message}");
            }
            
            private void LogError(string message)
            {
                Debug.LogError($"[Addressable Helper] {message}");
            }
            
            private bool IsAddressablePackageAvailable()
            {
                try
                {
                    var assembly = System.Reflection.Assembly.Load("Unity.Addressables.Editor");
                    return assembly != null;
                }
                catch
                {
                    return false;
                }
            }
            
            private AddressableAssetSettings GetAddressableSettings()
            {
                try
                {
                    var settings = AddressableAssetSettingsDefaultObject.Settings;
                    
                    if (settings == null)
                    {
                        LogWarning("Addressable settings not initialized.");
                        return null;
                    }
                    
                    return settings;
                }
                catch (Exception e)
                {
                    LogError($"Failed to get Addressable settings: {e.Message}");
                    return null;
                }
            }
            
            private IEnumerable<AddressableAssetGroup> GetAddressableGroups(AddressableAssetSettings settings)
            {
                try
                {
                    if (settings == null) return new List<AddressableAssetGroup>();
                    return settings.groups;
                }
                catch
                {
                    return new List<AddressableAssetGroup>();
                }
            }
            
            private string GetGroupName(AddressableAssetGroup group)
            {
                try
                {
                    return group?.name ?? "Unknown Group";
                }
                catch
                {
                    return "Unknown Group";
                }
            }
            
            private string GetGroupGUID(object group)
            {
                try
                {
                    var guidProperty = group.GetType().GetProperty("Guid");
                    return guidProperty?.GetValue(group)?.ToString() ?? "";
                }
                catch
                {
                    return "";
                }
            }
            
            private IEnumerable<object> GetGroupEntries(object group)
            {
                try
                {
                    var entriesProperty = group.GetType().GetProperty("entries");
                    if (entriesProperty == null) return new List<object>();
                    
                    var entries = entriesProperty.GetValue(group);
                    if (entries is System.Collections.IEnumerable enumerable)
                    {
                        return enumerable.Cast<object>();
                    }
                    return new List<object>();
                }
                catch
                {
                    return new List<object>();
                }
            }
            
            private string GetEntryAssetPath(object entry)
            {
                try
                {
                    var assetPathProperty = entry.GetType().GetProperty("AssetPath");
                    return assetPathProperty?.GetValue(entry)?.ToString() ?? "";
                }
                catch
                {
                    return "";
                }
            }
            
            private string GetEntryGUID(object entry)
            {
                try
                {
                    var guidProperty = entry.GetType().GetProperty("guid");
                    return guidProperty?.GetValue(entry)?.ToString() ?? "";
                }
                catch
                {
                    return "";
                }
            }
            
            private List<string> GetEntryLabels(object entry)
            {
                try
                {
                    var labelsProperty = entry.GetType().GetProperty("labels");
                    if (labelsProperty == null) return new List<string>();
                    
                    var labels = labelsProperty.GetValue(entry);
                    if (labels is System.Collections.IEnumerable enumerable)
                    {
                        return enumerable.Cast<string>().ToList();
                    }
                    return new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            
            private void RefreshAddressableData()
            {
                try
                {
                    var settings = GetAddressableSettings();
                    if (settings == null)
                    {
                        LogError("Addressable settings not found.");
                        return;
                    }
                    
                    availableGroups.Clear();
                    var groups = GetAddressableGroups(settings);
                    availableGroups.AddRange(groups);
                    
                    availableLabels.Clear();
                    try
                    {
                        var labels = settings.GetLabels();
                        availableLabels.AddRange(labels);
                    }
                    catch (Exception e)
                    {
                        LogWarning($"Failed to get label information: {e.Message}");
                    }
                    
                    LogInfo($"Data refresh completed: {availableGroups.Count} groups, {availableLabels.Count} labels");
                }
                catch (Exception e)
                {
                    LogError($"Error during data refresh: {e.Message}");
                }
            }
            
            private void AddToAddressable()
            {
                if (selectedGroup == null)
                {
                    LogError("No group selected.");
                    return;
                }
                
                int addedCount = 0;
                List<string> addedAssets = new List<string>();
                
                try
                {
                    if (selectedFolder != null)
                    {
                        string folderPath = AssetDatabase.GetAssetPath(selectedFolder);
                        string[] assetGuids = AssetDatabase.FindAssets("", new[] { folderPath });
                        
                        foreach (string guid in assetGuids)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (!assetPath.EndsWith(".meta") && !AssetDatabase.IsValidFolder(assetPath))
                            {
                                if (AddAssetToGroup(assetPath))
                                {
                                    addedAssets.Add(assetPath);
                                    addedCount++;
                                }
                            }
                        }
                    }
                    
                    foreach (UnityEngine.Object asset in selectedPrefabs)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(asset);
                        if (AddAssetToGroup(assetPath))
                        {
                            addedAssets.Add(assetPath);
                            addedCount++;
                        }
                    }
                    
                    var settings = GetAddressableSettings();
                    if (settings != null)
                    {
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                    }
                    
                    LogInfo($"Total {addedCount} assets added to '{GetGroupName(selectedGroup)}' group.");
                    
                    if (addedAssets.Count > 0)
                    {
                        LogInfo("Added assets:");
                        foreach (string asset in addedAssets)
                        {
                            LogInfo($"- {asset}");
                        }
                    }
                }
                catch (Exception e)
                {
                    LogError($"Error adding assets: {e.Message}");
                }
            }
            
            private bool AddAssetToGroup(string assetPath)
            {
                try
                {
                    var settings = GetAddressableSettings();
                    if (settings == null) return false;
                    
                    var existingEntry = settings.FindAssetEntry(assetPath);
                    if (existingEntry != null)
                    {
                        LogWarning($"'{assetPath}' is already registered in Addressable. (Group: {existingEntry.parentGroup.name})");
                        return false;
                    }
                    
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                    var entry = settings.CreateOrMoveEntry(guid, selectedGroup);
                    
                    if (entry != null)
                    {
                        foreach (string label in selectedLabels)
                        {
                            entry.SetLabel(label, true);
                        }
                        
                        return true;
                    }
                    
                    LogError($"Cannot add '{assetPath}' to group.");
                    return false;
                }
                catch (Exception e)
                {
                    LogError($"Error adding '{assetPath}': {e.Message}");
                    return false;
                }
            }
            
            private void AnalyzeSelectedGroup()
            {
                if (selectedGroup == null) return;
                
                isAnalyzing = true;
                currentAnalysisResult = new AddressableAnalysisResult();
                
                try
                {
                    var groupInfo = new AddressableGroupInfo
                    {
                        groupName = GetGroupName(selectedGroup),
                        groupGUID = GetGroupGUID(selectedGroup),
                        lastModified = DateTime.Now
                    };
                    
                    var entries = GetGroupEntries(selectedGroup);
                    foreach (var entry in entries)
                    {
                        if (entry == null) continue;
                        
                        var assetInfo = new AssetInfo
                        {
                            assetPath = GetEntryAssetPath(entry),
                            assetName = Path.GetFileNameWithoutExtension(GetEntryAssetPath(entry)),
                            assetGUID = GetEntryGUID(entry),
                            groupName = groupInfo.groupName,
                            isAddressable = true,
                            lastModified = DateTime.Now
                        };
                        
                        var labels = GetEntryLabels(entry);
                        assetInfo.labels.AddRange(labels);
                        
                        if (File.Exists(assetInfo.assetPath))
                        {
                            var fileInfo = new FileInfo(assetInfo.assetPath);
                            assetInfo.fileSize = fileInfo.Length;
                        }
                        
                        CheckAssetIssues(assetInfo);
                        
                        groupInfo.assets.Add(assetInfo);
                    }
                    
                    groupInfo.assetCount = groupInfo.assets.Count;
                    
                    CheckGroupIssues(groupInfo);
                    
                    currentAnalysisResult.groups.Add(groupInfo);
                    
                    UpdateStatistics();
                    
                    LogInfo($"Group '{groupInfo.groupName}' analysis completed! {groupInfo.assetCount} assets, {groupInfo.issues.Count} issues found");
                }
                catch (Exception e)
                {
                    LogError($"Error analyzing group: {e.Message}");
                }
                finally
                {
                    isAnalyzing = false;
                }
            }
            
            private int GetGroupAssetCount(AddressableAssetGroup group)
            {
                try
                {
                    return group.entries.Count;
                }
                catch
                {
                    return 0;
                }
            }
            
            private void DeleteGroup(AddressableAssetGroup group)
            {
                try
                {
                    var settings = GetAddressableSettings();
                    if (settings != null)
                    {
                        settings.RemoveGroup(group);
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                        
                        RefreshAddressableData();
                        LogInfo($"Group '{GetGroupName(group)}' deleted successfully.");
                    }
                }
                catch (Exception e)
                {
                    LogError($"Error deleting group: {e.Message}");
                }
            }
            
            private void AddNewLabel(string labelName)
            {
                try
                {
                    var settings = GetAddressableSettings();
                    if (settings != null)
                    {
                        settings.AddLabel(labelName);
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                        
                        RefreshAddressableData();
                        LogInfo($"New label '{labelName}' added successfully.");
                    }
                }
                catch (Exception e)
                {
                    LogError($"Error adding label: {e.Message}");
                }
            }
            
            private void DeleteLabel(string labelName)
            {
                try
                {
                    var settings = GetAddressableSettings();
                    if (settings != null)
                    {
                        settings.RemoveLabel(labelName);
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                        
                        RefreshAddressableData();
                        LogInfo($"Label '{labelName}' deleted successfully.");
                    }
                }
                catch (Exception e)
                {
                    LogError($"Error deleting label: {e.Message}");
                }
            }
            
            private IEnumerable<object> GetGroupEntries(AddressableAssetGroup group)
            {
                try
                {
                    if (group == null) return new List<object>();
                    return group.entries.Cast<object>();
                }
                catch
                {
                    return new List<object>();
                }
            }
            
            private string GetGroupGUID(AddressableAssetGroup group)
            {
                try
                {
                    return group?.Guid ?? "";
                }
                catch
                {
                    return "";
                }
            }
            
    }
}
