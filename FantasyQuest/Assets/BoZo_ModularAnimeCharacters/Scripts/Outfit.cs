using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;


namespace Bozo.AnimeCharacters
{
    public class Outfit : OutfitBase
    {
        public bool Initalized { get; set; }
        [SerializeField] bool AttachInEditMode;

        private OutfitSystem system;

        //[Header("Character Creator Settings")]
        public string OutfitName;
        public Sprite OutfitIcon;
        public string[] ColorChannels = new string[] { "Base" };
        public string TextureCatagory;
        public bool supportDecals;
        public bool supportPatterns;
        public bool showCharacterCreator = true;


        public SkinnedMeshRenderer skinnedRenderer { get; private set; }
        public SkinnedMeshRenderer[] skinnedRenderers { get; private set; }
        public Renderer outfitRenderer { get; private set; }

        //[field: Header("Outfit Settings")]
        [SerializeField] public OutfitType Type;
        public string AttachPoint;

        public Color[] defaultColors;

        public string[] tags;

        public GameObject[] optionalPieces;
        public Transform[] additionalBones;

        private Dictionary<string, int> tagShapes = new Dictionary<string, int>();
        private Dictionary<string, int> shapes = new Dictionary<string, int>();
        public LinkedColorSets[] LinkedColorSets;
        public OutfitType[] IncompatibleSets;
        public OutfitType[] HideTypes;

        //[Header("User Settings")]
        public int currentSwatch;
        public List<OutfitSwatch> outfitSwatches = new List<OutfitSwatch>();

        public Transform[] originalBones;
        public Transform originalRootBone;
        public Transform editorAttachPoint;


        private void OnValidate()
        {
            if (Application.isPlaying && gameObject.scene.isLoaded)
            {

                if (system == null) system = GetComponentInParent<OutfitSystem>();
                SetColorInital();
            }
#if UNITY_EDITOR
            if (AttachInEditMode && !Application.isPlaying) SoftAttach();
#endif
        }

        private void Awake()
        {

            skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedRenderer) material = skinnedRenderer.material;
            skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            outfitRenderer = GetComponentInChildren<Renderer>();

            foreach (var smr in skinnedRenderers)
            {
                smr.sharedMaterial = material;
            }

            InitSetUpShapes();
        }

        private void OnEnable()
        {
            Attach();

        }

        private void OnDisable()
        {

            if (!system) return;

            system.OnOutfitChanged -= OnOutfitUpdate;
            system.OnShapeChanged -= SetShape;
            system.RemoveOutfit(this, false);

        }

        private void OnDestroy()
        {
            if (!system) return;
            system.OnOutfitChanged -= OnOutfitUpdate;

        }

        private void Start()
        {
            if (Application.isPlaying && gameObject.scene.isLoaded)
            {
                if (!Initalized) Attach();
            }
            SetColorInital();
        }

        public void Attach(Transform parent)
        {
            transform.parent = parent;
            Attach();
        }

        public void Attach(OutfitSystem system)
        {
            transform.parent = system.transform;
            Attach();
        }

        public void Attach()
        {

            system = GetComponentInParent<OutfitSystem>();
            skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            outfitRenderer = GetComponentInChildren<Renderer>();
            if (system == null) return;
            if (!system.initalized) return;


            RemoveIncompatible();
            CheckTags();
            CopySystemShapes();

            system.OnOutfitChanged += OnOutfitUpdate;
            system.OnShapeChanged += SetShape;

            if (Initalized) { system.ReattachOutfit(this); return; }


            var extensions = GetComponentsInChildren<IOutfitExtension>();
            foreach (var item in extensions) { item.Initalize(system, this); }


            //Reassigning original bones incase it was attached in editor
            if (originalBones.Length > 0 && skinnedRenderer)
            {
                skinnedRenderer.bones = originalBones;
                skinnedRenderer.rootBone = originalRootBone;
            }

            system.AttachOutfit(this);




            foreach (var item in extensions) { item.Execute(system, this); }
        }

        public void ReturnBones()
        {
            foreach (var item in additionalBones)
            {
                if (item == null) continue;
                item.parent = transform;
            }
        }

        #region OnUpdate Methods

        private void OnOutfitUpdate(Outfit newOutfit)
        {
            if (RemoveIfIncompatible(newOutfit)) return;
            CheckTags();

        }
        private void CheckTags()
        {
            var shapes = new List<string>(tagShapes.Keys);
            foreach (var smr in skinnedRenderers)
            {
                for (int i = 0; i < shapes.Count; i++)
                {
                    var yes = system.ContainsTag(shapes[i]);
                    if (yes) { smr.SetBlendShapeWeight(tagShapes[shapes[i]], 100); }
                    else { smr.SetBlendShapeWeight(tagShapes[shapes[i]], 0); }
                }
            }
        }

        #endregion

        #region Shapes Methods
        private void CopySystemShapes()
        {
            if (system)
            {
                var shapesKeys = shapes.Keys.ToArray();



                for (int i = 0; i < shapesKeys.Length; i++)
                {
                    var systemValue = system.GetShape(shapesKeys[i]);
                    if (systemValue == -10000) continue;

                    SetShape(shapesKeys[i], systemValue);
                }
            }
        }

        private void InitSetUpShapes()
        {
            if (!skinnedRenderer) return;
            var mesh = skinnedRenderer.sharedMesh;
            var blendShapeCount = skinnedRenderer.sharedMesh.blendShapeCount;

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
                    if (sort[0] == "Tag") { tagShapes.Add(sort[1], i); }
                    if (sort[0] == "Shape")
                    {
                        shapes.Add(sort[1], i);
                    }
                }
            }
        }

        public void SetShape(string key, float value)
        {
            if (value <= -10000) return;
            if (!skinnedRenderer) return;
            var sort = key.Split(".");
            if (sort.Length > 1) { key = sort[1]; }

            if (!shapes.TryGetValue(key, out int index)) { return; }


            foreach (var smr in skinnedRenderers)
            {

                if (smr.sharedMesh.blendShapeCount <= index) continue;
                smr.SetBlendShapeWeight(index, value);
            }
        }

        #endregion

        #region Incompatible Outfit Methods

        private void RemoveIncompatible()
        {
            foreach (var item in IncompatibleSets)
            {
                system.RemoveOutfit(item, true);
            }
        }

        private bool RemoveIfIncompatible(Outfit outfit)
        {
            if (outfit == null) return false;
            foreach (var item in IncompatibleSets)
            {
                if (outfit.Type == item)
                {
                    system.RemoveOutfit(this, true);
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Color Methods

        private void SetColorInital()
        {
            if (!outfitRenderer) return;
            var mat = outfitRenderer.material;

            for (int i = 0; i < defaultColors.Length; i++)
            {
                mat.SetColor("_Color_" + (1 + i), defaultColors[i]);
            }
        }

        public override void SetColor(Color color, int index, bool linkedChanged = false)
        {
            if (system == null) { system = GetComponentInParent<OutfitSystem>(); }
            if (outfitRenderer == null) { outfitRenderer = GetComponentInChildren<Renderer>(); }

            if (customShader)
            {
                SetColor(color);
            }
            else
            {
                outfitRenderer.material.SetColor("_Color_" + index, color);
            }

            foreach (var item in LinkedColorSets)
            {
                if (!linkedChanged)
                {
                    var linkedOutfit = system.GetOutfit(item.linkedType);
                    if (linkedOutfit == null) continue;
                    if (index > item.linkedChannelRange) continue;
                    linkedOutfit.SetColor(color, index, true);
                }
            }
        }



        public override void SetSwatch(int swatchIndex, bool linkedChanged = false)
        {
            if (!customShader) return;
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            if (swatchIndex + 1 > outfitSwatches.Count) return;
            var swatchID = outfitSwatches[swatchIndex].swatchID;
            var tex = Resources.Load<Texture>(swatchID);
            material.mainTexture = tex;
            currentSwatch = swatchIndex;

            foreach (var item in LinkedColorSets)
            {
                if (!linkedChanged)
                {
                    var linkedOutfit = system.GetOutfit(item.linkedType);
                    if (linkedOutfit == null) continue;
                    linkedOutfit.SetSwatch(swatchIndex, true);
                }
            }
        }

        public OutfitData GetOutfitData()
        {
            var outfitData = new OutfitData();

            var path = Type.name + "/" + name;
            path = path.Replace("(Clone)", "");

            outfitData.outfit = path;

            if (customShader)
            {
                outfitData.color = GetColor(1);
                outfitData.swatch = currentSwatch;
            }
            else
            {
                outfitData.colors = GetColors();

                var decal = GetDecal();

                if (decal != null)
                {
                    outfitData.decal = "Decal/" + decal.name;
                    outfitData.decalColors = GetDecalColors();
                    outfitData.decalScale = GetDecalSize();
                }
                else
                {
                    outfitData.decal = "";
                }

                var pattern = GetPattern();
                if (pattern != null)
                {
                    outfitData.pattern = "Pattern/" + pattern.name;
                    outfitData.patternColors = GetPatternColors();
                    outfitData.patternScale = GetPatternSize();
                }
                else
                {
                    outfitData.pattern = "";
                }

            }

            var vis = new bool[optionalPieces.Length];
            for (int i = 0; i < optionalPieces.Length; i++)
            {
                if (optionalPieces[i] == null) continue;
                vis[i] = optionalPieces[i].activeSelf;
            }
            outfitData.partVisibility = vis;

            return outfitData;
        }

        #endregion

        #region Soft Attach
#if UNITY_EDITOR
        public void SoftAttach()
        {
            var system = GetComponentInParent<OutfitSystem>();
            if (system == null)
            {
                //return bones if no longer attach to system
                SoftDetach();
                return;
            }
            system.SoftAttach(this);
        }

        public void SoftDetach()
        {
            editorAttachPoint = null;
            if (originalBones.Length > 0)
            {
                var skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedRenderer == null) return;
                skinnedRenderer.bones = originalBones;
                skinnedRenderer.rootBone = originalRootBone;
                originalBones = new Transform[0];
                originalRootBone = null;
            }
        }
#endif
        #endregion

        #region Magicka Cloth2
        private void InitCloth()
        {

            /*
#if MAGICACLOTH2
            var cloth = GetComponentInChildren<MagicaCloth2.MagicaCloth>();
            if (!cloth) return;
            cloth.enabled = false;
            cloth.Initialize();
            cloth.DisableAutoBuild();
#endif
            */
        }

        public void ActivateCloth(Dictionary<string, Transform> boneMap)
        {

            /*
#if MAGICACLOTH2
var cloth = GetComponentInChildren<MagicaCloth2.MagicaCloth>();
if (!cloth) return;
cloth.ReplaceTransform(boneMap);

var col = system.GetClothColliders();
List<MagicaCloth2.ColliderComponent> ClothColliders = col
.Where(m => m != null)
.Select(m => m.GetComponent<MagicaCloth2.ColliderComponent>())
.ToList();

cloth.SerializeData.colliderCollisionConstraint.colliderList = ClothColliders;

cloth.enabled = true;
#endif
           */
        }
        #endregion

        #region Utility Scripts
        [ContextMenu("QuickName")]
        private void QuickName()
        {
            int underscoreIndex = name.IndexOf('_');
            string trimmed = underscoreIndex >= 0
                ? name.Substring(underscoreIndex + 1)
                : name;
            string spaced = Regex.Replace(trimmed, "(?<!^)([A-Z])", " $1");

            OutfitName = spaced;
        }
        #endregion

    }

    [System.Serializable]
    public class OutfitSwatch
    {
        public string swatchID;
        public Color IconColorTop = Color.white;
        public Color IconColorBottom = Color.black;
    }

    [System.Serializable]
    public class LinkedColorSets
    {
        public OutfitType linkedType;
        [Range(1, 9)] public int linkedChannelRange;
    }

}







