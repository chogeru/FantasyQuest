using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bozo.AnimeCharacters
{

    [CustomEditor(typeof(TextureBaker))]
    public class TextureBakerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TextureBaker baker = (TextureBaker)target;

            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Export Outfit"))
            {
                baker.ExportOutfit();
            }

            if (GUILayout.Button("Create Swatch"))
            {
                baker.CreateSwatch();
            }
            GUILayout.EndHorizontal();
        }
    }
}
