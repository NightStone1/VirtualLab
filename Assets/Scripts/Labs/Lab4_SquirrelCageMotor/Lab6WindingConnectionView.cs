using UnityEngine;
using UnityEngine.Rendering;

public class Lab6WindingConnectionView : MonoBehaviour
{
    private enum ConnectionMode
    {
        None,
        Delta,
        Star,
        ResistanceMeasurement
    }

    [Header("Terminals C1-C6")]
    [SerializeField] private Transform c1;
    [SerializeField] private Transform c2;
    [SerializeField] private Transform c3;
    [SerializeField] private Transform c4;
    [SerializeField] private Transform c5;
    [SerializeField] private Transform c6;
    [SerializeField] private Transform neutralPoint;

    [Header("Wire Visuals")]
    [SerializeField] private Transform wireRoot;
    [SerializeField] private Material wireMaterial;
    [SerializeField] private Material resistanceWireMaterial;
    [SerializeField] private float wireWidth = 0.01f;
    [SerializeField] private Color fallbackWireColor = Color.yellow;
    [SerializeField] private Color fallbackResistanceWireColor = Color.cyan;

    private readonly WireLine[] deltaLines = new WireLine[3];
    private readonly WireLine[] starLines = new WireLine[3];
    private readonly WireLine[] resistanceLines = new WireLine[3];
    private ConnectionMode currentMode = ConnectionMode.None;
    private bool initialized;
    private Material runtimeWireMaterial;
    private Material runtimeResistanceWireMaterial;

    private void Awake()
    {
        EnsureInitialized();
        ShowNone();
    }

    private void LateUpdate()
    {
        if (currentMode != ConnectionMode.None)
        {
            UpdateActiveLinePositions();
        }
    }

    private void OnDestroy()
    {
        DestroyRuntimeMaterial(runtimeWireMaterial, wireMaterial);
        DestroyRuntimeMaterial(runtimeResistanceWireMaterial, resistanceWireMaterial);
    }

    public void ShowForStage(Lab6Stage stage)
    {
        switch (stage)
        {
            case Lab6Stage.NoLoad:
            case Lab6Stage.ShortCircuit:
                ShowDelta();
                break;
            case Lab6Stage.Load:
                ShowStar();
                break;
            case Lab6Stage.ResistanceMeasurement:
                ShowResistanceMeasurement();
                break;
            default:
                ShowNone();
                break;
        }
    }

    public void ShowNone()
    {
        EnsureInitialized();
        if (currentMode == ConnectionMode.None)
        {
            return;
        }

        currentMode = ConnectionMode.None;
        SetLinesActive(deltaLines, false);
        SetLinesActive(starLines, false);
        SetLinesActive(resistanceLines, false);
    }

    public void ShowDelta()
    {
        ShowMode(ConnectionMode.Delta);
    }

    public void ShowStar()
    {
        ShowMode(ConnectionMode.Star);
    }

    public void ShowResistanceMeasurement()
    {
        ShowMode(ConnectionMode.ResistanceMeasurement);
    }

    private void ShowMode(ConnectionMode mode)
    {
        EnsureInitialized();
        if (currentMode == mode)
        {
            UpdateActiveLinePositions();
            return;
        }

        currentMode = mode;
        SetLinesActive(deltaLines, mode == ConnectionMode.Delta);
        SetStarLinesActive(mode == ConnectionMode.Star);
        SetLinesActive(resistanceLines, mode == ConnectionMode.ResistanceMeasurement);
        UpdateActiveLinePositions();
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        Transform root = wireRoot != null ? wireRoot : transform;
        Material defaultWireMaterial = GetWireMaterial(false);
        Material defaultResistanceMaterial = GetWireMaterial(true);

        CreateLine(deltaLines, 0, root, "Delta_C1_C6", c1, c6, defaultWireMaterial);
        CreateLine(deltaLines, 1, root, "Delta_C2_C4", c2, c4, defaultWireMaterial);
        CreateLine(deltaLines, 2, root, "Delta_C3_C5", c3, c5, defaultWireMaterial);

        if (neutralPoint != null)
        {
            CreateLine(starLines, 0, root, "Star_C4_N", c4, neutralPoint, defaultWireMaterial);
            CreateLine(starLines, 1, root, "Star_C5_N", c5, neutralPoint, defaultWireMaterial);
            CreateLine(starLines, 2, root, "Star_C6_N", c6, neutralPoint, defaultWireMaterial);
        }
        else
        {
            CreateLine(starLines, 0, root, "Star_C4_C5", c4, c5, defaultWireMaterial);
            CreateLine(starLines, 1, root, "Star_C5_C6", c5, c6, defaultWireMaterial);
            CreateLine(starLines, 2, root, "Star_Unused", null, null, defaultWireMaterial);
        }

        CreateLine(resistanceLines, 0, root, "Resistance_C1_C4", c1, c4, defaultResistanceMaterial);
        CreateLine(resistanceLines, 1, root, "Resistance_C2_C5", c2, c5, defaultResistanceMaterial);
        CreateLine(resistanceLines, 2, root, "Resistance_C3_C6", c3, c6, defaultResistanceMaterial);

        SetLinesActive(deltaLines, false);
        SetLinesActive(starLines, false);
        SetLinesActive(resistanceLines, false);
    }

    private void CreateLine(WireLine[] lines, int index, Transform root, string name, Transform start, Transform end, Material material)
    {
        GameObject lineObject = new GameObject(name, typeof(LineRenderer));
        lineObject.transform.SetParent(root, false);

        LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier = wireWidth;
        lineRenderer.startWidth = wireWidth;
        lineRenderer.endWidth = wireWidth;
        lineRenderer.numCapVertices = 6;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        if (material != null)
        {
            lineRenderer.material = material;
        }

        lines[index] = new WireLine(lineObject, lineRenderer, start, end);
    }

    private Material GetWireMaterial(bool resistance)
    {
        Material assignedMaterial = resistance ? resistanceWireMaterial : wireMaterial;
        if (assignedMaterial != null)
        {
            return assignedMaterial;
        }

        Material runtimeMaterial = resistance ? runtimeResistanceWireMaterial : runtimeWireMaterial;
        if (runtimeMaterial != null)
        {
            return runtimeMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        if (shader == null)
        {
            return null;
        }

        runtimeMaterial = new Material(shader)
        {
            color = resistance ? fallbackResistanceWireColor : fallbackWireColor
        };

        if (resistance)
        {
            runtimeResistanceWireMaterial = runtimeMaterial;
        }
        else
        {
            runtimeWireMaterial = runtimeMaterial;
        }

        return runtimeMaterial;
    }

    private void UpdateActiveLinePositions()
    {
        switch (currentMode)
        {
            case ConnectionMode.Delta:
                UpdateLinePositions(deltaLines);
                break;
            case ConnectionMode.Star:
                UpdateLinePositions(starLines);
                break;
            case ConnectionMode.ResistanceMeasurement:
                UpdateLinePositions(resistanceLines);
                break;
        }
    }

    private void SetStarLinesActive(bool active)
    {
        for (int i = 0; i < starLines.Length; i++)
        {
            bool lineActive = active && (neutralPoint != null || i < 2);
            SetLineActive(starLines[i], lineActive);
        }
    }

    private static void SetLinesActive(WireLine[] lines, bool active)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            SetLineActive(lines[i], active);
        }
    }

    private static void SetLineActive(WireLine line, bool active)
    {
        if (line != null && line.GameObject != null)
        {
            line.GameObject.SetActive(active && line.Start != null && line.End != null);
        }
    }

    private static void UpdateLinePositions(WireLine[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            WireLine line = lines[i];
            if (line == null || line.LineRenderer == null || line.Start == null || line.End == null)
            {
                continue;
            }

            line.LineRenderer.SetPosition(0, line.Start.position);
            line.LineRenderer.SetPosition(1, line.End.position);
        }
    }

    private static void DestroyRuntimeMaterial(Material runtimeMaterial, Material assignedMaterial)
    {
        if (runtimeMaterial != null && runtimeMaterial != assignedMaterial)
        {
            Destroy(runtimeMaterial);
        }
    }

    private sealed class WireLine
    {
        public readonly GameObject GameObject;
        public readonly LineRenderer LineRenderer;
        public readonly Transform Start;
        public readonly Transform End;

        public WireLine(GameObject gameObject, LineRenderer lineRenderer, Transform start, Transform end)
        {
            GameObject = gameObject;
            LineRenderer = lineRenderer;
            Start = start;
            End = end;
        }
    }
}
