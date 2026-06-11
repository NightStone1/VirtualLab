using TMPro;
using UnityEngine;

public enum Lab6MeterSource
{
    None,
    InputVoltage,
    MotorVoltage,
    MotorCurrent,
    GeneratorVoltage,
    GeneratorCurrent,
    FieldCurrent,
    AuxVoltage,
    AuxCurrent,
    Zero
}

[System.Serializable]
public class Lab6MeterBinding
{
    public Lab6MeterView meter;
    public Lab6MeterSource source;
}

public class Lab6StandView : MonoBehaviour
{
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

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
    [SerializeField] private Transform q1Handle;
    [SerializeField] private Transform q2Handle;
    [SerializeField] private Transform q3Handle;
    [SerializeField] private Transform q4Handle;
    [SerializeField] private Transform q5Handle;
    [SerializeField] private Transform q6Handle;
    [SerializeField] private Transform brakeHandle;
    [SerializeField] private float[] q2PositionAngles = { -70f, -50f, -30f, -10f, 10f, 30f, 50f, 70f };
    [SerializeField] private LocalRotationAxis q1RotationAxis = LocalRotationAxis.Y;
    [SerializeField] private LocalRotationAxis q2RotationAxis = LocalRotationAxis.Y;
    [SerializeField] private LocalRotationAxis q3RotationAxis = LocalRotationAxis.Y;
    [SerializeField] private LocalRotationAxis q4RotationAxis = LocalRotationAxis.Y;
    [SerializeField] private LocalRotationAxis q5RotationAxis = LocalRotationAxis.Y;
    [SerializeField] private LocalRotationAxis q6RotationAxis = LocalRotationAxis.Y;
    [SerializeField] private float q1OffAngle = -35f;
    [SerializeField] private float q1OnAngle = 35f;
    [SerializeField] private float q2MinAngle = -70f;
    [SerializeField] private float q2MaxAngle = 70f;
    [SerializeField] private float q3OffAngle = -35f;
    [SerializeField] private float q3OnAngle = 35f;
    [SerializeField] private float q4OffAngle = -35f;
    [SerializeField] private float q4OnAngle = 35f;
    [SerializeField] private float q5OffAngle = -35f;
    [SerializeField] private float q5OnAngle = 35f;
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

    [Header("Winding Connections")]
    [SerializeField] private Lab6WindingConnectionView windingConnectionView;

    [Header("RPM Display")]
    [SerializeField] private TMP_Text rpmText;
    [SerializeField] private string rpmTextFormat = "n = {0:0} об/мин";

    private bool q1MaterialWarningLogged;
    private bool q2MaterialWarningLogged;
    private bool q3MaterialWarningLogged;
    private bool q4MaterialWarningLogged;
    private bool q5MaterialWarningLogged;
    private bool q6MaterialWarningLogged;
    private bool loadStepMaterialWarningLogged;
    private bool hasVisualStateCache;
    private bool cachedQ1;
    private bool cachedQ2;
    private bool cachedQ3;
    private bool cachedQ4;
    private bool cachedQ5;
    private bool cachedQ6;
    private bool cachedBrake;
    private int cachedQ2Position = -1;
    private int cachedLoadStep = -1;
    private int lastDisplayedRpm = int.MinValue;
    private Lab6Stage lastDisplayedRpmStage = (Lab6Stage)(-1);
    private string lastDisplayedRpmText;
    private Lab6Stage cachedStage;
    private float cachedVoltage = -1f;
    private float cachedCurrent = -1f;
    private float cachedPowerInput = -1f;
    private float cachedSpeed = -1f;
    private MaterialPropertyBlock materialPropertyBlock;

    [Header("Lights")]
    [SerializeField] private Light powerLight;
    [SerializeField] private Light motorRunningLight;
    [SerializeField] private Light brakeLight;

    [Header("Meters")]
    [SerializeField] private Lab6MeterBinding[] meterBindings;
    [SerializeField] private Lab6MeterView inputVoltageMeter;
    [SerializeField] private Lab6MeterView motorVoltageMeter;
    [SerializeField] private Lab6MeterView motorCurrentMeter;
    [SerializeField] private Lab6MeterView generatorVoltageMeter;
    [SerializeField] private Lab6MeterView generatorCurrentMeter;
    [SerializeField] private Lab6MeterView fieldCurrentMeter;

    [Header("Legacy Meters")]
    [SerializeField] private Lab6MeterView voltageMeter;
    [SerializeField] private Lab6MeterView currentMeter;
    [SerializeField] private Lab6MeterView powerMeter;
    [SerializeField] private Lab6MeterView speedMeter;

    private void LateUpdate()
    {
        if (rpmText != null && !string.IsNullOrEmpty(lastDisplayedRpmText) && rpmText.text != lastDisplayedRpmText)
        {
            rpmText.text = lastDisplayedRpmText;
        }
    }

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
        UpdateRpmText(controller, rpm);

        if (!HasVisualStateChanged(controller, measurement, rpm))
        {
            return;
        }

        SetLocalAxisRotation(q1Handle, q1RotationAxis, controller.Q1Enabled ? q1OnAngle : q1OffAngle);
        SetLocalAxisRotation(q2Handle, q2RotationAxis, GetQ2HandleAngle(controller.Q2Position));
        SetLocalAxisRotation(q3Handle, q3RotationAxis, controller.Q3Enabled ? q3OnAngle : q3OffAngle);
        SetLocalAxisRotation(q4Handle, q4RotationAxis, controller.Q4Enabled ? q4OnAngle : q4OffAngle);
        SetLocalAxisRotation(q5Handle, q5RotationAxis, controller.Q5Enabled ? q5OnAngle : q5OffAngle);
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
        if (windingConnectionView != null)
        {
            windingConnectionView.ShowForStage(controller.CurrentStage);
        }

        UpdateMeters(controller, measurement);
        if (speedMeter != null) speedMeter.SetValue(measurement.speed);
        CacheVisualState(controller, measurement);
    }

    private void UpdateMeters(Lab6Controller controller, Lab6Measurement measurement)
    {
        float inputVoltage = controller.Q1Enabled ? controller.Data.nominalVoltage : 0f;
        float motorVoltage = measurement.voltage;
        float motorCurrent = measurement.current;
        float loadFraction = Mathf.Clamp01(controller.LoadPercent / 100f);
        bool loadStageActive = controller.CurrentStage == Lab6Stage.Load && controller.Q1Enabled && controller.Q3Enabled && controller.Q4Enabled;
        float generatorVoltage = loadStageActive ? controller.Data.nominalVoltage * 0.55f * loadFraction : 0f;
        float generatorCurrent = loadStageActive ? motorCurrent * Mathf.Lerp(0.25f, 0.85f, loadFraction) : 0f;
        float fieldCurrent = loadStageActive ? Mathf.Lerp(0.4f, 1.2f, Mathf.Max(0.1f, loadFraction)) : 0f;
        float auxVoltage = controller.Q1Enabled ? 24f : 0f;
        float auxCurrent = loadStageActive ? Mathf.Lerp(0.1f, 0.8f, loadFraction) : 0f;

        if (meterBindings != null && meterBindings.Length > 0)
        {
            for (int i = 0; i < meterBindings.Length; i++)
            {
                Lab6MeterBinding binding = meterBindings[i];
                if (binding == null || binding.meter == null || binding.source == Lab6MeterSource.None)
                {
                    continue;
                }

                binding.meter.SetValue(GetMeterSourceValue(binding.source, inputVoltage, motorVoltage, motorCurrent, generatorVoltage, generatorCurrent, fieldCurrent, auxVoltage, auxCurrent));
            }

            return;
        }

        SetMeterValue(inputVoltageMeter, inputVoltage);
        SetMeterValue(motorVoltageMeter, motorVoltage);
        SetMeterValue(motorCurrentMeter, motorCurrent);
        SetMeterValue(generatorVoltageMeter, generatorVoltage);
        SetMeterValue(generatorCurrentMeter, generatorCurrent);
        SetMeterValue(fieldCurrentMeter, fieldCurrent);

        SetMeterValue(voltageMeter, motorVoltage);
        SetMeterValue(currentMeter, motorCurrent);
        // 3D стенд Lab6 не имеет PW/wattmeter; мощность остаётся в TV Results UI.
        SetMeterValue(powerMeter, 0f);
    }

    private static float GetMeterSourceValue(
        Lab6MeterSource source,
        float inputVoltage,
        float motorVoltage,
        float motorCurrent,
        float generatorVoltage,
        float generatorCurrent,
        float fieldCurrent,
        float auxVoltage,
        float auxCurrent)
    {
        switch (source)
        {
            case Lab6MeterSource.InputVoltage:
                return inputVoltage;
            case Lab6MeterSource.MotorVoltage:
                return motorVoltage;
            case Lab6MeterSource.MotorCurrent:
                return motorCurrent;
            case Lab6MeterSource.GeneratorVoltage:
                return generatorVoltage;
            case Lab6MeterSource.GeneratorCurrent:
                return generatorCurrent;
            case Lab6MeterSource.FieldCurrent:
                return fieldCurrent;
            case Lab6MeterSource.AuxVoltage:
                return auxVoltage;
            case Lab6MeterSource.AuxCurrent:
                return auxCurrent;
            case Lab6MeterSource.Zero:
            default:
                return 0f;
        }
    }

    private static void SetMeterValue(Lab6MeterView meter, float value)
    {
        if (meter != null)
        {
            meter.SetValue(value);
        }
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

    private void UpdateRpmText(Lab6Controller controller, float rpm)
    {
        if (rpmText == null)
        {
            return;
        }

        int displayRpm = Mathf.RoundToInt(GetDisplayRpm(controller, rpm));
        string displayText = FormatRpmText(displayRpm);
        if (lastDisplayedRpm == displayRpm && lastDisplayedRpmStage == controller.CurrentStage && rpmText.text == displayText)
        {
            return;
        }

        lastDisplayedRpm = displayRpm;
        lastDisplayedRpmStage = controller.CurrentStage;
        lastDisplayedRpmText = displayText;
        rpmText.text = displayText;
    }

    private string FormatRpmText(int displayRpm)
    {
        string format = string.IsNullOrEmpty(rpmTextFormat) ? "n = {0:0} об/мин" : rpmTextFormat;
        try
        {
            return string.Format(format, displayRpm);
        }
        catch (System.FormatException)
        {
            return $"n = {displayRpm:0} об/мин";
        }
    }

    private static float GetDisplayRpm(Lab6Controller controller, float rpm)
    {
        switch (controller.CurrentStage)
        {
            case Lab6Stage.NoLoad:
            case Lab6Stage.Load:
                return rpm;
            default:
                return 0f;
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

        Material[] materials = target.sharedMaterials;
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

        SetRendererPropertyBlockColor(target, safeIndex, enabled ? onColor : offColor);
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

        Material[] materials = target.sharedMaterials;
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

        SetRendererPropertyBlockColor(target, safeIndex, color);
    }

    private void SetRendererPropertyBlockColor(Renderer target, int materialIndex, Color color)
    {
        if (target == null)
        {
            return;
        }

        if (materialPropertyBlock == null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        materialPropertyBlock.Clear();
        materialPropertyBlock.SetColor(ColorPropertyId, color);
        materialPropertyBlock.SetColor(BaseColorPropertyId, color);
        target.SetPropertyBlock(materialPropertyBlock, materialIndex);
    }

    private bool HasVisualStateChanged(Lab6Controller controller, Lab6Measurement measurement, float rpm)
    {
        if (!hasVisualStateCache)
        {
            return true;
        }

        return cachedQ1 != controller.Q1Enabled
            || cachedQ2 != controller.Q2Enabled
            || cachedQ3 != controller.Q3Enabled
            || cachedQ4 != controller.Q4Enabled
            || cachedQ5 != controller.Q5Enabled
            || cachedQ6 != controller.Q6Enabled
            || cachedBrake != controller.BrakeEnabled
            || cachedQ2Position != controller.Q2Position
            || cachedLoadStep != controller.LoadStep
            || cachedStage != controller.CurrentStage
            || !Mathf.Approximately(cachedVoltage, measurement.voltage)
            || !Mathf.Approximately(cachedCurrent, measurement.current)
            || !Mathf.Approximately(cachedPowerInput, measurement.powerInput)
            || !Mathf.Approximately(cachedSpeed, rpm);
    }

    private void CacheVisualState(Lab6Controller controller, Lab6Measurement measurement)
    {
        hasVisualStateCache = true;
        cachedQ1 = controller.Q1Enabled;
        cachedQ2 = controller.Q2Enabled;
        cachedQ3 = controller.Q3Enabled;
        cachedQ4 = controller.Q4Enabled;
        cachedQ5 = controller.Q5Enabled;
        cachedQ6 = controller.Q6Enabled;
        cachedBrake = controller.BrakeEnabled;
        cachedQ2Position = controller.Q2Position;
        cachedLoadStep = controller.LoadStep;
        cachedStage = controller.CurrentStage;
        cachedVoltage = measurement.voltage;
        cachedCurrent = measurement.current;
        cachedPowerInput = measurement.powerInput;
        cachedSpeed = measurement.speed;
    }

    private static float SanitizeFinite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
}
