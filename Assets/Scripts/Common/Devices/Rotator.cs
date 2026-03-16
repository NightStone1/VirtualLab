using UnityEngine;

public class Rotator : MonoBehaviour
{
    [Header("Knob range")]
    public float startAngle = 150f;
    public float endAngle = -150f;

    [Tooltip("Если true — использовать длинную дугу, а не короткую")]
    public bool useLongArc = true;

    [Tooltip("Инвертировать направление вращения")]
    public bool invert = true;

    [Header("Local rotation base")]
    public Vector3 baseEuler = new Vector3(0f, 90f, -90f);

    [Header("Cursor angle offset")]
    public float angleOffset = -90f;

    [Header("Values")]
    public bool isLLR = true;

    [Range(0f, 100f)]
    public float value = 0f;

    [Range(0f, 250f)]
    public float llrValue = 0f;

    public event System.Action<float> OnValueChanged;

    private bool isDragging;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        ApplyFromCurrentValue();
    }

    private void Update()
    {
        if (!isDragging) return;
        UpdateFromMouse();
    }

    private void OnMouseDown()
    {
        if (cam == null) cam = Camera.main;
        isDragging = true;
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void UpdateFromMouse()
    {
        Vector3 knobScreen = cam.WorldToScreenPoint(transform.position);
        Vector3 mouse = Input.mousePosition;

        Vector2 dir = new Vector2(mouse.x - knobScreen.x, mouse.y - knobScreen.y);
        if (dir.sqrMagnitude < 16f) return;

        float mouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float angle = Normalize360(mouseAngle + angleOffset);

        float t = ProjectAngleToArc01(angle, startAngle, endAngle, useLongArc);

        // Инверсию убрали
        SetValue01(t);
        ApplyFromCurrentValue();
    }

    private void ApplyFromCurrentValue()
    {
        float t = GetValue01();

        float angle = AngleOnArc(startAngle, endAngle, t, useLongArc);
        SetKnobAngle(angle);
    }

    private float GetValue01()
    {
        return isLLR ? llrValue / 250f : value / 100f;
    }

    private void SetValue01(float t)
    {
        t = Mathf.Clamp01(t);

        if (isLLR)
        {
            float newValue = t * 250f;
            if (!Mathf.Approximately(newValue, llrValue))
            {
                llrValue = newValue;
                OnValueChanged?.Invoke(llrValue);
            }
        }
        else
        {
            float newValue = t * 100f;
            if (!Mathf.Approximately(newValue, value))
            {
                value = newValue;
                OnValueChanged?.Invoke(value);
            }
        }
    }

    private void SetKnobAngle(float angle)
    {
        transform.localRotation = Quaternion.Euler(
            baseEuler.x + angle,
            baseEuler.y,
            baseEuler.z
        );
    }

    private float AngleOnArc(float from, float to, float t, bool longArc)
    {
        from = Normalize360(from);
        to = Normalize360(to);

        float cw = ClockwiseDistance(from, to);
        float ccw = 360f - cw;

        float delta;

        if (longArc)
        {
            // идем по длинной дуге
            delta = (cw > ccw) ? -ccw : cw;
            if (Mathf.Abs(delta) < 180f)
                delta = (delta >= 0f) ? delta - 360f : delta + 360f;
        }
        else
        {
            // идем по короткой дуге
            delta = Mathf.DeltaAngle(from, to);
        }

        float result = from + delta * t;
        return Normalize180(result);
    }

    private float ProjectAngleToArc01(float angle, float from, float to, bool longArc)
    {
        from = Normalize360(from);
        to = Normalize360(to);
        angle = Normalize360(angle);

        float cw = ClockwiseDistance(from, to);
        float ccw = 360f - cw;

        float arcLength;
        bool clockwise;

        if (longArc)
        {
            clockwise = cw >= ccw;
            arcLength = Mathf.Max(cw, ccw);
        }
        else
        {
            clockwise = cw <= ccw;
            arcLength = Mathf.Min(cw, ccw);
        }

        float pos = clockwise
            ? ClockwiseDistance(from, angle)
            : CounterClockwiseDistance(from, angle);

        pos = Mathf.Clamp(pos, 0f, arcLength);

        return arcLength <= 0.0001f ? 0f : pos / arcLength;
    }

    private float ClockwiseDistance(float from, float to)
    {
        from = Normalize360(from);
        to = Normalize360(to);

        float d = to - from;
        if (d < 0f) d += 360f;
        return d;
    }

    private float CounterClockwiseDistance(float from, float to)
    {
        return 360f - ClockwiseDistance(from, to);
    }

    private float Normalize360(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    private float Normalize180(float angle)
    {
        angle = Normalize360(angle);
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}