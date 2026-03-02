using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Bozo.AnimeCharacters
{
    public class BodyShapeSliders : MonoBehaviour 
    {
        private OutfitSystem system;
        private BodyShapeModifier bodyShape;
        [SerializeField] TMP_Text title;
        [SerializeField] Transform scaleContainer;
        [SerializeField] Slider scaleSlider;
        [SerializeField] Slider xScaleSlider;
        [SerializeField] Slider yScaleSlider;
        [SerializeField] Slider zScaleSlider;

        [SerializeField] Transform positionContainer;
        [SerializeField] Slider xPositionSlider;
        [SerializeField] Slider yPositionSlider;
        [SerializeField] Slider zPositionSlider;

        [SerializeField] Transform rotationContainer;
        [SerializeField] Slider rotationSlider;

        private void Start()
        {
            if(system != null && bodyShape != null)
            {
                Init(system, bodyShape);
            }
        }

        public void Init(OutfitSystem system, BodyShapeModifier mod) 
        {
            this.system = system;
            bodyShape = mod;
            title.text = mod.shapeName;

            if (bodyShape.useScale)
            {
                if (bodyShape.linkScaleAxis) 
                {
                    xScaleSlider.transform.parent.gameObject.SetActive(false);
                    yScaleSlider.transform.parent.gameObject.SetActive(false);
                    zScaleSlider.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    if (!bodyShape.useXScale) xScaleSlider.transform.parent.gameObject.SetActive(false);
                    if (!bodyShape.useYScale) yScaleSlider.transform.parent.gameObject.SetActive(false);
                    if (!bodyShape.useZScale) zScaleSlider.transform.parent.gameObject.SetActive(false);
                    scaleSlider.transform.parent.gameObject.SetActive(false);
                    scaleSlider.onValueChanged.AddListener(SetScale);
                }
                scaleSlider.onValueChanged.AddListener(SetScale);
                xScaleSlider.onValueChanged.AddListener(SetScale);
                yScaleSlider.onValueChanged.AddListener(SetScale);
                zScaleSlider.onValueChanged.AddListener(SetScale);

                scaleSlider.minValue = bodyShape.scaleRange.x; scaleSlider.maxValue = bodyShape.scaleRange.y;
                xScaleSlider.minValue = bodyShape.scaleRange.x; xScaleSlider.maxValue = bodyShape.scaleRange.y;
                yScaleSlider.minValue = bodyShape.scaleRange.x; yScaleSlider.maxValue = bodyShape.scaleRange.y;
                zScaleSlider.minValue = bodyShape.scaleRange.x; zScaleSlider.maxValue = bodyShape.scaleRange.y;
            }
            else 
            {
                scaleContainer.gameObject.SetActive(false);
            }

            if (bodyShape.usePosition)
            {
                if (!bodyShape.useXPos) xPositionSlider.transform.parent.gameObject.SetActive(false);
                if (!bodyShape.useYPos) yPositionSlider.transform.parent.gameObject.SetActive(false);
                if (!bodyShape.useZPos) zPositionSlider.transform.parent.gameObject.SetActive(false);

                xPositionSlider.minValue = bodyShape.posRange.x; xPositionSlider.maxValue = bodyShape.posRange.y;
                yPositionSlider.minValue = bodyShape.posRange.x; yPositionSlider.maxValue = bodyShape.posRange.y;
                zPositionSlider.minValue = bodyShape.posRange.x; zPositionSlider.maxValue = bodyShape.posRange.y;

                xPositionSlider.onValueChanged.AddListener(SetPosition);
                yPositionSlider.onValueChanged.AddListener(SetPosition);
                zPositionSlider.onValueChanged.AddListener(SetPosition);
            }
            else 
            {
                positionContainer.gameObject.SetActive(false);
            }

            if (bodyShape.useRotation) 
            {
                rotationSlider.onValueChanged.AddListener(SetRotation);
                rotationSlider.minValue = bodyShape.rotRange.x; rotationSlider.maxValue = bodyShape.rotRange.y;
            }
            else
            {
                rotationContainer.gameObject.SetActive(false);
            }
        }

        public void SetScale(float v) 
        {
            bodyShape.SetScale(xScaleSlider.value, yScaleSlider.value, zScaleSlider.value, scaleSlider.value);
        }

        public void SetPosition(float v)
        {
            bodyShape.SetPosition(xPositionSlider.value, yPositionSlider.value, zPositionSlider.value);
        }

        public void SetRotation(float v)
        {
            bodyShape.SetRotation(rotationSlider.value);
        }
    }

}
