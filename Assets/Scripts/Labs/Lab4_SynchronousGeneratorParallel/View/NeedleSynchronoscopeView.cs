using UnityEngine;
using UnityEngine.Serialization;

public enum NeedleAxis
{
    X,
    Y,
    Z
}

public class NeedleSynchronoscopeView : MonoBehaviour
{
    public Transform needle;
    public NeedleAxis rotationAxis = NeedleAxis.Z;
    public float minNeedleAngle = -90f;
    public float maxNeedleAngle = 90f;
    public float neutralAngle = 0f;
    public bool smoothNeedle = true;
    public float needleSmoothSpeed = 8f;
    public bool captureBaseRotationOnAwake = true;
    public bool invertDirection = false;
    public Renderer fasterIndicator;
    public Renderer slowerIndicator;
    [FormerlySerializedAs("synchronizationWindowIndicator")]
    public Renderer syncWindowIndicator;
    [SerializeField] private Color inactiveColor = Color.black;
    [SerializeField] private Color fasterColor = Color.red;
    [SerializeField] private Color slowerColor = Color.cyan;
    [SerializeField] private Color windowColor = Color.green;
    [SerializeField] private float equalFrequencyThreshold = 0.01f;

    private MaterialPropertyBlock propertyBlock;
    private Quaternion baseLocalRotation = Quaternion.identity;
    private float currentNeedleAngle;
    private bool hasNeedleAngle;
    private bool hasBaseLocalRotation;

    private void Awake()
    {
        if (captureBaseRotationOnAwake)
        {
            CaptureCurrentNeedleRotationAsBase();
        }
    }

    public void CaptureCurrentNeedleRotationAsBase()
    {
        if (needle == null)
        {
            return;
        }

        baseLocalRotation = needle.localRotation;
        hasBaseLocalRotation = true;
        currentNeedleAngle = neutralAngle;
        hasNeedleAngle = false;
    }

    public void SetPhaseDifference(float phaseDeg, float deltaFrequency)
    {
        SetSynchronizationState(phaseDeg, deltaFrequency, true, true, Mathf.Abs(Mathf.DeltaAngle(0f, phaseDeg)) <= 10f, false, true);
    }

    public void SetSynchronizationState(float phaseDeg, float deltaFrequency, bool voltageMatched, bool synchronizationWindowOpen)
    {
        SetSynchronizationState(phaseDeg, deltaFrequency, voltageMatched, true, synchronizationWindowOpen, false, true);
    }

    public void SetSynchronizationState(
        float phaseDeg,
        float deltaFrequency,
        bool voltageMatched,
        bool frequencyMatched,
        bool phaseWindowOpen,
        bool connectedToGrid,
        bool circuitReady)
    {
        bool active = circuitReady && !connectedToGrid;
        bool windowReady = active && voltageMatched && frequencyMatched && phaseWindowOpen;
        float targetAngle = CalculateNeedleAngle(phaseDeg, deltaFrequency, active, windowReady);

        if (needle == null)
        {
            UpdateIndicators(deltaFrequency, active, windowReady);
            return;
        }

        EnsureBaseLocalRotation();

        ApplyNeedleAngle(targetAngle, Mathf.Abs(deltaFrequency));
        UpdateIndicators(deltaFrequency, active, windowReady);
    }

    public void SetSynchronizationState(
        float phaseDeg,
        float generatorFrequency,
        float gridFrequency,
        float generatorVoltage,
        float gridVoltage,
        bool voltageMatched,
        bool frequencyMatched,
        bool phaseWindowOpen,
        bool connectedToGrid,
        bool circuitReady)
    {
        SetSynchronizationState(
            phaseDeg,
            generatorFrequency - gridFrequency,
            voltageMatched,
            frequencyMatched,
            phaseWindowOpen,
            connectedToGrid,
            circuitReady);
    }

    private float CalculateNeedleAngle(float phaseDeg, float deltaFrequency, bool active, bool windowReady)
    {
        if (!active || windowReady)
        {
            return neutralAngle;
        }

        if (Mathf.Abs(deltaFrequency) <= equalFrequencyThreshold)
        {
            return neutralAngle;
        }

        float phaseMagnitude = Mathf.Abs(Mathf.DeltaAngle(0f, phaseDeg)) / 180f;
        float sectorAngle = deltaFrequency > 0f ? maxNeedleAngle : minNeedleAngle;
        return Mathf.Lerp(neutralAngle, sectorAngle, Mathf.Clamp01(phaseMagnitude));
    }

    private void ApplyNeedleAngle(float targetAngle, float deltaFrequencyMagnitude)
    {
        if (!hasNeedleAngle)
        {
            currentNeedleAngle = targetAngle;
            hasNeedleAngle = true;
        }
        else
        {
            currentNeedleAngle = targetAngle;
        }

        Quaternion targetRotation = GetLocalRotation(currentNeedleAngle);
        if (smoothNeedle)
        {
            float speedMultiplier = Mathf.Lerp(0.5f, 2f, Mathf.Clamp01(deltaFrequencyMagnitude / 0.5f));
            float step = 1f - Mathf.Exp(-Mathf.Max(0.01f, needleSmoothSpeed) * speedMultiplier * Time.deltaTime);
            needle.localRotation = Quaternion.Slerp(needle.localRotation, targetRotation, step);
            return;
        }

        needle.localRotation = targetRotation;
    }

    private Quaternion GetLocalRotation(float angle)
    {
        // The imported needle mesh has its visible Z rotation mirrored relative to the scale labels.
        // Keep logical angles conventional, then correct the applied model rotation here.
        float appliedAngle = invertDirection ? angle : -angle;
        Quaternion additiveRotation;
        switch (rotationAxis)
        {
            case NeedleAxis.X:
                additiveRotation = Quaternion.AngleAxis(appliedAngle, Vector3.right);
                break;
            case NeedleAxis.Y:
                additiveRotation = Quaternion.AngleAxis(appliedAngle, Vector3.up);
                break;
            default:
                additiveRotation = Quaternion.AngleAxis(appliedAngle, Vector3.forward);
                break;
        }

        return baseLocalRotation * additiveRotation;
    }

    private void EnsureBaseLocalRotation()
    {
        if (!hasBaseLocalRotation)
        {
            CaptureCurrentNeedleRotationAsBase();
        }
    }

    private void UpdateIndicators(float deltaFrequency, bool indicatorsEnabled, bool synchronizationWindowOpen)
    {
        bool faster = indicatorsEnabled && deltaFrequency > equalFrequencyThreshold;
        bool slower = indicatorsEnabled && deltaFrequency < -equalFrequencyThreshold;

        ApplyColor(fasterIndicator, faster ? fasterColor : inactiveColor, faster);
        ApplyColor(slowerIndicator, slower ? slowerColor : inactiveColor, slower);
        ApplyColor(syncWindowIndicator, synchronizationWindowOpen ? windowColor : inactiveColor, synchronizationWindowOpen);
    }

    private void ApplyColor(Renderer target, Color color, bool emissionEnabled)
    {
        if (target == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", color);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_EmissionColor", emissionEnabled ? color : Color.black);
        target.SetPropertyBlock(propertyBlock);
    }
}
