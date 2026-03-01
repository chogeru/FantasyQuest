using UnityEngine;
using UnityEditor;

namespace ThornyDevtudio.RuntimeDebuggerToolkit
{

    public class URPToStandardConverter : EditorWindow
    {
        [MenuItem("Tools/Convert URP Materials to Standard")]
        public static void ConvertMaterials()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            int count = 0;

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat.shader.name.Contains("Universal Render Pipeline"))
                {
                    Debug.Log("Converting: " + mat.name);
                    Texture baseMap = mat.GetTexture("_BaseMap");
                    Color baseColor = mat.GetColor("_BaseColor");
                    float metallic = mat.GetFloat("_Metallic");
                    float smoothness = mat.GetFloat("_Smoothness");

                    mat.shader = Shader.Find("Standard");

                    mat.SetColor("_Color", baseColor);
                    mat.SetTexture("_MainTex", baseMap);
                    mat.SetFloat("_Metallic", metallic);
                    mat.SetFloat("_Glossiness", smoothness);

                    count++;
                }
            }

            Debug.Log($"Conversion done. {count} materials updated.");
        }
    }
}