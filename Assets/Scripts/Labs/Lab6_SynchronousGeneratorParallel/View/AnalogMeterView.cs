using UnityEngine;

public class AnalogMeterView : MonoBehaviour
{
    public float minValue;
    public float maxValue = 1f;
    public float minAngle = -90f;
    public float maxAngle = 90f;
    public Transform needle;

    public void SetValue(float value)
    {
        if (needle == null)
        {
            return;
        }

        float t = Mathf.InverseLerp(minValue, maxValue, value);
        float angle = Mathf.Lerp(minAngle, maxAngle, t);
        needle.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
