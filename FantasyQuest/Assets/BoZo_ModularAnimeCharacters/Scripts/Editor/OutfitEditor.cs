using UnityEditor;
using UnityEngine;

namespace Bozo.AnimeCharacters
{
    [CustomEditor(typeof(Outfit))]
    [CanEditMultipleObjects]
    public class OutfitEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Outfit outfit = (Outfit)target;
            Color originalColor = GUI.color;
            GUIStyle frameStyle = new GUIStyle(GUI.skin.box);

            serializedObject.Update();


            EditorGUILayout.Space(20);

            GUILayout.Label("Character Creator Settings");
            GUILayout.BeginVertical(frameStyle);
            GUILayout.BeginHorizontal(frameStyle);
            GUILayout.BeginVertical(frameStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OutfitName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OutfitIcon"));

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ColorChannels"));
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("TextureCatagory"));
            GUILayout.BeginHorizontal(frameStyle);

            var decalsupport = serializedObject.FindProperty("supportDecals");
            EditorGUILayout.LabelField("Supports Decal", GUILayout.Width(200));
            decalsupport.boolValue = EditorGUILayout.Toggle(decalsupport.boolValue);

            var patternsupport = serializedObject.FindProperty("supportPatterns");
            EditorGUILayout.LabelField("Supports Pattern", GUILayout.Width(200));
            patternsupport.boolValue = EditorGUILayout.Toggle(patternsupport.boolValue);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(frameStyle);
            EditorGUILayout.LabelField("Available In Character Creator", GUILayout.Width(200));

            EditorGUILayout.Space(20);

            var buttonText = "";
            if (outfit.showCharacterCreator)
            {
                buttonText = "(Available)";
            }
            else
            {
                GUI.color = Color.yellow;
                buttonText = "(Hidden)";
            }

            if (GUILayout.Button(buttonText))
            {
                outfit.showCharacterCreator = !outfit.showCharacterCreator;
            }

            GUI.color = originalColor;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if (outfit.OutfitIcon != null)
            {

                // Control the size of the image
                float size = 128;
                Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
                EditorGUI.DrawTextureTransparent(rect, outfit.OutfitIcon.texture);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            EditorGUILayout.Space(20);

            GUILayout.Label("Outfit Settings");
            GUILayout.BeginVertical(frameStyle);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AttachPoint"));

            GUILayout.BeginHorizontal(frameStyle);

            var customShader = serializedObject.FindProperty("customShader");
            EditorGUILayout.LabelField("Uses Custom Shader", GUILayout.Width(200));
            customShader.boolValue = EditorGUILayout.Toggle(customShader.boolValue);

            var attachEditMode = serializedObject.FindProperty("AttachInEditMode");
            EditorGUILayout.LabelField("Follow Skeleton In Edit Mode", GUILayout.Width(200));
            attachEditMode.boolValue = EditorGUILayout.Toggle(attachEditMode.boolValue);

            GUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultColors"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("optionalPieces"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("additionalBones"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("LinkedColorSets"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IncompatibleSets"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HideTypes"));
            EditorGUI.indentLevel--;

            GUILayout.EndVertical();

            EditorGUILayout.Space(20);

            GUILayout.Label("Custom Settings");
            GUILayout.BeginVertical(frameStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentSwatch"));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("outfitSwatches"));
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            //base.OnInspectorGUI();
        }
    }
}
