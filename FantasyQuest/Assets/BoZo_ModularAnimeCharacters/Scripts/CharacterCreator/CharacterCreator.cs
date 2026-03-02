using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Collections;
using TMPro;
using System.Linq;


namespace Bozo.AnimeCharacters
{
    public class CharacterCreator : MonoBehaviour
    {
        [Header("Creator Dependencies")]
        public OutfitSystem character;
        public ColorPickerControl colorPickerControl;
        [SerializeField] CharacterSpinner Spinner;
        [SerializeField] Camera iconCamera;
        [SerializeField] RenderTexture iconTexture;
        public OutfitType[] outfitTypes;

        [Header("Outfit Dependencies")]
        private Dictionary<string, Outfit> OutfitDataBase = new Dictionary<string, Outfit>();
        [SerializeField] OutfitSelector outfitSelectorObject;
        private List<OutfitSelector> outfitSelectors = new List<OutfitSelector>();
        [SerializeField] Transform outfitContainer;

        [Header("Texture Dependencies")]
        [SerializeField] TextureSelector textureSelectorObject;
        private List<TextureSelector> textureSelectors = new List<TextureSelector>();
        [SerializeField] Transform decalContainer;
        [SerializeField] Transform patternContainer;

        [Header("BodyShape Dependencies")]
        [SerializeField] BlendSlider blendSliderObject;
        private List<BlendSlider> blendSliders = new List<BlendSlider>();
        private List<BlendSlider> faceBlendSliders = new List<BlendSlider>();

        [SerializeField] BodyShapeSliders modSliderObject;
        private List<BodyShapeSliders> ModSliders = new List<BodyShapeSliders>();

        [SerializeField] Transform bodyShapeContainer;
        [SerializeField] Transform bodyModContainer;
        [SerializeField] Transform faceShapeContainer;
        [SerializeField] Transform faceModContainer;

        [SerializeField] GameObject currentPage;
        private GameObject previousPage;

        private Dictionary<string, List<GameObject>> outfits = new Dictionary<string, List<GameObject>>();
        private List<TexturePackage> textures = new List<TexturePackage>();

        [Header("Save Dependencies")]
        [SerializeField] SaveSelector saveSelector;
        [SerializeField] Dictionary<string, SaveSelector> saveSlots = new Dictionary<string, SaveSelector>();
        [SerializeField] Transform saveContainer;
        [SerializeField] GameObject DeleteConfirmWindow;
        [SerializeField] TMP_Text loadedCharacterNameText;
        [SerializeField] TMP_Text DeleteCharacterNameText;
        [SerializeField] OutfitVisibilityToggler visibilityToggler;


        private OutfitType type;

        [Header("Save Options")]
        public TMP_InputField CharacterName;

        private void Awake()
        {
            outfits.Clear();
            OutfitDataBase.Clear();

            var ob = Resources.LoadAll<Outfit>("");
            var textureObjects = Resources.LoadAll<TexturePackage>("");
            foreach (var item in ob)
            {
                if (!item.showCharacterCreator) continue;

                if (OutfitDataBase.ContainsKey(item.name))
                {
                    Debug.LogWarning($"Outfit: {item.name} has already been added, you may have a duplicate outfit in your project");
                }
                else
                {
                    OutfitDataBase.Add(item.name, item.GetComponent<Outfit>());
                }

            }
            foreach (var item in textureObjects)
            {
                textures.Add(item.GetComponent<TexturePackage>());
            }

            GenerateOutfitSelection();
            GenerateTextureSelection();
        }

        private void OnEnable()
        {
            character.OnOutfitChanged += OnOutfitUpdate;
            character.OnRigChanged += OnRigUpdate;
        }

        private void OnDisable()
        {
            character.OnOutfitChanged -= OnOutfitUpdate;
            character.OnRigChanged -= OnRigUpdate;
        }

        public void Start()
        {
            GetBodyBlends();
            GetFaceBlends();
            GetBodyMods();
            SwitchCatagory("Top");

            UpdateCharacterSaves();

        }

        public void GenerateOutfitSelection()
        {
            var outfits = OutfitDataBase.Values.ToArray();
            foreach (var item in outfits)
            {
                var selector = Instantiate(outfitSelectorObject, outfitContainer);
                selector.Init(item, this);
                outfitSelectors.Add(selector);
            }
        }

        public void GenerateTextureSelection()
        {
            foreach (var item in textures)
            {
                Transform container = null;

                if (item.type == TextureType.Decal) container = decalContainer;
                if (item.type == TextureType.Pattern) container = patternContainer;

                var selector = Instantiate(textureSelectorObject, container);
                selector.Init(item, this);
                textureSelectors.Add(selector);
            }
        }

        public void GetBodyBlends()
        {
            for (int i = 0; i < blendSliders.Count; i++)
            {
                Destroy(blendSliders[i].gameObject);
            }
            blendSliders.Clear();

            var shapes = character.GetShapes();
            foreach (var item in shapes)
            {
                var blendSlider = Instantiate(blendSliderObject, bodyShapeContainer);
                blendSlider.Init(character, item);
                blendSliders.Add(blendSlider);
            }
        }

        public void GetFaceBlends()
        {
            for (int i = 0; i < faceBlendSliders.Count; i++)
            {
                Destroy(faceBlendSliders[i].gameObject);
            }
            faceBlendSliders.Clear();

            var shapes = character.GetFaceShapes();
            foreach (var item in shapes)
            {
                var blendSlider = Instantiate(blendSliderObject, faceShapeContainer);
                blendSlider.Init(character, item);
                faceBlendSliders.Add(blendSlider);
            }
        }

        public void GetBodyMods()
        {
            var mods = character.GetMods().Values.ToList();

            for (int i = 0; i < ModSliders.Count; i++)
            {
                Destroy(ModSliders[i].gameObject);
            }
            ModSliders.Clear();

            var container = bodyShapeContainer;
            foreach (var item in mods)
            {
                if (item.sorting == "Head") container = faceModContainer;
                else container = bodyModContainer;
                var blendSlider = Instantiate(modSliderObject, container);
                blendSlider.Init(character, item);
                ModSliders.Add(blendSlider);
            }
        }

        public void UpdateCharacterSaves()
        {
            var saves = saveSlots.Values.ToArray();
            foreach (var item in saves)
            {
                Destroy(item.gameObject);
            }
            saveSlots.Clear();

            if (!System.IO.Directory.Exists(BMAC_SaveSystem.filePath))
            {
                System.IO.Directory.CreateDirectory(BMAC_SaveSystem.filePath);
                System.IO.Directory.CreateDirectory(BMAC_SaveSystem.iconFilePath);
                print("Created Save JSON save Location At: " + BMAC_SaveSystem.filePath);
            }

            string path = BMAC_SaveSystem.filePath;
            string[] jsonFiles = System.IO.Directory.GetFiles(path, "*.json");
            string[] icons = System.IO.Directory.GetFiles(BMAC_SaveSystem.iconFilePath, "*.png");

            for (int i = 0; i < jsonFiles.Length; i++)
            {
                var json = System.IO.File.ReadAllText(jsonFiles[i]);
                var data = JsonUtility.FromJson<CharacterData>(json);
                var image = System.IO.File.ReadAllBytes(icons[i]);
                var texture = new Texture2D(2, 2);
                Sprite icon = null;
                if (texture.LoadImage(image))
                {
                    icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }

                var selector = Instantiate(saveSelector, saveContainer);
                selector.Init(data, icon, this);
                saveSlots.Add(data.characterName, selector);
            }

            var saveObjects = Resources.LoadAll<CharacterObject>("");

            for (int i = 0; i < saveObjects.Length; i++)
            {
                var ob = saveObjects[i];
                if (saveSlots.ContainsKey(ob.data.characterName))
                {
                    continue;
                }
                else
                {
                    var selector = Instantiate(saveSelector, saveContainer);
                    Sprite icon = null;
                    if (ob.icon != null)
                    {
                        icon = Sprite.Create(ob.icon, new Rect(0, 0, ob.icon.width, ob.icon.height), new Vector2(0.5f, 0.5f));
                    }
                    selector.Init(ob.data, icon, this);
                    saveSlots.Add(ob.data.characterName, selector);
                }
            }
        }
        public void OpenPage(GameObject page)
        {
            currentPage.SetActive(false);
            previousPage = currentPage;
            currentPage = page;
            currentPage.SetActive(true);
        }

        public void BackPage()
        {
            currentPage.SetActive(false);
            currentPage = previousPage;
            currentPage.SetActive(true);
            colorPickerControl.RemoveObject();
        }

        public void SetOutfit(Outfit outfit)
        {
            var inst = Instantiate(outfit, character.transform);
            SetColorPickerObject(inst);
            SwitchTextureCatagory(outfit.TextureCatagory);
            type = outfit.Type;

            // var type = (OutfitType)Enum.Parse(typeof(OutfitType), catagory);
        }

        public void OnOutfitUpdate(Outfit outfit)
        {
            visibilityToggler.Set(outfit);

            if (outfit == null) return;
            if (outfit.Type.name == "Head")
            {
                GetFaceBlends();
            }
            if (outfit.Type.name == "Body")
            {
                GetBodyBlends();
            }
        }

        public void OnRigUpdate(SkinnedMeshRenderer rig)
        {
            //GetBodyBlends();
            GetBodyMods();
        }

        public void SetOutfitDecal(Texture texture, Color[] colors = null)
        {
            if (!colorPickerControl.colorObject) return;
            colorPickerControl.colorObject.SetDecal(texture);
        }
        public void SetOutfitPattern(Texture texture, Color[] colors = null)
        {
            if (!colorPickerControl.colorObject) return;
            colorPickerControl.colorObject.SetPattern(texture);
        }

        public void RemoveOutfit()
        {
            if (type == null) return;
            character.RemoveOutfit(type, true);
            colorPickerControl.RemoveObject();
        }

        public void RemoveOutfitDecal()
        {
            if (!colorPickerControl.colorObject) return;
            colorPickerControl.colorObject.SetDecal(null);
        }

        public void RemoveOutfitPattern()
        {
            if (!colorPickerControl.colorObject) return;
            colorPickerControl.colorObject.SetPattern(null);
        }


        public void SwitchCatagory(string catagory)
        {
            foreach (var item in outfitSelectors)
            {
                item.SetVisable(catagory);
            }

            var outfit = character.GetOutfit(catagory);

            SetColorPickerObject(outfit);
            visibilityToggler.Set(outfit);

            if (outfit == null) return;

            SwitchTextureCatagory(outfit.TextureCatagory);
            type = outfit.Type;
        }

        public void SwitchTextureCatagory(string catagory)
        {
            if (catagory == "")
            {
                catagory = "Outfit";
            }

            foreach (var item in textureSelectors)
            {
                item.SetVisable(catagory);
            }
        }

        public void SetColorPickerObject(string type)
        {
            var outfit = character.GetOutfit(type);
            colorPickerControl.ChangeObject(outfit);
        }

        public void SetColorPickerObject(Outfit outfit)
        {
            colorPickerControl.ChangeObject(outfit);
        }

        public void ReplaceCharacter(OutfitSystem character)
        {
            Destroy(this.character.gameObject);
            this.character = character;
            Spinner.SetCharacter(character.transform);
        }

        public void GetCurrentCatagory()
        {
            if (type == null) return;
            SwitchCatagory(type.name);
        }

        public void CopyColor(string copyTo)
        {
            var copyOutfit = character.GetOutfit((OutfitType)Enum.Parse(typeof(OutfitType), copyTo));
            colorPickerControl.CopyColor(copyOutfit);
        }

        public void ToggleWalk(bool value)
        {
            character.animator.SetBool("isWalk", value);
        }

        public void SaveCharacter()
        {
            StartCoroutine(Save());
        }

        public Outfit GetOutfit(string outfitName)
        {
            return OutfitDataBase[outfitName];
        }

        [ContextMenu("Save")]
        private IEnumerator Save()
        {
            yield return new WaitForEndOfFrame();
            if (CharacterName.text.Length == 0)
            {
                Debug.LogWarning("Please enter in a name with at least one letter");
                yield break;
            }

            RenderTexture.active = iconTexture;
            Texture2D icon = new Texture2D(iconTexture.width, iconTexture.height, TextureFormat.RGBA32, false);
            Rect rect = new Rect(new Rect(0, 0, iconTexture.width, iconTexture.height));
            icon.ReadPixels(rect, 0, 0);
            icon.Apply();

            byte[] bytes = icon.EncodeToPNG();

            Texture2D characterIcon = null;

#if UNITY_EDITOR

            System.IO.File.WriteAllBytes(Application.dataPath + BMAC_SaveSystem.iconAssetPath + CharacterName.text + ".png", bytes);
            AssetDatabase.Refresh();

            characterIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + BMAC_SaveSystem.iconAssetPath + CharacterName.text + ".png");
            print(BMAC_SaveSystem.iconAssetPath + CharacterName.text + ".png");
            TextureImporter importer = AssetImporter.GetAtPath("Assets/" + BMAC_SaveSystem.iconAssetPath + CharacterName.text + ".png") as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
#endif

            if (!System.IO.Directory.Exists(BMAC_SaveSystem.iconFilePath))
            {
                System.IO.Directory.CreateDirectory(BMAC_SaveSystem.iconFilePath);
            }

            System.IO.File.WriteAllBytes(BMAC_SaveSystem.iconFilePath + "/" + CharacterName.text + ".png", bytes);
            BMAC_SaveSystem.SaveCharacter(character, CharacterName.text, characterIcon);

            UpdateCharacterSaves();


        }

        public void LoadCharacter(CharacterData data)
        {
            loadedCharacterNameText.text = data.characterName;
            BMAC_SaveSystem.LoadCharacter(character, data);
        }

        public void DeleteCharacter()
        {
            if (loadedCharacterNameText.text == "") return;
            DeleteCharacterNameText.text = "Delete: " + loadedCharacterNameText.text;
            DeleteConfirmWindow.SetActive(true);
        }

        public void ConfirmDelete()
        {
            BMAC_SaveSystem.DeleteCharacter(loadedCharacterNameText.text);
            loadedCharacterNameText.text = "";
            UpdateCharacterSaves();
        }

    }
}
