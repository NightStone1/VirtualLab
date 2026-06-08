using UnityEngine;

public enum Lab4AxisSliderTarget
{
    R1Excitation,
    DriveRegulator
}

public enum Lab4SliderAxis
{
    X,
    Y,
    Z
}

public enum CoordinateSpace
{
    Local,
    World
}

public class Lab4AxisSliderControl : MonoBehaviour
{
    public Lab4AxisSliderTarget target;
    public Lab4SliderAxis axis;
    public CoordinateSpace coordinateSpace = CoordinateSpace.Local;
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;
    public Transform handle;
    public float min;
    public float max = 1f;
    public bool inverted = false;
    [Range(0f, 1f)] public float value;
    public bool setHandlePositionOnStart = true;
    public bool logDebug = true;

    private Camera dragCamera;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private bool isDragging;

    private void Awake()
    {
        ResolveController();

        if (handle == null)
        {
            handle = transform;
        }
    }

    private void Start()
    {
        if (setHandlePositionOnStart)
        {
            SetValue(value);
        }
        else
        {
            UpdateValueFromHandle();
            ApplyValueToController();
        }
    }

    public void SetValue(float normalizedValue)
    {
        value = Mathf.Clamp01(normalizedValue);
        UpdateHandlePosition();
        ApplyValueToController();
    }

    public void SetValueFromController(float normalizedValue)
    {
        value = Mathf.Clamp01(normalizedValue);
        UpdateHandlePosition();
    }

    private void OnMouseDown()
    {
        if (!ValidateHandle())
        {
            return;
        }

        dragCamera = Camera.main;
        if (dragCamera == null)
        {
            Debug.LogWarning("Lab4 axis slider cannot drag: main camera not found.", this);
            return;
        }

        dragPlane = new Plane(-dragCamera.transform.forward, handle.position);
        dragOffset = Vector3.zero;

        Ray ray = dragCamera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float distance))
        {
            dragOffset = handle.position - ray.GetPoint(distance);
        }

        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (!isDragging || dragCamera == null || !ValidateHandle())
        {
            return;
        }

        Ray ray = dragCamera.ScreenPointToRay(Input.mousePosition);
        if (!dragPlane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector3 worldPoint = ray.GetPoint(distance) + dragOffset;
        Vector3 point = coordinateSpace == CoordinateSpace.Local && handle.parent != null
            ? handle.parent.InverseTransformPoint(worldPoint)
            : worldPoint;
        float axisPosition = GetAxisValue(point);
        axisPosition = Mathf.Clamp(axisPosition, Mathf.Min(min, max), Mathf.Max(min, max));

        Vector3 position = GetHandlePosition();
        SetAxisValue(ref position, axisPosition);
        SetHandlePosition(position);

        float rawValue = Mathf.InverseLerp(min, max, axisPosition);
        value = inverted ? 1f - rawValue : rawValue;
        value = Mathf.Clamp01(value);
        ApplyValueToController();
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void UpdateHandlePosition()
    {
        if (!ValidateHandle())
        {
            return;
        }

        float rawValue = inverted ? 1f - value : value;
        float axisPosition = Mathf.Lerp(min, max, Mathf.Clamp01(rawValue));
        Vector3 position = GetHandlePosition();
        SetAxisValue(ref position, axisPosition);
        SetHandlePosition(position);
    }

    private void UpdateValueFromHandle()
    {
        if (!ValidateHandle())
        {
            return;
        }

        float rawValue = Mathf.InverseLerp(min, max, GetAxisValue(GetHandlePosition()));
        value = inverted ? 1f - rawValue : rawValue;
        value = Mathf.Clamp01(value);
    }

    private void ApplyValueToController()
    {
        ResolveController();

        if (controller == null)
        {
            Debug.LogWarning($"Lab4 axis slider skipped: controller not found for {target}.", this);
            return;
        }

        switch (target)
        {
            case Lab4AxisSliderTarget.R1Excitation:
                controller.SetR1Normalized(value);
                break;
            case Lab4AxisSliderTarget.DriveRegulator:
                controller.SetDriveRegulatorNormalized(value);
                break;
        }

        if (logDebug)
        {
            Debug.Log($"Lab4 axis slider value applied: {target} = {value:0.00}", this);
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

    private bool ValidateHandle()
    {
        if (handle != null)
        {
            return true;
        }

        Debug.LogWarning("Lab4 axis slider handle is not assigned.", this);
        return false;
    }

    private float GetAxisValue(Vector3 position)
    {
        switch (axis)
        {
            case Lab4SliderAxis.X:
                return position.x;
            case Lab4SliderAxis.Y:
                return position.y;
            case Lab4SliderAxis.Z:
                return position.z;
            default:
                return position.x;
        }
    }

    private void SetAxisValue(ref Vector3 position, float axisPosition)
    {
        switch (axis)
        {
            case Lab4SliderAxis.X:
                position.x = axisPosition;
                break;
            case Lab4SliderAxis.Y:
                position.y = axisPosition;
                break;
            case Lab4SliderAxis.Z:
                position.z = axisPosition;
                break;
        }
    }

    private Vector3 GetHandlePosition()
    {
        return coordinateSpace == CoordinateSpace.Local ? handle.localPosition : handle.position;
    }

    private void SetHandlePosition(Vector3 position)
    {
        if (coordinateSpace == CoordinateSpace.Local)
        {
            handle.localPosition = position;
            return;
        }

        handle.position = position;
    }
}
