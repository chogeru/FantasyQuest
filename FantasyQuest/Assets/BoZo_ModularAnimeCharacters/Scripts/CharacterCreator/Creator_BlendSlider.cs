using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace Bozo.AnimeCharacters
{
    public class BlendSlider : MonoBehaviour 
    {
        Slider slider;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text sliderValue;

        [SerializeField] OutfitSystem system;
        [SerializeField] string shape;

        public void Init(OutfitSystem system, string key) 
        {
            slider = GetComponentInChildren<Slider>();
            slider.onValueChanged.AddListener(Apply);

            this.system = system;
            title.text = key;
            shape = key;
            var weight = system.GetShapeValue(key);
            slider.value = weight;

        }

        private void OnEnable()
        {
            var weight = system.GetShapeValue(shape);
            slider.value = weight;
        }

        private void UpdateSlider()
        {

        }

        public void Apply(float value) 
        {
            system.SetShape(shape, value);
            sliderValue.text = $"{slider.value}%";
        }
    }
}
