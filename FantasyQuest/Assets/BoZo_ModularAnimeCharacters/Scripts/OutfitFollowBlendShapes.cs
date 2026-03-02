using System;
using System.Collections.Generic;
using UnityEngine;


namespace Bozo.AnimeCharacters
{
    public class OutfitFollowBlendShapes : MonoBehaviour, IOutfitExtension
    {
        OutfitSystem system;
        SkinnedMeshRenderer mesh;
        SkinnedMeshRenderer followTarget;

        [SerializeField] OutfitType follow;
        [SerializeField] List<Vector2> shapes = new List<Vector2>();

        private void OnDestroy()
        {
            if (system == null) return;
            system.OnOutfitChanged -= OnNewSetUpHead;
        }

        private void Init()
        {

        }

        private void OnNewSetUpHead(Outfit outfit)
        {
            if (outfit == null) { return; }
            if (outfit.Type != follow) { return; }
            if (!outfit.skinnedRenderer) { return; }
            followTarget = outfit.skinnedRenderer;
            SetUp();
        }

        private void SetUp()
        {
            mesh = GetComponentInChildren<SkinnedMeshRenderer>();

            var characterShapeTitle = followTarget.sharedMesh.GetBlendShapeName(0);
            var sort = characterShapeTitle.Split(".");
            if (sort.Length > 1) { characterShapeTitle = sort[0] + "."; }
            else { characterShapeTitle = ""; }

            shapes.Clear();
            for (int i = 0; i < mesh.sharedMesh.blendShapeCount; i++)
            {
                var shapeName = mesh.sharedMesh.GetBlendShapeName(i);
                sort = shapeName.Split(".");
                if (sort.Length > 1) { shapeName = sort[1]; }

                var shapeIndex = followTarget.sharedMesh.GetBlendShapeIndex(characterShapeTitle + shapeName);

                if (shapeIndex != -1)
                {
                    //print(characterShapeTitle + " | " + shapeName + " | " + shapeIndex);
                    shapes.Add(new Vector2 (i,shapeIndex));
                }
            }
        }

        private void Update()
        {
            if (followTarget == null) return;
            for (int i = 0; i < shapes.Count; i++)
            {
                mesh.SetBlendShapeWeight((int)shapes[i].x, followTarget.GetBlendShapeWeight((int)shapes[i].y));
            }
        }

        public string GetID()
        {
            return "BlendShapeFollow";
        }

        public void Initalize(OutfitSystem outfitSystem, Outfit outfit)
        {
            system = outfitSystem;
            if (system == null) return;
            system.OnOutfitChanged += OnNewSetUpHead;
            mesh = outfit.skinnedRenderer;

            var followOutfit = system.GetOutfit(follow);
            if (followOutfit == null) { return; }
            if (followOutfit.skinnedRenderer == null) { return; }
            followTarget = followOutfit.skinnedRenderer;

            SetUp();

        }

        public void Execute(OutfitSystem outfitSystem, Outfit outfit)
        {

        }

        public object GetValue()
        {
            return null;
        }

        public Type GetValueType()
        {
            return null;
        }
    }
}
