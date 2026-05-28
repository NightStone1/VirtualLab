using UnityEngine;

public class Lab2Terminal : MonoBehaviour
{
    [SerializeField] private Lab2TerminalId terminalId;
    [SerializeField] private Lab2CircuitController controller;
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.05f, 1f);
    [SerializeField] private float markerScale = 0.08f;

    public Lab2TerminalId TerminalId => terminalId;

    private void Awake()
    {
        EnsureMarker();

        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();
    }

    private void EnsureMarker()
    {
        if (!TryGetComponent(out Collider _))
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = markerScale * 0.6f;
        }

        if (GetComponentInChildren<Renderer>() != null)
            return;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "Marker";
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = Vector3.one * markerScale;

        if (marker.TryGetComponent(out Collider markerCollider))
        {
            markerCollider.enabled = false;
            Destroy(markerCollider);
        }
    }

    private void Start()
    {
        SetSelected(false);
    }

    private void OnMouseDown()
    {
        if (controller == null)
        {
            Debug.LogWarning($"Lab2Terminal {terminalId}: controller is not assigned.");
            return;
        }

        controller.SelectTerminal(this);
    }

    public void SetSelected(bool selected)
    {
        if (highlightRenderers == null)
            return;

        Color color = selected ? selectedColor : normalColor;

        for (int i = 0; i < highlightRenderers.Length; i++)
        {
            if (highlightRenderers[i] != null)
                highlightRenderers[i].material.color = color;
        }
    }
}
