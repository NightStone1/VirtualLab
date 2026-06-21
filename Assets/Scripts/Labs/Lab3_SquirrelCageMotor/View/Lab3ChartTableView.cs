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
    public TableType tableType = TableType.Table3_2_NoLoad;
    public TMP_Text targetText;
    public bool autoFindText = true;
    public int maxRows = 15;
    public bool refreshEveryFrame;

    private readonly StringBuilder builder = new StringBuilder(2048);
    private readonly List<ResistancePoint> resistancePoints = new List<ResistancePoint>();

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
            voltage = controller.PV2Value,
            current = controller.PA1Value
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
        builder.AppendLine("Таблица 3.2 — Характеристика холостого хода");
        builder.AppendLine("Восходящая ветвь             | Нисходящая ветвь");
        builder.AppendLine("If, А    | Ea, В             | If, А    | Ea, В");
        builder.AppendLine("---------|---------          |---------|---------");

        var points = mvpController != null ? mvpController.GetNoLoadData() : controller.GetNoLoadData();
        if (points.Count == 0)
        {
            builder.Append("Нет данных. Записывайте точки через кнопку «Записать ХХХ».");
            return;
        }

        int splitIndex = FindSplitIndex(points);
        int maxRows = Mathf.Min(this.maxRows, points.Count);

        for (int i = 0; i < maxRows; i++)
        {
            string ifAsc  = i < splitIndex ? points[i].x.ToString("F3") : "—";
            string eaAsc  = i < splitIndex ? points[i].y.ToString("F2") : "—";
            int descIdx = i + splitIndex;
            string ifDesc = descIdx < points.Count ? points[descIdx].x.ToString("F3") : "—";
            string eaDesc = descIdx < points.Count ? points[descIdx].y.ToString("F2") : "—";

            builder.Append(ifAsc).Append("     | ").Append(eaAsc).Append("           | ")
                .Append(ifDesc).Append("     | ").Append(eaDesc)
                .AppendLine();
        }
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
        builder.AppendLine("Таблица 3.5 — Регулировочная характеристика (U = const)");
        builder.AppendLine("Восходящая ветвь             | Нисходящая ветвь");
        builder.AppendLine("Iа, А    | If, А             | Iа, А    | If, А");
        builder.AppendLine("---------|---------          |---------|---------");

        var points = mvpController != null ? mvpController.GetRegulatingData() : controller.GetRegulatingData();
        if (points.Count == 0)
        {
            builder.Append("Нет данных.");
            return;
        }

        int splitIndex = FindSplitIndex(points);
        int maxRows = Mathf.Min(this.maxRows, points.Count);

        for (int i = 0; i < maxRows; i++)
        {
            string iaAsc  = i < splitIndex ? points[i].x.ToString("F2") : "—";
            string ifAsc  = i < splitIndex ? points[i].y.ToString("F4") : "—";
            int descIdx = i + splitIndex;
            string iaDesc = descIdx < points.Count ? points[descIdx].x.ToString("F2") : "—";
            string ifDesc = descIdx < points.Count ? points[descIdx].y.ToString("F4") : "—";

            builder.Append(iaAsc).Append("     | ").Append(ifAsc).Append("     | ")
                .Append(iaDesc).Append("     | ").Append(ifDesc)
                .AppendLine();
        }
    }

    private void BuildTable3_6()
    {
        builder.AppendLine("Таблица 3.6 — Характеристика короткого замыкания");
        builder.AppendLine("№ | Iа, А | If, А");
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
    }

    private void RebuildLayout()
    {
        if (targetText != null)
        {
            var rt = targetText.GetComponent<RectTransform>();
            if (rt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }
}
