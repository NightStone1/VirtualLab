using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab4UCurveGraphView : MonoBehaviour
{
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;
    public RectTransform plotRoot;
    public TMP_Text legendText;
    public bool drawStatorCurrentGraph = true;
    public bool drawCosPhiGraph = false;
    public Vector2 graphSize = new Vector2(500f, 300f);
    public bool refreshOnUCurveChanged = true;
    public bool logDebug = false;

    private static readonly Color NoLoadColor = new Color(0.2f, 0.75f, 1f, 1f);
    private static readonly Color HalfLoadColor = new Color(1f, 0.8f, 0.2f, 1f);
    private static readonly Color FullLoadColor = new Color(1f, 0.35f, 0.35f, 1f);
    private static readonly Color AxisColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    private RectTransform generatedRoot;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (controller != null && refreshOnUCurveChanged)
        {
            controller.OnUCurveChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.OnUCurveChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        ResolveReferences();
        UpdateLegendText();

        if (controller == null || plotRoot == null)
        {
            RebuildLayouts();
            return;
        }

        EnsureGeneratedRoot();
        ClearGeneratedRoot();
        DrawAxes();

        GraphBounds bounds = CalculateBounds();
        DrawSeries(UCurveSeries.NoLoad, NoLoadColor, bounds);
        DrawSeries(UCurveSeries.HalfLoad, HalfLoadColor, bounds);
        DrawSeries(UCurveSeries.FullLoad, FullLoadColor, bounds);

        if (logDebug)
        {
            Debug.Log($"{nameof(Lab4UCurveGraphView)} refreshed on {name}: If {bounds.MinX:0.00}-{bounds.MaxX:0.00}, Y {bounds.MinY:0.00}-{bounds.MaxY:0.00}.", this);
        }

        RebuildLayouts();
    }

    private void ResolveReferences()
    {
        if (controller == null && autoFindController)
        {
            controller = FindFirstObjectByType<SyncGeneratorLabController>();
        }

        if (plotRoot == null)
        {
            plotRoot = GetComponent<RectTransform>();
        }
    }

    private void UpdateLegendText()
    {
        if (legendText != null)
        {
            legendText.text = "Легенда: P0 — синяя; 0.5Pн — жёлтая; Pн — красная";
        }
    }

    private void RebuildLayouts()
    {
        if (plotRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(plotRoot);
        }

        if (legendText != null)
        {
            RectTransform legendTransform = legendText.GetComponent<RectTransform>();
            if (legendTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(legendTransform);
            }
        }
    }

    private void EnsureGeneratedRoot()
    {
        if (generatedRoot != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("GeneratedUCurveGraph", typeof(RectTransform));
        rootObject.transform.SetParent(plotRoot, false);
        generatedRoot = rootObject.GetComponent<RectTransform>();
        generatedRoot.anchorMin = new Vector2(0.5f, 0.5f);
        generatedRoot.anchorMax = new Vector2(0.5f, 0.5f);
        generatedRoot.pivot = new Vector2(0.5f, 0.5f);
        generatedRoot.anchoredPosition = Vector2.zero;
        generatedRoot.sizeDelta = graphSize;
    }

    private void ClearGeneratedRoot()
    {
        for (int i = generatedRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(generatedRoot.GetChild(i).gameObject);
        }

        generatedRoot.sizeDelta = graphSize;
    }

    private void DrawAxes()
    {
        DrawLine(new Vector2(0f, 0f), new Vector2(graphSize.x, 0f), AxisColor, 2f, "Axis X");
        DrawLine(new Vector2(0f, 0f), new Vector2(0f, graphSize.y), AxisColor, 2f, "Axis Y");
    }

    private GraphBounds CalculateBounds()
    {
        GraphBounds bounds = new GraphBounds
        {
            MinX = 0f,
            MaxX = 1.5f,
            MinY = 0f,
            MaxY = drawCosPhiGraph && !drawStatorCurrentGraph ? 1f : 1f
        };

        ExpandBounds(controller.GetUCurvePoints(UCurveSeries.NoLoad), ref bounds);
        ExpandBounds(controller.GetUCurvePoints(UCurveSeries.HalfLoad), ref bounds);
        ExpandBounds(controller.GetUCurvePoints(UCurveSeries.FullLoad), ref bounds);

        bounds.MaxX = Mathf.Max(bounds.MaxX, bounds.MinX + 0.001f);
        bounds.MaxY = Mathf.Max(bounds.MaxY, bounds.MinY + 0.001f);
        return bounds;
    }

    private void ExpandBounds(IReadOnlyList<UCurvePoint> points, ref GraphBounds bounds)
    {
        for (int i = 0; i < points.Count; i++)
        {
            UCurvePoint point = points[i];
            bounds.MaxX = Mathf.Max(bounds.MaxX, point.If);

            if (drawStatorCurrentGraph)
            {
                bounds.MaxY = Mathf.Max(bounds.MaxY, point.Istat);
            }

            if (drawCosPhiGraph)
            {
                bounds.MaxY = Mathf.Max(bounds.MaxY, point.cosPhi);
            }
        }
    }

    private void DrawSeries(UCurveSeries series, Color color, GraphBounds bounds)
    {
        IReadOnlyList<UCurvePoint> sourcePoints = controller.GetUCurvePoints(series);
        if (sourcePoints.Count == 0)
        {
            return;
        }

        List<UCurvePoint> points = new List<UCurvePoint>(sourcePoints);
        points.Sort((left, right) => left.If.CompareTo(right.If));

        List<Vector2> positions = new List<Vector2>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            UCurvePoint point = points[i];
            float yValue = drawCosPhiGraph && !drawStatorCurrentGraph ? point.cosPhi : point.Istat;
            Vector2 position = MapPoint(point.If, yValue, bounds);
            positions.Add(position);
            DrawPoint(position, color, series + " Point");
        }

        if (positions.Count < 2)
        {
            return;
        }

        for (int i = 1; i < positions.Count; i++)
        {
            DrawLine(positions[i - 1], positions[i], color, 3f, series + " Line");
        }
    }

    private Vector2 MapPoint(float x, float y, GraphBounds bounds)
    {
        float normalizedX = Mathf.InverseLerp(bounds.MinX, bounds.MaxX, x);
        float normalizedY = Mathf.InverseLerp(bounds.MinY, bounds.MaxY, y);
        return new Vector2(normalizedX * graphSize.x, normalizedY * graphSize.y);
    }

    private void DrawPoint(Vector2 position, Color color, string objectName)
    {
        GameObject pointObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        pointObject.transform.SetParent(generatedRoot, false);

        RectTransform rectTransform = pointObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(7f, 7f);

        Image image = pointObject.GetComponent<Image>();
        image.color = color;
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness, string objectName)
    {
        Vector2 delta = end - start;
        if (delta.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        GameObject lineObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(generatedRoot, false);

        RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = (start + end) * 0.5f;
        rectTransform.sizeDelta = new Vector2(delta.magnitude, thickness);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Image image = lineObject.GetComponent<Image>();
        image.color = color;
    }

    private struct GraphBounds
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}
