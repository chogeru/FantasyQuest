using UnityEngine;
using UnityEditor;

namespace Kirist.EditorTool
{
    public static class UnityVersionHelper
    {
        #if UNITY_6000_0_OR_NEWER
        private static readonly int UnityMajorVersion = 6;
        #elif UNITY_2023_1_OR_NEWER
        private static readonly int UnityMajorVersion = 2023;
        #elif UNITY_2021_1_OR_NEWER
        private static readonly int UnityMajorVersion = 2021;
        #elif UNITY_2019_1_OR_NEWER
        private static readonly int UnityMajorVersion = 2019;
        #elif UNITY_2018_3_OR_NEWER
        private static readonly int UnityMajorVersion = 2018;
        #else
        private static readonly int UnityMajorVersion = 2017;
        #endif

        #region Prefab Utility 호환성

        public static bool IsPrefab(GameObject obj)
        {
            if (obj == null) return false;

            #if UNITY_2018_3_OR_NEWER
            return PrefabUtility.IsPartOfPrefabAsset(obj) ||
                   PrefabUtility.IsPartOfPrefabInstance(obj);
            #else
            var prefabType = PrefabUtility.GetPrefabType(obj);
            return prefabType == PrefabType.Prefab ||
                   prefabType == PrefabType.PrefabInstance;
            #endif
        }

        public static bool IsPrefabAsset(GameObject obj)
        {
            if (obj == null) return false;

            #if UNITY_2018_3_OR_NEWER
            return PrefabUtility.IsPartOfPrefabAsset(obj);
            #else
            var prefabType = PrefabUtility.GetPrefabType(obj);
            return prefabType == PrefabType.Prefab;
            #endif
        }

        public static bool IsPrefabInstance(GameObject obj)
        {
            if (obj == null) return false;

            #if UNITY_2018_3_OR_NEWER
            return PrefabUtility.IsPartOfPrefabInstance(obj);
            #else
            var prefabType = PrefabUtility.GetPrefabType(obj);
            return prefabType == PrefabType.PrefabInstance;
            #endif
        }

        public static bool IsModelPrefab(GameObject obj)
        {
            if (obj == null) return false;

            #if UNITY_2018_3_OR_NEWER
            return PrefabUtility.IsPartOfModelPrefab(obj);
            #else
            var prefabType = PrefabUtility.GetPrefabType(obj);
            return prefabType == PrefabType.ModelPrefab ||
                   prefabType == PrefabType.ModelPrefabInstance;
            #endif
        }

        public static bool IsPrefabAssetMissing(GameObject obj)
        {
            if (obj == null) return false;

            #if UNITY_2018_3_OR_NEWER
            return PrefabUtility.IsPrefabAssetMissing(obj);
            #else
            var prefabType = PrefabUtility.GetPrefabType(obj);
            return prefabType == PrefabType.MissingPrefabInstance;
            #endif
        }

        public static GameObject GetPrefabSource(GameObject obj)
        {
            if (obj == null) return null;

            #if UNITY_2018_3_OR_NEWER
            return PrefabUtility.GetCorrespondingObjectFromSource(obj);
            #else
            return PrefabUtility.GetPrefabParent(obj) as GameObject;
            #endif
        }

        #endregion

        #region Shader Utility 호환성

        public static int GetShaderMessageCount(Shader shader)
        {
            if (shader == null) return 0;

            #if UNITY_2019_1_OR_NEWER
            return ShaderUtil.GetShaderMessageCount(shader);
            #else
            return ShaderUtil.ShaderHasError(shader) ? 1 : 0;
            #endif
        }

        public static ShaderMessage[] GetShaderMessages(Shader shader)
        {
            if (shader == null) return new ShaderMessage[0];

            #if UNITY_2019_1_OR_NEWER
            var unityMessages = ShaderUtil.GetShaderMessages(shader);
            var customMessages = new ShaderMessage[unityMessages.Length];

            for (int i = 0; i < unityMessages.Length; i++)
            {
                customMessages[i] = new ShaderMessage
                {
                    message = unityMessages[i].message,
                    severity = unityMessages[i].severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error
                        ? ShaderCompilerMessageSeverity.Error
                        : ShaderCompilerMessageSeverity.Warning,
                    line = unityMessages[i].line,
                    file = unityMessages[i].file
                };
            }

            return customMessages;
            #else
            if (ShaderUtil.ShaderHasError(shader))
            {
                return new ShaderMessage[]
                {
                    new ShaderMessage
                    {
                        message = "Shader compilation failed",
                        severity = ShaderCompilerMessageSeverity.Error,
                        line = 0,
                        file = ""
                    }
                };
            }
            return new ShaderMessage[0];
            #endif
        }

        public static bool ShaderHasError(Shader shader)
        {
            if (shader == null) return true;

            #if UNITY_2019_1_OR_NEWER
            int messageCount = ShaderUtil.GetShaderMessageCount(shader);
            if (messageCount > 0)
            {
                var messages = ShaderUtil.GetShaderMessages(shader);
                for (int i = 0; i < messages.Length; i++)
                {
                    if (messages[i].severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    {
                        return true;
                    }
                }
            }
            return false;
            #else
            return ShaderUtil.ShaderHasError(shader);
            #endif
        }

        #endregion

        #region Shader Message 구조체 (구버전 호환용)

        public struct ShaderMessage
        {
            public string message;
            public ShaderCompilerMessageSeverity severity;
            public int line;
            public string file;
        }

        public enum ShaderCompilerMessageSeverity
        {
            Error,
            Warning
        }

        #endregion
    }
}