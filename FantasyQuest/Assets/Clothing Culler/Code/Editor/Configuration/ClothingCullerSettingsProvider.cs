using UnityEditor;

namespace Salvage.ClothingCuller.Editor.Configuration
{
    public class ClothingCullerSettingsProvider : SettingsProvider
    {


        #region Constructor

        public ClothingCullerSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
        {

        }

        #endregion

        #region Override Methods

        public override void OnGUI(string searchContext)
        {
            EditorGUIUtility.labelWidth = 300f;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Painting scene", EditorStyles.boldLabel);
            OcclusionDataViewerConfiguration.PersistentValues.ViewMeshesAsSkinned = EditorGUILayout.Toggle("View meshes as skinned", OcclusionDataViewerConfiguration.PersistentValues.ViewMeshesAsSkinned);
            OcclusionDataViewerConfiguration.PersistentValues.DefaultPokeThroughRaycastDistance = EditorGUILayout.FloatField("Default poke-through raycast distance", OcclusionDataViewerConfiguration.PersistentValues.DefaultPokeThroughRaycastDistance);
            OcclusionDataViewerConfiguration.PersistentValues.DefaultAmountOfRaycastsPerVertex = EditorGUILayout.IntField("Default raycasts per vertex", OcclusionDataViewerConfiguration.PersistentValues.DefaultAmountOfRaycastsPerVertex);
        }

        #endregion

        #region Private Methods



        #endregion

        #region Public Methods

        [SettingsProvider]
        public static SettingsProvider CreateClothingCullerSettingsProvider()
        {
            return new ClothingCullerSettingsProvider("Preferences/Clothing Culler");
        }

        #endregion

    }
}
