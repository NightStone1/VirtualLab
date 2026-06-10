using UnityEngine;

public class Lab6StandView : MonoBehaviour
{
    private enum LocalRotationAxis
    {
        X,
        Y,
        Z,
        NegativeX,
        NegativeY,
        NegativeZ
    }

    [Header("Rotating Parts")]
    [SerializeField] private Transform motorRotor;
    [SerializeField] private Transform generatorRotor;
    [SerializeField] private Transform shaft;

    [Header("Handles")]
    [SerializeField] private Transform q2Handle;
    [SerializeField] private Transform q6Handle;
    [SerializeField] private Transform brakeHandle;
    [SerializeField] private float[] q2PositionAngles = { -70f, -50f, -30f, -10f, 10f, 30f, 50f, 70f };
    [SerializeField] private LocalRotationAxis q2RotationAxis = LocalRotationAxis.Y;
    [SerializeField] private LocalRotationAxis q6RotationAxis = LocalRotationAxis.Y;
    [SerializeField] private float q2MinAngle = -70f;
    [SerializeField] private float q2MaxAngle = 70f;
    [SerializeField] private float q6OffAngle = -35f;
    [SerializeField] private float q6OnAngle = 35f;
    [SerializeField] private float brakeOffAngle = -35f;
    [SerializeField] private float brakeOnAngle = 35f;

    [Header("Switch Renderers")]
    [SerializeField] private Renderer q1Renderer;
    [SerializeField] private Renderer q3Renderer;
    [SerializeField] private Renderer q4Renderer;
    [SerializeField] private Renderer q5Renderer;
    [SerializeField] private Renderer q6Renderer;
    [SerializeField] private Renderer q2Renderer;
    [SerializeField] private int q1MaterialIndex;
    [SerializeField] private int q3MaterialIndex;
    [SerializeField] private int q4MaterialIndex;
    [SerializeField] private int q5MaterialIndex;
    [SerializeField] private int q6MaterialIndex;
    [SerializeField] private int q2MaterialIndex;
    [SerializeField] private Color offColor = Color.gray;
    [SerializeField] private Color onColor = Color.green;

    [Header("Load R Block")]
    [SerializeField] private Renderer[] loadStepRenderers;
    [SerializeField] private Transform[] loadStepTransforms;
    [SerializeField] private float loadStepOffAngle;
    [SerializeField] private float loadStepOnAngle = 25f;
    [SerializeField] private LocalRotationAxis loadStepRotationAxis = LocalRotationAxis.Y;
    [SerializeField] private Color loadStepOffColor = Color.gray;
    [SerializeField] private Color loadStepOnColor = Color.green;
    [SerializeField] private int loadStepMaterialIndex;

    private bool q1MaterialWarningLogged;
    private bool q2MaterialWarningLogged;
    private bool q3MaterialWarningLogged;
    private bool q4MaterialWarningLogged;
    private bool q5MaterialWarningLogged;
    private bool q6MaterialWarningLogged;
    private bool loadStepMaterialWarningLogged;

    [Header("Lights")]
    [SerializeField] private Light powerLight;
    [SerializeField] private Light motorRunningLight;
    [SerializeField] private Light brakeLight;

    [Header("Meters")]
    [SerializeField] private Lab6MeterView voltageMeter;
    [SerializeField] private Lab6MeterView currentMeter;
    [SerializeField] private Lab6MeterView powerMeter;
    [SerializeField] private Lab6MeterView speedMeter;

    public void UpdateView(Lab6Controller controller, Lab6Measurement measurement, float deltaTime)
    {
        if (controller == null || measurement == null)
        {
            return;
        }

        float rpm = SanitizeFinite(measurement.speed);
        if (controller.CurrentStage == Lab6Stage.ShortCircuit && controller.BrakeEnabled)
        {
            rpm = 0f;
        }

        RotateIfAssigned(motorRotor, rpm, deltaTime);
        RotateIfAssigned(generatorRotor, rpm, deltaTime);
        RotateIfAssigned(shaft, rpm, deltaTime);

        SetLocalAxisRotation(q2Handle, q2RotationAxis, GetQ2HandleAngle(controller.Q2Position));
        SetLocalAxisRotation(q6Handle, q6RotationAxis, controller.Q6Enabled ? q6OnAngle : q6OffAngle);
        SetYRotation(brakeHandle, controller.BrakeEnabled ? brakeOnAngle : brakeOffAngle);

        SetRendererColor(q1Renderer, q1MaterialIndex, controller.Q1Enabled, "Q1", ref q1MaterialWarningLogged);
        SetRendererColor(q2Renderer, q2MaterialIndex, controller.Q2Enabled, "Q2", ref q2MaterialWarningLogged);
        SetRendererColor(q3Renderer, q3MaterialIndex, controller.Q3Enabled, "Q3", ref q3MaterialWarningLogged);
        SetRendererColor(q4Renderer, q4MaterialIndex, controller.Q4Enabled, "Q4", ref q4MaterialWarningLogged);
        SetRendererColor(q5Renderer, q5MaterialIndex, controller.Q5Enabled, "Q5", ref q5MaterialWarningLogged);
        SetRendererColor(q6Renderer, q6MaterialIndex, controller.Q6Enabled, "Q6", ref q6MaterialWarningLogged);
        UpdateLoadStepVisuals(controller.LoadStep);

        SetLight(powerLight, controller.Q1Enabled);
        SetLight(motorRunningLight, rpm > 1f);
        SetLight(brakeLight, controller.BrakeEnabled);

        if (voltageMeter != null) voltageMeter.SetValue(measurement.voltage);
        if (currentMeter != null) currentMeter.SetValue(measurement.current);
        if (powerMeter != null) powerMeter.SetValue(measurement.powerInput);
        if (speedMeter != null) speedMeter.SetValue(measurement.speed);
    }

    private static void RotateIfAssigned(Transform target, float rpm, float deltaTime)
    {
        if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
        {
            deltaTime = 0f;
        }

        if (target != null && rpm > 1f)
        {
            target.Rotate(Vector3.forward, rpm * 6f * deltaTime, Space.Self);
        }
    }

    private static void SetYRotation(Transform target, float angle)
    {
        if (target != null)
        {
            target.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    private static void SetLocalAxisRotation(Transform target, LocalRotationAxis axis, float angle)
    {
        if (target != null)
        {
            target.localRotation = Quaternion.AngleAxis(angle, GetAxisVector(axis));
        }
    }

    private float GetQ2HandleAngle(int position)
    {
        int clampedPosition = Mathf.Clamp(position, 0, 7);
        if (q2PositionAngles != null && q2PositionAngles.Length >= 8)
        {
            return q2PositionAngles[clampedPosition];
        }

        return Mathf.Lerp(q2MinAngle, q2MaxAngle, clampedPosition / 7f);
    }

    private static Vector3 GetAxisVector(LocalRotationAxis axis)
    {
        switch (axis)
        {
            case LocalRotationAxis.X:
                return Vector3.right;
            case LocalRotationAxis.Y:
                return Vector3.up;
            case LocalRotationAxis.Z:
                return Vector3.forward;
            case LocalRotationAxis.NegativeX:
                return Vector3.left;
            case LocalRotationAxis.NegativeY:
                return Vector3.down;
            case LocalRotationAxis.NegativeZ:
                return Vector3.back;
            default:
                return Vector3.up;
        }
    }

    private void SetRendererColor(Renderer target, int materialIndex, bool enabled, string label, ref bool warningLogged)
    {
        if (target == null)
        {
            return;
        }

        Material[] materials = target.materials;
        if (materials == null || materials.Length == 0)
        {
            if (!warningLogged)
            {
                Debug.LogWarning($"Lab6StandView: {label} renderer has no materials.");
                warningLogged = true;
            }

            return;
        }

        int safeIndex = materialIndex;
        if (safeIndex < 0 || safeIndex >= materials.Length)
        {
            if (!warningLogged)
            {
                Debug.LogWarning($"Lab6StandView: {label} material index {materialIndex} is out of range. Using index 0.");
                warningLogged = true;
            }

            safeIndex = 0;
        }

        if (materials[safeIndex] != null)
        {
            materials[safeIndex].color = enabled ? onColor : offColor;
        }
    }

    private static void SetLight(Light target, bool enabled)
    {
        if (target != null)
        {
            target.enabled = enabled;
        }
    }

    private void UpdateLoadStepVisuals(int activeStep)
    {
        int clampedStep = Mathf.Clamp(activeStep, 0, 4);

        if (loadStepRenderers != null)
        {
            for (int i = 0; i < loadStepRenderers.Length && i < 4; i++)
            {
                bool isActiveStep = clampedStep == i + 1;
                SetRendererColor(loadStepRenderers[i], loadStepMaterialIndex, isActiveStep ? loadStepOnColor : loadStepOffColor, "RStep" + (i + 1), ref loadStepMaterialWarningLogged);
            }
        }

        if (loadStepTransforms != null)
        {
            for (int i = 0; i < loadStepTransforms.Length && i < 4; i++)
            {
                if (loadStepTransforms[i] != null)
                {
                    bool isActiveStep = clampedStep == i + 1;
                    SetLocalAxisRotation(loadStepTransforms[i], loadStepRotationAxis, isActiveStep ? loadStepOnAngle : loadStepOffAngle);
                }
            }
        }
    }

    private void SetRendererColor(Renderer target, int materialIndex, Color color, string label, ref bool warningLogged)
    {
        if (target == null)
        {
            return;
        }

        Material[] materials = target.materials;
        if (materials == null || materials.Length == 0)
        {
            if (!warningLogged)
            {
                Debug.LogWarning($"Lab6StandView: {label} renderer has no materials.");
                warningLogged = true;
            }

            return;
        }

        int safeIndex = materialIndex;
        if (safeIndex < 0 || safeIndex >= materials.Length)
        {
            if (!warningLogged)
            {
                Debug.LogWarning($"Lab6StandView: {label} material index {materialIndex} is out of range. Using index 0.");
                warningLogged = true;
            }

            safeIndex = 0;
        }

        if (materials[safeIndex] != null)
        {
            materials[safeIndex].color = color;
        }
    }

    private static float SanitizeFinite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
}
