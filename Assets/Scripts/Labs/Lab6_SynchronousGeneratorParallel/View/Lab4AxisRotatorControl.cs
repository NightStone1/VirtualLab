using UnityEngine;

public enum Lab4RotatorTarget
{
    DriveRegulator,
    R1Excitation
}

public enum Lab4RotationAxis
{
    X,
    Y,
    Z
}

public class Lab4AxisRotatorControl : MonoBehaviour
{
    [Header("Lab4 target")]
    public Lab4RotatorTarget target;
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;

    [Header("Knob range")]
    public float startAngle = 150f;
    public float endAngle = -150f;

    [Tooltip("Use true to rotate through the longer arc instead of the shorter one")]
    public bool useLongArc = true;

    [Tooltip("Invert value direction")]
    public bool invert = true;

    [Header("Rotation axis")]
    public Lab4RotationAxis axis = Lab4RotationAxis.X;

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

    private bool isDragging;
    private Camera cam;
    private bool controllerWarningShown;

    private void Start()
    {
        cam = Camera.main;
        ResolveController();
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

        SetValue01(t);
        ApplyFromCurrentValue();
    }

    private void ApplyFromCurrentValue()
    {
        float t = GetValue01();

        float angle = AngleOnArc(startAngle, endAngle, t, useLongArc);
        SetKnobAngle(angle);
    }

    public void SetValueFromController(float normalizedValue)
    {
        float t = Mathf.Clamp01(normalizedValue);

        if (isLLR)
        {
            llrValue = t * 250f;
        }
        else
        {
            value = t * 100f;
        }

        ApplyFromCurrentValue();
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
                ApplyValueToController();
            }
        }
        else
        {
            float newValue = t * 100f;
            if (!Mathf.Approximately(newValue, value))
            {
                value = newValue;
                ApplyValueToController();
            }
        }
    }

    private void SetKnobAngle(float angle)
    {
        Vector3 euler = baseEuler;
        switch (axis)
        {
            case Lab4RotationAxis.X:
                euler.x += angle;
                break;
            case Lab4RotationAxis.Y:
                euler.y += angle;
                break;
            case Lab4RotationAxis.Z:
                euler.z += angle;
                break;
        }

        transform.localRotation = Quaternion.Euler(euler);
    }

    private void ApplyValueToController()
    {
        ResolveController();

        if (controller == null)
        {
            if (!controllerWarningShown)
            {
                Debug.LogWarning($"Lab4 axis rotator skipped: controller not found for {target}.", this);
                controllerWarningShown = true;
            }

            return;
        }

        float normalized = GetValue01();

        switch (target)
        {
            case Lab4RotatorTarget.DriveRegulator:
                controller.SetDriveRegulatorNormalized(normalized);
                break;
            case Lab4RotatorTarget.R1Excitation:
                controller.SetR1Normalized(normalized);
                break;
        }
    }

    private void ResolveController()
    {
        if (controller != null || !autoFindController)
        {
            return;
        }

        controller = Object.FindFirstObjectByType<SyncGeneratorLabController>();
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
            delta = (cw > ccw) ? -ccw : cw;
            if (Mathf.Abs(delta) < 180f)
                delta = (delta >= 0f) ? delta - 360f : delta + 360f;
        }
        else
        {
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
