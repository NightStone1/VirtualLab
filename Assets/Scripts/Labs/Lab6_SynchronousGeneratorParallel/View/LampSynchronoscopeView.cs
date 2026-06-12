using UnityEngine;
using UnityEngine.Serialization;

public class LampSynchronoscopeView : MonoBehaviour
{
    [Header("HL1")]
    [SerializeField] private Renderer hl1Renderer;
    [SerializeField] private Light hl1Light;

    [Header("HL2")]
    [SerializeField] private Renderer hl2Renderer;
    [SerializeField] private Light hl2Light;

    [Header("HL3")]
    [SerializeField] private Renderer hl3Renderer;
    [SerializeField] private Light hl3Light;

    [Header("Legacy Renderer Array")]
    [SerializeField] private Renderer[] lamps;

    [Header("Lamp Appearance")]
    [SerializeField] private Material offMaterial;
    [SerializeField] private Material onMaterial;
    [FormerlySerializedAs("darkColor")]
    [SerializeField] private Color offColor = Color.black;
    [FormerlySerializedAs("brightColor")]
    [SerializeField] private Color onColor = Color.yellow;
    [FormerlySerializedAs("maxEmissionIntensity")]
    public float maxMaterialEmissionIntensity = 15f;
    public float maxLightIntensity = 0.002f;
    [FormerlySerializedAs("usePhaseShiftedLamps")]
    [SerializeField] private bool usePhaseShift120 = true;
    [SerializeField] private bool invertLamps = true;
    [SerializeField] private float voltageMismatchBrightness = 0.15f;

    private MaterialPropertyBlock propertyBlock;

    public void SetPhaseDifference(float phaseDeg)
    {
        SetSynchronizationState(phaseDeg, 0f, true, true, Mathf.Abs(Mathf.DeltaAngle(0f, phaseDeg)) <= 10f, false, true);
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
        bool synchronizationWindowOpen,
        bool connectedToGrid,
        bool circuitReady)
    {
        bool lampsActive = circuitReady && !connectedToGrid;
        ApplyLamp(hl1Renderer, hl1Light, phaseDeg, 0, lampsActive, voltageMatched, synchronizationWindowOpen);
        ApplyLamp(hl2Renderer, hl2Light, phaseDeg, 1, lampsActive, voltageMatched, synchronizationWindowOpen);
        ApplyLamp(hl3Renderer, hl3Light, phaseDeg, 2, lampsActive, voltageMatched, synchronizationWindowOpen);

        if (lamps == null)
        {
            return;
        }

        for (int i = 0; i < lamps.Length; i++)
        {
            ApplyLamp(lamps[i], null, phaseDeg, i, lampsActive, voltageMatched, synchronizationWindowOpen);
        }
    }

    public void SetSynchronizationState(
        float phaseDeg,
        float generatorFrequency,
        float gridFrequency,
        float generatorVoltage,
        float gridVoltage,
        bool voltageMatched,
        bool frequencyMatched,
        bool synchronizationWindowOpen,
        bool connectedToGrid,
        bool circuitReady)
    {
        SetSynchronizationState(
            phaseDeg,
            generatorFrequency - gridFrequency,
            voltageMatched,
            frequencyMatched,
            synchronizationWindowOpen,
            connectedToGrid,
            circuitReady);
    }

    private void ApplyLamp(Renderer renderer, Light lightSource, float phaseDeg, int lampIndex, bool lampsActive, bool voltageMatched, bool synchronizationWindowOpen)
    {
        float brightness = lampsActive
            ? CalculateBrightness(phaseDeg, lampIndex, voltageMatched, synchronizationWindowOpen)
            : 0f;

        Color color = Color.Lerp(offColor, onColor, brightness);
        Color emissionColor = onColor * (brightness * Mathf.Max(0f, maxMaterialEmissionIntensity));

        ApplyRenderer(renderer, color, emissionColor, brightness);
        ApplyLight(lightSource, color, brightness);
    }

    private float CalculateBrightness(float phaseDeg, int lampIndex, bool voltageMatched, bool synchronizationWindowOpen)
    {
        if (synchronizationWindowOpen && invertLamps)
        {
            return 0f;
        }

        float shiftedPhase = usePhaseShift120 ? phaseDeg + lampIndex * 120f : phaseDeg;
        float phaseRadians = Mathf.Deg2Rad * Mathf.Abs(Mathf.DeltaAngle(0f, shiftedPhase));
        float darkAtMatchBrightness = Mathf.Abs(Mathf.Sin(phaseRadians * 0.5f));
        float brightness = invertLamps ? darkAtMatchBrightness : 1f - darkAtMatchBrightness;

        if (!voltageMatched)
        {
            brightness = Mathf.Min(Mathf.Max(brightness, voltageMismatchBrightness), 0.35f);
        }

        return Mathf.Clamp01(brightness);
    }

    private void ApplyRenderer(Renderer lamp, Color color, Color emissionColor, float brightness)
    {
        if (lamp == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        lamp.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", color);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_EmissionColor", emissionColor);
        lamp.SetPropertyBlock(propertyBlock);
    }

    private void ApplyLight(Light lightSource, Color color, float brightness)
    {
        if (lightSource == null)
        {
            return;
        }

        lightSource.color = color;
        lightSource.intensity = Mathf.Max(0f, maxLightIntensity) * brightness;
        lightSource.enabled = brightness > 0.001f;
    }
}
