// Copyright (c) 2026 Бабичева Екатерина Анатольевна,
// Бибко Эдуард Александрович.
//
// Данный программный код разработан в рамках выпускной квалификационной работы
// "Виртуальный методический комплекс по дисциплине "Электрические машины"".
//
// Использование программного комплекса в учебном процессе АМТИ допускается
// в рамках подписанного акта о внедрении.
//
// Дальнейшее распространение, модификация, переработка, передача третьим лицам,
// публикация исходного кода, а также использование за пределами указанного
// внедрения допускаются только с письменного согласия авторов, если иное
// не предусмотрено отдельным соглашением.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab3ChartGraphView : MonoBehaviour
{
    public Lab3_ElectricCircuit controller;
    public Lab3Controller mvpController;
    public bool autoFindController = true;
    public Lab3ChartTableView syncTableView;
    public RectTransform plotRoot;
    public TMP_Text legendText;
    public bool refreshOnDataChanged = true;
    public bool logDebug = false;

    private static readonly Color AscendingColor = new Color(0.2f, 0.75f, 1f, 1f);
    private static readonly Color DescendingColor = new Color(1f, 0.5f, 0.2f, 1f);
    private static readonly Color SingleSeriesColor = new Color(0.2f, 1f, 0.5f, 1f);
    private static readonly Color AxisColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    private RectTransform generatedRoot;
    private Lab3ChartTableView.TableType currentTableType;
    private Canvas canvas;

    private Vector2 PlotSize => plotRoot != null ? plotRoot.rect.size : new Vector2(500f, 300f);

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();
        UpdateTableType();

        if ((controller == null && mvpController == null) || plotRoot == null)
        {
            RebuildLayouts();
            return;
        }

        EnsureMask();
        EnsureGeneratedRoot();
        ClearGeneratedRoot();
        UpdateLegendText();

        if (currentTableType != Lab3ChartTableView.TableType.Table3_1_Resistance)
        {
            DrawAxes();
            DrawCurrentGraph();
        }

        RebuildLayouts();
    }

    private void ResolveReferences()
    {
        if (controller == null && autoFindController)
            controller = FindFirstObjectByType<Lab3_ElectricCircuit>();
        if (mvpController == null && autoFindController)
            mvpController = FindFirstObjectByType<Lab3Controller>();

        if (plotRoot == null)
            plotRoot = GetComponent<RectTransform>();
    }

    private void UpdateTableType()
    {
        if (syncTableView != null)
            currentTableType = syncTableView.tableType;
    }

    private void UpdateLegendText()
    {
        if (legendText == null)
            return;

        switch (currentTableType)
        {
            case Lab3ChartTableView.TableType.Table3_1_Resistance:
                legendText.text = "Для таблицы 1.1 график не строится";
                return;
            case Lab3ChartTableView.TableType.Table3_2_NoLoad:
                legendText.text = "I_в (ток возбуждения) — X, E_a (ЭДС) — Y\nГолубая — восходящая ветвь, Оранжевая — нисходящая";
                return;
            case Lab3ChartTableView.TableType.Table3_3_Load:
                legendText.text = "X: I_в, А | Y: U, В";
                return;
            case Lab3ChartTableView.TableType.Table3_4_External:
                legendText.text = "X: I_a, А | Y: U, В";
                return;
            case Lab3ChartTableView.TableType.Table3_5_Regulating:
                legendText.text = "X: I_a, А | Y: I_в, А";
                return;
            case Lab3ChartTableView.TableType.Table3_6_ShortCircuit:
                legendText.text = "X: I_в, А | Y: I_к, А";
                return;
        }
    }

    private void EnsureMask()
    {
        if (plotRoot.GetComponent<Mask>() == null)
            plotRoot.gameObject.AddComponent<Mask>();
    }

    private void EnsureCanvas()
    {
        if (canvas != null)
            return;
        canvas = GetComponentInParent<Canvas>();
    }

    private void RebuildLayouts()
    {
        if (plotRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(plotRoot);
    }

    private void EnsureGeneratedRoot()
    {
        if (generatedRoot != null)
            return;

        var rootObject = new GameObject("GeneratedGraph", typeof(RectTransform));
        rootObject.transform.SetParent(plotRoot, false);
        generatedRoot = rootObject.GetComponent<RectTransform>();
        generatedRoot.anchorMin = Vector2.zero;
        generatedRoot.anchorMax = Vector2.one;
        generatedRoot.pivot = new Vector2(0.5f, 0.5f);
        generatedRoot.anchoredPosition = Vector2.zero;
        generatedRoot.sizeDelta = Vector2.zero;
    }

    private void ClearGeneratedRoot()
    {
        for (int i = generatedRoot.childCount - 1; i >= 0; i--)
            Destroy(generatedRoot.GetChild(i).gameObject);
    }

    private void DrawAxes()
    {
        var size = PlotSize;
        DrawLine(new Vector2(0f, 0f), new Vector2(size.x, 0f), AxisColor, 2f, "AxisX");
        DrawLine(new Vector2(0f, 0f), new Vector2(0f, size.y), AxisColor, 2f, "AxisY");
    }

    private void DrawCurrentGraph()
    {
        switch (currentTableType)
        {
            case Lab3ChartTableView.TableType.Table3_2_NoLoad:
                DrawNoLoadGraph();
                break;
            case Lab3ChartTableView.TableType.Table3_3_Load:
                DrawSingleGraph(GetLoadData(), SingleSeriesColor, "Load");
                break;
            case Lab3ChartTableView.TableType.Table3_4_External:
                DrawSingleGraph(GetExternalData(), SingleSeriesColor, "External");
                break;
            case Lab3ChartTableView.TableType.Table3_5_Regulating:
                DrawSingleGraph(GetRegulatingData(), SingleSeriesColor, "Regulating");
                break;
            case Lab3ChartTableView.TableType.Table3_6_ShortCircuit:
                DrawSingleGraph(GetShortCircuitData(), SingleSeriesColor, "ShortCircuit");
                break;
        }
    }

    private void DrawNoLoadGraph()
    {
        var allPoints = GetNoLoadData();
        if (allPoints.Count == 0)
            return;

        int split = FindAscendingSplit(allPoints);
        var ascending = allPoints.GetRange(0, split);
        var descending = allPoints.GetRange(split, allPoints.Count - split);
        SortByX(ascending);
        SortByX(descending);

        var bounds = CalculateBounds(ascending, descending);
        if (ascending.Count >= 2)
            DrawSeries(ascending, AscendingColor, bounds, "NoLoadAsc");
        if (descending.Count >= 2)
            DrawSeries(descending, DescendingColor, bounds, "NoLoadDesc");

        foreach (var p in ascending)
            DrawPoint(MapPoint(p.x, p.y, bounds), AscendingColor, "PtNoLoadAsc");
        foreach (var p in descending)
            DrawPoint(MapPoint(p.x, p.y, bounds), DescendingColor, "PtNoLoadDesc");
    }

    private void DrawSingleGraph(List<Vector2> points, Color color, string name)
    {
        if (points.Count == 0)
            return;

        SortByX(points);

        var bounds = CalculateBounds(points);
        foreach (var p in points)
            DrawPoint(MapPoint(p.x, p.y, bounds), color, "Pt" + name);
        if (points.Count >= 2)
            DrawSeries(points, color, bounds, name);
    }

    private int FindAscendingSplit(List<Vector2> points)
    {
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].x < points[i - 1].x)
                return i;
        }
        return points.Count;
    }

    private List<Vector2> GetNoLoadData()
    {
        return mvpController != null ? mvpController.GetNoLoadData() : controller.GetNoLoadData();
    }

    private List<Vector2> GetLoadData()
    {
        return mvpController != null ? mvpController.GetLoadData() : controller.GetLoadData();
    }

    private List<Vector2> GetExternalData()
    {
        return mvpController != null ? mvpController.GetExternalData() : controller.GetExternalData();
    }

    private List<Vector2> GetRegulatingData()
    {
        return mvpController != null ? mvpController.GetRegulatingData() : controller.GetRegulatingData();
    }

    private List<Vector2> GetShortCircuitData()
    {
        return mvpController != null ? mvpController.GetShortCircuitData() : controller.GetShortCircuitData();
    }

    private static void SortByX(List<Vector2> points)
    {
        points.Sort((a, b) => a.x.CompareTo(b.x));
    }

    private GraphBounds CalculateBounds(params List<Vector2>[] series)
    {
        var bounds = new GraphBounds { MinX = 0f, MaxX = 0.001f, MinY = 0f, MaxY = 0.001f };
        bool hasData = false;

        foreach (var points in series)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (!hasData)
                {
                    bounds.MinX = bounds.MaxX = points[i].x;
                    bounds.MinY = bounds.MaxY = points[i].y;
                    hasData = true;
                }
                else
                {
                    if (points[i].x < bounds.MinX) bounds.MinX = points[i].x;
                    if (points[i].x > bounds.MaxX) bounds.MaxX = points[i].x;
                    if (points[i].y < bounds.MinY) bounds.MinY = points[i].y;
                    if (points[i].y > bounds.MaxY) bounds.MaxY = points[i].y;
                }
            }
        }

        if (bounds.MaxX <= bounds.MinX) bounds.MaxX = bounds.MinX + 0.001f;
        if (bounds.MaxY <= bounds.MinY) bounds.MaxY = bounds.MinY + 0.001f;

        float xPad = (bounds.MaxX - bounds.MinX) * 0.1f;
        float yPad = (bounds.MaxY - bounds.MinY) * 0.1f;
        bounds.MinX -= xPad;
        bounds.MaxX += xPad;
        bounds.MinY -= yPad;
        bounds.MaxY += yPad;

        return bounds;
    }

    private void DrawSeries(List<Vector2> points, Color color, GraphBounds bounds, string name)
    {
        for (int i = 1; i < points.Count; i++)
        {
            var start = MapPoint(points[i - 1].x, points[i - 1].y, bounds);
            var end = MapPoint(points[i].x, points[i].y, bounds);
            DrawLine(start, end, color, 3f, name + "Line" + i);
        }
    }

    private Vector2 MapPoint(float x, float y, GraphBounds bounds)
    {
        var size = PlotSize;
        float nx = Mathf.InverseLerp(bounds.MinX, bounds.MaxX, x);
        float ny = Mathf.InverseLerp(bounds.MinY, bounds.MaxY, y);
        return new Vector2(nx * size.x, ny * size.y);
    }

    private void DrawPoint(Vector2 position, Color color, string objectName)
    {
        var obj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(generatedRoot, false);

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(7f, 7f);

        obj.GetComponent<Image>().color = color;
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness, string objectName)
    {
        var delta = end - start;
        if (delta.sqrMagnitude <= 0.0001f)
            return;

        var obj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(generatedRoot, false);

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (start + end) * 0.5f;
        rt.sizeDelta = new Vector2(delta.magnitude, thickness);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        obj.GetComponent<Image>().color = color;
    }

    private struct GraphBounds
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}
