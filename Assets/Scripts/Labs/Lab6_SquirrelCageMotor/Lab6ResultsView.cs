using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Lab6ResultsView : MonoBehaviour
{
    private enum ActiveView
    {
        NoLoadTable,
        ShortCircuitTable,
        LoadTable,
        ResistanceTable,
        GraphCurrent,
        GraphSpeed,
        GraphEfficiency
    }

    [SerializeField] private Lab6Controller controller;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI tableTitleText;
    [SerializeField] private Transform tableRowsRoot;
    [SerializeField] private Lab6TableRowView tableRowPrefab;
    [SerializeField] private Lab6GraphView graphView;
    [SerializeField] private GameObject tablePanel;
    [SerializeField] private GameObject graphPanel;

    private ActiveView activeView = ActiveView.NoLoadTable;
    private ActiveView renderedView = (ActiveView)(-1);
    private int renderedPointCount = -1;
    private bool refreshAllInProgress;
    private bool missingTableSetupWarningLogged;
    private bool missingControllerWarningLogged;

    private void Awake()
    {
        if (controller == null)
        {
            controller = FindAnyLab6Controller();
        }

        if (controller != null)
        {
            BindController(controller);
        }
    }

    private void Start()
    {
        RefreshAll();
    }

    public void BindController(Lab6Controller value)
    {
        controller = value;
        RefreshAll();
    }

    public void RefreshAll()
    {
        SetText(titleText, "Лабораторная 6. Результаты испытаний асинхронного двигателя");
        SetText(stageText, controller != null ? "Этап: " + GetStageName(controller.CurrentStage) + "\n" + controller.WindingConnectionText : "Этап: контроллер не назначен");

        refreshAllInProgress = true;
        switch (activeView)
        {
            case ActiveView.ShortCircuitTable:
                ShowShortCircuitTable();
                break;
            case ActiveView.LoadTable:
                ShowLoadTable();
                break;
            case ActiveView.ResistanceTable:
                ShowResistanceTable();
                break;
            case ActiveView.GraphCurrent:
                ShowCurrentGraph();
                break;
            case ActiveView.GraphSpeed:
                ShowSpeedGraph();
                break;
            case ActiveView.GraphEfficiency:
                ShowEfficiencyGraph();
                break;
            default:
                ShowNoLoadTable();
                break;
        }
        refreshAllInProgress = false;
    }

    public void ShowNoLoadTable()
    {
        activeView = ActiveView.NoLoadTable;
        SetPanels(true);
        SetText(tableTitleText, "Опыт холостого хода");
        RebuildTable(
            controller != null ? controller.NoLoadMeasurements : null,
            new[] { "№", "U0", "I0", "P0", "cosφ", "n2" },
            (index, point) => new[]
            {
                (index + 1).ToString(),
                point.voltage.ToString("F0"),
                point.current.ToString("F2"),
                point.powerInput.ToString("F0"),
                point.cosPhi.ToString("F2"),
                point.speed.ToString("F0")
            }, !refreshAllInProgress);
    }

    public void ShowShortCircuitTable()
    {
        activeView = ActiveView.ShortCircuitTable;
        SetPanels(true);
        SetText(tableTitleText, "Опыт короткого замыкания / заторможенного ротора");
        RebuildTable(
            controller != null ? controller.ShortCircuitMeasurements : null,
            new[] { "№", "Uk", "Ik", "Pk", "cosφk", "n2" },
            (index, point) => new[]
            {
                (index + 1).ToString(),
                point.voltage.ToString("F0"),
                point.current.ToString("F2"),
                point.powerInput.ToString("F0"),
                point.cosPhi.ToString("F2"),
                point.speed.ToString("F0")
            }, !refreshAllInProgress);
    }

    public void ShowLoadTable()
    {
        activeView = ActiveView.LoadTable;
        SetPanels(true);
        SetText(tableTitleText, "Опыт непосредственной нагрузки");
        RebuildTable(
            controller != null ? controller.LoadMeasurements : null,
            new[] { "№", "Load%", "U1", "I1", "P1", "P2", "n2", "M2", "cosφ", "η", "S" },
            (index, point) => new[]
            {
                (index + 1).ToString(),
                point.loadPercent.ToString("F0"),
                point.voltage.ToString("F0"),
                point.current.ToString("F2"),
                point.powerInput.ToString("F0"),
                point.powerOutput.ToString("F0"),
                point.speed.ToString("F0"),
                point.torque.ToString("F2"),
                point.cosPhi.ToString("F2"),
                point.efficiency.ToString("F2"),
                point.slip.ToString("F2")
            }, !refreshAllInProgress);
    }

    public void ShowGraphs()
    {
        ShowCurrentGraph();
    }

    public void ShowResistanceTable()
    {
        activeView = ActiveView.ResistanceTable;
        SetPanels(true);
        SetText(tableTitleText, "Измерение сопротивлений обмоток, Ом");
        RebuildTable(
            controller != null ? controller.ResistanceMeasurements : null,
            new[] { "Za", "Zb", "Zc", "Zср" },
            (index, point) => new[]
            {
                point.za.ToString("F2"),
                point.zb.ToString("F2"),
                point.zc.ToString("F2"),
                point.zAverage.ToString("F2")
            }, !refreshAllInProgress);
    }

    public void ShowCurrentGraph()
    {
        activeView = ActiveView.GraphCurrent;
        SetPanels(false);
        if (graphView != null)
        {
            graphView.ShowCurrentByPowerGraph(controller != null ? controller.LoadMeasurements : null);
        }
    }

    public void ShowSpeedGraph()
    {
        activeView = ActiveView.GraphSpeed;
        SetPanels(false);
        if (graphView != null)
        {
            graphView.ShowSpeedByPowerGraph(controller != null ? controller.LoadMeasurements : null);
        }
    }

    public void ShowEfficiencyGraph()
    {
        activeView = ActiveView.GraphEfficiency;
        SetPanels(false);
        if (graphView != null)
        {
            graphView.ShowEfficiencyByPowerGraph(controller != null ? controller.LoadMeasurements : null);
        }
    }

    public void ResetLab()
    {
        if (controller == null)
        {
            if (!missingControllerWarningLogged)
            {
                Debug.LogWarning("Lab6ResultsView: controller is not assigned, ResetLab cannot be executed.");
                missingControllerWarningLogged = true;
            }

            return;
        }

        controller.ResetLab();
    }

    public void RecordPoint()
    {
        if (TryGetController())
        {
            controller.RecordPoint();
        }
    }

    public void RemoveLastPoint()
    {
        if (TryGetController())
        {
            controller.RemoveLastPointInCurrentStage();
        }
    }

    public void NextStage()
    {
        if (TryGetController())
        {
            controller.NextStage();
        }
    }

    private void RebuildTable(IReadOnlyList<Lab6Measurement> points, string[] headers, System.Func<int, Lab6Measurement, string[]> createRow, bool forceRebuild)
    {
        int pointCount = points != null ? points.Count : 0;
        if (!forceRebuild && activeView == renderedView && pointCount == renderedPointCount)
        {
            return;
        }

        ClearRows();

        if (!HasTableSetup())
        {
            return;
        }

        Instantiate(tableRowPrefab, tableRowsRoot).SetCells(headers);

        if (points == null)
        {
            return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            Lab6Measurement point = points[i];
            if (point == null)
            {
                continue;
            }

            Instantiate(tableRowPrefab, tableRowsRoot).SetCells(createRow(i, point));
        }

        renderedView = activeView;
        renderedPointCount = pointCount;
    }

    private void ClearRows()
    {
        if (tableRowsRoot == null)
        {
            return;
        }

        for (int i = tableRowsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(tableRowsRoot.GetChild(i).gameObject);
        }
    }

    private bool HasTableSetup()
    {
        if (tableRowsRoot != null && tableRowPrefab != null)
        {
            return true;
        }

        if (!missingTableSetupWarningLogged)
        {
            Debug.LogWarning("Lab6ResultsView: tableRowsRoot and tableRowPrefab must be assigned to show tables.");
            missingTableSetupWarningLogged = true;
        }

        return false;
    }

    private bool TryGetController()
    {
        if (controller != null)
        {
            return true;
        }

        controller = FindAnyLab6Controller();
        if (controller != null)
        {
            return true;
        }

        if (!missingControllerWarningLogged)
        {
            Debug.LogWarning("Lab6ResultsView: controller is not assigned.");
            missingControllerWarningLogged = true;
        }

        return false;
    }

    private void SetPanels(bool showTable)
    {
        if (tablePanel != null)
        {
            if (tablePanel.activeSelf != showTable)
            {
                tablePanel.SetActive(showTable);
            }
        }

        if (graphPanel != null)
        {
            bool showGraph = !showTable;
            if (graphPanel.activeSelf != showGraph)
            {
                graphPanel.SetActive(showGraph);
            }
        }
    }

    private static Lab6Controller FindAnyLab6Controller()
    {
        Lab6Controller[] controllers = FindObjectsByType<Lab6Controller>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return controllers.Length > 0 ? controllers[0] : null;
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null && target.text != value)
        {
            target.text = value;
        }
    }

    private static string GetStageName(Lab6Stage stage)
    {
        switch (stage)
        {
            case Lab6Stage.NoLoad:
                return "Опыт холостого хода";
            case Lab6Stage.ShortCircuit:
                return "Опыт короткого замыкания";
            case Lab6Stage.Load:
                return "Опыт непосредственной нагрузки";
            case Lab6Stage.ResistanceMeasurement:
                return "Измерение сопротивлений обмоток";
            case Lab6Stage.Completed:
                return "Лабораторная завершена";
            default:
                return "Подготовка";
        }
    }
}
