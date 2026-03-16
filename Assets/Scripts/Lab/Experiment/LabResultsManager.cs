using System.Collections.Generic;
using UnityEngine;

public class LabResultsManager : MonoBehaviour
{
    [SerializeField] private ExperimentManager experimentManager;
    [SerializeField] private LabTableMapper tableMapper;

    [SerializeField] private LabMode currentMode = LabMode.Table22_Working;

    private readonly List<Table22Row> table22Rows = new();
    private readonly List<Table23Row> table23Rows = new();
    private readonly List<Table24Row> table24Rows = new();
    private readonly List<Table25Row> table25Rows = new();

    public IReadOnlyList<Table22Row> Table22Rows => table22Rows;
    public IReadOnlyList<Table23Row> Table23Rows => table23Rows;
    public IReadOnlyList<Table24Row> Table24Rows => table24Rows;
    public IReadOnlyList<Table25Row> Table25Rows => table25Rows;

    public LabMode CurrentMode => currentMode;

    public void SetMode(int modeIndex)
    {
        currentMode = (LabMode)modeIndex;
        Debug.Log($"Текущий режим лабораторной: {currentMode}");
    }

    public void SetModeTable22()
    {
        currentMode = LabMode.Table22_Working;
        
        Debug.Log($"Текущий режим лабораторной: {currentMode}");
    }

    public void SetModeTable23()
    {
        currentMode = LabMode.Table23_OmegaFromU;
        Debug.Log($"Текущий режим лабораторной: {currentMode}");
    }

    public void SetModeTable24()
    {
        currentMode = LabMode.Table24_IfFromIa;
        Debug.Log($"Текущий режим лабораторной: {currentMode}");
    }

    public void SetModeTable25()
    {
        currentMode = LabMode.Table25_OmegaFromIf;
        Debug.Log($"Текущий режим лабораторной: {currentMode}");
    }

    public void CaptureCurrentModePoint()
    {
        if (experimentManager == null)
        {
            Debug.LogError("LabResultsManager: ExperimentManager не назначен.");
            return;
        }

        if (tableMapper == null)
        {
            Debug.LogError("LabResultsManager: LabTableMapper не назначен.");
            return;
        }

        ExperimentSeries series = ConvertModeToSeries(currentMode);

        MeasurementPoint point = experimentManager.CapturePoint(series);
        if (point == null)
            return;

        switch (currentMode)
        {
            case LabMode.Table22_Working:
                {
                    Table22Row row = tableMapper.BuildTable22Row(point);
                    table22Rows.Add(row);

                    Debug.Log(
                        $"Добавлена строка Table22: " +
                        $"Ug={row.Ug}, Iaq={row.Iaq}, Ifg={row.Ifg}, N={row.N}, Ur={row.Ur}, Iag={row.Iag}, " +
                        $"P2g={row.P2g}, P2d={row.P2d}, M2d={row.M2d}, Omega={row.Omega}, EtaD={row.EtaD}"
                    );
                    break;
                }

            case LabMode.Table23_OmegaFromU:
                {
                    Table23Row row = tableMapper.BuildTable23Row(point);
                    table23Rows.Add(row);

                    Debug.Log(
                        $"Добавлена строка Table23: " +
                        $"U={row.U}, N={row.N}, Omega={row.Omega}"
                    );
                    break;
                }

            case LabMode.Table24_IfFromIa:
                {
                    Table24Row row = tableMapper.BuildTable24Row(point);
                    table24Rows.Add(row);

                    Debug.Log(
                        $"Добавлена строка Table24: " +
                        $"If={row.If}, Ia={row.Ia}"
                    );
                    break;
                }

            case LabMode.Table25_OmegaFromIf:
                {
                    Table25Row row = tableMapper.BuildTable25Row(point);
                    table25Rows.Add(row);

                    Debug.Log(
                        $"Добавлена строка Table25: " +
                        $"If={row.If}, Omega={row.Omega}"
                    );
                    break;
                }

            default:
                Debug.LogWarning("LabResultsManager: не выбран режим лабораторной.");
                break;
        }
    }

    public void ClearCurrentMode()
    {
        switch (currentMode)
        {
            case LabMode.Table22_Working:
                table22Rows.Clear();
                break;

            case LabMode.Table23_OmegaFromU:
                table23Rows.Clear();
                break;

            case LabMode.Table24_IfFromIa:
                table24Rows.Clear();
                break;

            case LabMode.Table25_OmegaFromIf:
                table25Rows.Clear();
                break;
        }

        Debug.Log($"Очищены данные режима: {currentMode}");
    }

    public void ClearAllTables()
    {
        table22Rows.Clear();
        table23Rows.Clear();
        table24Rows.Clear();
        table25Rows.Clear();

        Debug.Log("Очищены все таблицы лабораторной.");
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