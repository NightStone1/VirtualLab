using UnityEngine;

public class Lever8Positions : MonoBehaviour
{
    [Header("Rotation settings")]
    public int steps = 8; // Количество позиций (8 = шаг 45 градусов)
    public float startAngle = 0f;   // Начальный угол (0°)
    public float endAngle = 360f;    // Конечный угол (360°)

    [Header("Local rotation base")]
    public Vector3 baseEuler = new Vector3(0f, 90f, -90f);

    [Header("Cursor angle offset")]
    public float angleOffset = -90f;

    [Header("Current step")]
    [Range(0, 7)] public int stepIndex = 0; // 0..7 для 8 позиций

    public event System.Action<int> OnStepChanged;

    private bool isDragging;
    private Camera cam;

    private float StepAngle => 360f / steps; // 45° для steps=8

    private void Start()
    {
        cam = Camera.main;
        ApplyFromCurrentStep();
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

        // Преобразуем угол в шаг (0..steps-1)
        float t = angle / 360f; // 0..1
        int newStep = Mathf.RoundToInt(t * (steps - 1));
        newStep = Mathf.Clamp(newStep, 0, steps - 1);

        SetStep(newStep);
        ApplyFromCurrentStep();
    }

    private void ApplyFromCurrentStep()
    {
        float angle = stepIndex * StepAngle;
        SetKnobAngle(angle);
    }

    private void SetStep(int newStep)
    {
        if (newStep != stepIndex)
        {
            stepIndex = newStep;
            OnStepChanged?.Invoke(stepIndex);
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

    private float Normalize360(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }
}