using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

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
    public bool autoFindController = true;
    public TableType tableType = TableType.Table5_1_NoLoad;
    public TMP_Text targetText;
    public bool autoFindText = true;
    public int maxRows = 15;
    public bool refreshEveryFrame;

    private readonly StringBuilder builder = new StringBuilder(2048);
    private float lastRecordedIf;

    private void Awake()
    {
        ResolveReferences();
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
    }

    private void RefreshTable()
    {
        builder.Clear();
        builder.AppendLine($"Таблица 5.{(int)tableType + 1} — {GetTableTitle()}");
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

    private string GetTableTitle()
    {
        switch (tableType)
        {
            case TableType.Table5_1_NoLoad: return "Характеристика холостого хода E_0 = f(I_в)";
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
        var asc = controller.noLoadAscending;
        var desc = controller.noLoadDescending;

        if (asc.Count == 0 && desc.Count == 0)
        {
            builder.AppendLine("Нет данных. Записывайте точки через кнопку «Записать точку».");
            return;
        }

        builder.AppendLine("Восходящая ветвь:");
        builder.AppendLine("№\tI_в, А\tE_0, В");
        int idx = 1;
        foreach (var p in asc)
        {
            if (idx > maxRows) { builder.AppendLine("..."); break; }
            builder.AppendLine($"{idx}\t{p.x:F3}\t{p.y:F1}");
            idx++;
        }

        if (desc.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Нисходящая ветвь:");
            builder.AppendLine("№\tI_в, А\tE_0, В");
            idx = 1;
            foreach (var p in desc)
            {
                if (idx > maxRows) { builder.AppendLine("..."); break; }
                builder.AppendLine($"{idx}\t{p.x:F3}\t{p.y:F1}");
                idx++;
            }
        }
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
