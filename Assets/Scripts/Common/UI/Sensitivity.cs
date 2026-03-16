using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Sensitivity : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Slider sensSlider;
    [SerializeField] private float minSens = 50f;
    [SerializeField] private float maxSens = 500f;
    public TextMeshProUGUI text;

    private void OnEnable()
    {
        float currentSens = SettingsManager.Instance.sensY;

        sensSlider.value = Mathf.InverseLerp(minSens, maxSens, currentSens);

        float displayValue = Mathf.Lerp(1f, 10f, sensSlider.value);
        text.text = "Чувствительность камеры: " + displayValue.ToString("0.0");

    }

    public void OnSensChanged(float sliderValue)
    {
        float realSens = Mathf.Lerp(minSens, maxSens, sliderValue);
        SettingsManager.Instance.sensX = realSens;
        SettingsManager.Instance.sensY = realSens;
        SettingsManager.Instance.SaveSettings();

        float displayValue = Mathf.Lerp(1f, 10f, sliderValue);
        text.text = "Чувствительность камеры: " + displayValue.ToString("0.0");
    }
}
