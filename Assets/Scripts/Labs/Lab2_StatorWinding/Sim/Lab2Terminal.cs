using UnityEngine;

public class Lab2Terminal : MonoBehaviour
{
    [SerializeField] private Lab2TerminalId terminalId;
    [SerializeField] private Lab2CircuitController controller;
    [SerializeField] private Transform connectionAnchor;
    [SerializeField] private Transform clickArea;
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.05f, 1f);
    [SerializeField] private Vector3 autoAnchorLocalOffset = new(0f, 0f, -0.05f);
    [SerializeField] private Vector3 clickAreaLocalOffset = Vector3.zero;
    [SerializeField] private float clickAreaRadius = 0.0002f;

    private Material[][] highlightMaterials;
    private Color[][] normalColors;

    public Lab2TerminalId TerminalId => terminalId;
    public Transform ConnectionAnchor => EnsureConnectionAnchor();
    public Vector3 ConnectionPosition => ConnectionAnchor.position;
    public Vector3 VisualConnectionPosition => GetVisualConnectionPosition();

    private void Awake()
    {
        EnsureClickArea();
        EnsureConnectionAnchor();

        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();

        CacheNormalColors();
    }

    public void Initialize(Lab2TerminalId id, Lab2CircuitController owner)
    {
        terminalId = id;
        controller = owner;
        EnsureClickArea();
        EnsureConnectionAnchor();

        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();

        CacheNormalColors();
    }

    private void EnsureClickArea()
    {
        if (clickArea == null)
        {
            string clickAreaName = GetClickAreaName();
            Transform existingClickArea = transform.Find(clickAreaName);

            if (existingClickArea != null)
            {
                clickArea = existingClickArea;
            }
            else
            {
                GameObject clickAreaObject = new(clickAreaName);
                clickAreaObject.transform.SetParent(transform, false);
                clickAreaObject.transform.localPosition = clickAreaLocalOffset;
                clickAreaObject.transform.localRotation = Quaternion.identity;
                clickAreaObject.transform.localScale = Vector3.one;
                clickArea = clickAreaObject.transform;
            }
        }

        if (!clickArea.TryGetComponent(out SphereCollider sphereCollider))
            sphereCollider = clickArea.gameObject.AddComponent<SphereCollider>();

        sphereCollider.radius = clickAreaRadius;
        sphereCollider.isTrigger = true;

        if (!clickArea.TryGetComponent(out Lab2TerminalClickArea clickProxy))
            clickProxy = clickArea.gameObject.AddComponent<Lab2TerminalClickArea>();

        clickProxy.Initialize(this);
    }

    private Transform EnsureConnectionAnchor()
    {
        if (connectionAnchor != null)
            return connectionAnchor;

        string anchorName = GetAnchorName();
        Transform existingAnchor = transform.Find(anchorName);

        if (existingAnchor != null)
        {
            connectionAnchor = existingAnchor;
            return connectionAnchor;
        }

        GameObject anchorObject = new(anchorName);
        anchorObject.transform.SetParent(transform, false);
        anchorObject.transform.localPosition = autoAnchorLocalOffset;
        anchorObject.transform.localRotation = Quaternion.identity;
        anchorObject.transform.localScale = Vector3.one;

        connectionAnchor = anchorObject.transform;
        return connectionAnchor;
    }

    private string GetAnchorName()
    {
        return terminalId == Lab2TerminalId.None
            ? "Anchor"
            : $"Anchor_{terminalId}";
    }

    private string GetClickAreaName()
    {
        return terminalId == Lab2TerminalId.None
            ? "ClickArea"
            : $"ClickArea_{terminalId}";
    }

    private Vector3 GetVisualConnectionPosition()
    {
        if (clickArea != null)
            return clickArea.position;

        if (connectionAnchor != null)
            return connectionAnchor.position;

        return transform.position;
    }

    private void Start()
    {
        SetSelected(false);
    }

    public void HandleClick()
    {
        if (controller == null)
        {
            Debug.LogWarning($"Lab2Terminal {terminalId}: controller is not assigned.");
            return;
        }

        controller.SelectTerminal(this);
    }

    private void OnMouseDown()
    {
        HandleClick();
    }

    public void SetSelected(bool selected)
    {
        if (highlightMaterials == null || normalColors == null)
            CacheNormalColors();

        if (highlightMaterials == null || normalColors == null)
            return;

        for (int rendererIndex = 0; rendererIndex < highlightMaterials.Length; rendererIndex++)
        {
            Material[] materials = highlightMaterials[rendererIndex];
            Color[] colors = normalColors[rendererIndex];

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                if (materials[materialIndex] == null || !materials[materialIndex].HasProperty("_Color"))
                    continue;

                materials[materialIndex].color = selected ? selectedColor : colors[materialIndex];
            }
        }
    }

    private void CacheNormalColors()
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0)
            return;

        highlightMaterials = new Material[highlightRenderers.Length][];
        normalColors = new Color[highlightRenderers.Length][];

        for (int rendererIndex = 0; rendererIndex < highlightRenderers.Length; rendererIndex++)
        {
            Renderer currentRenderer = highlightRenderers[rendererIndex];

            if (currentRenderer == null)
            {
                highlightMaterials[rendererIndex] = System.Array.Empty<Material>();
                normalColors[rendererIndex] = System.Array.Empty<Color>();
                continue;
            }

            Material[] materials = currentRenderer.materials;
            highlightMaterials[rendererIndex] = materials;
            normalColors[rendererIndex] = new Color[materials.Length];

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                normalColors[rendererIndex][materialIndex] = materials[materialIndex] != null && materials[materialIndex].HasProperty("_Color")
                    ? materials[materialIndex].color
                    : Color.white;
            }
        }
    }
}
