using UnityEngine;

public class Lab4MotorRotationView : MonoBehaviour
{
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;
    public Transform[] rotatingParts;
    public Vector3 localRotationAxis = Vector3.forward;
    public float visualSpeedMultiplier = 1f;
    public bool rotateOnlyWhenPrimeMoverOn = true;
    public bool logDebug = false;

    private void Awake()
    {
        if (controller == null && autoFindController)
        {
            controller = FindFirstObjectByType<SyncGeneratorLabController>();
        }

        if (logDebug && controller == null)
        {
            Debug.LogWarning($"{nameof(Lab4MotorRotationView)} on {name} has no controller.", this);
        }
    }

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        if (rotateOnlyWhenPrimeMoverOn && !controller.IsQ3Enabled)
        {
            return;
        }

        float rpm = controller.IsConnectedToGrid
            ? controller.GridFrequency * 60f
            : controller.RotorSpeedRpm;

        if (rpm <= 0.01f)
        {
            return;
        }

        Vector3 axis = localRotationAxis.sqrMagnitude > 0.0001f
            ? localRotationAxis.normalized
            : Vector3.forward;
        float degrees = rpm * 6f * visualSpeedMultiplier * Time.deltaTime;

        if (rotatingParts == null || rotatingParts.Length == 0)
        {
            transform.Rotate(axis, degrees, Space.Self);
            return;
        }

        for (int i = 0; i < rotatingParts.Length; i++)
        {
            Transform part = rotatingParts[i];
            if (part == null)
            {
                continue;
            }

            part.Rotate(axis, degrees, Space.Self);
        }
    }
}
