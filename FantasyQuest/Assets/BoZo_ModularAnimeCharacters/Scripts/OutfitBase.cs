using System.Collections.Generic;
using UnityEngine;


namespace Bozo.AnimeCharacters
{ 
    public abstract class OutfitBase : MonoBehaviour
    {
        public bool customShader;
        protected Material material;

        private void Awake()
        {
            material = GetComponentInChildren<Renderer>().material;
        }

        public virtual void SetColor(Color color, int index, bool linkedChanged = false)
        {

        }

        public virtual void SetColor(Color color)
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            if (material.HasProperty("_Color_1"))
            {
                material.SetColor("_Color_1", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            else if (material.HasProperty("_MainColor"))
            {
                material.SetColor("_MainColor", color);
            }
        }

        public virtual List<Color> GetColors()
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            var colors = new List<Color>();

            if (customShader)
            {
                colors.Add(material.color);
                return colors;
            }

            for (int i = 1; i <= 9; i++)
            {
                colors.Add(material.GetColor("_Color_" + i));
            }

            return colors;
        }

        public virtual Color GetColor(int channel)
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }

            if (customShader)
            {
                if (material.HasProperty("_Color_1"))
                {
                    return material.GetColor("_Color_1");
                }
                else if (material.HasProperty("_Color"))
                {
                    return material.GetColor("_Color");
                }
                else if (material.HasProperty("_MainColor"))
                {
                    return material.GetColor("_MainColor");
                }
                else
                {
                    return Color.white;
                }
            }

            return material.GetColor("_Color_" + channel);
        }

        public virtual void SetSwatch(int swatchIndex, bool linkedChange = false)
        {

        }

        public virtual void SetDecal(Texture texture)
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            if(texture == null) material.SetTexture("_DecalMap", null);
            else material.SetTexture("_DecalMap", texture);
        }

        public virtual void SetDecalSize(Vector4 size)
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            material.SetVector("_DecalScale", size);
        }

        public virtual Vector4 GetDecalSize()
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            var v = material.GetVector("_DecalScale");

            return new Vector4(v.x, v.y , 0 ,0);
        }

        public virtual Texture GetDecal()
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }

            return material.GetTexture("_DecalMap");
        }

        public virtual void SetDecalColor(Color color, int index)
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            material.SetColor("_DecalColor_" + index, color);
        }

        public virtual Color GetDecalColor(int index)
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            return material.GetColor("_DecalColor_" + index);
        }

        public virtual List<Color> GetDecalColors()
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            var colors = new List<Color>();

            for (int i = 1; i < 4; i++)
            {
                colors.Add(material.GetColor("_DecalColor_" + i));
            }

            return colors;
        }

        public virtual void SetPattern(Texture texture)
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }

            material.SetTexture("_PatternMap", texture);
        }

        public virtual Texture GetPattern()
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            return material.GetTexture("_PatternMap");
        }

        public virtual void SetPatternColor(Color color, int index)
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            material.SetColor("_PatternColor_" + index, color);
        }

        public virtual Color GetPatternColor(int index) 
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            return material.GetColor("_PatternColor_" + index);
        }

        public virtual List<Color> GetPatternColors()
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            var colors = new List<Color>();

            for (int i = 1; i < 4; i++)
            {
                colors.Add(material.GetColor("_PatternColor_" + i));
            }

            return colors;
        }

        public virtual void SetPatternSize(Vector2 size)
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            material.SetVector("_PatternScale", size);
        }

        public virtual Vector4 GetPatternSize()
        {
            if (!material) { material = GetComponentInChildren<Renderer>().material; }
            var v = material.GetVector("_PatternScale");

            return new Vector4(v.x, v.y, 0 ,0);
        }

        public virtual void SetBaseTexture(Texture texture, Texture normalTexture = null)
        {

        }


    }
} 
