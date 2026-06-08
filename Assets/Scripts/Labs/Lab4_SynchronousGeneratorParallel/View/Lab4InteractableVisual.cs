using UnityEngine;

public class Lab4InteractableVisual : MonoBehaviour
{
    public Renderer targetRenderer;
    public Color hoverColor = Color.yellow;
    public bool autoFindRenderer = true;

    private Color originalColor;
    private Vector3 originalScale;
    private bool hasOriginalColor;
    private bool isHovered;

    private void Awake()
    {
        ResolveRenderer();
        originalScale = transform.localScale;

        if (TryGetMaterialColor(out Color color))
        {
            originalColor = color;
            hasOriginalColor = true;
        }
    }

    private void OnMouseEnter()
    {
        isHovered = true;
        SetMaterialColor(hoverColor);
    }

    private void OnMouseExit()
    {
        isHovered = false;
        RestoreVisualState();
    }

    private void OnMouseDown()
    {
        transform.localScale = originalScale * 1.03f;
        Invoke(nameof(RestoreClickScale), 0.08f);
    }

    private void RestoreClickScale()
    {
        transform.localScale = originalScale;

        if (isHovered)
        {
            SetMaterialColor(hoverColor);
        }
    }

    private void RestoreVisualState()
    {
        transform.localScale = originalScale;

        if (hasOriginalColor)
        {
            SetMaterialColor(originalColor);
        }
    }

    private void ResolveRenderer()
    {
        if (targetRenderer != null || !autoFindRenderer)
        {
            return;
        }

        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private bool TryGetMaterialColor(out Color color)
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

    private void SetMaterialColor(Color color)
    {
        ResolveRenderer();

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
}
