using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab1GraphView : MonoBehaviour
{
    private const int RequiredRows = 5;
    private const float GraphPadding = 20f;

    private enum GraphKind
    {
        IaByP2,
        MByP2,
        OmegaByP2,
        EtaByP2,
        OmegaByU,
        IfByIa,
        OmegaByIf
    }

    [SerializeField] private LabResultsManager resultsManager;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private readonly GraphKind[] graphOrder =
    {
        GraphKind.IaByP2,
        GraphKind.MByP2,
        GraphKind.OmegaByP2,
        GraphKind.EtaByP2,
        GraphKind.OmegaByU,
        GraphKind.IfByIa,
        GraphKind.OmegaByIf
    };

    private RectTransform graphArea;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI legendText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI counterText;
    private int currentGraphIndex;

    public void Initialize(LabResultsManager manager)
    {
        resultsManager = manager;
        BuildRuntimeUi();
        Refresh(true);
    }

    public void Refresh(bool forceRebuild = false)
    {
        if (resultsManager == null)
        {
            SetText(statusText, "Графики недоступны: LabResultsManager не найден.");
            ClearGraph();
            return;
        }

        GraphKind kind = graphOrder[Mathf.Clamp(currentGraphIndex, 0, graphOrder.Length - 1)];
        IReadOnlyList<Vector2> points = BuildPoints(kind, out string title, out string legend, out int rowCount);

        SetText(titleText, title);
        SetText(legendText, legend);
        SetText(counterText, $"{currentGraphIndex + 1}/{graphOrder.Length}");

        if (rowCount < RequiredRows)
        {
            ClearGraph();
            SetText(statusText, $"Для построения заполните таблицу: {rowCount}/{RequiredRows} точек.");
            return;
        }

        SetText(statusText, string.Empty);
        DrawGraph(points);
    }

    public void ResetToFirstGraph()
    {
        currentGraphIndex = 0;
        Refresh(true);
    }

    private void ShowPreviousGraph()
    {
        currentGraphIndex = (currentGraphIndex - 1 + graphOrder.Length) % graphOrder.Length;
        Refresh(true);
    }

    private void ShowNextGraph()
    {
        currentGraphIndex = (currentGraphIndex + 1) % graphOrder.Length;
        Refresh(true);
    }

    private void BuildRuntimeUi()
    {
        if (graphArea != null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        Transform parent = canvas != null ? canvas.transform : transform;

        RectTransform panel = gameObject.GetComponent<RectTransform>();
        if (panel == null)
        {
            panel = gameObject.AddComponent<RectTransform>();
        }

        transform.SetParent(parent, false);
        gameObject.name = "Lab1GraphPanelRuntime";
        panel.anchorMin = new Vector2(1f, 0.5f);
        panel.anchorMax = new Vector2(1f, 0.5f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.sizeDelta = new Vector2(360f, 300f);
        panel.anchoredPosition = new Vector2(-24f, -40f);

        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.color = new Color(0.05f, 0.07f, 0.1f, 0.86f);

        titleText = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-16f, 30f), 18f, TextAlignmentOptions.Center);
        legendText = CreateText("Legend", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(-16f, 26f), 12f, TextAlignmentOptions.Center);
        statusText = CreateText("Status", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-16f, 28f), 12f, TextAlignmentOptions.Center);
        counterText = CreateText("Counter", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(80f, 24f), 12f, TextAlignmentOptions.Center);

        graphArea = CreateGraphArea(panel);
        CreateButton("PrevButton", panel, "<", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, 32f), ShowPreviousGraph);
        CreateButton("NextButton", panel, ">", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 32f), ShowNextGraph);
    }

    private RectTransform CreateGraphArea(RectTransform parent)
    {
        GameObject areaObject = new GameObject("GraphArea", typeof(RectTransform), typeof(Image));
        areaObject.transform.SetParent(parent, false);

        RectTransform rect = areaObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(18f, 68f);
        rect.offsetMax = new Vector2(-18f, -70f);

        Image image = areaObject.GetComponent<Image>();
        image.color = new Color(0.9f, 0.94f, 1f, 0.12f);

        return rect;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void CreateButton(string name, RectTransform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(38f, 26f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(action);

        TextMeshProUGUI text = CreateText("Text", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 16f, TextAlignmentOptions.Center);
        text.text = label;
        text.color = Color.black;
    }

    private IReadOnlyList<Vector2> BuildPoints(GraphKind kind, out string title, out string legend, out int rowCount)
    {
        List<Vector2> points = new List<Vector2>();
        switch (kind)
        {
            case GraphKind.IaByP2:
                title = "Ia = f(P2)";
                legend = "X: P2, Вт; Y: Ia, А";
                IReadOnlyList<Table22Row> iaRows = resultsManager.GetTable22RowsForGraphs();
                rowCount = iaRows.Count;
                AddPoints(iaRows, points, row => row.P2d, row => row.Iaq);
                return points;

            case GraphKind.MByP2:
                title = "M = f(P2)";
                legend = "X: P2, Вт; Y: M, Н*м";
                IReadOnlyList<Table22Row> mRows = resultsManager.GetTable22RowsForGraphs();
                rowCount = mRows.Count;
                AddPoints(mRows, points, row => row.P2d, row => row.M2d);
                return points;

            case GraphKind.OmegaByP2:
                title = "ω = f(P2)";
                legend = "X: P2, Вт; Y: ω, рад/с";
                IReadOnlyList<Table22Row> omegaRows = resultsManager.GetTable22RowsForGraphs();
                rowCount = omegaRows.Count;
                AddPoints(omegaRows, points, row => row.P2d, row => row.Omega);
                return points;

            case GraphKind.EtaByP2:
                title = "η = f(P2)";
                legend = "X: P2, Вт; Y: η, %";
                IReadOnlyList<Table22Row> etaRows = resultsManager.GetTable22RowsForGraphs();
                rowCount = etaRows.Count;
                AddPoints(etaRows, points, row => row.P2d, row => row.EtaD);
                return points;

            case GraphKind.OmegaByU:
                title = "ω = f(U)";
                legend = "X: U, В; Y: ω, рад/с";
                IReadOnlyList<Table23Row> uRows = resultsManager.GetTable23RowsForGraphs();
                rowCount = uRows.Count;
                AddPoints(uRows, points, row => row.U, row => row.Omega);
                return points;

            case GraphKind.IfByIa:
                title = "If = f(Ia)";
                legend = "X: Ia, А; Y: If, А";
                IReadOnlyList<Table24Row> ifRows = resultsManager.GetTable24RowsForGraphs();
                rowCount = ifRows.Count;
                AddPoints(ifRows, points, row => row.Ia, row => row.If);
                return points;

            case GraphKind.OmegaByIf:
                title = "ω = f(If)";
                legend = "X: If, А; Y: ω, рад/с";
                IReadOnlyList<Table25Row> ifOmegaRows = resultsManager.GetTable25RowsForGraphs();
                rowCount = ifOmegaRows.Count;
                AddPoints(ifOmegaRows, points, row => row.If, row => row.Omega);
                return points;

            default:
                title = "График";
                legend = string.Empty;
                rowCount = 0;
                return points;
        }
    }

    private void DrawGraph(IReadOnlyList<Vector2> points)
    {
        ClearGraph();
        if (points == null || points.Count == 0 || graphArea == null)
        {
            return;
        }

        float minX = points[0].x;
        float maxX = points[0].x;
        float minY = points[0].y;
        float maxY = points[0].y;

        for (int i = 1; i < points.Count; i++)
        {
            minX = Mathf.Min(minX, points[i].x);
            maxX = Mathf.Max(maxX, points[i].x);
            minY = Mathf.Min(minY, points[i].y);
            maxY = Mathf.Max(maxY, points[i].y);
        }

        Rect rect = graphArea.rect;
        float width = Mathf.Max(1f, rect.width - GraphPadding * 2f);
        float height = Mathf.Max(1f, rect.height - GraphPadding * 2f);
        Vector2[] uiPoints = new Vector2[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            float x = GraphPadding + Normalize(points[i].x, minX, maxX) * width;
            float y = GraphPadding + Normalize(points[i].y, minY, maxY) * height;
            uiPoints[i] = new Vector2(x, y);
        }

        for (int i = 1; i < uiPoints.Length; i++)
        {
            CreateLine(uiPoints[i - 1], uiPoints[i]);
        }

        for (int i = 0; i < uiPoints.Length; i++)
        {
            CreatePoint(uiPoints[i]);
        }
    }

    private void CreatePoint(Vector2 position)
    {
        GameObject pointObject = new GameObject("GraphPoint", typeof(RectTransform), typeof(Image));
        pointObject.transform.SetParent(graphArea, false);

        RectTransform rect = pointObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(8f, 8f);

        Image image = pointObject.GetComponent<Image>();
        image.color = new Color(1f, 0.8f, 0.25f, 1f);
        spawnedObjects.Add(pointObject);
    }

    private void CreateLine(Vector2 from, Vector2 to)
    {
        GameObject lineObject = new GameObject("GraphLine", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(graphArea, false);

        Vector2 delta = to - from;
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = from + delta * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, 3f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Image image = lineObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.75f, 1f, 1f);
        spawnedObjects.Add(lineObject);
    }

    private void ClearGraph()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();
    }

    private static void AddPoints<T>(IReadOnlyList<T> rows, List<Vector2> points, Func<T, float> getX, Func<T, float> getY)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            float x = SafeFinite(getX(rows[i]));
            float y = SafeFinite(getY(rows[i]));
            points.Add(new Vector2(x, y));
        }
    }

    private static float Normalize(float value, float min, float max)
    {
        if (Mathf.Approximately(min, max))
        {
            return 0.5f;
        }

        return Mathf.Clamp01((value - min) / (max - min));
    }

    private static float SafeFinite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null && target.text != value)
        {
            target.text = value;
        }
    }
}
