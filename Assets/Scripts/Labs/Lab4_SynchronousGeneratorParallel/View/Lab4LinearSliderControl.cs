using UnityEngine;

public enum Lab4LinearSliderTarget
{
    R1Excitation,
    DriveRegulator
}

public class Lab4LinearSliderControl : MonoBehaviour
{
    public Lab4LinearSliderTarget target;
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;
    public Transform handle;
    public Transform minPoint;
    public Transform maxPoint;
    [Range(0f, 1f)] public float value;
    public bool setHandlePositionOnStart = true;
    public bool logDebug = true;

    private Camera dragCamera;
    private Plane dragPlane;
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
            value = Mathf.Clamp01(value);
            UpdateHandlePosition();
        }
        else
        {
            UpdateValueFromHandle();
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
        if (!ValidateReferences())
        {
            return;
        }

        dragCamera = Camera.main;
        if (dragCamera == null)
        {
            Debug.LogWarning("Lab4 linear slider cannot drag: main camera not found.", this);
            return;
        }

        dragPlane = new Plane(-dragCamera.transform.forward, handle.position);
        isDragging = true;
        DragToMousePosition();
    }

    private void OnMouseDrag()
    {
        if (!isDragging)
        {
            return;
        }

        DragToMousePosition();
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void DragToMousePosition()
    {
        if (dragCamera == null || !ValidateReferences())
        {
            return;
        }

        Ray ray = dragCamera.ScreenPointToRay(Input.mousePosition);
        if (!dragPlane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector3 mouseWorld = ray.GetPoint(distance);
        Vector3 min = minPoint.position;
        Vector3 max = maxPoint.position;
        Vector3 line = max - min;
        float lineLengthSqr = line.sqrMagnitude;

        if (lineLengthSqr <= 0.000001f)
        {
            Debug.LogWarning("Lab4 linear slider cannot drag: minPoint and maxPoint are too close.", this);
            return;
        }

        float t = Vector3.Dot(mouseWorld - min, line) / lineLengthSqr;
        SetValue(t);
    }

    private void UpdateHandlePosition()
    {
        if (!ValidateReferences())
        {
            return;
        }

        handle.position = Vector3.Lerp(minPoint.position, maxPoint.position, value);
    }

    private void UpdateValueFromHandle()
    {
        if (!ValidateReferences())
        {
            return;
        }

        Vector3 min = minPoint.position;
        Vector3 max = maxPoint.position;
        Vector3 line = max - min;
        float lineLengthSqr = line.sqrMagnitude;

        if (lineLengthSqr <= 0.000001f)
        {
            value = 0f;
            return;
        }

        value = Mathf.Clamp01(Vector3.Dot(handle.position - min, line) / lineLengthSqr);
    }

    private void ApplyValueToController()
    {
        ResolveController();

        if (controller == null)
        {
            Debug.LogWarning($"Lab4 linear slider skipped: controller not found for {target}.", this);
            return;
        }

        switch (target)
        {
            case Lab4LinearSliderTarget.R1Excitation:
                controller.SetR1Normalized(value);
                break;
            case Lab4LinearSliderTarget.DriveRegulator:
                controller.SetDriveRegulatorNormalized(value);
                break;
        }

        if (logDebug)
        {
            Debug.Log($"Lab4 linear slider value applied: {target} = {value:0.00}", this);
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

    private bool ValidateReferences()
    {
        if (handle == null)
        {
            Debug.LogWarning("Lab4 linear slider handle is not assigned.", this);
            return false;
        }

        if (minPoint == null || maxPoint == null)
        {
            Debug.LogWarning("Lab4 linear slider minPoint/maxPoint are not assigned.", this);
            return false;
        }

        return true;
    }
}
