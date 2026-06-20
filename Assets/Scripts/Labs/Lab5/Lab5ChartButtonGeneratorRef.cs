using UnityEngine;
using UnityEngine.UI;

public class Lab5ChartButtonGeneratorRef : MonoBehaviour
{
    public enum TargetTable
    {
        Table5_1_NoLoad,
        Table5_2_InductiveLoad,
        Table5_3_External,
        Table5_4_Regulating,
        Table5_5_ShortCircuit,
        Table5_6_ReactiveTriangle
    }

    public Lab5ChartTableView tableView;
    public Lab5ChartGraphView graphView;
    public Lab5SyncGeneratorModel controller;

    public bool isSwitchToTable;
    public TargetTable switchToTable;
    public bool isRecordToCurrentTable;
    public bool isClearAll;
    public bool isResetCircuit;
    public bool isEnableShortCircuit;
    public bool isDisableShortCircuit;
    public bool isEnableShortCircuit2Phase;
    public bool isDisableShortCircuit2Phase;

    private void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClick);
    }

    public void ResolveReferences()
    {
        if (tableView == null)
            tableView = FindFirstObjectByType<Lab5ChartTableView>();
        if (graphView == null)
            graphView = FindFirstObjectByType<Lab5ChartGraphView>();
        if (controller == null)
            controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();
    }

    public void OnClick()
    {
        ResolveReferences();

        if (isSwitchToTable && tableView != null)
            tableView.tableType = (Lab5ChartTableView.TableType)(int)switchToTable;

        if (isRecordToCurrentTable)
            RecordToCurrent();

        if (isClearAll && controller != null)
            controller.ClearAllCharacteristicData();

        if (isResetCircuit && controller != null)
            controller.ResetCircuit();

        if (isEnableShortCircuit && controller != null)
            controller.EnableShortCircuitMode();

        if (isDisableShortCircuit && controller != null)
            controller.DisableShortCircuitMode();

        if (isEnableShortCircuit2Phase && controller != null)
            controller.EnableShortCircuit2PhaseMode();

        if (isDisableShortCircuit2Phase && controller != null)
            controller.DisableShortCircuit2PhaseMode();

        if (tableView != null)
            tableView.Refresh();
        if (graphView != null)
            graphView.Refresh();
    }

    private void RecordToCurrent()
    {
        if (tableView == null || controller == null) return;

        switch (tableView.tableType)
        {
            case Lab5ChartTableView.TableType.Table5_1_NoLoad:
                controller.RecordNoLoadPoint();
                break;
            case Lab5ChartTableView.TableType.Table5_2_InductiveLoad:
                controller.RecordInductiveLoadPoint();
                break;
            case Lab5ChartTableView.TableType.Table5_3_External:
                controller.RecordExternalPoint();
                break;
            case Lab5ChartTableView.TableType.Table5_4_Regulating:
                controller.RecordRegulatingPoint();
                break;
            case Lab5ChartTableView.TableType.Table5_5_ShortCircuit:
                if (controller.isShortCircuit2PhaseMode)
                    controller.RecordShortCircuit2PhasePoint();
                else
                    controller.RecordShortCircuitPoint();
                break;
        }
    }
}
