using TMPro;
using UnityEngine;

public enum MeterRotationAxis
{
    X,
    Y,
    Z,
    NegativeX,
    NegativeY,
    NegativeZ
}

public class Lab6MeterView : MonoBehaviour
{
    public Transform needle;
    public TextMeshPro valueText;
    public float minValue;
    public float maxValue = 1f;
    public float minAngle = -120f;
    public float maxAngle = 120f;
    public MeterRotationAxis rotationAxis = MeterRotationAxis.Z;
    public string unit;
    [SerializeField] private bool debugMeterRotation;

    private bool hasValue;
    private float lastValue;
    private float lastMinValue;
    private float lastMaxValue;
    private float lastMinAngle;
    private float lastMaxAngle;
    private MeterRotationAxis lastRotationAxis;
    private string lastUnit;
    private Transform cachedNeedle;
    private bool hasInitialLocalRotation;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        CacheInitialRotation();
    }

    private void CacheInitialRotation()
    {
        cachedNeedle = needle;
        hasInitialLocalRotation = false;

        if (cachedNeedle != null)
        {
            initialLocalRotation = cachedNeedle.localRotation;
            hasInitialLocalRotation = true;
        }
    }

    public void SetValue(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            value = 0f;
        }

        value = Mathf.Clamp(value, Mathf.Min(minValue, maxValue), Mathf.Max(minValue, maxValue));
        if (hasValue && Mathf.Approximately(lastValue, value) && !HasSettingsChanged())
        {
            return;
        }

        if (needle != null)
        {
            if (!hasInitialLocalRotation || cachedNeedle != needle)
            {
                CacheInitialRotation();
            }

            float t = Mathf.Approximately(minValue, maxValue) ? 0f : Mathf.InverseLerp(minValue, maxValue, value);
            float angle = Mathf.Lerp(minAngle, maxAngle, Mathf.Clamp01(t));
            Vector3 axis = GetAxis(rotationAxis);
            needle.localRotation = initialLocalRotation * Quaternion.AngleAxis(angle, axis);

            if (debugMeterRotation)
            {
                Debug.Log($"{name}: value={value}, normalized={Mathf.Clamp01(t)}, angle={angle}, rotationAxis={rotationAxis}", this);
            }
        }

        if (valueText != null)
        {
            valueText.text = string.IsNullOrEmpty(unit)
                ? value.ToString("F1")
                : $"{value:F1} {unit}";
        }

        SaveLastState(value);
    }

    private bool HasSettingsChanged()
    {
        return !Mathf.Approximately(lastMinValue, minValue)
            || !Mathf.Approximately(lastMaxValue, maxValue)
            || !Mathf.Approximately(lastMinAngle, minAngle)
            || !Mathf.Approximately(lastMaxAngle, maxAngle)
            || lastRotationAxis != rotationAxis
            || lastUnit != unit
            || cachedNeedle != needle;
    }

    private void SaveLastState(float value)
    {
        hasValue = true;
        lastValue = value;
        lastMinValue = minValue;
        lastMaxValue = maxValue;
        lastMinAngle = minAngle;
        lastMaxAngle = maxAngle;
        lastRotationAxis = rotationAxis;
        lastUnit = unit;
    }

    private static Vector3 GetAxis(MeterRotationAxis axis)
    {
        switch (axis)
        {
            case MeterRotationAxis.X:
                return Vector3.right;
            case MeterRotationAxis.Y:
                return Vector3.up;
            case MeterRotationAxis.Z:
                return Vector3.forward;
            case MeterRotationAxis.NegativeX:
                return Vector3.left;
            case MeterRotationAxis.NegativeY:
                return Vector3.down;
            case MeterRotationAxis.NegativeZ:
                return Vector3.back;
            default:
                return Vector3.forward;
        }
    }
}
