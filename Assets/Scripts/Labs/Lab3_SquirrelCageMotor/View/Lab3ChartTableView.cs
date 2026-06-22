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
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab3ChartTableView : MonoBehaviour
{
    public enum TableType
    {
        Table3_1_Resistance,
        Table3_2_NoLoad,
        Table3_3_Load,
        Table3_4_External,
        Table3_5_Regulating,
        Table3_6_ShortCircuit
    }

    public Lab3_ElectricCircuit controller;
    public Lab3Controller mvpController;
    public bool autoFindController = true;
    public TableType tableType = TableType.Table3_1_Resistance;
    public TMP_Text targetText;
    public bool autoFindText = true;
    public Lab3ChartGraphView graphView;
    public bool autoCreateGraph = true;
    public RectTransform contentRoot;
    public int maxRows = 15;
    public bool refreshEveryFrame;

    private const float LayoutSpacing = 14f;
    private const float MinTableHeight = 140f;
    private const float GraphPreferredHeight = 240f;

    private readonly StringBuilder builder = new StringBuilder(2048);
    private readonly List<ResistancePoint> resistancePoints = new List<ResistancePoint>();
    private LayoutElement tableLayoutElement;
    private LayoutElement graphLayoutElement;

    private struct ResistancePoint
    {
        public float voltage;
        public float current;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (refreshEveryFrame)
            Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();

        if (targetText == null)
            return;

        if (controller == null && mvpController == null)
        {
            targetText.text = "Нет данных";
            RebuildLayout();
            return;
        }

        builder.Clear();

        switch (tableType)
        {
            case TableType.Table3_1_Resistance: BuildTable3_1(); break;
            case TableType.Table3_2_NoLoad:     BuildTable3_2(); break;
            case TableType.Table3_3_Load:       BuildTable3_3(); break;
            case TableType.Table3_4_External:   BuildTable3_4(); break;
            case TableType.Table3_5_Regulating: BuildTable3_5(); break;
            case TableType.Table3_6_ShortCircuit: BuildTable3_6(); break;
        }

        targetText.text = builder.ToString();
        RebuildLayout();

        if (graphView != null)
        {
            bool showGraph = tableType != TableType.Table3_1_Resistance;
            graphView.gameObject.SetActive(showGraph);
            graphView.Refresh();
        }

        RebuildLayout();
    }

    public void RecordCurrentPoint()
    {
        ResolveReferences();

        if (mvpController != null)
        {
            mvpController.RecordPoint();
            Refresh();
            return;
        }

        if (controller == null)
            return;

        resistancePoints.Add(new ResistancePoint
        {
            voltage = controller.PV2Value,     // PV2 — напряжение на зажимах G1
            current = controller.PA2Value      // PA2 — ток якоря G1 I_a, А
        });
        Refresh();
    }

    public void ClearRecordedPoints()
    {
        ResolveReferences();

        if (mvpController != null)
        {
            mvpController.ClearCurrentStagePoints();
            Refresh();
            return;
        }

        resistancePoints.Clear();
        Refresh();
    }

    private void BuildTable3_1()
    {
        builder.AppendLine("Таблица 3.1 — Измерение сопротивлений");
        builder.AppendLine("№ | U,В | I,А | Ra,Ом (хол.) | Rar,Ом (гор.)");
        builder.AppendLine("---");

        List<Vector2> mvpResistancePoints = mvpController != null ? mvpController.GetResistanceData() : null;
        if (mvpResistancePoints != null && mvpResistancePoints.Count > 0)
        {
            int mvpStartIndex = Mathf.Max(0, mvpResistancePoints.Count - maxRows);
            for (int i = mvpStartIndex; i < mvpResistancePoints.Count; i++)
            {
                Vector2 p = mvpResistancePoints[i];
                float ra = p.y > 0.001f ? p.x / p.y : 0f;
                float rar = Lab3_CoeffCalculation.GetArmatureResistance();

                builder.Append(i + 1).Append(" | ")
                    .Append(p.x.ToString("F2")).Append(" | ")
                    .Append(p.y.ToString("F3")).Append(" | ")
                    .Append(ra.ToString("F2")).Append(" | ")
                    .Append(rar.ToString("F2"))
                    .AppendLine();
            }

            return;
        }

        if (resistancePoints.Count == 0)
        {
            builder.Append("Нет записанных точек. Нажмите «Записать» для фиксации замера.");
            return;
        }

        int startIndex = Mathf.Max(0, resistancePoints.Count - maxRows);
        for (int i = startIndex; i < resistancePoints.Count; i++)
        {
            var p = resistancePoints[i];
            float ra = p.current > 0.001f ? p.voltage / p.current : 0f;
            float rar = Lab3_CoeffCalculation.GetArmatureResistance();

            builder.Append(i + 1).Append(" | ")
                .Append(p.voltage.ToString("F2")).Append(" | ")
                .Append(p.current.ToString("F3")).Append(" | ")
                .Append(ra.ToString("F2")).Append(" | ")
                .Append(rar.ToString("F2"))
                .AppendLine();
        }
    }

    private void BuildTable3_2()
    {
        builder.AppendLine("Таблица 3.2 — Характеристика холостого хода Ea = f(If)");
        builder.AppendLine("Точки отсортированы по If для построения одной кривой");
        builder.AppendLine("№ | If, А | Ea, В");
        builder.AppendLine("---");

        var points = BuildSortedUniquePoints(mvpController != null ? mvpController.GetNoLoadData() : controller.GetNoLoadData());
        if (points.Count == 0)
        {
            builder.Append("Нет данных. Записывайте точки через кнопку «Записать ХХХ».");
            return;
        }

        AppendPoints(points);
    }

    private void BuildTable3_3()
    {
        builder.AppendLine("Таблица 3.3 — Нагрузочная характеристика (I_a = const)");
        builder.AppendLine("№ | If, А | U, В");
        builder.AppendLine("---");

        var points = mvpController != null ? mvpController.GetLoadData() : controller.GetLoadData();
        AppendPoints(points);
    }

    private void BuildTable3_4()
    {
        builder.AppendLine("Таблица 3.4 — Внешняя характеристика (I_в = const)");
        builder.AppendLine("№ | Iа, А | U, В");
        builder.AppendLine("---");

        var points = mvpController != null ? mvpController.GetExternalData() : controller.GetExternalData();
        AppendPoints(points);
    }

    private void BuildTable3_5()
    {
        builder.AppendLine("Таблица 3.5 — Регулировочная характеристика If = f(Ia), PV2 = const");
        builder.AppendLine("Точки отсортированы по Ia для построения одной кривой");
        builder.AppendLine("№ | Ia, А | If, А");
        builder.AppendLine("---");

        var points = BuildSortedUniquePoints(mvpController != null ? mvpController.GetRegulatingData() : controller.GetRegulatingData());
        if (points.Count == 0)
        {
            builder.Append("Нет данных.");
            return;
        }

        AppendPoints(points);
    }

    private void BuildTable3_6()
    {
        builder.AppendLine("Таблица 3.6 — Характеристика короткого замыкания");
        builder.AppendLine("№ | If, А | Iк, А");
        builder.AppendLine("---");

        var points = mvpController != null ? mvpController.GetShortCircuitData() : controller.GetShortCircuitData();
        AppendPoints(points);
    }

    private void AppendPoints(List<Vector2> points)
    {
        if (points.Count == 0)
        {
            builder.Append("Нет данных.");
            return;
        }

        int rows = Mathf.Min(maxRows, points.Count);
        int startIndex = Mathf.Max(0, points.Count - rows);

        for (int i = startIndex; i < points.Count; i++)
        {
            builder.Append(i + 1).Append(" | ")
                .Append(points[i].x.ToString("F3")).Append(" | ")
                .Append(points[i].y.ToString("F2"))
                .AppendLine();
        }
    }

    private int FindSplitIndex(List<Vector2> points)
    {
        if (points.Count < 2)
            return points.Count;

        float maxX = float.MinValue;
        int maxIndex = 0;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].x > maxX)
            {
                maxX = points[i].x;
                maxIndex = i;
            }
        }

        return maxIndex + 1;
    }

    private void ResolveReferences()
    {
        if (mvpController == null && autoFindController)
            mvpController = FindFirstObjectByType<Lab3Controller>();

        if (controller == null && autoFindController)
            controller = FindFirstObjectByType<Lab3_ElectricCircuit>();

        if (targetText == null && autoFindText)
        {
            targetText = GetComponent<TMP_Text>();
            if (targetText == null)
                targetText = GetComponentInChildren<TMP_Text>(true);
        }

        if (contentRoot == null && targetText != null)
            contentRoot = targetText.transform.parent as RectTransform;

        ConfigureContentLayout();

        if (graphView == null)
            graphView = FindFirstObjectByType<Lab3ChartGraphView>();

        if (graphView == null && autoCreateGraph && targetText != null)
            graphView = CreateRuntimeGraphView();

        if (graphView != null)
        {
            graphView.controller = controller;
            graphView.mvpController = mvpController;
            graphView.syncTableView = this;
            if (graphLayoutElement == null)
                graphLayoutElement = graphView.GetComponent<LayoutElement>() ?? graphView.gameObject.AddComponent<LayoutElement>();
            graphLayoutElement.preferredHeight = GraphPreferredHeight;
            graphLayoutElement.minHeight = tableType == TableType.Table3_1_Resistance ? 0f : GraphPreferredHeight;
            graphLayoutElement.flexibleHeight = 0f;
        }
    }

    private void ConfigureContentLayout()
    {
        if (contentRoot == null)
            return;

        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = LayoutSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (targetText != null)
        {
            tableLayoutElement = targetText.GetComponent<LayoutElement>() ?? targetText.gameObject.AddComponent<LayoutElement>();
            tableLayoutElement.flexibleHeight = 0f;
        }
    }

    private Lab3ChartGraphView CreateRuntimeGraphView()
    {
        RectTransform tableRect = targetText.GetComponent<RectTransform>();
        RectTransform contentRect = contentRoot != null ? contentRoot : tableRect != null ? tableRect.parent as RectTransform : transform as RectTransform;
        if (contentRect == null)
            return null;

        GameObject graphObject = new GameObject("Graph_TableContent", typeof(RectTransform), typeof(LayoutElement));
        graphObject.transform.SetParent(contentRect, false);
        if (tableRect != null)
            graphObject.transform.SetSiblingIndex(tableRect.GetSiblingIndex() + 1);
        RectTransform graphRect = graphObject.GetComponent<RectTransform>();
        graphRect.anchorMin = new Vector2(0f, 1f);
        graphRect.anchorMax = new Vector2(1f, 1f);
        graphRect.pivot = new Vector2(0.5f, 1f);
        graphRect.sizeDelta = new Vector2(0f, GraphPreferredHeight);
        graphLayoutElement = graphObject.GetComponent<LayoutElement>();
        graphLayoutElement.preferredHeight = GraphPreferredHeight;
        graphLayoutElement.minHeight = GraphPreferredHeight;
        graphLayoutElement.flexibleHeight = 0f;

        GameObject legendObject = new GameObject("GraphLegend", typeof(RectTransform), typeof(TextMeshProUGUI));
        legendObject.transform.SetParent(graphObject.transform, false);
        RectTransform legendRect = legendObject.GetComponent<RectTransform>();
        legendRect.anchorMin = new Vector2(0f, 1f);
        legendRect.anchorMax = new Vector2(1f, 1f);
        legendRect.pivot = new Vector2(0.5f, 1f);
        legendRect.anchoredPosition = Vector2.zero;
        legendRect.sizeDelta = new Vector2(0f, 48f);
        TextMeshProUGUI legend = legendObject.GetComponent<TextMeshProUGUI>();
        legend.fontSize = 18f;
        legend.color = Color.white;
        legend.alignment = TextAlignmentOptions.Left;
        legend.raycastTarget = false;
        legend.text = "Для таблицы 3.1 график не строится";

        GameObject plotObject = new GameObject("GraphPlot", typeof(RectTransform), typeof(Image));
        plotObject.transform.SetParent(graphObject.transform, false);
        RectTransform plotRect = plotObject.GetComponent<RectTransform>();
        plotRect.anchorMin = new Vector2(0f, 1f);
        plotRect.anchorMax = new Vector2(1f, 1f);
        plotRect.pivot = new Vector2(0.5f, 1f);
        plotRect.anchoredPosition = new Vector2(0f, -56f);
        plotRect.sizeDelta = new Vector2(0f, 178f);
        Image plotImage = plotObject.GetComponent<Image>();
        plotImage.color = new Color(0f, 0f, 0f, 0.18f);
        plotImage.raycastTarget = false;

        Lab3ChartGraphView view = graphObject.AddComponent<Lab3ChartGraphView>();
        view.controller = controller;
        view.mvpController = mvpController;
        view.syncTableView = this;
        view.plotRoot = plotRect;
        view.legendText = legend;
        return view;
    }

    private void RebuildLayout()
    {
        if (targetText != null)
        {
            targetText.ForceMeshUpdate();
            var rt = targetText.GetComponent<RectTransform>();
            if (tableLayoutElement == null)
                tableLayoutElement = targetText.GetComponent<LayoutElement>() ?? targetText.gameObject.AddComponent<LayoutElement>();
            float preferredHeight = Mathf.Max(MinTableHeight, targetText.preferredHeight + 24f);
            tableLayoutElement.preferredHeight = preferredHeight;
            tableLayoutElement.minHeight = preferredHeight;
            tableLayoutElement.flexibleHeight = 0f;
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        if (graphLayoutElement != null)
        {
            bool showGraph = tableType != TableType.Table3_1_Resistance;
            graphLayoutElement.preferredHeight = showGraph ? GraphPreferredHeight : 0f;
            graphLayoutElement.minHeight = showGraph ? GraphPreferredHeight : 0f;
        }

        Canvas.ForceUpdateCanvases();
        if (contentRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        RectTransform parent = contentRoot != null ? contentRoot.parent as RectTransform : null;
        while (parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            parent = parent.parent as RectTransform;
        }
    }

    private static List<Vector2> BuildSortedUniquePoints(List<Vector2> source)
    {
        List<Vector2> points = new List<Vector2>(source);
        points.Sort((a, b) => a.x.CompareTo(b.x));

        for (int i = points.Count - 2; i >= 0; i--)
        {
            if (Mathf.Abs(points[i].x - points[i + 1].x) <= 0.001f)
                points.RemoveAt(i + 1);
        }

        return points;
    }
}
