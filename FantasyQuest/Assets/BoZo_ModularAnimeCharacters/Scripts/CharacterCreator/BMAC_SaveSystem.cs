using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;


namespace Bozo.AnimeCharacters
{


    public static class BMAC_SaveSystem
    {
        public static string filePath = Application.persistentDataPath + "/BoZo_ModularAnimeCharacters/CharacterSaves";
        public static string iconFilePath = Application.persistentDataPath + "/BoZo_ModularAnimeCharacters/CharacterSaves/Icons";
        public static string assetPath = "/BoZo_ModularAnimeCharacters/CharacterSaves/Resources/";
        public static string iconAssetPath = "/BoZo_ModularAnimeCharacters/CharacterSaves/Icons/";

        public static void SaveCharacter(OutfitSystem outfitSystem, string saveName, Texture2D icon = null)
        {
            var data = GetCharacterData(outfitSystem);

            data.characterName = saveName;


#if UNITY_EDITOR

            var CharacterSave = ScriptableObject.CreateInstance<CharacterObject>();
            CharacterSave.data = data;
            Debug.Log(icon);
            CharacterSave.icon = icon;
            AssetDatabase.CreateAsset(CharacterSave, "Assets/" + assetPath + "/" + saveName + ".asset");
            AssetDatabase.Refresh();
#endif

            string saveData = JsonUtility.ToJson(data);
            System.IO.File.WriteAllText(filePath + "/" + saveName + ".json", saveData);
            Debug.Log("Character Saved:" + filePath + "/" + saveName + ".json");
        }

        public static CharacterData GetCharacterData(OutfitSystem outfitSystem)
        {
            if (outfitSystem.mergedMode) return outfitSystem.data;

            var data = new CharacterData();

            //Saving BlendShapes
            var bodyShapeValues = outfitSystem.GetBodyShapeValues();
            data.bodyIDs = bodyShapeValues.Keys.ToList();
            data.bodyShapes = bodyShapeValues.Values.ToList();

            var faceShapeValues = outfitSystem.GetFaceShapeValues();
            data.faceIDs = faceShapeValues.Keys.ToList();
            data.faceShapes = faceShapeValues.Values.ToList();

            //Saving Body Mods
            var modData = new List<BodyModData>();
            var modKeys = outfitSystem.bodyModifiers.Keys.ToList();
            for (int i = 0; i < modKeys.Count; i++)
            {
                var mod = outfitSystem.bodyModifiers[modKeys[i]].GetData();
                modData.Add(mod);
            }
            data.bodyMods = modData;
            data.bodyModsKeys = modKeys;

            //Saving Outfits
            var outfits = outfitSystem.GetOutfits();
            var outfitDataList = new List<OutfitData>();

            for (int i = 0; i < outfits.Count; i++)
            {
                if (outfits[i] == null) continue;
                outfitDataList.Add(outfits[i].GetOutfitData());

            }

            data.stance = outfitSystem.stance;
            data.outfitDatas = outfitDataList;

            return data;
        }

        public static async Task LoadCharacter(OutfitSystem outfitSystem, CharacterData characterObject = null, bool manualShapeApply = false, bool async = false)
        {

            CharacterData loadData = characterObject;


            //Loading Outfits

            List<Outfit> outfits = new List<Outfit>();

            if (async)
            {
                outfits = await LoadOutfits(loadData.outfitDatas);
            }
            else
            {
                foreach (var item in loadData.outfitDatas)
                {
                    outfits.Add(Resources.Load<Outfit>(item.outfit));
                }

            }



            outfitSystem.RemoveAllOutfits();

            for (int i = 0; i < loadData.outfitDatas.Count; i++)
            {
                var outfitData = loadData.outfitDatas[i];
                var outfit = outfits[i];


                if (outfit == null)
                {
                    Debug.LogWarning("Outfit Path: " + outfitData.outfit + " returns null make sure Prefab is named correctly");
                    continue;
                }
                //Check If Outfit is the same

                var inst = outfitSystem.InstantiateOutfit(outfit);


                //inst.Attach();

                if (inst.customShader)
                {
                    inst.SetSwatch(outfitData.swatch);
                    inst.SetColor(outfitData.color);
                }
                else
                {
                    for (int c = 0; c < 9; c++)
                    {
                        if (outfitData.colors.Count > c)
                        {
                            inst.SetColor(outfitData.colors[c], c + 1);
                        }

                        if (c + 1 <= 3 && outfitData.decal != "")
                        {
                            inst.SetDecalColor(outfitData.decalColors[c], c + 1);
                        }
                        if (c + 1 <= 3 && outfitData.pattern != "")
                        {
                            inst.SetPatternColor(outfitData.patternColors[c], c + 1);
                        }
                    }

                    var decal = Resources.Load<Texture>(outfitData.decal);
                    inst.SetDecal(decal);
                    inst.SetDecalSize(outfitData.decalScale);

                    var pattern = Resources.Load<Texture>(outfitData.pattern);
                    inst.SetPattern(pattern);
                    inst.SetPatternSize(outfitData.patternScale);
                }

                try
                {
                    for (int v = 0; v < outfit.optionalPieces.Length; v++)
                    {
                        if (inst.optionalPieces[v] == null) continue;
                        inst.optionalPieces[v].SetActive(loadData.outfitDatas[i].partVisibility[v]);
                    }
                }
                catch
                {
                    Debug.LogError("Something Went Wrong");
                }

            }



            //Loading Body Morphs

            for (int i = 0; i < loadData.bodyIDs.Count; i++)
            {
                outfitSystem.SetShape(loadData.bodyIDs[i], loadData.bodyShapes[i]);
            }

            for (int i = 0; i < loadData.faceIDs.Count; i++)
            {
                outfitSystem.SetShape(loadData.faceIDs[i], loadData.faceShapes[i]);
            }

            if (!manualShapeApply) LoadBodyMods(outfitSystem, loadData);


            outfitSystem.animator.Rebind();
            outfitSystem.SetStance(loadData.stance);
        }

        public static void LoadBodyMods(OutfitSystem outfitSystem, CharacterData loadData)
        {
            for (int i = 0; i < loadData.bodyModsKeys.Count; i++)
            {
                outfitSystem.bodyModifiers[loadData.bodyModsKeys[i]].SetData(loadData.bodyMods[i]);
            }
        }

        public static CharacterData GetDataFromID(string saveName)
        {
            CharacterData loadData;
            Debug.Log("Attempted Load at: " + filePath + "/" + saveName + ".json");
            if (!System.IO.File.Exists(filePath + "/" + saveName + ".json"))
            {
                Debug.LogWarning($"Save ID: {saveName} does not exist. Make sure input matches an existing Save");
                return null;
            }
            else
            {
                string data = System.IO.File.ReadAllText(filePath + "/" + saveName + ".json");
                loadData = JsonUtility.FromJson<CharacterData>(data);
                return loadData;
            }
        }

        public static async Task<List<Outfit>> LoadOutfits(List<OutfitData> outfitDatas)
        {
            var loadTasks = outfitDatas.Select(data => LoadResourceAsync<Outfit>(data.outfit));
            return (await Task.WhenAll(loadTasks)).ToList();
        }

        public static async Task<T> LoadResourceAsync<T>(string path) where T : UnityEngine.Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            var tcs = new TaskCompletionSource<T>();

            request.completed += operation =>
            {
                if (request.asset == null)
                {
                    tcs.SetResult(null);
                }
                else if (request.asset is not T result)
                {
                    tcs.SetResult(null);
                }
                else
                {
                    tcs.SetResult(result);
                }
            };

            return await tcs.Task;
        }

        public static void DeleteCharacter(string characterName)
        {
            System.IO.File.Delete(filePath + "/" + characterName + ".json");
            System.IO.File.Delete(iconFilePath + "/" + characterName + ".png");
            System.IO.File.Delete("Assets/" + assetPath + "/" + characterName + ".asset");
            System.IO.File.Delete("Assets/" + assetPath + "/" + characterName + ".meta");
            System.IO.File.Delete("Assets/" + iconAssetPath + characterName + ".png");
            System.IO.File.Delete("Assets/" + iconAssetPath + characterName + ".meta");
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
    }

    [System.Serializable]
    public class CharacterData
    {
        public string characterName;
        public List<string> bodyIDs;
        public List<float> bodyShapes;
        public List<string> faceIDs;
        public List<float> faceShapes;
        public List<string> bodyModsKeys;
        public List<BodyModData> bodyMods;
        public List<OutfitData> outfitDatas;
        public OutfitData bodyData;
        public float stance;
    }

    [System.Serializable]
    public class OutfitData
    {
        public string outfit;
        public List<Color> colors;

        public string decal;
        public List<Color> decalColors;
        public Vector4 decalScale;

        public string pattern;
        public List<Color> patternColors;
        public Vector4 patternScale;

        public bool[] partVisibility;

        //Custom Shader Data
        public Color color = Color.white;
        public int swatch;

    }

}