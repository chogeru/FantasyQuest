using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Bozo.AnimeCharacters
{
    public class ColorPickerControl : MonoBehaviour
    {
        [Header("Picker Dependencies")]
        public CharacterCreator creator;
        public SVImageControl svContoller;
        [Header("Picker")]
        public float currentHue;
        public float currentSat;
        public float currentVal;
        public float currentColor;

        [SerializeField] private RawImage hueImage;
        [SerializeField] private RawImage satValImage;
        [SerializeField] private RawImage outputImage;

        [SerializeField] private Slider hueSlider;

        private Texture2D hueTexture;
        private Texture2D svTexture;
        private Texture2D outputTexture;

        public OutfitBase colorObject;
        public OutfitType outfitType;
        public Material colorMaterial;
        public int MaterialSlot;

        [Header("Editor")]

        [SerializeField] OutfitSystem outfitSystem;
        [SerializeField] TMP_Text objectName;
        [SerializeField] TMP_Text channelText;
        [SerializeField] Image Swatch;

        [SerializeField] TMP_Text CopyCatagoryText;
        public List<string> outfitTypes = new List<string>();
        private int copyIndex;

        [SerializeField] TextureType mode = TextureType.Base;
        [SerializeField] int currentChannel;
        [SerializeField] int maxChannel;
        [SerializeField] string[] channelNames;

        [Header("Decal Editor")]
        [SerializeField] GameObject decalContainer;
        [SerializeField] Slider DecalXSlider;
        [SerializeField] Slider DecalYSlider;
        private Vector2 outfitDefaultDecalSize;

        [Header("Pattern Editor")]
        [SerializeField] GameObject patternContainer;
        [SerializeField] Slider patternXSlider;
        [SerializeField] Slider patternYSlider;
        private Vector2 outfitDefaultPatternSize;

        [Header("Swatch Editor")]
        [SerializeField] GameObject swatchParentContainer;
        [SerializeField] Transform swatchContainer;
        [SerializeField] SwatchSelector swatchSelectorObject;
        private List<SwatchSelector> swatchSelectors = new List<SwatchSelector>();

        [Header("AdvancedEditor")]
        [SerializeField] TMP_Text hexValueTex;
        [SerializeField] List<Color> HeldColors = new List<Color>();
        [SerializeField] bool maintainColors;
        [SerializeField] Toggle maintainColorsToggle;
        [SerializeField] TMP_InputField inputR;
        [SerializeField] TMP_InputField inputG;
        [SerializeField] TMP_InputField inputB;

        [SerializeField] TMP_InputField inputH;
        [SerializeField] TMP_InputField inputS;
        [SerializeField] TMP_InputField inputV;
        private Dictionary<OutfitType, OutfitPickerSettings> outfitPickerSettings = new Dictionary<OutfitType, OutfitPickerSettings>();

        private void Awake()
        {
            CreateHueImage();
            CreateSVImage();
            CreateOutputImage();
            UpdateOutputImage();

            foreach (var type in creator.outfitTypes)
            {
                outfitTypes.Add(type.name);
            }
            CopyCatagoryText.text = outfitTypes[0];
            if (colorObject == null) gameObject.SetActive(false);
        }

        private void CreateHueImage()
        {
            hueTexture = new Texture2D(1, 16);
            hueTexture.wrapMode = TextureWrapMode.Clamp;
            hueTexture.name = "HueTexture";

            for (int i = 0; i < hueTexture.height; i++)
            {
                hueTexture.SetPixel(0, i, Color.HSVToRGB((float)i / hueTexture.height, 1, 1));
            }

            hueTexture.Apply();

            currentHue = 0;
            hueImage.texture = hueTexture;
        }

        private void CreateSVImage()
        {
            svTexture = new Texture2D(16, 16);
            svTexture.wrapMode = TextureWrapMode.Clamp;
            svTexture.name = "SVTexture";

            for (int y = 0; y < svTexture.height; y++)
            {
                for (int x = 0; x < svTexture.width; x++)
                {
                    svTexture.SetPixel(x, y, Color.HSVToRGB(currentHue, (float)x / svTexture.width, (float)y / svTexture.height));
                }
            }

            svTexture.Apply();

            currentSat = 0;
            currentVal = 0;
            satValImage.texture = svTexture;
        }

        private void CreateOutputImage()
        {
            outputTexture = new Texture2D(1, 16);
            outputTexture.wrapMode = TextureWrapMode.Clamp;
            outputTexture.name = "OutputTexture";

            Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);

            for (int i = 0; i < hueTexture.height; i++)
            {
                outputTexture.SetPixel(0, 1, currentColor);
            }

            outputTexture.Apply();

            currentHue = 0;
            outputImage.texture = outputTexture;
        }

        private void UpdateOutputImage()
        {
            Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);

            for (int i = 0; i < outputTexture.height; i++)
            {
                outputTexture.SetPixel(0, i, currentColor);
            }

            //Updating RBG Input
            inputR.text = ((int)(currentColor.r * 255f)).ToString();
            inputG.text = ((int)(currentColor.g * 255f)).ToString();
            inputB.text = ((int)(currentColor.b * 255f)).ToString();

            //Updating HSV Input
            inputH.text = (currentHue.ToString("F2"));
            inputS.text = (currentSat.ToString("F2"));
            inputV.text = (currentVal.ToString("F2"));

            //Updating Hex Input
            string hexRGB = ColorUtility.ToHtmlStringRGB(currentColor);
            hexValueTex.text = "#" + hexRGB;


            outputTexture.Apply();

            if (!colorObject) return;

            Swatch.color = currentColor;
            SetColor(currentColor, currentChannel);
            channelText.color = new Color(1 - currentVal, 1 - currentVal, 1 - currentVal, 1);

            svContoller.setPickerPosition(currentSat, currentVal);
        }

        public void SetSV(float S, float V)
        {
            currentSat = S;
            currentVal = V;
            UpdateOutputImage();
        }

        public void SetHSV(float H, float S, float V)
        {
            currentHue = H;
            currentSat = S;
            currentVal = V;
            UpdateOutputImage();
        }

        public void SetHSV()
        {
            float.TryParse(inputH.text, out currentHue);
            float.TryParse(inputS.text, out currentSat);
            float.TryParse(inputV.text, out currentVal);
            UpdateOutputImage();
        }

        public void SetRGB()
        {
            byte r = 0;
            byte.TryParse(inputR.text, out r);
            byte g = 0;
            byte.TryParse(inputG.text, out g);
            byte b = 0;
            byte.TryParse(inputB.text, out b);

            var color = new Color32(r, g, b, 255);
            Color.RGBToHSV(color, out float h, out float s, out float v);
            SetHSV(h, s, v);
        }


        public void UpdateSVImage()
        {
            currentHue = hueSlider.value;

            for (int y = 0; y < svTexture.height; y++)
            {
                for (int x = 0; x < svTexture.width; x++)
                {
                    svTexture.SetPixel(x, y, Color.HSVToRGB(currentHue, (float)x / svTexture.width, (float)y / svTexture.height));

                }
            }

            svTexture.Apply();
            UpdateOutputImage();
        }

        private void SetColor(Color color, int channel)
        {
            switch (mode)
            {
                case TextureType.Base:
                    colorObject.SetColor(color, channel);
                    break;
                case TextureType.Decal:
                    maxChannel = 3;
                    colorObject.SetDecalColor(color, channel);
                    break;
                case TextureType.Pattern:
                    maxChannel = 3;
                    colorObject.SetPatternColor(color, channel);
                    break;

            }
        }

        public void SetDecal(Texture texture, Color[] colors, Vector2 maxScale)
        {
            SwitchMode("Decal");
            colorObject.SetDecal(texture);
            for (int i = 0; i < colors.Length; i++) { SetColor(colors[i], i + 1); }



            if (maxScale.x < 0)
            {
                DecalXSlider.gameObject.SetActive(false);
                DecalXSlider.maxValue = 1;
                DecalXSlider.value = 1;
            }
            else
            {
                DecalXSlider.gameObject.SetActive(true);
                DecalXSlider.maxValue = maxScale.x;
                DecalXSlider.value = 1;
            }

            if (maxScale.y < 0)
            {
                DecalYSlider.gameObject.SetActive(false);
                DecalYSlider.maxValue = 1;
                DecalYSlider.value = 1;
            }
            else
            {
                DecalYSlider.gameObject.SetActive(true);
                DecalYSlider.maxValue = maxScale.y;
                DecalYSlider.value = 1;
            }

            SetDecalSize();
        }

        public void SetPattern(Texture texture, Color[] colors, Vector2 maxScale)
        {
            SwitchMode("Pattern");
            colorObject.SetPattern(texture);
            for (int i = 0; i < colors.Length; i++) { SetColor(colors[i], i + 1); }

            if (maxScale.x < 0)
            {
                patternXSlider.gameObject.SetActive(false);
                patternXSlider.maxValue = 1;
                patternXSlider.value = 1;
            }
            else
            {
                patternXSlider.gameObject.SetActive(true);
                patternXSlider.maxValue = maxScale.x;
                patternXSlider.value = 1;
            }

            if (maxScale.y < 0)
            {
                patternYSlider.gameObject.SetActive(false);
                patternYSlider.maxValue = 1;
                patternYSlider.value = 1;
            }
            else
            {
                patternYSlider.gameObject.SetActive(true);
                patternYSlider.maxValue = maxScale.y;
                patternYSlider.value = 1;
            }

            SetPatternSize();
        }

        public void ChangeSwatch(int value)
        {
            currentChannel += value;

            if (currentChannel > maxChannel) { currentChannel = 1; }
            if (currentChannel < 1) { currentChannel = maxChannel; };

            Color swatchColor = Color.black;

            switch (mode)
            {
                case TextureType.Base:
                    swatchColor = colorObject.GetColor(currentChannel);
                    break;
                case TextureType.Decal:
                    swatchColor = colorObject.GetDecalColor(currentChannel);
                    break;
                case TextureType.Pattern:
                    swatchColor = colorObject.GetPatternColor(currentChannel);
                    break;
                default:
                    break;
            }

            Swatch.color = swatchColor;

            SetChannelName();

            Color.RGBToHSV(swatchColor, out float h, out float s, out float v);
            hueSlider.value = h;
            SetHSV(h, s, v);
            UpdateSVImage();
        }

        public void ChangeObject(OutfitBase ob)
        {
            if (!ob) { RemoveObject(); return; }
            var colorOutfit = ob.GetComponent<Outfit>();

            //Keeping color of previous Outfit if turned on and type is the same
            if (maintainColors && !colorOutfit.customShader && colorObject != null)
            {
                HeldColors = colorObject.GetColors();
                if (colorOutfit.Type == outfitType)
                {
                    for (int i = 0; i < HeldColors.Count; i++)
                    {
                        colorOutfit.SetColor(HeldColors[i], i + 1);
                    }
                }
            }

            //initalizing values
            channelNames = new string[] { "Base" };
            outfitType = colorOutfit.Type;
            mode = TextureType.Base;
            colorObject = ob;

            //RememberSettings
            if (!outfitPickerSettings.ContainsKey(colorOutfit.Type))
            {
                var newSettings = new OutfitPickerSettings();
                outfitPickerSettings.Add(colorOutfit.Type, newSettings);

            }
            var settings = outfitPickerSettings[colorOutfit.Type];
            SetMaintainColors(settings.maintainColors);
            SetCopyIndex(settings.copyIndex);

            //Custom Shaders
            if (colorObject.customShader == true)
            {
                decalContainer.SetActive(false);
                patternContainer.SetActive(false);
                swatchParentContainer.SetActive(true);
                maxChannel = 1;

                foreach (var item in swatchSelectors)
                {
                    Destroy(item.gameObject);
                }
                swatchSelectors.Clear();
                for (int i = 0; i < colorOutfit.outfitSwatches.Count; i++)
                {
                    var swatch = Instantiate(swatchSelectorObject, swatchContainer);
                    swatch.Init(this, colorOutfit.outfitSwatches[i], i);
                    swatchSelectors.Add(swatch);
                }

            }
            //BoZo_Toon Shader
            else
            {
                var color = colorObject.GetColor(1);
                color.a = 1;
                currentChannel = 1;
                Swatch.color = color;
                decalContainer.SetActive(true);
                patternContainer.SetActive(true);
                channelNames = colorOutfit.ColorChannels;

                if (colorOutfit)
                {
                    maxChannel = colorOutfit.ColorChannels.Length;
                    if (!colorOutfit.supportDecals) decalContainer.SetActive(false);
                    if (!colorOutfit.supportPatterns) patternContainer.SetActive(false);
                    swatchParentContainer.SetActive(false);
                }
                else
                {
                    maxChannel = 9;
                }



                var decalSize = colorObject.GetDecalSize();
                DecalXSlider.value = decalSize.x;
                DecalYSlider.value = decalSize.y;

                var patternSize = colorObject.GetPatternSize();
                patternXSlider.value = patternSize.x;
                patternYSlider.value = patternSize.y;
            }

            SetChannelName();

            ChangeSwatch(0);
            objectName.text = colorObject.name.Replace("(Clone)", "");

            if (colorObject == null) gameObject.SetActive(false);
            else gameObject.SetActive(true);

        }

        public void SetDecalSize()
        {
            var scale = new Vector2(DecalXSlider.value, DecalYSlider.value);
            colorObject.SetDecalSize(scale);
        }

        public void SetPatternSize()
        {
            var scale = new Vector2(patternXSlider.value, patternYSlider.value);
            colorObject.SetPatternSize(scale);
        }

        public void RemoveObject()
        {
            colorObject = null;
            SetMaintainColors(false);
            gameObject.SetActive(false);
        }

        public void SetBaseTexture(int textureIndex)
        {
            var outfit = colorObject.GetComponent<Outfit>();
            outfit.SetSwatch(textureIndex);
        }

        public void SwitchMode(string mode)
        {
            var type = (TextureType)Enum.Parse(typeof(TextureType), mode);
            this.mode = type;

            switch (this.mode)
            {
                case TextureType.Base:
                    ChangeObject(colorObject);
                    break;
                case TextureType.Decal:
                    maxChannel = 3;
                    channelText.text = "Decal 1/" + maxChannel;

                    var decal = colorObject.GetDecalColor(1);
                    decal.a = 1;
                    currentChannel = 1;
                    Swatch.color = decal;
                    break;
                case TextureType.Pattern:
                    maxChannel = 3;
                    channelText.text = "Pattern 1/" + maxChannel;
                    var pattern = colorObject.GetPatternColor(1);
                    pattern.a = 1;
                    currentChannel = 1;
                    Swatch.color = pattern;
                    break;

            }
        }

        public void ChangeCopyIndex(int value)
        {
            copyIndex += value;
            if (copyIndex > outfitTypes.Count - 1)
            {
                copyIndex = 0;
            }
            else if (copyIndex < 0)
            {
                copyIndex = outfitTypes.Count - 1;
            }
            CopyCatagoryText.text = outfitTypes[copyIndex];
            if (outfitType != null)
            {
                outfitPickerSettings[outfitType].copyIndex = copyIndex;
            }
        }

        public void SetCopyIndex(int value)
        {
            copyIndex = value;
            CopyCatagoryText.text = outfitTypes[copyIndex];
            outfitPickerSettings[outfitType].copyIndex = copyIndex;
        }

        public void CopyColor(OutfitBase copyOutfit)
        {
            var from = colorObject;
            var to = copyOutfit;

            for (int i = 1; i < 9; i++)
            {
                to.SetColor(from.GetColor(i), i);
            }
        }

        public void CopyColor()
        {
            var from = colorObject;
            var to = outfitSystem.GetOutfit(CopyCatagoryText.text);

            if (to == null) return;

            for (int i = 1; i < 9; i++)
            {
                to.SetColor(from.GetColor(i), i);
            }
        }

        public void SetColorByHex(string hex)
        {
            if (hex[0].ToString() != "#")
            {
                hex = "#" + hex;
            }

            if (ColorUtility.TryParseHtmlString(hex, out Color hexColor))
            {
                print(hexColor);
                Color.RGBToHSV(hexColor, out float h, out float s, out float v);
                SetHSV(h, s, v);
            }
            else
            {
                Debug.LogWarning("Invalid hex string!");
            }
        }

        public void CopyHex()
        {
            GUIUtility.systemCopyBuffer = hexValueTex.text;
            print("Copied HEX to Clipboard: " + hexValueTex.text);
        }

        public void SetChannelName()
        {
            var channelName = "";

            switch (mode)
            {
                case TextureType.Base:
                    if (colorObject.customShader) break;
                    if (currentChannel - 1 < channelNames.Length)
                    {
                        channelName = channelNames[currentChannel - 1];
                    }
                    break;
                case TextureType.Decal:
                    channelName = "Decal";
                    break;
                case TextureType.Pattern:
                    channelName = "Pattern";
                    break;

            }

            channelText.text = channelName + " " + currentChannel + "/" + maxChannel;
        }

        public void SetMaintainColors(bool value)
        {
            if (value && colorObject != null)
            {
                HeldColors = colorObject.GetColors();
            }
            maintainColors = value;
            maintainColorsToggle.isOn = maintainColors;
            if (outfitType != null)
            {
                outfitPickerSettings[outfitType].maintainColors = maintainColors;
            }
        }

        private class OutfitPickerSettings
        {
            public bool maintainColors = false;
            public int copyIndex = 0;
        }
    }
}
