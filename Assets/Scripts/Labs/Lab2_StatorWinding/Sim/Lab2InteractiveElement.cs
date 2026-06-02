using System.Collections;
using UnityEngine;

public class Lab2InteractiveElement : MonoBehaviour
{
    public enum ElementType
    {
        Q1,
        Q2,
        StartButton,
        StopButton
    }

    [SerializeField] private ElementType elementType;
    [SerializeField] private Lab2CircuitController controller;
    [SerializeField] private Transform visualTarget;
    [SerializeField] private float clickRadius = 0.0007f;
    [SerializeField] private Vector3 switchRotationAxis = Vector3.right;
    [SerializeField] private float switchOnAngle = -20f;
    [SerializeField] private float switchOffAngle = 20f;
    [SerializeField] private Vector3 buttonPressDirection = Vector3.back;
    [SerializeField] private float buttonPressDistance = 0.0005f;
    [SerializeField] private float buttonPressDuration = 0.06f;
    [SerializeField] private Color enabledColor = new(0.35f, 1f, 0.35f, 1f);
    [SerializeField] private Color pressedColor = new(1f, 0.85f, 0.2f, 1f);

    private Renderer[] renderers;
    private Material[][] materials;
    private Color[][] normalColors;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalPosition;
    private Coroutine pressAnimation;

    public ElementType Type => elementType;

    public void Initialize(
        ElementType type,
        Lab2CircuitController owner,
        Transform target,
        float colliderRadius,
        Vector3 switchAxis,
        float onAngle,
        float offAngle,
        Vector3 pressDirection,
        float pressDistance,
        float pressDuration)
    {
        elementType = type;
        controller = owner;
        visualTarget = target != null ? target : transform;
        clickRadius = colliderRadius;
        switchRotationAxis = switchAxis;
        switchOnAngle = onAngle;
        switchOffAngle = offAngle;
        buttonPressDirection = pressDirection;
        buttonPressDistance = pressDistance;
        buttonPressDuration = pressDuration;
        CacheVisualState();
        EnsureCollider();
    }

    private void Awake()
    {
        if (visualTarget == null)
            visualTarget = transform;

        CacheVisualState();
        EnsureCollider();
    }

    private void OnMouseDown()
    {
        if (controller != null)
            controller.HandleInteractiveElementClick(elementType);
    }

    public void SetSwitchState(bool enabled)
    {
        ApplySwitchRotation(enabled);
        SetColor(enabled ? enabledColor : default, enabled);
    }

    private void ApplySwitchRotation(bool enabled)
    {
        Vector3 axis = switchRotationAxis.sqrMagnitude > 0f ? switchRotationAxis.normalized : Vector3.right;
        float angle = enabled ? switchOnAngle : switchOffAngle;
        visualTarget.localRotation = initialLocalRotation * Quaternion.AngleAxis(angle, axis);
    }

    public void ResetVisualState()
    {
        if (pressAnimation != null)
        {
            StopCoroutine(pressAnimation);
            pressAnimation = null;
        }

        visualTarget.localPosition = initialLocalPosition;
        visualTarget.localRotation = initialLocalRotation;

        if (elementType == ElementType.Q1 || elementType == ElementType.Q2)
            ApplySwitchRotation(false);

        SetColor(default, false);
    }

    public void PlayPressFeedback()
    {
        if (pressAnimation != null)
            StopCoroutine(pressAnimation);

        pressAnimation = StartCoroutine(AnimatePress());
    }

    private IEnumerator AnimatePress()
    {
        Vector3 direction = buttonPressDirection.sqrMagnitude > 0f ? buttonPressDirection.normalized : Vector3.back;
        Vector3 pressedPosition = initialLocalPosition + direction * buttonPressDistance;
        SetColor(pressedColor, true);

        yield return MoveButton(initialLocalPosition, pressedPosition, buttonPressDuration);
        yield return MoveButton(pressedPosition, initialLocalPosition, buttonPressDuration);

        SetColor(default, false);
        pressAnimation = null;
    }

    private IEnumerator MoveButton(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            visualTarget.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }

        visualTarget.localPosition = to;
    }

    private void CacheVisualState()
    {
        if (visualTarget == null)
            visualTarget = transform;

        initialLocalRotation = visualTarget.localRotation;
        initialLocalPosition = visualTarget.localPosition;

        if (renderers == null || renderers.Length == 0)
            renderers = visualTarget.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
            return;

        materials = new Material[renderers.Length][];
        normalColors = new Color[renderers.Length][];

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer currentRenderer = renderers[rendererIndex];

            if (currentRenderer == null)
            {
                materials[rendererIndex] = System.Array.Empty<Material>();
                normalColors[rendererIndex] = System.Array.Empty<Color>();
                continue;
            }

            Material[] rendererMaterials = currentRenderer.materials;
            materials[rendererIndex] = rendererMaterials;
            normalColors[rendererIndex] = new Color[rendererMaterials.Length];

            for (int materialIndex = 0; materialIndex < rendererMaterials.Length; materialIndex++)
                normalColors[rendererIndex][materialIndex] = rendererMaterials[materialIndex] != null && rendererMaterials[materialIndex].HasProperty("_Color")
                    ? rendererMaterials[materialIndex].color
                    : Color.white;
        }
    }

    private void SetColor(Color color, bool overrideColor)
    {
        if (materials == null || normalColors == null)
            return;

        for (int rendererIndex = 0; rendererIndex < materials.Length; rendererIndex++)
        {
            for (int materialIndex = 0; materialIndex < materials[rendererIndex].Length; materialIndex++)
            {
                Material material = materials[rendererIndex][materialIndex];

                if (material == null || !material.HasProperty("_Color"))
                    continue;

                material.color = overrideColor ? color : normalColors[rendererIndex][materialIndex];
            }
        }
    }

    private void EnsureCollider()
    {
        Collider[] colliders = GetComponents<Collider>();
        SphereCollider sphereCollider = null;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] is SphereCollider currentSphere)
            {
                sphereCollider = currentSphere;
                continue;
            }

            colliders[i].enabled = false;
        }

        if (sphereCollider == null)
            sphereCollider = gameObject.AddComponent<SphereCollider>();

        sphereCollider.isTrigger = true;
        sphereCollider.radius = clickRadius;
    }
}
