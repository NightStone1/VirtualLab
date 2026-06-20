using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Lab1GraphView : MonoBehaviour
{
    private const int RequiredRows = 5;
    private const float GraphPanelPreferredWidth = 862f;
    private const float GraphPanelPreferredHeight = 420f;
    private const float GraphPadding = 12f;
    private const string RootContentPath = "Canvas/Scroll View/Viewport/Content";
    private const string TvContentPath = "TV 1/Canvas/Scroll View/Viewport/Content";

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
    private RectTransform panelRect;
    private RectTransform contentParent;
    private bool fallbackWarningLogged;
    private float uiScale = 1f;
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
        contentParent = ResolveContentParent();
        Transform parent = contentParent != null ? contentParent : transform.parent;

        panelRect = gameObject.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            panelRect = gameObject.AddComponent<RectTransform>();
        }

        if (parent != null)
        {
            transform.SetParent(parent, false);
        }

        gameObject.name = "Lab1GraphPanelRuntime";
        ConfigurePanelTransform();
        uiScale = GetUiScale();
        transform.SetAsLastSibling();
        EnsureContentHeightIncludesGraph();

        if (graphArea != null)
        {
            return;
        }

        Image background = gameObject.GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }
        background.color = new Color(0.05f, 0.07f, 0.1f, 0.86f);
        background.raycastTarget = false;

        titleText = CreateText("Title", panelRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Scale(new Vector2(0f, -8f)), Scale(new Vector2(-16f, 32f)), 20f * uiScale, TextAlignmentOptions.Center);
        legendText = CreateText("Legend", panelRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Scale(new Vector2(0f, -42f)), Scale(new Vector2(-16f, 28f)), 13f * uiScale, TextAlignmentOptions.Center);
        statusText = CreateText("Status", panelRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Scale(new Vector2(0f, 8f)), Scale(new Vector2(-16f, 30f)), 13f * uiScale, TextAlignmentOptions.Center);
        counterText = CreateText("Counter", panelRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Scale(new Vector2(0f, 44f)), Scale(new Vector2(90f, 28f)), 14f * uiScale, TextAlignmentOptions.Center);

        graphArea = CreateGraphArea(panelRect);
        CreateButton("PrevButton", panelRect, "←", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), Scale(new Vector2(14f, 34f)), ShowPreviousGraph);
        CreateButton("NextButton", panelRect, "→", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), Scale(new Vector2(-14f, 34f)), ShowNextGraph);
    }

    private RectTransform ResolveContentParent()
    {
        RectTransform rootContent = FindContentByPath(RootContentPath);
        if (rootContent != null && LooksLikeLab1TvContent(rootContent))
        {
            EnsureCanvasCanReceiveUi(rootContent);
            return rootContent;
        }

        RectTransform exactContent = FindExactTvContent();
        if (exactContent != null)
        {
            EnsureCanvasCanReceiveUi(exactContent);
            return exactContent;
        }

        Transform tvTransform = FindTransformByName("TV 1");
        RectTransform tvContent = tvTransform != null ? FindChildPath(tvTransform, "Canvas/Scroll View/Viewport/Content") as RectTransform : null;
        if (tvContent != null)
        {
            EnsureCanvasCanReceiveUi(tvContent);
            return tvContent;
        }

        RectTransform contentWithTables = FindContentWithLab1Tables();
        if (contentWithTables != null)
        {
            EnsureCanvasCanReceiveUi(contentWithTables);
            return contentWithTables;
        }

        RectTransform fallbackContent = FindAnyScrollRectContent();
        if (fallbackContent != null)
        {
            EnsureCanvasCanReceiveUi(fallbackContent);
            LogFallbackWarning("TV Content not found. Graph panel was placed into the first ScrollRect content fallback.");
            return fallbackContent;
        }

        LogFallbackWarning("TV Content not found and no ScrollRect content fallback exists. Graph panel keeps its current parent.");
        return null;
    }

    private void LogFallbackWarning(string message)
    {
        if (!fallbackWarningLogged)
        {
            Debug.LogWarning("Lab1GraphView: " + message);
            fallbackWarningLogged = true;
        }
    }

    private void ConfigurePanelTransform()
    {
        panelRect.localRotation = Quaternion.identity;
        panelRect.localScale = Vector3.one;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(GraphPanelPreferredWidth, GraphPanelPreferredHeight);

        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = GraphPanelPreferredWidth;
        layoutElement.preferredWidth = GraphPanelPreferredWidth;
        layoutElement.flexibleWidth = 0f;
        layoutElement.minHeight = 360f;
        layoutElement.preferredHeight = GraphPanelPreferredHeight;
        layoutElement.flexibleHeight = 0f;
    }

    private RectTransform FindExactTvContent()
    {
        return FindContentByPath(TvContentPath);
    }

    private RectTransform FindContentByPath(string path)
    {
        GameObject contentObject = GameObject.Find(path);
        if (contentObject == null)
        {
            return null;
        }

        return contentObject.transform as RectTransform;
    }

    private RectTransform FindContentWithLab1Tables()
    {
        RectTransform[] rectTransforms = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            RectTransform candidate = rectTransforms[i];
            if (candidate != null && string.Equals(candidate.name, "Content", StringComparison.OrdinalIgnoreCase) && LooksLikeLab1TvContent(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool LooksLikeLab1TvContent(Transform candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        return FindDirectChild(candidate, "Table22Panel") != null &&
               FindDirectChild(candidate, "Table23Panel") != null &&
               FindDirectChild(candidate, "Table24Panel") != null &&
               FindDirectChild(candidate, "Table25Panel") != null;
    }

    private Transform FindTransformByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && string.Equals(transforms[i].name, objectName, StringComparison.OrdinalIgnoreCase))
            {
                return transforms[i];
            }
        }

        return null;
    }

    private Transform FindChildPath(Transform root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path))
        {
            return null;
        }

        string[] parts = path.Split('/');
        Transform current = root;
        for (int i = 0; i < parts.Length; i++)
        {
            current = FindDirectChild(current, parts[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private RectTransform FindAnyScrollRectContent()
    {
        ScrollRect[] scrollRects = FindObjectsByType<ScrollRect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < scrollRects.Length; i++)
        {
            if (scrollRects[i] != null && scrollRects[i].content != null)
            {
                return scrollRects[i].content;
            }
        }

        return null;
    }

    private void EnsureCanvasCanReceiveUi(RectTransform parent)
    {
        Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            return;
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (EventSystem.current == null)
        {
            Debug.LogWarning("Lab1GraphView: EventSystem not found. Graph UI buttons require an EventSystem to receive clicks.");
        }
    }

    private float GetUiScale()
    {
        RectTransform rectTransform = panelRect != null ? panelRect.parent as RectTransform : null;
        if (rectTransform == null)
        {
            return 1f;
        }

        Rect rect = rectTransform.rect;
        float scaleByWidth = Mathf.Abs(rect.width) / 860f;
        return Mathf.Clamp(scaleByWidth, 0.8f, 1.25f);
    }

    private void EnsureContentHeightIncludesGraph()
    {
        if (contentParent == null || panelRect == null)
        {
            return;
        }

        float requiredHeight = EstimateChildrenHeight(contentParent);
        if (requiredHeight <= 0f)
        {
            requiredHeight = Mathf.Abs(contentParent.sizeDelta.y) + GraphPanelPreferredHeight;
        }

        contentParent.sizeDelta = new Vector2(contentParent.sizeDelta.x, Mathf.Max(contentParent.sizeDelta.y, requiredHeight));
    }

    private static float EstimateChildrenHeight(RectTransform parent)
    {
        VerticalLayoutGroup layoutGroup = parent.GetComponent<VerticalLayoutGroup>();
        float height = 0f;

        if (layoutGroup != null)
        {
            height += layoutGroup.padding.top + layoutGroup.padding.bottom;
        }

        int activeChildCount = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            RectTransform child = parent.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            LayoutElement layoutElement = child.GetComponent<LayoutElement>();
            float childHeight = layoutElement != null && layoutElement.preferredHeight > 0f
                ? layoutElement.preferredHeight
                : child.sizeDelta.y;

            height += Mathf.Max(0f, childHeight);
            activeChildCount++;
        }

        if (layoutGroup != null && activeChildCount > 1)
        {
            height += layoutGroup.spacing * (activeChildCount - 1);
        }

        return height;
    }

    private Vector2 Scale(Vector2 value)
    {
        return value * uiScale;
    }

    private RectTransform CreateGraphArea(RectTransform parent)
    {
        GameObject areaObject = new GameObject("GraphArea", typeof(RectTransform), typeof(Image));
        areaObject.transform.SetParent(parent, false);

        RectTransform rect = areaObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Scale(new Vector2(18f, 70f));
        rect.offsetMax = Scale(new Vector2(-18f, -76f));

        Image image = areaObject.GetComponent<Image>();
        image.color = new Color(0.9f, 0.94f, 1f, 0.12f);
        image.raycastTarget = false;

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
        button.interactable = true;
        button.onClick.AddListener(action);

        rect.sizeDelta = Scale(new Vector2(52f, 36f));

        TextMeshProUGUI text = CreateText("Text", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 22f * uiScale, TextAlignmentOptions.Center);
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
        image.raycastTarget = false;
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
        image.raycastTarget = false;
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
