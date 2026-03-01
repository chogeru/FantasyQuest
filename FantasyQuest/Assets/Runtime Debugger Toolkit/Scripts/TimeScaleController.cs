using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace ThornyDevtudio.RuntimeDebuggerToolkit
{

    public class TimeScaleController : MonoBehaviour
    {
        [SerializeField] private Slider timeScaleSlider;
        [SerializeField] private TextMeshProUGUI timeScaleLabel;

        float defaultMaxScale = 3f;

        private void Start()
        {
            if (timeScaleSlider != null)
            {
                timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
                timeScaleSlider.value = Time.timeScale;
            }
        }

        private void OnTimeScaleChanged(float value)
        {
            Time.timeScale = value;
            timeScaleLabel.text = $"Time Scale: {value:0.0#}";
        }
        public void DoubleSliderMax()
        {
            timeScaleSlider.maxValue *= 2f;

            timeScaleSlider.value = Mathf.Min(timeScaleSlider.value, timeScaleSlider.maxValue);
        }

        public void ResetTime()
        {
            Time.timeScale = 1f;
            timeScaleSlider.value = 1f;
            timeScaleSlider.maxValue = defaultMaxScale;
        }
    }
}

