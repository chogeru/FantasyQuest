using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace Project.Editor
{
    /// <summary>
    /// アセットの命名規則を自動チェックするバリデーター。
    /// （アイディア5番の具現化）
    /// </summary>
    public class NamingValidator : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                CheckNamingConvention(path);
            }
        }

        private static void CheckNamingConvention(string assetPath)
        {
            // _Project 以下のスクリプトを対象としてチェック
            if (assetPath.StartsWith("Assets/_Project/Scripts/") && assetPath.EndsWith(".cs"))
            {
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                
                // パスカルケース（大文字始まり）かどうかをチェックする簡単な正規表現
                if (!Regex.IsMatch(fileName, @"^[A-Z][a-zA-Z0-9]*$"))
                {
                    Debug.LogWarning($"[Project Linter Warning] 自作スクリプト ({fileName}) が「大文字始まり(パスカルケース)」ではありません。\nコーディング規約に従い、ファイル名を修正してください。 パス: {assetPath}");
                }
            }
        }
    }
}
