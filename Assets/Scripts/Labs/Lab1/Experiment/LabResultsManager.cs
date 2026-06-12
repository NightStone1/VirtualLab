using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LabResultsManager : MonoBehaviour
{
    private const int MaxTable22Rows = 5;
    private const int MaxTable23Rows = 5;
    private const int MaxTable24Rows = 5;
    private const int MaxTable25Rows = 5;
    private const float NominalVoltageMin = 200f;
    private const float NominalVoltageMax = 230f;
    private const float NominalVoltageTarget = 220f;
    private const float NominalVoltageTolerance = 10f;
    private const float MinRunningRpm = 500f;
    private const float MinTable23Voltage = 150f;
    private const float R1ZeroTolerance = 3f;
    private const float R1HundredTolerance = 3f;
    private const float R3RecommendedMin = 15f;
    private const float R3RecommendedMax = 35f;
    private const float Table24RpmTolerance = 100f;
    private const float RegulatorTolerancePercent = 2f;
    private const float VoltageTolerance = 2f;
    private const float CurrentTolerance = 0.03f;
    private const float MilliAmpTolerance = 5f;

    [SerializeField] private ExperimentManager experimentManager;
    [SerializeField] private LabTableMapper tableMapper;

    [SerializeField] private LabMode currentMode = LabMode.Table22_Working;

    private readonly List<Table22Row> table22Rows = new();
    private readonly List<Table23Row> table23Rows = new();
    private readonly List<Table24Row> table24Rows = new();
    private readonly List<Table25Row> table25Rows = new();

    private readonly List<MeasurementPoint> table22Points = new();
    private readonly List<MeasurementPoint> table23Points = new();
    private readonly List<MeasurementPoint> table24Points = new();
    private readonly List<MeasurementPoint> table25Points = new();

    public IReadOnlyList<Table22Row> Table22Rows => table22Rows;
    public IReadOnlyList<Table23Row> Table23Rows => table23Rows;
    public IReadOnlyList<Table24Row> Table24Rows => table24Rows;
    public IReadOnlyList<Table25Row> Table25Rows => table25Rows;

    public LabMode CurrentMode => currentMode;
    public string LastMessage { get; private set; }

    public void SetMode(LabMode mode)
    {
        currentMode = mode;
    }

    public void SetModeTable22()
    {
        currentMode = LabMode.Table22_Working;

        Debug.Log($"������� ����� ������������: {currentMode}");
    }

    public void SetModeTable23()
    {
        currentMode = LabMode.Table23_OmegaFromU;
        Debug.Log($"������� ����� ������������: {currentMode}");
    }

    public void SetModeTable24()
    {
        currentMode = LabMode.Table24_IfFromIa;
        Debug.Log($"������� ����� ������������: {currentMode}");
    }

    public void SetModeTable25()
    {
        currentMode = LabMode.Table25_OmegaFromIf;
        Debug.Log($"������� ����� ������������: {currentMode}");
    }

    public bool RemoveLastRowInCurrentMode()
    {
        bool wasCompleted = IsCompleted();
        bool removed;

        switch (currentMode)
        {
            case LabMode.Table22_Working:
                removed = RemoveLast(table22Rows, table22Points, false);
                break;

            case LabMode.Table23_OmegaFromU:
                removed = RemoveLast(table23Rows, table23Points, false);
                break;

            case LabMode.Table24_IfFromIa:
                removed = RemoveLast(table24Rows, table24Points, false);
                break;

            case LabMode.Table25_OmegaFromIf:
                removed = RemoveLast(table25Rows, table25Points, false);
                break;

            default:
                Debug.LogWarning($"RemoveLastRowInCurrentMode: ����������� ����� {currentMode}");
                return false;
        }

        if (removed && wasCompleted && !IsCompleted())
        {
            SetMessage("Последняя точка удалена. Завершение снято: доберите точку в текущей таблице.");
        }

        return removed;
    }

    private bool RemoveLast<T>(List<T> rows, List<MeasurementPoint> points, bool setDefaultMessage = true)
    {
        if (rows == null || rows.Count == 0)
        {
            SetMessage("В текущей таблице нет точек для удаления.", true);
            return false;
        }

        if (points != null && points.Count > 0)
        {
            MeasurementPoint point = points[points.Count - 1];
            if (point != null && experimentManager != null)
            {
                experimentManager.RemovePoint(point.index);
            }

            points.RemoveAt(points.Count - 1);
        }

        rows.RemoveAt(rows.Count - 1);
        if (setDefaultMessage)
        {
            SetMessage("Последняя точка текущей таблицы удалена.");
        }
        else
        {
            LastMessage = "Последняя точка текущей таблицы удалена.";
        }

        return true;
    }


    public void CaptureCurrentModePoint()
    {
        TryCaptureCurrentModePoint();
    }

    public bool TryCaptureCurrentModePoint()
    {

        if (experimentManager == null)
        {
            Debug.LogError("LabResultsManager: ExperimentManager �� ��������.");
            SetMessage("Не удалось записать точку: ExperimentManager не назначен.", true);
            return false;
        }

        if (tableMapper == null)
        {
            Debug.LogError("LabResultsManager: LabTableMapper �� ��������.");
            SetMessage("Не удалось записать точку: LabTableMapper не назначен.", true);
            return false;
        }

        if (IsCompleted())
        {
            SetMessage("Все таблицы уже заполнены. Удалите точку для исправления или перейдите к графикам.", true);
            return false;
        }

        ExperimentSeries series = ConvertModeToSeries(currentMode);
        if (series == ExperimentSeries.None)
        {
            SetMessage("Не выбран режим таблицы для записи точки.", true);
            return false;
        }

        if (GetCurrentRowCount() >= GetMaxRows(currentMode))
        {
            SetMessage("В этой таблице уже записано 5 точек. Удалите последнюю точку или перейдите к следующей таблице.", true);
            return false;
        }

        MeasurementPoint point = experimentManager.CapturePoint(series);
        if (point == null)
        {
            SetMessage("Не удалось получить измерительную точку.", true);
            return false;
        }

        if (!ValidatePointForCurrentMode(point, out string validationMessage))
        {
            experimentManager.RemovePoint(point.index);
            SetMessage(validationMessage, true);
            return false;
        }

        if (IsDuplicatePoint(point))
        {
            experimentManager.RemovePoint(point.index);
            SetMessage("Такая точка уже записана. Измените режим стенда перед повторной записью.", true);
            return false;
        }

        Debug.Log(
    $"MEASURE RAW | " +
    $"PV1={point.pv1Voltage:F3}, " +
    $"PA1={point.pa1Current:F3}, " +
    $"PA2mA={point.pa2CurrentMilliAmp:F3}, " +
    $"PV2={point.pv2Voltage:F3}, " +
    $"PA4={point.pa4Current:F3}, " +
    $"RPM={point.rpm:F1}"
);
        switch (currentMode)
        {
            case LabMode.Table22_Working:
                {
                    Table22Row row = tableMapper.BuildTable22Row(point);
                    table22Rows.Add(row);
                    table22Points.Add(point);

                    Debug.Log(
                        $"��������� ������ Table22: " +
                        $"Ug={row.Ug}, Iaq={row.Iaq}, Ifg={row.Ifg}, N={row.N}, Ur={row.Ur}, Iag={row.Iag}, " +
                        $"P2g={row.P2g}, P2d={row.P2d}, M2d={row.M2d}, Omega={row.Omega}, EtaD={row.EtaD}"
                    );
                    Debug.Log(
    $"TABLE22 | " +
    $"P2g={row.P2g:F2}, " +
    $"P1d={row.P1d:F2}, " +
    $"P2d={row.P2d:F2}, " +
    $"EtaD={row.EtaD:F2}%"
);
                    break;
                }

            case LabMode.Table23_OmegaFromU:
                {
                    Table23Row row = tableMapper.BuildTable23Row(point);
                    table23Rows.Add(row);
                    table23Points.Add(point);

                    Debug.Log(
                        $"��������� ������ Table23: " +
                        $"U={row.U}, N={row.N}, Omega={row.Omega}"
                    );
                    break;
                }

            case LabMode.Table24_IfFromIa:
                {
                    Table24Row row = tableMapper.BuildTable24Row(point);
                    table24Rows.Add(row);
                    table24Points.Add(point);

                    Debug.Log(
                        $"��������� ������ Table24: " +
                        $"If={row.If}, Ia={row.Ia}"
                    );
                    break;
                }

            case LabMode.Table25_OmegaFromIf:
                {
                    Table25Row row = tableMapper.BuildTable25Row(point);
                    table25Rows.Add(row);
                    table25Points.Add(point);

                    Debug.Log(
                        $"��������� ������ Table25: " +
                        $"If={row.If}, Omega={row.Omega}"
                    );
                    break;
                }

            default:
                Debug.LogWarning("LabResultsManager: �� ������ ����� ������������.");
                experimentManager.RemovePoint(point.index);
                SetMessage("Не выбран режим таблицы для записи точки.", true);
                return false;
        }

        SetMessage("Точка записана.");
        return true;
    }

    public void ClearCurrentMode()
    {
        switch (currentMode)
        {
            case LabMode.Table22_Working:
                RemovePoints(table22Points);
                table22Rows.Clear();
                table22Points.Clear();
                break;

            case LabMode.Table23_OmegaFromU:
                RemovePoints(table23Points);
                table23Rows.Clear();
                table23Points.Clear();
                break;

            case LabMode.Table24_IfFromIa:
                RemovePoints(table24Points);
                table24Rows.Clear();
                table24Points.Clear();
                break;

            case LabMode.Table25_OmegaFromIf:
                RemovePoints(table25Points);
                table25Rows.Clear();
                table25Points.Clear();
                break;
        }

        SetMessage("Текущая таблица очищена.");
    }

    public void ClearAllTables()
    {
        RemovePoints(table22Points);
        RemovePoints(table23Points);
        RemovePoints(table24Points);
        RemovePoints(table25Points);

        table22Rows.Clear();
        table23Rows.Clear();
        table24Rows.Clear();
        table25Rows.Clear();
        table22Points.Clear();
        table23Points.Clear();
        table24Points.Clear();
        table25Points.Clear();

        SetMessage("Все таблицы очищены.");
    }

    public void ResetLabResults()
    {
        ClearAllTables();
        currentMode = LabMode.Table22_Working;
        LastMessage = string.Empty;
        SetMessage("Лабораторная работа сброшена. Начните прохождение заново.");
    }

    public IReadOnlyList<Table22Row> GetTable22RowsForGraphs()
    {
        if (table22Rows.Count == 0)
        {
            Debug.LogWarning("LabResultsManager: Table 2.2 graph data requested, but table is empty.");
        }

        return table22Rows.OrderBy(row => row.P2d).ToList();
    }

    public IReadOnlyList<Table23Row> GetTable23RowsForGraphs()
    {
        if (table23Rows.Count == 0)
        {
            Debug.LogWarning("LabResultsManager: Table 2.3 graph data requested, but table is empty.");
        }

        return table23Rows.OrderBy(row => row.U).ToList();
    }

    public IReadOnlyList<Table24Row> GetTable24RowsForGraphs()
    {
        if (table24Rows.Count == 0)
        {
            Debug.LogWarning("LabResultsManager: Table 2.4 graph data requested, but table is empty.");
        }

        return table24Rows.OrderBy(row => row.Ia).ToList();
    }

    public IReadOnlyList<Table25Row> GetTable25RowsForGraphs()
    {
        if (table25Rows.Count == 0)
        {
            Debug.LogWarning("LabResultsManager: Table 2.5 graph data requested, but table is empty.");
        }

        return table25Rows.OrderBy(row => row.If).ToList();
    }

    public bool IsCompleted()
    {
        return table22Rows.Count >= MaxTable22Rows &&
               table23Rows.Count >= MaxTable23Rows &&
               table24Rows.Count >= MaxTable24Rows &&
               table25Rows.Count >= MaxTable25Rows;
    }

    private int GetCurrentRowCount()
    {
        return currentMode switch
        {
            LabMode.Table22_Working => table22Rows.Count,
            LabMode.Table23_OmegaFromU => table23Rows.Count,
            LabMode.Table24_IfFromIa => table24Rows.Count,
            LabMode.Table25_OmegaFromIf => table25Rows.Count,
            _ => 0
        };
    }

    private int GetMaxRows(LabMode mode)
    {
        return mode switch
        {
            LabMode.Table22_Working => MaxTable22Rows,
            LabMode.Table23_OmegaFromU => MaxTable23Rows,
            LabMode.Table24_IfFromIa => MaxTable24Rows,
            LabMode.Table25_OmegaFromIf => MaxTable25Rows,
            _ => int.MaxValue
        };
    }

    private bool IsDuplicatePoint(MeasurementPoint point)
    {
        switch (currentMode)
        {
            case LabMode.Table22_Working:
                return ContainsDuplicateTable22(point);
            case LabMode.Table23_OmegaFromU:
                return ContainsDuplicateTable23(point);
            case LabMode.Table24_IfFromIa:
                return ContainsDuplicateTable24(point);
            case LabMode.Table25_OmegaFromIf:
                return ContainsDuplicateTable25(point);
            default:
                return false;
        }
    }

    private bool ContainsDuplicateTable22(MeasurementPoint point)
    {
        for (int i = 0; i < table22Points.Count; i++)
        {
            MeasurementPoint existing = table22Points[i];
            if (existing != null && existing.q3Enabled == point.q3Enabled && Nearly(existing.r3Percent, point.r3Percent, RegulatorTolerancePercent))
            {
                return true;
            }
        }

        return false;
    }

    private bool ContainsDuplicateTable23(MeasurementPoint point)
    {
        for (int i = 0; i < table23Points.Count; i++)
        {
            MeasurementPoint existing = table23Points[i];
            if (existing != null && Nearly(existing.pv1Voltage, point.pv1Voltage, VoltageTolerance))
            {
                return true;
            }
        }

        return false;
    }

    private bool ContainsDuplicateTable24(MeasurementPoint point)
    {
        for (int i = 0; i < table24Points.Count; i++)
        {
            MeasurementPoint existing = table24Points[i];
            if (existing != null &&
                Nearly(existing.pa1Current, point.pa1Current, CurrentTolerance) &&
                Nearly(existing.pa2CurrentMilliAmp, point.pa2CurrentMilliAmp, MilliAmpTolerance))
            {
                return true;
            }
        }

        return false;
    }

    private bool ContainsDuplicateTable25(MeasurementPoint point)
    {
        for (int i = 0; i < table25Points.Count; i++)
        {
            MeasurementPoint existing = table25Points[i];
            if (existing != null &&
                (Nearly(existing.pa2CurrentMilliAmp, point.pa2CurrentMilliAmp, MilliAmpTolerance) ||
                 Nearly(existing.r1Percent, point.r1Percent, RegulatorTolerancePercent)))
            {
                return true;
            }
        }

        return false;
    }

    private bool ValidatePointForCurrentMode(MeasurementPoint point, out string message)
    {
        if (!point.q1Enabled || !point.q2Enabled)
        {
            message = "Для записи точки включите Q1 и Q2 и дождитесь рабочего режима.";
            return false;
        }

        if (point.rpm <= MinRunningRpm)
        {
            message = "Двигатель должен устойчиво вращаться перед записью точки.";
            return false;
        }

        switch (currentMode)
        {
            case LabMode.Table22_Working:
                return ValidateTable22Point(point, out message);
            case LabMode.Table23_OmegaFromU:
                return ValidateTable23Point(point, out message);
            case LabMode.Table24_IfFromIa:
                return ValidateTable24Point(point, out message);
            case LabMode.Table25_OmegaFromIf:
                return ValidateTable25Point(point, out message);
            default:
                message = "Не выбран режим таблицы для записи точки.";
                return false;
        }
    }

    private bool ValidateTable22Point(MeasurementPoint point, out string message)
    {
        if (!IsNominalVoltage(point.pv1Voltage))
        {
            message = table22Rows.Count == 0
                ? "Для первой точки таблицы 2.2 установите PV1 в рабочий диапазон 200–230 В."
                : "Для таблицы 2.2 поддерживайте PV1 в рабочем диапазоне 200–230 В.";
            return false;
        }

        if (table22Rows.Count == 0)
        {
            if (point.q3Enabled)
            {
                message = "Первая точка таблицы 2.2 снимается на холостом ходу: выключите Q3.";
                return false;
            }

            if (!IsR1Zero(point.r1Percent))
            {
                message = "Для первой точки таблицы 2.2 установите R1 = 0%.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        if (!point.q3Enabled)
        {
            message = "Для нагрузочных точек таблицы 2.2 включите Q3.";
            return false;
        }

        if (point.r3Percent < R3RecommendedMin)
        {
            message = "Для нагрузочной точки таблицы 2.2 задайте ненулевую нагрузку R3, примерно 20–25%.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool ValidateTable23Point(MeasurementPoint point, out string message)
    {
        if (point.q3Enabled)
        {
            message = "Таблица 2.3 снимается без нагрузки: выключите Q3.";
            return false;
        }

        if (!IsR1Zero(point.r1Percent))
        {
            message = "Для таблицы 2.3 установите R1 = 0% и меняйте только PV1.";
            return false;
        }

        if (point.pv1Voltage < MinTable23Voltage)
        {
            message = "Для таблицы 2.3 задайте рабочее значение PV1 и затем запишите точку.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool ValidateTable24Point(MeasurementPoint point, out string message)
    {
        if (!point.q3Enabled)
        {
            message = "Для таблицы 2.4 включите Q3: нужна изменяемая нагрузка.";
            return false;
        }

        if (!IsNearNominalVoltage(point.pv1Voltage))
        {
            message = table24Rows.Count == 0
                ? "Для первой точки таблицы 2.4 установите PV1 примерно 220 В."
                : "Для таблицы 2.4 поддерживайте PV1 примерно 220 В.";
            return false;
        }

        if (table24Rows.Count == 0)
        {
            if (point.r3Percent < R3RecommendedMin || point.r3Percent > R3RecommendedMax)
            {
                message = "Для первой точки таблицы 2.4 задайте R3 примерно 20–30%.";
                return false;
            }

            if (!IsR1Hundred(point.r1Percent))
            {
                message = "Для первой точки таблицы 2.4 установите R1 = 100%, чтобы оставить запас регулирования скорости.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        float targetRpm = table24Points.Count > 0 && table24Points[0] != null ? table24Points[0].rpm : point.rpm;
        if (!Nearly(point.rpm, targetRpm, Table24RpmTolerance))
        {
            message = "Для таблицы 2.4 нужно удерживать скорость: подстройте R1, чтобы RPM было примерно как в первой точке.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool ValidateTable25Point(MeasurementPoint point, out string message)
    {
        if (point.q3Enabled)
        {
            message = "Таблица 2.5 снимается при отключённой нагрузке: выключите Q3.";
            return false;
        }

        if (!IsNearNominalVoltage(point.pv1Voltage))
        {
            message = "Для таблицы 2.5 установите PV1 примерно 220 В.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void RemovePoints(List<MeasurementPoint> points)
    {
        if (points == null || experimentManager == null)
        {
            return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] != null)
            {
                experimentManager.RemovePoint(points[i].index);
            }
        }
    }

    private void SetMessage(string message, bool warning = false)
    {
        LastMessage = message;
        if (warning)
        {
            Debug.LogWarning("LabResultsManager: " + message);
        }
        else
        {
            Debug.Log("LabResultsManager: " + message);
        }
    }

    private static bool Nearly(float a, float b, float tolerance)
    {
        return Mathf.Abs(a - b) <= tolerance;
    }

    private static bool IsNominalVoltage(float voltage)
    {
        return voltage >= NominalVoltageMin && voltage <= NominalVoltageMax;
    }

    private static bool IsNearNominalVoltage(float voltage)
    {
        return Mathf.Abs(voltage - NominalVoltageTarget) <= NominalVoltageTolerance;
    }

    private static bool IsR1Zero(float r1Percent)
    {
        return r1Percent <= R1ZeroTolerance;
    }

    private static bool IsR1Hundred(float r1Percent)
    {
        return r1Percent >= 100f - R1HundredTolerance;
    }

    private ExperimentSeries ConvertModeToSeries(LabMode mode)
    {
        return mode switch
        {
            LabMode.Table22_Working => ExperimentSeries.R3,
            LabMode.Table23_OmegaFromU => ExperimentSeries.PHO,
            LabMode.Table24_IfFromIa => ExperimentSeries.R1,
            LabMode.Table25_OmegaFromIf => ExperimentSeries.R1,
            _ => ExperimentSeries.None
        };
    }

    [ContextMenu("Capture Current Mode Point")]
    private void DebugCaptureCurrentModePoint()
    {
        CaptureCurrentModePoint();
    }
}
