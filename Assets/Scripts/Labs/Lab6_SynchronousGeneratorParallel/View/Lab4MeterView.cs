using UnityEngine;

public class Lab4MeterView : MonoBehaviour
{
    public Meter meter;
    public Transform needle;

    [Header("Scale")]
    public float minValue;
    public float maxValue = 1f;
    public float minAngle = -49f;
    public float maxAngle = -131f;

    [Header("Rotation")]
    public Vector3 baseEuler = new Vector3(-180f, 0f, 0f);
    public Vector3 rotationAxis = Vector3.forward;
    public float smoothSpeed = 8f;

    private void Awake()
    {
        if (meter == null)
        {
            meter = GetComponent<Meter>();
        }

        if (needle == null)
        {
            needle = transform;
        }
    }

    private void Update()
    {
        if (meter == null || needle == null)
        {
            return;
        }

        float t = Mathf.InverseLerp(minValue, maxValue, meter.current);
        float angle = Mathf.Lerp(minAngle, maxAngle, Mathf.Clamp01(t));
        Quaternion targetRotation = Quaternion.Euler(baseEuler) * Quaternion.AngleAxis(angle, rotationAxis.normalized);

        if (smoothSpeed <= 0f)
        {
            needle.localRotation = targetRotation;
            return;
        }

        float step = Mathf.Clamp01(Time.deltaTime * smoothSpeed);
        needle.localRotation = Quaternion.Slerp(needle.localRotation, targetRotation, step);
    }
}
