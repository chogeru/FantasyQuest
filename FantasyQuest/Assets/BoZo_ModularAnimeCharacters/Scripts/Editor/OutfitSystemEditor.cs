using Bozo.AnimeCharacters;
using UnityEditor;
using UnityEngine;

namespace Bozo.ModularCharacters
{

    [CustomEditor(typeof(OutfitSystem))]
    public class OutfitSystemEditor : Editor
    {
        private bool showMergedOptions;
        private bool dependencies;
        private Texture2D banner;

        private void OnEnable()
        {
            banner = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BoZo_StylizedModularCharacters/Textures/Editor/Banner_OutfitSystem.png");
        }
        public override void OnInspectorGUI()
        {
            OutfitSystem system = (OutfitSystem)target;
            Color originalColor = GUI.color;


            GUIStyle frameStyle = new GUIStyle(GUI.skin.box);

            serializedObject.Update();

            if (banner != null)
            {
                float maxWidth = EditorGUIUtility.currentViewWidth - 20;
                float aspect = (float)banner.width / banner.height;
                float desiredWidth = Mathf.Min(banner.width, maxWidth);
                float height = desiredWidth / aspect;

                // Center it using flexible space
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(banner, GUILayout.Width(desiredWidth), GUILayout.Height(height));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal(frameStyle);
            GUILayout.BeginVertical();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("characterData"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("SaveID"));

            GUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("loadMode"), GUILayout.ExpandWidth(true));

            var async = serializedObject.FindProperty("async");
            EditorGUILayout.LabelField("Async", GUILayout.Width(50));
            async.boolValue = EditorGUILayout.Toggle(async.boolValue, GUILayout.Width(50));



            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Character ID"))
            {
                system.SaveByID();
            }
            if (GUILayout.Button("Load Character ID"))
            {
                system.LoadFromID();
            }

            GUILayout.EndHorizontal();



            GUILayout.EndVertical();

            if (system.characterData)
            {
                if (system.characterData.GetCharacterIcon() != null)
                {

                    // Control the size of the image
                    float size = 64;
                    Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawTextureTransparent(rect, system.characterData.GetCharacterIcon());
                }
            }
            GUILayout.EndHorizontal();



            GUIContent mergedTooltip = new GUIContent("(Merged Mode)", "");
            GUIContent dyanmicTooltip = new GUIContent("(Dynamic Mode)", "");
            if (system.mergedMode)
            {
                GUI.color = Color.green;
                if (GUILayout.Button(mergedTooltip, GUILayout.Height(30)))
                {
                    system.mergedMode = false;
                }
                GUI.color = originalColor;
            }
            else
            {
                GUI.color = Color.yellow;
                if (GUILayout.Button(dyanmicTooltip, GUILayout.Height(30)))
                {
                    system.mergedMode = true;
                }
                GUI.color = originalColor;
            }


            //MERGED OPTIONS
            showMergedOptions = EditorGUILayout.Foldout(showMergedOptions, "Merge Options", true);

            GUILayout.BeginVertical(frameStyle);
            if (showMergedOptions)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty("mergeMaterial"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabName"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("materialData"));

                GUILayout.BeginHorizontal(frameStyle);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mergeOnAwake"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoUpdate"));
                GUILayout.EndHorizontal();



                GUILayout.BeginHorizontal(frameStyle);

                if (GUILayout.Button("Merge Character"))
                {
                    system.MergeCharacter();
                }
                if (GUILayout.Button("Save as Prefab"))
                {
                    system.SaveCharacterToPrefab();
                }

                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            GUILayout.EndVertical();



            dependencies = EditorGUILayout.Foldout(dependencies, "Dependencies", true);
            if (dependencies)
            {
                GUILayout.BeginHorizontal(frameStyle);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("CharacterBody"));
                GUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
            //base.OnInspectorGUI();
        }


    }
}
