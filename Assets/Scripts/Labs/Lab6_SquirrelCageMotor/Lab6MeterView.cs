using TMPro;
using UnityEngine;

public class Lab6MeterView : MonoBehaviour
{
    public Transform needle;
    public TextMeshPro valueText;
    public float minValue;
    public float maxValue = 1f;
    public float minAngle = -120f;
    public float maxAngle = 120f;
    public string unit;

    public void SetValue(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            value = 0f;
        }

        if (needle != null)
        {
            float t = Mathf.Approximately(minValue, maxValue) ? 0f : Mathf.InverseLerp(minValue, maxValue, value);
            float angle = Mathf.Lerp(minAngle, maxAngle, Mathf.Clamp01(t));
            needle.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (valueText != null)
        {
            valueText.text = string.IsNullOrEmpty(unit)
                ? value.ToString("F1")
                : $"{value:F1} {unit}";
        }
    }
}
