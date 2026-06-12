using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class Lab4FrequencyIndicatorView : MonoBehaviour
{
    public Meter sourceMeter;
    public float minFrequency = 45f;
    public float maxFrequency = 55f;
    public float step = 0.5f;
    public Renderer[] markRenderers;
    public bool autoFindMarks = true;
    public Material inactiveMaterial;
    public Material activeMaterial;
    public bool hideWhenZero = true;
    public bool logDebug = false;
    public bool useEmissionOverride = true;
    public Color activeEmissionColor = Color.white;
    public float activeEmissionIntensity = 8f;
    public Color inactiveEmissionColor = Color.black;
    public float inactiveEmissionIntensity = 0f;

    private readonly List<float> markFrequencies = new List<float>();
    private MaterialPropertyBlock propertyBlock;
    private int activeIndex = -2;
    private bool lastUseEmissionOverride;
    private bool hasAppliedEmissionOverride;

    private void Awake()
    {
        BuildMarks();
        ApplyActiveIndex(-1);
    }

    private void Update()
    {
        if (sourceMeter == null || markRenderers == null || markRenderers.Length == 0)
        {
            return;
        }

        if (hideWhenZero && sourceMeter.current <= 0.01f)
        {
            ApplyActiveIndex(-1);
            return;
        }

        float clamped = Mathf.Clamp(sourceMeter.current, minFrequency, maxFrequency);
        float rounded = RoundToStep(clamped);
        ApplyActiveIndex(FindNearestMarkIndex(rounded));
    }

    private void BuildMarks()
    {
        if (autoFindMarks && (markRenderers == null || markRenderers.Length == 0))
        {
            AutoFindMarks();
        }

        if (markRenderers == null)
        {
            markRenderers = new Renderer[0];
        }

        if (markFrequencies.Count == markRenderers.Length)
        {
            return;
        }

        markFrequencies.Clear();
        for (int i = 0; i < markRenderers.Length; i++)
        {
            markFrequencies.Add(minFrequency + step * i);
        }
    }

    private void AutoFindMarks()
    {
        List<FrequencyMark> marks = new List<FrequencyMark>();
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == transform)
            {
                continue;
            }

            if (!TryParseFrequencyName(child.name, out float frequency))
            {
                continue;
            }

            if (frequency < minFrequency - 0.001f || frequency > maxFrequency + 0.001f)
            {
                continue;
            }

            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = child.GetComponentInChildren<Renderer>(true);
            }

            if (renderer == null)
            {
                continue;
            }

            marks.Add(new FrequencyMark(frequency, renderer));
        }

        marks.Sort((left, right) => left.Frequency.CompareTo(right.Frequency));

        markFrequencies.Clear();
        markRenderers = new Renderer[marks.Count];
        for (int i = 0; i < marks.Count; i++)
        {
            markFrequencies.Add(marks[i].Frequency);
            markRenderers[i] = marks[i].Renderer;
        }

        if (logDebug)
        {
            Debug.Log($"{nameof(Lab4FrequencyIndicatorView)} found {marks.Count} marks on {name}.", this);
        }
    }

    private bool TryParseFrequencyName(string objectName, out float frequency)
    {
        frequency = 0f;
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        string normalized = objectName;
        int parenthesisIndex = normalized.IndexOf('(');
        if (parenthesisIndex >= 0)
        {
            normalized = normalized.Substring(0, parenthesisIndex);
        }

        normalized = normalized.Trim().Replace(',', '.');
        return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out frequency);
    }

    private float RoundToStep(float value)
    {
        float safeStep = Mathf.Max(0.0001f, step);
        return Mathf.Round((value - minFrequency) / safeStep) * safeStep + minFrequency;
    }

    private int FindNearestMarkIndex(float frequency)
    {
        if (markFrequencies.Count == 0)
        {
            return -1;
        }

        int nearestIndex = 0;
        float nearestDistance = Mathf.Abs(markFrequencies[0] - frequency);

        for (int i = 1; i < markFrequencies.Count; i++)
        {
            float distance = Mathf.Abs(markFrequencies[i] - frequency);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void ApplyActiveIndex(int newActiveIndex)
    {
        if ((newActiveIndex == activeIndex && lastUseEmissionOverride == useEmissionOverride) || markRenderers == null)
        {
            return;
        }

        activeIndex = newActiveIndex;
        lastUseEmissionOverride = useEmissionOverride;
        for (int i = 0; i < markRenderers.Length; i++)
        {
            Renderer mark = markRenderers[i];
            if (mark == null)
            {
                continue;
            }

            bool isActive = i == activeIndex;
            Material material = isActive ? activeMaterial : inactiveMaterial;
            if (material != null)
            {
                mark.sharedMaterial = material;
            }

            if (useEmissionOverride)
            {
                ApplyEmissionOverride(mark, isActive);
            }
            else if (hasAppliedEmissionOverride)
            {
                ClearEmissionOverride(mark);
            }
        }

        hasAppliedEmissionOverride = useEmissionOverride;

        if (logDebug)
        {
            LogActiveMarkState();
        }
    }

    private void ApplyEmissionOverride(Renderer mark, bool isActive)
    {
        propertyBlock ??= new MaterialPropertyBlock();
        mark.GetPropertyBlock(propertyBlock);

        Color emissionColor = isActive
            ? activeEmissionColor * activeEmissionIntensity
            : inactiveEmissionColor * inactiveEmissionIntensity;

        propertyBlock.SetColor("_EmissionColor", emissionColor);
        mark.SetPropertyBlock(propertyBlock);
    }

    private void ClearEmissionOverride(Renderer mark)
    {
        mark.SetPropertyBlock(null);
    }

    private void LogActiveMarkState()
    {
        Renderer activeRenderer = activeIndex >= 0 && activeIndex < markRenderers.Length
            ? markRenderers[activeIndex]
            : null;
        string activeRendererName = activeRenderer != null ? activeRenderer.name : "none";
        string selectedFrequency = activeIndex >= 0 && activeIndex < markFrequencies.Count
            ? markFrequencies[activeIndex].ToString("0.###", CultureInfo.InvariantCulture)
            : "none";
        float current = sourceMeter != null ? sourceMeter.current : 0f;

        Debug.Log(
            $"{nameof(Lab4FrequencyIndicatorView)} {name}: marks={markRenderers.Length}, " +
            $"current={current:0.###}, selected={selectedFrequency}, activeRenderer={activeRendererName}, " +
            $"activeMaterial={(activeMaterial != null)}, inactiveMaterial={(inactiveMaterial != null)}, " +
            $"useEmissionOverride={useEmissionOverride}.",
            this);
    }

    private readonly struct FrequencyMark
    {
        public FrequencyMark(float frequency, Renderer renderer)
        {
            Frequency = frequency;
            Renderer = renderer;
        }

        public float Frequency { get; }
        public Renderer Renderer { get; }
    }
}
