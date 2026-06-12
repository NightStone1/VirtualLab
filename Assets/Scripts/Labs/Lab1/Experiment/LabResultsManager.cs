using System.Collections.Generic;
using UnityEngine;

public class LabResultsManager : MonoBehaviour
{
    private const int MaxTable22Rows = 5;
    private const int MaxTable23Rows = 5;
    private const int MaxTable24Rows = 5;
    private const int MaxTable25Rows = 5;
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
        switch (currentMode)
        {
            case LabMode.Table22_Working:
                return RemoveLast(table22Rows, table22Points);

            case LabMode.Table23_OmegaFromU:
                return RemoveLast(table23Rows, table23Points);

            case LabMode.Table24_IfFromIa:
                return RemoveLast(table24Rows, table24Points);

            case LabMode.Table25_OmegaFromIf:
                return RemoveLast(table25Rows, table25Points);

            default:
                Debug.LogWarning($"RemoveLastRowInCurrentMode: ����������� ����� {currentMode}");
                return false;
        }
    }

    private bool RemoveLast<T>(List<T> rows, List<MeasurementPoint> points)
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
        SetMessage("Последняя точка текущей таблицы удалена.");
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
