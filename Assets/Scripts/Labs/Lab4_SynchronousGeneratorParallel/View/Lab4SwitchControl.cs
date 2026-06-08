using System.Collections;
using UnityEngine;

public enum Lab4SwitchTarget
{
    Q1,
    Q2,
    Q3,
    Q4,
    Q5
}

public class Lab4SwitchControl : MonoBehaviour
{
    [Header("Lab4 target")]
    public Lab4SwitchTarget target;
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;
    public bool syncFromController = true;
    public bool animateExternalStateChanges = true;

    [Header("Switch rotation")]
    public Vector3 onEuler;
    public Vector3 offEuler;
    public float rotationSpeed = 2f;
    public bool isOn;
    public bool applyRotationOnStart = true;

    [Header("Switch color")]
    public Renderer switchColorRenderer;
    public GameObject circleObject;
    public bool autoFindSwitchRenderer;
    public Color onColor = Color.green;
    public Color offColor = Color.red;

    public event System.Action<bool> OnValueChanged;

    private Renderer circleRenderer;
    private Renderer switchRenderer;
    private bool isAnimating;
    private bool controllerWarningShown;

    private void Start()
    {
        if (switchColorRenderer != null)
        {
            switchRenderer = switchColorRenderer;
        }
        else if (autoFindSwitchRenderer)
        {
            switchRenderer = GetComponent<Renderer>();
        }

        if (circleObject != null)
        {
            circleRenderer = circleObject.GetComponent<Renderer>();
        }

        ResolveController();

        if (syncFromController && TryGetControllerState(out bool controllerState))
        {
            isOn = controllerState;
        }

        if (applyRotationOnStart)
        {
            transform.localRotation = Quaternion.Euler(isOn ? onEuler : offEuler);
        }

        SetAllColors(isOn ? onColor : offColor);
    }

    private void Update()
    {
        if (!syncFromController || isAnimating)
        {
            return;
        }

        if (TryGetControllerState(out bool controllerState) && controllerState != isOn)
        {
            SetSwitchState(controllerState, animateExternalStateChanges);
        }
    }

    private void OnMouseDown()
    {
        if (isAnimating)
        {
            return;
        }

        bool nextState = !isOn;

        if (ExecuteControllerAction() && syncFromController && TryGetControllerState(out bool controllerState))
        {
            nextState = controllerState;
        }

        SetSwitchState(nextState, true);
    }

    public void SetSwitchState(bool on, bool animate)
    {
        if (on == isOn && !isAnimating)
        {
            return;
        }

        if (animate)
        {
            StartCoroutine(RotateSwitch(on));
            return;
        }

        isOn = on;
        transform.localRotation = Quaternion.Euler(isOn ? onEuler : offEuler);
        SetAllColors(isOn ? onColor : offColor);
        OnValueChanged?.Invoke(isOn);
    }

    private IEnumerator RotateSwitch(bool toOn)
    {
        isAnimating = true;

        Quaternion startRotation = transform.localRotation;
        Quaternion endRotation = Quaternion.Euler(toOn ? onEuler : offEuler);

        Color startColor = GetCurrentColor();
        Color endColor = toOn ? onColor : offColor;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            transform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);

            Color currentColor = Color.Lerp(startColor, endColor, t);
            SetAllColors(currentColor);

            yield return null;
        }

        isOn = toOn;
        transform.localRotation = endRotation;
        SetAllColors(endColor);

        OnValueChanged?.Invoke(toOn);
        isAnimating = false;
    }

    private bool ExecuteControllerAction()
    {
        ResolveController();

        if (controller == null)
        {
            if (!controllerWarningShown)
            {
                Debug.LogWarning($"Lab4 switch skipped: controller not found for {target}.", this);
                controllerWarningShown = true;
            }

            return false;
        }

        switch (target)
        {
            case Lab4SwitchTarget.Q1:
                controller.ToggleQ1();
                break;
            case Lab4SwitchTarget.Q2:
                controller.ToggleQ2();
                break;
            case Lab4SwitchTarget.Q3:
                controller.ToggleQ3();
                break;
            case Lab4SwitchTarget.Q4:
                controller.ToggleQ4();
                break;
            case Lab4SwitchTarget.Q5:
                controller.ToggleQ5();
                break;
        }

        return true;
    }

    private bool TryGetControllerState(out bool state)
    {
        ResolveController();

        state = false;
        if (controller == null)
        {
            return false;
        }

        switch (target)
        {
            case Lab4SwitchTarget.Q1:
                state = controller.IsQ1Enabled;
                return true;
            case Lab4SwitchTarget.Q2:
                state = controller.IsQ2Enabled;
                return true;
            case Lab4SwitchTarget.Q3:
                state = controller.IsQ3Enabled;
                return true;
            case Lab4SwitchTarget.Q4:
                state = controller.IsQ4Enabled;
                return true;
            case Lab4SwitchTarget.Q5:
                state = controller.IsQ5Enabled;
                return true;
            default:
                return false;
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

    private void SetAllColors(Color color)
    {
        SetRendererColor(switchRenderer, color);
        SetRendererColor(circleRenderer, color);
    }

    private Color GetCurrentColor()
    {
        if (TryGetRendererColor(switchRenderer, out Color switchColor))
        {
            return switchColor;
        }

        if (TryGetRendererColor(circleRenderer, out Color circleColor))
        {
            return circleColor;
        }

        return Color.white;
    }

    private void SetRendererColor(Renderer targetRenderer, Color color)
    {
        if (targetRenderer == null || targetRenderer.material == null)
        {
            return;
        }

        Material material = targetRenderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
            return;
        }

        if (material.HasProperty("_Color"))
        {
            material.color = color;
        }
    }

    private bool TryGetRendererColor(Renderer targetRenderer, out Color color)
    {
        color = Color.white;

        if (targetRenderer == null || targetRenderer.material == null)
        {
            return false;
        }

        Material material = targetRenderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            color = material.GetColor("_BaseColor");
            return true;
        }

        if (material.HasProperty("_Color"))
        {
            color = material.color;
            return true;
        }

        return false;
    }
}
