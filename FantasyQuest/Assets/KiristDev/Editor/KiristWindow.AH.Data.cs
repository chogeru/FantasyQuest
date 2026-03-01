using UnityEngine;
using System;
using System.Collections.Generic;

namespace Kirist.EditorTool
{
        [System.Serializable]
        public class AddressableGroupInfo
        {
            public string groupName;
            public string groupGUID;
            public int assetCount;
            public List<AssetInfo> assets;
            public List<string> labels;
            public bool hasIssues;
            public List<string> issues;
            public DateTime lastModified;
            
            public AddressableGroupInfo()
            {
                assets = new List<AssetInfo>();
                labels = new List<string>();
                issues = new List<string>();
                lastModified = DateTime.Now;
            }
        }
        
        [System.Serializable]
        public class AssetInfo
        {
            public string assetPath;
            public string assetName;
            public string assetGUID;
            public UnityEngine.Object assetObject;
            public string groupName;
            public List<string> labels;
            public bool isAddressable;
            public bool hasIssues;
            public List<string> issues;
            public long fileSize;
            public DateTime lastModified;
            
            public AssetInfo()
            {
                labels = new List<string>();
                issues = new List<string>();
                lastModified = DateTime.Now;
            }
        }
        
        [System.Serializable]
        public class LabelInfo
        {
            public string labelName;
            public int usageCount;
            public List<string> assetPaths;
            public bool isActive;
            
            public LabelInfo()
            {
                assetPaths = new List<string>();
                isActive = true;
            }
        }
        
        [System.Serializable]
        public class AddressableAnalysisResult
        {
            public DateTime analysisTime;
            public List<AddressableGroupInfo> groups;
            public List<AssetInfo> orphanedAssets;
            public List<LabelInfo> allLabels;
            public List<string> globalIssues;
            public int totalGroups;
            public int totalAssets;
            public int totalLabels;
            public int groupsWithIssues;
            public int assetsWithIssues;
            
            public AddressableAnalysisResult()
            {
                analysisTime = DateTime.Now;
                groups = new List<AddressableGroupInfo>();
                orphanedAssets = new List<AssetInfo>();
                allLabels = new List<LabelInfo>();
                globalIssues = new List<string>();
            }
        }
        
        [System.Serializable]
        public class AssetSelectionInfo
        {
            public UnityEngine.Object asset;
            public string assetPath;
            public string assetName;
            public bool isSelected;
            public string targetGroupName;
            public List<string> selectedLabels;
            public bool createNewGroup;
            public string newGroupName;
            
            public AssetSelectionInfo()
            {
                selectedLabels = new List<string>();
                createNewGroup = false;
                isSelected = false;
            }
        }
        
        [System.Serializable]
        public class GroupCreationInfo
        {
            public string groupName;
            public string groupSchema;
            public bool isDefaultGroup;
            public List<string> availableSchemas;
            public List<AssetSelectionInfo> selectedAssets;
            public List<string> selectedLabels;
            
            public GroupCreationInfo()
            {
                selectedAssets = new List<AssetSelectionInfo>();
                selectedLabels = new List<string>();
                availableSchemas = new List<string>();
                isDefaultGroup = false;
            }
        }
        
        public enum AddressableIssueType
        {
            EmptyGroup,
            DuplicateAsset,
            InvalidPath,
            MissingAsset,
            CircularDependency,
            InvalidLabel,
            LargeAsset,
            UnusedLabel,
            GroupSchemaMismatch,
            BuildError
        }
        
        public enum AddressableIssueSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }
        
        [System.Serializable]
        public class AddressableIssue
        {
            public AddressableIssueType type;
            public AddressableIssueSeverity severity;
            public string message;
            public string assetPath;
            public string groupName;
            public string suggestion;
            public bool canAutoFix;
            
            public AddressableIssue()
            {
                canAutoFix = false;
            }
        }
}
