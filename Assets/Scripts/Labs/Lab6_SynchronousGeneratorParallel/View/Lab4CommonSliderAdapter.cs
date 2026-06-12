using UnityEngine;
using System.Reflection;

public enum Lab4SliderTarget
{
    R1Excitation,
    DriveRegulator
}

public class Lab4CommonSliderAdapter : MonoBehaviour
{
    public Lab4SliderTarget target;
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;
    public bool logDebug = true;
    public Slider sliderSource;
    public Rotator rotatorSource;
    public bool autoFindSource = true;
    public float sourceMin = 0f;
    public float sourceMax = 1f;

    private float lastNormalized = -1f;
    private bool hasSliderSubscription;
    private bool hasRotatorSubscription;
    private bool sourceWarningShown;
    private static readonly FieldInfo SliderPercentBackingField = typeof(Slider).GetField("<Percent>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private void Awake()
    {
        ResolveController();
        ResolveSource();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (!hasSliderSubscription && !hasRotatorSubscription && (sliderSource != null || rotatorSource != null))
        {
            PushCurrentValue();
        }
    }

    private void Subscribe()
    {
        ResolveSource();

        if (sliderSource != null && !hasSliderSubscription)
        {
            sliderSource.OnValueChanged += HandleSliderValueChanged;
            hasSliderSubscription = true;
        }

        if (rotatorSource != null && !hasRotatorSubscription)
        {
            rotatorSource.OnValueChanged += HandleRotatorValueChanged;
            hasRotatorSubscription = true;
        }
    }

    private void Unsubscribe()
    {
        if (sliderSource != null && hasSliderSubscription)
        {
            sliderSource.OnValueChanged -= HandleSliderValueChanged;
        }

        if (rotatorSource != null && hasRotatorSubscription)
        {
            rotatorSource.OnValueChanged -= HandleRotatorValueChanged;
        }

        hasSliderSubscription = false;
        hasRotatorSubscription = false;
    }

    private void HandleSliderValueChanged(float percent)
    {
        ApplyNormalized(Mathf.Clamp01(percent / 100f));
    }

    private void HandleRotatorValueChanged(float value)
    {
        ApplyNormalized(NormalizeRotatorValue(value));
    }

    private void PushCurrentValue()
    {
        ResolveController();
        ResolveSource();

        if (sliderSource != null)
        {
            ApplyNormalized(Mathf.Clamp01(sliderSource.Percent / 100f));
            return;
        }

        if (rotatorSource != null)
        {
            float rawValue = rotatorSource.isLLR ? rotatorSource.llrValue : rotatorSource.value;
            ApplyNormalized(NormalizeRotatorValue(rawValue));
            return;
        }

        if (logDebug && !sourceWarningShown)
        {
            Debug.LogWarning("Lab4 slider adapter source is not assigned.", this);
            sourceWarningShown = true;
        }
    }

    private void ApplyNormalized(float normalized)
    {
        ResolveController();

        normalized = Mathf.Clamp01(normalized);
        if (Mathf.Approximately(normalized, lastNormalized))
        {
            return;
        }

        lastNormalized = normalized;

        if (controller == null)
        {
            Debug.LogWarning($"Lab4 slider adapter skipped: controller not found for {target}.", this);
            return;
        }

        switch (target)
        {
            case Lab4SliderTarget.R1Excitation:
                controller.SetR1Normalized(normalized);
                break;
            case Lab4SliderTarget.DriveRegulator:
                controller.SetDriveRegulatorNormalized(normalized);
                break;
        }

        if (logDebug)
        {
            Debug.Log($"Lab4 slider value applied: {target} = {normalized:0.00}", this);
        }
    }

    public void SetValueFromController(float normalized)
    {
        ResolveSource();

        normalized = Mathf.Clamp01(normalized);
        lastNormalized = normalized;

        if (sliderSource != null)
        {
            SetSliderSourceValue(normalized);
        }

        if (rotatorSource != null)
        {
            SetRotatorSourceValue(normalized);
        }
    }

    private float NormalizeRotatorValue(float value)
    {
        if (rotatorSource != null)
        {
            return Mathf.Clamp01(rotatorSource.isLLR ? value / 250f : value / 100f);
        }

        if (Mathf.Approximately(sourceMin, sourceMax))
        {
            return 0f;
        }

        return Mathf.InverseLerp(sourceMin, sourceMax, value);
    }

    private void SetSliderSourceValue(float normalized)
    {
        float percent = Mathf.Clamp01(normalized) * 100f;
        float visualPercent = sliderSource.inverted ? 100f - percent : percent;
        float y = Mathf.Lerp(sliderSource.minY, sliderSource.maxY, visualPercent / 100f);
        Transform sourceTransform = sliderSource.transform;
        sourceTransform.position = new Vector3(sourceTransform.position.x, y, sourceTransform.position.z);

        if (SliderPercentBackingField != null)
        {
            SliderPercentBackingField.SetValue(sliderSource, percent);
        }
    }

    private void SetRotatorSourceValue(float normalized)
    {
        float t = Mathf.Clamp01(normalized);
        if (rotatorSource.isLLR)
        {
            rotatorSource.llrValue = t * 250f;
        }
        else
        {
            rotatorSource.value = t * 100f;
        }

        float angle = AngleOnArc(rotatorSource.startAngle, rotatorSource.endAngle, t, rotatorSource.useLongArc);
        rotatorSource.transform.localRotation = Quaternion.Euler(
            rotatorSource.baseEuler.x + angle,
            rotatorSource.baseEuler.y,
            rotatorSource.baseEuler.z);
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
            delta = cw > ccw ? -ccw : cw;
            if (Mathf.Abs(delta) < 180f)
            {
                delta = delta >= 0f ? delta - 360f : delta + 360f;
            }
        }
        else
        {
            delta = Mathf.DeltaAngle(from, to);
        }

        return Normalize180(from + delta * t);
    }

    private float ClockwiseDistance(float from, float to)
    {
        from = Normalize360(from);
        to = Normalize360(to);

        float distance = to - from;
        if (distance < 0f)
        {
            distance += 360f;
        }

        return distance;
    }

    private float Normalize360(float angle)
    {
        angle %= 360f;
        if (angle < 0f)
        {
            angle += 360f;
        }

        return angle;
    }

    private float Normalize180(float angle)
    {
        angle = Normalize360(angle);
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    private void ResolveController()
    {
        if (controller != null || !autoFindController)
        {
            return;
        }

        controller = Object.FindFirstObjectByType<SyncGeneratorLabController>();
    }

    private void ResolveSource()
    {
        if (!autoFindSource)
        {
            return;
        }

        if (sliderSource == null)
        {
            sliderSource = GetComponent<Slider>();
        }

        if (rotatorSource == null)
        {
            rotatorSource = GetComponent<Rotator>();
        }
    }
}
