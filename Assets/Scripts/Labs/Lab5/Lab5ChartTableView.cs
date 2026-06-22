using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab5ChartTableView : MonoBehaviour
{
    public enum TableType
    {
        Table5_1_NoLoad,
        Table5_2_InductiveLoad,
        Table5_3_External,
        Table5_4_Regulating,
        Table5_5_ShortCircuit,
        Table5_6_ReactiveTriangle
    }

    public Lab5SyncGeneratorModel controller;
    public Lab5SyncGeneratorLabController labController;
    public bool autoFindController = true;
    public TableType tableType = TableType.Table5_1_NoLoad;
    public TMP_Text targetText;
    public bool autoFindText = true;
    public bool showControllerMessage = true;
    public Lab5ChartGraphView graphView;
    public RectTransform graphContainer;
    public RectTransform graphPlotRoot;
    public TMP_Text graphLegendText;
    public bool createGraphUnderTable = true;
    public Vector2 graphSize = new Vector2(620f, 260f);
    public float tableMinHeight = 90f;
    public float tableGraphSpacing = 14f;
    public int maxRows = 15;
    public bool refreshEveryFrame;

    private readonly StringBuilder builder = new StringBuilder(2048);
    private float lastRecordedIf;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        StartCoroutine(InitializeDefaultTableNextFrame());
    }

    private IEnumerator InitializeDefaultTableNextFrame()
    {
        yield return null;

        ResolveReferences();
        SwitchToTable(TableType.Table5_1_NoLoad);
        FinalizeTvLayoutPass(resetScroll: true);

        yield return null;

        Refresh();
        FinalizeTvLayoutPass(resetScroll: true);
    }

    private void Update()
    {
        if (refreshEveryFrame)
            Refresh();
    }

    public void ResolveReferences()
    {
        if (controller == null && autoFindController)
            controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();
        if (labController == null)
            labController = FindFirstObjectByType<Lab5SyncGeneratorLabController>();

        if (autoFindText && targetText == null)
        {
            var tmp = GetComponentInChildren<TMP_Text>();
            if (tmp != null && tmp.gameObject != gameObject)
                targetText = tmp;
        }
    }

    public void Refresh()
    {
        ResolveReferences();
        if (targetText == null) return;

        RefreshTable();
        UpdateTableTextLayout();
        RefreshGraph();
        RebuildTvLayout();
    }

    public void SwitchToTable(TableType targetTable)
    {
        tableType = targetTable;
        Refresh();
    }

    private void RefreshTable()
    {
        builder.Clear();
        builder.AppendLine($"Таблица 5.{(int)tableType + 1} — {GetTableTitle()}");
        if (showControllerMessage && labController != null && !string.IsNullOrEmpty(labController.LastMessage))
            builder.AppendLine(labController.LastMessage);
        builder.AppendLine();

        switch (tableType)
        {
            case TableType.Table5_1_NoLoad: BuildNoLoadTable(); break;
            case TableType.Table5_2_InductiveLoad: BuildInductiveLoadTable(); break;
            case TableType.Table5_3_External: BuildExternalTable(); break;
            case TableType.Table5_4_Regulating: BuildRegulatingTable(); break;
            case TableType.Table5_5_ShortCircuit: BuildShortCircuitTable(); break;
            case TableType.Table5_6_ReactiveTriangle: BuildReactiveTriangleTable(); break;
        }

        targetText.text = builder.ToString();
    }

    private void RefreshGraph()
    {
        EnsureGraphView();

        bool isCalculationOnly = tableType == TableType.Table5_6_ReactiveTriangle;
        if (graphContainer != null)
            graphContainer.gameObject.SetActive(!isCalculationOnly);
        if (isCalculationOnly || graphView == null)
            return;

        if (graphPlotRoot != null)
            graphPlotRoot.gameObject.SetActive(true);

        graphView.syncTableView = this;
        graphView.controller = controller;
        graphView.Refresh();
    }

    private void EnsureGraphView()
    {
        if (!createGraphUnderTable)
            return;
        if (graphView != null && graphPlotRoot != null)
            return;
        if (targetText == null)
            return;

        Transform parent = targetText.transform.parent;
        if (parent == null)
            return;

        ConfigureRightPanelLayout(parent as RectTransform);

        if (graphContainer == null)
        {
            Transform existing = parent.Find("Graph_TableContent");
            if (existing != null)
                graphContainer = existing as RectTransform;
        }

        if (graphContainer == null)
            graphContainer = CreateGraphContainer(parent);

        graphContainer.SetSiblingIndex(targetText.transform.GetSiblingIndex() + 1);

        if (graphLegendText == null)
            graphLegendText = graphContainer.GetComponentInChildren<TMP_Text>(true);

        if (graphPlotRoot == null)
        {
            Transform existingPlot = graphContainer.Find("GraphPlot");
            if (existingPlot != null)
                graphPlotRoot = existingPlot as RectTransform;
        }

        if (graphPlotRoot == null)
            graphPlotRoot = CreateGraphPlot(graphContainer);

        if (graphView == null)
            graphView = graphPlotRoot.GetComponent<Lab5ChartGraphView>();
        if (graphView == null)
            graphView = graphPlotRoot.gameObject.AddComponent<Lab5ChartGraphView>();

        graphView.controller = controller;
        graphView.syncTableView = this;
        graphView.plotRoot = graphPlotRoot;
        graphView.legendText = graphLegendText;
    }

    private void UpdateTableTextLayout()
    {
        if (targetText == null) return;

        targetText.ForceMeshUpdate();
        var rt = targetText.rectTransform;
        float preferredHeight = Mathf.Max(tableMinHeight, targetText.preferredHeight + 12f);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, preferredHeight);

        var layoutElement = targetText.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = targetText.gameObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = 0f;
    }

    private void ConfigureRightPanelLayout(RectTransform panel)
    {
        if (panel == null) return;

        var layout = panel.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = tableGraphSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;

        var fitter = panel.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void RebuildTvLayout()
    {
        RectTransform current = targetText != null ? targetText.rectTransform : null;
        Canvas.ForceUpdateCanvases();
        while (current != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(current);
            current = current.parent as RectTransform;
        }
        Canvas.ForceUpdateCanvases();
    }

    private void FinalizeTvLayoutPass(bool resetScroll)
    {
        UpdateTableTextLayout();
        RebuildTvLayout();
        if (resetScroll)
            ScrollTableToTop();
    }

    private void ScrollTableToTop()
    {
        if (targetText == null) return;

        var scrollRect = targetText.GetComponentInParent<ScrollRect>();
        if (scrollRect == null) return;

        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    private RectTransform CreateGraphContainer(Transform parent)
    {
        var obj = new GameObject("Graph_TableContent", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, graphSize.y + 54f);

        var image = obj.GetComponent<Image>();
        image.color = new Color(0.03f, 0.035f, 0.045f, 0.55f);
        image.raycastTarget = false;

        var layout = obj.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var layoutElement = obj.GetComponent<LayoutElement>();
        layoutElement.minHeight = graphSize.y + 54f;
        layoutElement.preferredHeight = graphSize.y + 54f;
        layoutElement.flexibleHeight = 0f;

        CreateGraphLegend(rt);
        return rt;
    }

    private TMP_Text CreateGraphLegend(RectTransform parent)
    {
        var obj = new GameObject("GraphLegend", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        var rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 42f);

        var layout = obj.GetComponent<LayoutElement>();
        layout.minHeight = 42f;
        layout.preferredHeight = 42f;

        var text = obj.GetComponent<TextMeshProUGUI>();
        text.fontSize = 14f;
        text.alignment = TextAlignmentOptions.Left;
        text.color = new Color(0.9f, 0.92f, 0.96f, 1f);
        text.text = string.Empty;
        graphLegendText = text;
        return text;
    }

    private RectTransform CreateGraphPlot(RectTransform parent)
    {
        var obj = new GameObject("GraphPlot", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);

        var rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = graphSize;

        var image = obj.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.18f);
        image.raycastTarget = false;

        var layout = obj.GetComponent<LayoutElement>();
        layout.minHeight = graphSize.y;
        layout.preferredHeight = graphSize.y;

        return rt;
    }

    private string GetTableTitle()
    {
        switch (tableType)
        {
            case TableType.Table5_1_NoLoad: return "Характеристика холостого хода E0 = f(If)";
            case TableType.Table5_2_InductiveLoad: return "Индукционная нагрузочная характеристика U = f(I_в)";
            case TableType.Table5_3_External: return "Внешняя характеристика U = f(I_а)";
            case TableType.Table5_4_Regulating: return "Регулировочная характеристика I_в = f(I_а)";
            case TableType.Table5_5_ShortCircuit: return "Характеристика короткого замыкания I_к = f(I_в)";
            case TableType.Table5_6_ReactiveTriangle: return "Реактивный треугольник и расчёт X_σ, X_d, диаграмма ЭДС";
        }
        return "";
    }

    private void BuildNoLoadTable()
    {
        var points = GetSortedNoLoadPoints();

        if (points.Count == 0)
        {
            builder.AppendLine("Нет данных. Записывайте точки через кнопку «Записать точку».");
            return;
        }

        builder.AppendLine("Точки отсортированы по I_в для построения одной кривой ХХХ:");
        builder.AppendLine("№\tI_в, А\tE_0, В");
        int idx = 1;
        foreach (var p in points)
        {
            if (idx > maxRows) { builder.AppendLine("..."); break; }
            builder.AppendLine($"{idx}\t{p.x:F3}\t{p.y:F1}");
            idx++;
        }
    }

    private List<Vector2> GetSortedNoLoadPoints()
    {
        var points = new List<Vector2>(controller.noLoadAscending.Count + controller.noLoadDescending.Count);
        points.AddRange(controller.noLoadAscending);
        points.AddRange(controller.noLoadDescending);
        points.Sort((a, b) => a.x.CompareTo(b.x));
        return points;
    }

    private void BuildInductiveLoadTable()
    {
        var points = controller.inductiveLoadData;
        if (points.Count == 0)
        {
            builder.AppendLine("Нет данных. Записывайте точки при включённой нагрузке (Q2).");
            return;
        }

        builder.AppendLine("№\tI_в, А\tU, В");
        int idx = 1;
        foreach (var p in points)
        {
            if (idx > maxRows) { builder.AppendLine("..."); break; }
            builder.AppendLine($"{idx}\t{p.x:F3}\t{p.y:F1}");
            idx++;
        }
    }

    private void BuildExternalTable()
    {
        var points = controller.externalData;
        if (points.Count == 0)
        {
            builder.AppendLine("Нет данных. Записывайте точки при изменении нагрузки.");
            return;
        }

        builder.AppendLine("№\tI_а, А\tU, В");
        int idx = 1;
        foreach (var p in points)
        {
            if (idx > maxRows) { builder.AppendLine("..."); break; }
            builder.AppendLine($"{idx}\t{p.x:F3}\t{p.y:F1}");
            idx++;
        }
    }

    private void BuildRegulatingTable()
    {
        var points = controller.regulatingData;
        if (points.Count == 0)
        {
            builder.AppendLine("Нет данных. Записывайте точки при регулировании тока возбуждения.");
            return;
        }

        builder.AppendLine("№\tI_а, А\tI_в, А");
        int idx = 1;
        foreach (var p in points)
        {
            if (idx > maxRows) { builder.AppendLine("..."); break; }
            builder.AppendLine($"{idx}\t{p.x:F3}\t{p.y:F3}");
            idx++;
        }
    }

    private void BuildShortCircuitTable()
    {
        var points3 = controller.shortCircuitData;
        var points2 = controller.shortCircuit2PhaseData;

        builder.AppendLine("Трёхфазное короткое замыкание:");
        if (points3.Count == 0)
            builder.AppendLine("   Нет данных.");
        else
        {
            builder.AppendLine("№\tI_в, А\tI_к3, А");
            int idx = 1;
            foreach (var p in points3)
            {
                if (idx > maxRows) { builder.AppendLine("..."); break; }
                builder.AppendLine($"{idx}\t{p.x:F3}\t{p.y:F3}");
                idx++;
            }
        }

        builder.AppendLine();
        builder.AppendLine("Двухфазное короткое замыкание:");
        if (points2.Count == 0)
            builder.AppendLine("   Нет данных. Включите режим 2-фазного КЗ и записывайте точки.");
        else
        {
            builder.AppendLine("№\tI_в, А\tI_к2, А");
            int idx = 1;
            foreach (var p in points2)
            {
                if (idx > maxRows) { builder.AppendLine("..."); break; }
                builder.AppendLine($"{idx}\t{p.x:F3}\t{p.y:F3}");
                idx++;
            }
        }
    }

    private void BuildReactiveTriangleTable()
    {
        builder.AppendLine("Исходные данные:");
        builder.AppendLine($"U_ном = {controller.nominalVoltage:F1} В");
        builder.AppendLine($"I_а_ном = {controller.nominalStatorCurrent:F1} А");
        builder.AppendLine($"cos φ_ном = 0.8");
        builder.AppendLine($"R_я(75°C) = {controller.statorResistance75C:F3} Ом");
        builder.AppendLine();

        controller.CalculateReactiveTriangle(out float Xσ, out float Fa,
            out float Xd_unsat, out float Xd_sat, out var triDetails);

        if (triDetails.ContainsKey("Error"))
        {
            builder.AppendLine("ОШИБКА: " + triDetails["Error"]);
            builder.AppendLine();
            builder.AppendLine("Для расчёта необходимо записать точки:");
            builder.AppendLine("  • ХХХ (восходящая ветвь) — Таблица 5.1");
            builder.AppendLine("  • Индукционная нагрузочная — Таблица 5.2");
            builder.AppendLine("  • Короткое замыкание — Таблица 5.5");
            return;
        }

        builder.AppendLine("=== РАСЧЁТ РЕАКТИВНОГО ТРЕУГОЛЬНИКА ===");
        builder.AppendLine();

        if (triDetails.ContainsKey("UsedTables"))
        {
            builder.AppendLine(triDetails["UsedTables"]);
            builder.AppendLine();
        }

        if (triDetails.ContainsKey("Warning_Inductive"))
        {
            builder.AppendLine(triDetails["Warning_Inductive"]);
            builder.AppendLine();
        }

        builder.AppendLine("1. Ток нагрузки для испытаний:");
        builder.AppendLine("   " + triDetails["Ia_target"]);
        builder.AppendLine();

        builder.AppendLine("2. Начальный участок ХХХ (воздушный зазор):");
        builder.AppendLine("   slope = " + triDetails["Slope_XXX"]);
        builder.AppendLine();

        builder.AppendLine("3. Характеристика КЗ:");
        builder.AppendLine("   I_кз = " + triDetails["I_k3"]);
        builder.AppendLine();

        builder.AppendLine("4. Реактивный треугольник A1-O1-C1:");
        builder.AppendLine("   Отрицательные значения X здесь являются координатами построения, а не отрицательными физическими токами.");
        builder.AppendLine("   A1 = " + triDetails["A1"]);
        builder.AppendLine("   O1 = " + triDetails["O1"]);
        builder.AppendLine("   C1 = " + triDetails["C1"]);
        builder.AppendLine();

        builder.AppendLine("5. Индуктивное сопротивление рассеяния Xσ:");
        builder.AppendLine("   ΔU = " + triDetails["deltaU_Xσ"]);
        builder.AppendLine("   Xσ = " + triDetails["Xσ"]);
        builder.AppendLine();

        builder.AppendLine("6. МДС реакции якоря:");
        builder.AppendLine("   Fa = " + triDetails["Fa"]);
        builder.AppendLine();

        builder.AppendLine("7. Синхронное индуктивное сопротивление Xd:");
        builder.AppendLine("   по спрямлённой ХХХ (воздушный зазор):");
        builder.AppendLine("     ненасыщенное: " + triDetails["Xd_unsat"]);
        if (triDetails.ContainsKey("Xd_raw"))
            builder.AppendLine("   по реальной ХХХ (с насыщением): " + triDetails["Xd_raw"]);
        builder.AppendLine("   " + triDetails["E_at_O1"]);
        builder.AppendLine("   " + triDetails["A1F"]);
        builder.AppendLine("   насыщенное: " + triDetails["Xd_sat"]);
        builder.AppendLine();

        builder.AppendLine("=== ДИАГРАММА ЭДС (МПО) — п. 5.9 ===");
        builder.AppendLine();

        controller.CalculateEmfVectorDiagram(Xσ, Fa,
            out Vector2 v_Eδ, out Vector2 v_Fδ, out Vector2 v_Fa, out Vector2 v_F0, out Vector2 v_E0,
            out float deltaU, out var emfDetails);

        if (emfDetails.ContainsKey("Error"))
        {
            builder.AppendLine("ОШИБКА: " + emfDetails["Error"]);
            return;
        }

        builder.AppendLine("Исходные условия диаграммы:");
        builder.AppendLine($"  {emfDetails["U_н"]}");
        builder.AppendLine($"  {emfDetails["I_a"]}");
        builder.AppendLine($"  {emfDetails["cosφ"]}");
        builder.AppendLine();

        builder.AppendLine("Векторная диаграмма:");
        builder.AppendLine("  Отрицательные Y/углы ниже — это проекции векторов на координатные оси.");

        builder.AppendLine($"  1. E_δ = U_н + I_a·R_a + j·I_a·Xσ");
        builder.AppendLine($"     Состав: " + emfDetails["E_δ_components"]);
        builder.AppendLine($"     |E_δ| = " + emfDetails["E_δ"]);
        builder.AppendLine();

        builder.AppendLine($"  2. F_δ по ХХХ из |E_δ|: " + emfDetails["F_δ"]);
        builder.AppendLine($"     (угол F_δ = ∠E_δ + 90° — опережает)");
        builder.AppendLine();

        builder.AppendLine($"  3. Fa (реакция якоря): " + emfDetails["Fa (вектор)"]);
        builder.AppendLine($"     (в фазе с I_a, отстаёт от U_н)");
        builder.AppendLine();

        builder.AppendLine($"  4. F_0 = F_δ + Fa (геометрическая сумма):");
        builder.AppendLine($"     F_δ ({v_Fδ.x:F4}; {v_Fδ.y:F4}) + Fa ({v_Fa.x:F4}; {v_Fa.y:F4})");
        builder.AppendLine($"     |F_0| = " + emfDetails["F_0"]);
        builder.AppendLine();

        builder.AppendLine($"  5. E_0 по ХХХ из |F_0|: " + emfDetails["E_0 (вектор)"]);
        builder.AppendLine($"     (угол E_0 = ∠F_0 - 90° — отстаёт)");
        builder.AppendLine();

        builder.AppendLine("=== ПОВЫШЕНИЕ НАПРЯЖЕНИЯ ПРИ СБРОСЕ НАГРУЗКИ ===");
        builder.AppendLine($"  ΔU_o = (E_0 - U_ном) / U_ном · 100%");
        builder.AppendLine($"       = " + emfDetails["ΔU_o"]);
    }
}
