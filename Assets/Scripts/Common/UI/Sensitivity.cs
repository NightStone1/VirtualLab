// Copyright (c) 2026 Бабичева Екатерина Анатольевна,
// Бибко Эдуард Александрович.
//
// Данный программный код разработан в рамках выпускной квалификационной работы
// "Виртуальный методический комплекс по дисциплине "Электрические машины"".
//
// Использование программного комплекса в учебном процессе АМТИ допускается
// в рамках подписанного акта о внедрении.
//
// Дальнейшее распространение, модификация, переработка, передача третьим лицам,
// публикация исходного кода, а также использование за пределами указанного
// внедрения допускаются только с письменного согласия авторов, если иное
// не предусмотрено отдельным соглашением.

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
