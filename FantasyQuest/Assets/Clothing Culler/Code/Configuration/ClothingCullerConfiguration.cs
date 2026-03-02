using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Salvage.ClothingCuller.Configuration
{
    [CreateAssetMenu(fileName = nameof(ClothingCullerConfiguration), menuName = "Clothing Culler/" + nameof(ClothingCullerConfiguration))]
    public class ClothingCullerConfiguration : ScriptableObject
    {
        public bool IsModularClothingWorkflowEnabled => modularClothingWorkflow;

        #region SerializeFields

        [SerializeField]
        private bool modularClothingWorkflow;

#if UNITY_EDITOR

        [field: SerializeField]
        public List<OccludeeCategory> OccludeeCategories { get; private set; }

#endif
        #endregion

        #region Unity Messages

#if UNITY_EDITOR

        private bool isInitialized;
        private bool previousValue;

        private void OnValidate()
        {
            if (!isInitialized)
            {
                previousValue = modularClothingWorkflow;
                isInitialized = true;
                return;
            }

            if (modularClothingWorkflow == previousValue)
            {
                return;
            }

            foreach (OccludeeCategory occludeeCategory in OccludeeCategories)
            {
                foreach (OccludeeConfiguration occludee in occludeeCategory.Occludees)
                {
                    if (modularClothingWorkflow)
                    {
                        occludee.CreateSkinnedMeshData();
                    }
                    else
                    {
                        occludee.ClearSkinnedMeshData();
                    }

                    EditorUtility.SetDirty(occludee);
                }
            }

            previousValue = modularClothingWorkflow;
        }

#endif

        #endregion


        #region Private Methods



        #endregion

        #region Public Methods

#if UNITY_EDITOR

        public static ClothingCullerConfiguration Find()
        {
            string guid = AssetDatabase.FindAssets($"t:{typeof(ClothingCullerConfiguration)}").FirstOrDefault();

            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<ClothingCullerConfiguration>(AssetDatabase.GUIDToAssetPath(guid));
        }

#endif

        #endregion

    }
}
