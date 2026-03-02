using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Bozo.AnimeCharacters
{
    public class OutfitSystem : MonoBehaviour
    {
        //[Header("Save Data")]

        public CharacterObject characterData;
        private CharacterObject _characterData;
        public string SaveID;


        //[Header("Dependencies")]
        [SerializeField] SkinnedMeshRenderer CharacterBody;

        //Height
        public bool muteHeightChange { get; private set; }
        public float height { get; private set; }
        public float heeledHeight { get; private set; }

        //Animation
        public Animator animator
        {
            get
            {
                if (_animator == null)
                {
                    _animator = GetComponentInParent<Animator>();
                    if (_animator == null) { _animator = GetComponentInChildren<Animator>(); }
                }

                return _animator;
            }
            private set
            { _animator = value; }
        }
        private Animator _animator;
        public float stance { get; private set; }

        //Dimensions
        private Bounds CharacterRenderBounds;
        public Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();

        //Outfits
        public Dictionary<OutfitType, Outfit> Outfits = new Dictionary<OutfitType, Outfit>();
        public Dictionary<string, OutfitType> KnownOutfitTypes = new Dictionary<string, OutfitType>();
        public Dictionary<OutfitType, List<Outfit>> hiddenTypes = new Dictionary<OutfitType, List<Outfit>>();

        //Shapes
        private Dictionary<string, int> bodyShapes = new Dictionary<string, int>();
        private Dictionary<string, int> faceShapes = new Dictionary<string, int>();
        private Dictionary<string, int> tagShapes = new Dictionary<string, int>();
        public Dictionary<string, BodyShapeModifier> bodyModifiers = new Dictionary<string, BodyShapeModifier>();
        private List<string> tags = new List<string>();


        //Events
        public UnityAction<Outfit> OnOutfitChanged;
        public UnityAction<SkinnedMeshRenderer> OnRigChanged;
        public UnityAction<string, float> OnShapeChanged;
        public UnityAction<List<string>> OnTagsChanged;

        public bool initalized { get; private set; }


        // Merged Properties
        public string prefabName;
        public Material mergeMaterial;
        public bool mergedMode;
        public bool mergeOnAwake;
        public bool autoUpdate;
        public bool mergeBase;
        public CharacterData data;
        private Dictionary<string, OutfitData> outfitData = new Dictionary<string, OutfitData>();
        public Dictionary<string, Texture2D> customMaps = new Dictionary<string, Texture2D>();
        public bool isDirty { get; private set; }
        public MergedMaterialData[] materialData;

        public enum LoadMode { OnStartAndOnValidate, OnStart, Manual }
        public LoadMode loadMode;

        public bool async;


#if MAGICACLOTH2
        //MagicaCloth
        private MagicaCloth2.ColliderComponent[] ClothColliders;
#endif


        private void OnValidate()
        {
            if (Application.isPlaying && gameObject.scene.isLoaded && loadMode == LoadMode.OnStartAndOnValidate)
            {
                Invoke("LoadFromObject", 0f);
            }
        }

        private void Awake()
        {
            Init();
            if (mergeOnAwake) mergedMode = true;
        }

        private void Start()
        {
            InitClothColliders();
            if (loadMode == LoadMode.OnStart || loadMode == LoadMode.OnStartAndOnValidate)
            {
                LoadFromObject();
            }
        }

        #region Initalizers

        public void Init()
        {

            if (initalized) return;

            if (CharacterBody == null)
            {
                Debug.LogWarning("Outfit System does not have a Rig assigned please assign one to prevent this warning", gameObject);
                Debug.LogWarning("Attempting auto rig assignment...");
                var skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var item in skinnedMeshes)
                {
                    if (item.name == "BMAC_Body")
                    {
                        CharacterBody = item;
                        Debug.Log("Rig Found Successfully!");
                        break;
                    }
                }
                Debug.LogError("Search Failed. Please Assign Mannually", gameObject);
                return;
            }

            CharacterRenderBounds = CharacterBody.localBounds;


            InitBoneMap();
            InitBodyShapes();
            InitBodyMods();


            initalized = true;
        }

        private void InitBoneMap()
        {
            boneMap.Clear();
            foreach (Transform bone in CharacterBody.bones)
            {
                if (boneMap.ContainsKey(bone.name) == false)
                {
                    boneMap.Add(bone.name, bone);
                }
            }
        }



        private void InitBodyShapes()
        {

            var body = GetOutfit("Body");


            bodyShapes.Clear();
            tagShapes.Clear();

            Mesh mesh;
            int blendShapeCount = 0;

            if (body != null)
            {

                mesh = body.skinnedRenderer.sharedMesh;
                blendShapeCount = body.skinnedRenderer.sharedMesh.blendShapeCount;
            }
            else
            {
                mesh = CharacterBody.sharedMesh;
                blendShapeCount = CharacterBody.sharedMesh.blendShapeCount;
            }



            for (int i = 0; i < blendShapeCount; i++)
            {
                var blendFullName = mesh.GetBlendShapeName(i);
                var blendName = mesh.GetBlendShapeName(i);

                //removing nameshape that maya gives
                var sort = blendName.Split(".");
                if (sort.Length > 1) { blendName = sort[1]; }


                sort = blendName.Split("_");
                if (sort.Length > 1)
                {
                    if (sort[0] == "Shape") { bodyShapes.Add(sort[1], i); }
                    if (sort[0] == "Tag") { tagShapes.Add(sort[1], i); }
                }
            }
        }



        private void InitBodyMods()
        {
            var bodyMods = new List<BodyShapeModifier>(GetComponentsInChildren<BodyShapeModifier>());
            bodyModifiers.Clear();
            for (int i = 0; i < bodyMods.Count; i++)
            {
                bodyModifiers.Add(bodyMods[i].name, bodyMods[i]);
            }
        }

        private void InitFaceShapes()
        {
            var head = GetOutfit("Head");

            faceShapes.Clear();

            Mesh mesh;
            if (head != null)
            {
                mesh = head.skinnedRenderer.sharedMesh;
            }
            else
            {
                mesh = CharacterBody.sharedMesh;
            }

            var blendShapeCount = mesh.blendShapeCount;

            for (int i = 0; i < blendShapeCount; i++)
            {
                var blendFullName = mesh.GetBlendShapeName(i);
                var blendName = mesh.GetBlendShapeName(i);

                //removing nameshape that maya gives
                var sort = blendName.Split(".");
                if (sort.Length > 1) { blendName = sort[1]; }

                sort = blendName.Split("_");
                if (sort.Length > 1)
                {
                    if (sort[0] == "Shape") { faceShapes.Add(sort[1], i); }
                }
            }
        }
        #endregion

        private void InitClothColliders()
        {
#if MAGICACLOTH2
            ClothColliders = GetComponentsInChildren<MagicaCloth2.ColliderComponent>();
#endif      
        }

        #region Saving and Loading



        public void LoadFromObject(CharacterObject saveData)
        {
            characterData = saveData;
            LoadFromObject();
        }

        [ContextMenu("Load")]
        public void LoadFromObject()
        {

            if (characterData)
            {
                if (_characterData != characterData)
                {
                    SaveID = characterData.name;

                    if (mergedMode)
                    {
                        data = characterData.GetCharacterData();
                        isDirty = true;
                        MergeCharacter();
                    }
                    else
                    {
                        _characterData = characterData;
                        LoadCharacter(characterData.GetCharacterData());
                    }
                }
            }
        }



        [ContextMenu("LoadByID")]
        public void LoadFromID()
        {
            LoadFromID(SaveID);
        }

        public void LoadFromID(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return;
            SaveID = saveName;

            var data = BMAC_SaveSystem.GetDataFromID(SaveID);
            if (data == null) return;

            LoadCharacter(data);
        }

        private async void LoadCharacter(CharacterData data)
        {
            if (mergedMode)
            {
                this.data = data;
                isDirty = true;
                MergeCharacter();
            }
            else
            {
                await BMAC_SaveSystem.LoadCharacter(this, data, false, async);
            }
        }

        [ContextMenu("SaveToObject")]
        public void SaveToObject()
        {
            if (!characterData)
            {
                Debug.LogWarning("Character Data Field is empty. Please provide a BSMC_CharacterObject to " + transform.name);
                return;
            }
            BMAC_SaveSystem.SaveCharacter(this, characterData.GetCharacterData().characterName, characterData.GetCharacterIcon());
        }

        [ContextMenu("SaveByID")]

        public void SaveByID()
        {
            SaveByID(SaveID);
        }

        public void SaveByID(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Debug.LogWarning("No ID provided saving aborted");
                return;
            }

            //Creating EmptyIcon
            if (!System.IO.File.Exists(BMAC_SaveSystem.iconFilePath + "/" + characterName + ".png"))
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                byte[] bytes = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes(BMAC_SaveSystem.iconFilePath + "/" + characterName + ".png", bytes);
            }

            BMAC_SaveSystem.SaveCharacter(this, characterName);
        }

        #endregion


        #region Outfit Removeal
        public void RemoveOutfit(Outfit outfit, bool destory)
        {


            if (Outfits.TryGetValue(outfit.Type, out Outfit currentOutfitInSlot))
            {
                if (destory == true && currentOutfitInSlot != null)
                {
                    currentOutfitInSlot.ReturnBones();
                    Destroy(currentOutfitInSlot.gameObject);
                    Outfits[outfit.Type] = null;
                }
            }
            RemoveHide(outfit);
            RemoveTags(outfit.tags);
            OnOutfitChanged?.Invoke(null);
        }


        public void RemoveOutfit(OutfitType type, bool destory)
        {
            if (Outfits.TryGetValue(type, out Outfit currentOutfitInSlot))
            {
                if (destory == true && currentOutfitInSlot != null)
                {
                    currentOutfitInSlot.ReturnBones();
                    Destroy(currentOutfitInSlot.gameObject);
                    Outfits[type] = null;
                }
            }

            if (currentOutfitInSlot)
            {
                RemoveHide(currentOutfitInSlot);
                RemoveTags(currentOutfitInSlot.tags);
            }
            OnOutfitChanged?.Invoke(null);
        }

        public void RemoveTags(string[] outfitTags)
        {
            foreach (var item in outfitTags)
            {
                tags.Remove(item);
            }
            OnTagsChanged?.Invoke(tags);
            //tags.RemoveAll(item => removedOutfit.tags.Contains(item));
        }

        public void RemoveAllOutfits()
        {
            List<Outfit> list = new List<Outfit>(Outfits.Values);
            foreach (var item in list)
            {
                if (item == null) continue;

                Destroy(item.gameObject);
            }
            Outfits.Clear();
            tags.Clear();
            hiddenTypes.Clear();
            OnOutfitChanged?.Invoke(null);
        }
        #endregion

        public Outfit InstantiateOutfit(Outfit outfit)
        {
            var inst = Instantiate(outfit, transform);
            inst.name = inst.name.Replace("(Clone)", "");
            return inst;
        }

        //Legacy Method
        public void AttachSkinnedOutfit(Outfit outfit)
        {
            AttachOutfit(outfit);
        }

        public void ReattachOutfit(Outfit outfit)
        {
            OnOutfitChanged?.Invoke(outfit);
            AddTags(outfit.tags);
            SetHide(outfit);
            ApplyTags();
            OnOutfitChanged?.Invoke(outfit);
        }

        public void AttachOutfit(Outfit outfit)
        {
            if (!initalized) return;

            if (mergedMode)
            {
                outfitData[outfit.Type.name] = outfit.GetOutfitData();
                data.outfitDatas = outfitData.Values.ToList();
                isDirty = true;
                Destroy(outfit.gameObject);
                if (autoUpdate) MergeCharacter();
                return;
            }

            if (!KnownOutfitTypes.ContainsKey(outfit.Type.name))
            {
                KnownOutfitTypes.Add(outfit.Type.name, outfit.Type);
            }


            //check if an outfit is already in that slot and replace it
            ReplaceOutfit(outfit);


            //Merging outfit bones or attaching outfit to specified bone
            MergeBones(outfit);


            //Adjusting Mesh bounds so the meshes don't unexpectingly disappear.
            if (outfit.skinnedRenderer)
            {
                UpdateCharacterBounds(outfit);
            }


            //Apply the Current Body Morphs to the Outfit
            ApplyShapesToOufit(outfit);

            //If Head get its Morphs
            if (outfit.Type.name == "Head") { InitFaceShapes(); }

            //If Body get its Morphs
            if (outfit.Type.name == "Body") { InitBodyShapes(); }

            SetHide(outfit);
            AddTags(outfit.tags);
            ApplyTags();

            if (hiddenTypes.ContainsKey(outfit.Type)) outfit.gameObject.SetActive(false);

            OnOutfitChanged?.Invoke(outfit);


        }

        private void ApplyShapesToOufit(Outfit outfit)
        {
            var keys = new List<string>(bodyShapes.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                GetBodyShapeValues();
                outfit.SetShape(keys[i], GetShape(keys[i]));
            }
        }

        public void SetShape(string key, float value)
        {
            SkinnedMeshRenderer renderer = null;
            var index = -1;

            var body = GetOutfit("Body");
            var head = GetOutfit("Head");

            if (bodyShapes.TryGetValue(key, out int bodyValue) && body)
            {
                index = bodyValue;
                if (body != null) { renderer = body.skinnedRenderer; }
            }
            else if (faceShapes.TryGetValue(key, out int faceValue) && head)
            {
                index = faceValue;
                if (head != null) { renderer = head.skinnedRenderer; }
            }
            else
            {
                if (bodyShapes.TryGetValue(key, out int blendValue))
                {
                    index = blendValue;
                    renderer = CharacterBody;
                }

            }

            if (renderer != null) renderer.SetBlendShapeWeight(index, value);

            OnShapeChanged?.Invoke(key, value);
        }

        public void AddTags(string[] tags)
        {
            this.tags.AddRange(tags);
            OnTagsChanged?.Invoke(this.tags);
        }

        public void SetHide(Outfit outfit)
        {
            if (outfit == null) return;
            foreach (var item in outfit.HideTypes)
            {
                if (hiddenTypes.ContainsKey(item))
                {
                    hiddenTypes[item].Add(outfit);
                }
                else
                {
                    hiddenTypes.Add(item, new List<Outfit>());
                    hiddenTypes[item].Add(outfit);
                }
                var hidden = GetOutfit(item);
                if (hidden) hidden.gameObject.SetActive(false);
            }

        }

        public void RemoveHide(Outfit outfit)
        {
            if (outfit == null) return;
            foreach (var item in outfit.HideTypes)
            {
                if (hiddenTypes.ContainsKey(item))
                {
                    hiddenTypes[item].Remove(outfit);
                    if (hiddenTypes[item].Count == 0)
                    {
                        hiddenTypes.Remove(item);
                        var hidden = GetOutfit(item);
                        if (hidden) hidden.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void ApplyTags()
        {
            // This method is intented for when you merged the body but still want to attach outfits dynamically
            if (GetOutfit("Body") != null) { return; }

            var shapes = new List<string>(tagShapes.Keys);
            if (!CharacterBody) return;
            if (CharacterBody.sharedMesh.blendShapeCount == 0) return;
            for (int i = 0; i < shapes.Count; i++)
            {
                var yes = ContainsTag(shapes[i]);
                if (yes) { CharacterBody.SetBlendShapeWeight(tagShapes[shapes[i]], 100); }
                else { CharacterBody.SetBlendShapeWeight(tagShapes[shapes[i]], 0); }
            }
        }

        public void SetStance(float value)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "Stance") animator.SetFloat("Stance", value);
            }
            stance = value;
        }

        public void SetHeight(float value)
        {
            //Check if has heels animaton property
            bool HasHeeledParamter = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "HeelHeight") HasHeeledParamter = true;
            }

            //remove Previous Height
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - height, transform.localPosition.z);
            //Apply New Height
            height = value;
            if (HasHeeledParamter) heeledHeight = animator.GetFloat("HeelHeight");
            if (!muteHeightChange)
            {
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + value, transform.localPosition.z);
                if (HasHeeledParamter)
                {

                    animator.SetFloat("HeelHeight", 0);
                }

            }


        }

        public void MuteHeightChange(bool value)
        {
            if (value == muteHeightChange) return;

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                animator.SetFloat("HeelHeight", heeledHeight);
            }

            muteHeightChange = value;

            var height = this.height;
            if (muteHeightChange)
            {
                height = -height;
            }

            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + height, transform.localPosition.z);
        }

        private void ReplaceOutfit(Outfit outfit)
        {
            if (Outfits.TryGetValue(outfit.Type, out Outfit currentOutfitInSlot))
            {
                if (Outfits[outfit.Type])
                {
                    if (outfit.transform != Outfits[outfit.Type].transform)
                    {
                        currentOutfitInSlot.ReturnBones();
                        Destroy(currentOutfitInSlot.gameObject);
                    }
                    else
                    {
                        OnOutfitChanged?.Invoke(outfit);

                    }
                }
                Outfits[outfit.Type] = outfit;
            }
            else
            {
                Outfits.Add(outfit.Type, outfit);
            }
        }

        private void MergeBones(Outfit outfit)
        {
            if (outfit.additionalBones.Length != 0)
            {
                for (int i = 0; i < outfit.additionalBones.Length; i++)
                {
                    var bone = outfit.additionalBones[i];
                    var newParent = GetBones()[bone.parent.name];

                    bone.parent.SetPositionAndRotation(newParent.position, newParent.rotation);

                    bone.parent = newParent;

                    foreach (Transform item in bone.GetComponentsInChildren<Transform>(true))
                    {
                        if (boneMap.ContainsKey(bone.name))
                        {
                            boneMap[item.name] = item;
                        }
                        else
                        {
                            boneMap.Add(item.name, item);
                        }
                    }
                }
            }

            foreach (var smr in outfit.skinnedRenderers)
            {
                var renderer = smr;

                if (outfit.AttachPoint == "" && renderer)
                {
                    if (outfit.Initalized == false)
                    {

                        var oldBones = renderer.bones.ToArray();
                        var newBones = new Transform[renderer.bones.Length];
                        for (int i = 0; i < oldBones.Length; i++)
                        {
                            var bone = oldBones[i];
                            boneMap.TryGetValue(bone.name, out Transform baseBone);
                            if (bone == baseBone)
                            {
                                newBones[i] = baseBone;
                                continue;
                            }
                            else
                            {
                                newBones[i] = baseBone;
                                //Destroy(bone.gameObject);
                            }
                        }
                        renderer.bones = newBones;
                        renderer.rootBone = CharacterBody.rootBone;


                    }
                }
                else
                {
                    Transform bone = null;
                    try
                    {
                        bone = boneMap[outfit.AttachPoint];
                    }
                    catch
                    {
                        Debug.LogError(name + " is missing " + outfit.AttachPoint + " that " + outfit.name + " requires");
                        return;
                    }


                    outfit.transform.parent = bone.transform;
                    outfit.transform.position = bone.position;
                    outfit.transform.rotation = bone.rotation;
                    outfit.transform.localScale = Vector3.one;
                }
            }

            outfit.ActivateCloth(boneMap);
            outfit.Initalized = true;

            if (outfit.outfitRenderer && outfit.AttachPoint != "")
            {
                Transform bone = null;
                try
                {
                    bone = boneMap[outfit.AttachPoint];
                }
                catch
                {
                    Debug.LogError(name + " is missing " + outfit.AttachPoint + " that " + outfit.name + " requires");
                    return;
                }


                outfit.transform.parent = bone.transform;
                outfit.transform.position = bone.position;
                outfit.transform.rotation = bone.rotation;
                outfit.transform.localScale = Vector3.one;
            }
        }

        public void UpdateCharacterBounds(Outfit outfit)
        {
            foreach (var item in Outfits.Values)
            {
                if (item == null) continue;
                foreach (var smr in item.skinnedRenderers)
                {
                    if (smr != null) smr.localBounds = CharacterRenderBounds;
                }
            }
        }

        public bool ContainsTag(string tag)
        {
            return tags.Contains(tag);
        }

        #region Getters

        public Outfit GetOutfit(OutfitType outfitType)
        {
            if (Outfits.TryGetValue(outfitType, out Outfit item))
            {
                return item;
            }

            return null;
        }

        public Outfit GetOutfit(string outfitType)
        {
            if (KnownOutfitTypes.TryGetValue(outfitType, out OutfitType type))
            {
                if (Outfits.TryGetValue(type, out Outfit item))
                {
                    return item;
                }
            }

            return null;
        }

        public List<Outfit> GetOutfits()
        {
            return new List<Outfit>(Outfits.Values);
        }

        public List<string> GetShapes()
        {
            return bodyShapes.Keys.ToList();
        }

        public List<string> GetFaceShapes()
        {
            return faceShapes.Keys.ToList();
        }

#if MAGICACLOTH2
        public MagicaCloth2.ColliderComponent[] GetClothColliders()
        {
            return ClothColliders;
        }
#endif

        public float GetShape(string key)
        {
            if (bodyShapes.TryGetValue(key, out int value))
            {
                var body = GetOutfit("Body");
                if (body != null)
                {
                    var weightValue = body.skinnedRenderer.GetBlendShapeWeight(value);
                    return weightValue;
                }
                else return -10000;
            }
            else return -10000;
        }

        public Dictionary<string, BodyShapeModifier> GetMods()
        {
            return bodyModifiers;
        }
        public Dictionary<string, Transform> GetBones()
        {
            return boneMap;
        }

        public float GetShapeValue(string key)
        {
            var weight = -1f;

            var body = GetOutfit("Body");

            if (body == null) return -1;
            if (bodyShapes.TryGetValue(key, out int bodyValue))
            {
                weight = body.skinnedRenderer.GetBlendShapeWeight(bodyValue);
            }
            else if (faceShapes.TryGetValue(key, out int faceValue))
            {
                var face = GetOutfit("Head");
                if (face == null) return -1;
                weight = face.skinnedRenderer.GetBlendShapeWeight(faceValue);
            }

            return weight;
        }

        public float GetShapeValue(int key)
        {
            SkinnedMeshRenderer renderer;
            var body = GetOutfit("Body");
            if (body == null) renderer = CharacterBody;
            else renderer = body.skinnedRenderer;

            var weightValue = renderer.GetBlendShapeWeight(key);
            return weightValue;
        }

        public Dictionary<string, float> GetBodyShapeValues()
        {
            var bodyShapeValues = new Dictionary<string, float>();
            var shapes = bodyShapes.Values.ToArray();
            var keys = bodyShapes.Keys.ToArray();

            SkinnedMeshRenderer renderer;
            var body = GetOutfit("Body");
            if (body == null) renderer = CharacterBody;
            else renderer = body.skinnedRenderer;

            for (int i = 0; i < shapes.Length; i++)
            {
                var weightValue = renderer.GetBlendShapeWeight(shapes[i]);
                bodyShapeValues.Add(keys[i], weightValue);
            }

            return bodyShapeValues;
        }

        public Dictionary<string, float> GetFaceShapeValues()
        {
            var faceShapeValues = new Dictionary<string, float>();
            var shapes = faceShapes.Values.ToArray();
            var keys = faceShapes.Keys.ToArray();

            SkinnedMeshRenderer renderer;
            var head = GetOutfit("Head");
            if (head == null) renderer = CharacterBody;
            else renderer = head.skinnedRenderer;


            for (int i = 0; i < shapes.Length; i++)
            {
                var weightValue = renderer.GetBlendShapeWeight(shapes[i]);
                faceShapeValues.Add(keys[i], weightValue);
            }

            return faceShapeValues;
        }
        #endregion

#if UNITY_EDITOR

        public void SoftAttach(Outfit outfit)
        {
            //For Attaching outfits during in the Editor 
            if (CharacterBody == null)
            {
                Debug.LogWarning("Soft Attach attempted but OuftitSystem did not have a CharacterBody please assign in the inspector", gameObject);
                return;
            }

            Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
            foreach (Transform bone in CharacterBody.bones)
            {
                if (boneMap.ContainsKey(bone.name) == false)
                {
                    boneMap.Add(bone.name, bone);
                }
            }

            var renderers = outfit.GetComponentsInChildren<SkinnedMeshRenderer>();


            //Already Attached
            if (outfit.originalBones.Length > 0)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].localBounds = CharacterBody.localBounds;

                if (outfit.AttachPoint == "" && renderers[i])
                {
                    if (outfit.Initalized == false)
                    {
                        outfit.originalBones = renderers[i].bones;
                        outfit.originalRootBone = renderers[i].rootBone;

                        var oldBones = renderers[i].bones.ToArray();
                        var newBones = new Transform[renderers[i].bones.Length];
                        for (int b = 0; b < oldBones.Length; b++)
                        {
                            var bone = oldBones[b];
                            boneMap.TryGetValue(bone.name, out newBones[b]);
                        }
                        renderers[i].bones = newBones;
                        renderers[i].rootBone = CharacterBody.rootBone;

                    }
                }
            }

        }
#endif

        public SkinnedMeshRenderer GetCharacterBody() { return CharacterBody; }
        public void SetCharacterBody(GameObject newBody)
        {
            var smr = newBody.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null) return;

            RemoveAllOutfits();
            DestroyImmediate(CharacterBody.transform.parent.gameObject);

            newBody.transform.parent = transform;
            newBody.transform.localPosition = Vector3.zero;
            newBody.transform.localRotation = Quaternion.identity;
            newBody.transform.localScale = Vector3.one;

            CharacterBody = smr;

            InitBoneMap();
            InitBodyShapes();
            InitBodyMods();
            InitClothColliders();

            OnRigChanged?.Invoke(CharacterBody);

            Invoke("RebindBody", 0);
        }

        public void RebindBody()
        {
            animator.Rebind();

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "HeelHeight") animator.SetFloat("HeelHeight", heeledHeight);
                if (param.name == "Stance") animator.SetFloat("Stance", stance);
            }
        }

        [ContextMenu("Merge")]
        public void MergeCharacter()
        {
            if (Application.isPlaying && gameObject.scene.isLoaded)
            {
                var optimizer = new BoZo_CharacterOptimizer();

                if (mergedMode && isDirty)
                {
                    optimizer.OptimizeCharacter(this, data);
                }
                else
                {
                    var preMergedData = BMAC_SaveSystem.GetCharacterData(this);
                    data = preMergedData;

                    foreach (var item in GetOutfits())
                    {
                        if (!item) { continue; }
                        var outfit = outfitData[item.Type.name] = item.GetOutfitData();

                    }


                    optimizer.OptimizeCharacter(this, preMergedData);
                    mergedMode = true;
                }
            }
            else
            {
                Debug.LogWarning("For stability reason Character Merging is only available in Play Mode");
            }



        }

        [ContextMenu("SaveToPrefab")]
        public void SaveCharacterToPrefab()
        {
#if UNITY_EDITOR
            if (Application.isPlaying && gameObject.scene.isLoaded)
            {
                var optimizer = new BoZo_CharacterOptimizer();

                if (mergedMode && isDirty)
                {
                    optimizer.SaveOptimizedCharacter(this, data);
                }
                else
                {
                    var preMergedData = BMAC_SaveSystem.GetCharacterData(this);
                    data = preMergedData;

                    foreach (var item in GetOutfits())
                    {
                        outfitData[item.Type.name] = item.GetOutfitData();
                    }

                    optimizer.SaveOptimizedCharacter(this, preMergedData);
                    mergedMode = true;
                }
            }
#endif
        }
    }
}
