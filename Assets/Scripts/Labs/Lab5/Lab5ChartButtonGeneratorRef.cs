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
    private Lab5SyncGeneratorLabController labController;

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
        if (labController == null)
            labController = FindFirstObjectByType<Lab5SyncGeneratorLabController>();
    }

    public void OnClick()
    {
        ResolveReferences();

        if (isSwitchToTable && tableView != null)
            tableView.tableType = (Lab5ChartTableView.TableType)(int)switchToTable;

        if (isRecordToCurrentTable)
            RecordToCurrent();

        if (isClearAll)
        {
            if (labController != null)
                labController.ClearAllCharacteristicData();
            else if (controller != null)
                controller.ClearAllCharacteristicData();
        }

        if (isResetCircuit && controller != null)
            controller.ResetCircuit();

        if (isEnableShortCircuit)
        {
            if (labController != null)
                labController.EnableShortCircuitMode();
            else if (controller != null)
                controller.EnableShortCircuitMode();
        }

        if (isDisableShortCircuit)
        {
            if (labController != null)
                labController.DisableShortCircuitMode();
            else if (controller != null)
                controller.DisableShortCircuitMode();
        }

        if (isEnableShortCircuit2Phase)
        {
            if (labController != null)
                labController.EnableShortCircuit2PhaseMode();
            else if (controller != null)
                controller.EnableShortCircuit2PhaseMode();
        }

        if (isDisableShortCircuit2Phase)
        {
            if (labController != null)
                labController.DisableShortCircuit2PhaseMode();
            else if (controller != null)
                controller.DisableShortCircuit2PhaseMode();
        }

        if (tableView != null)
            tableView.Refresh();
        if (graphView != null)
            graphView.Refresh();
    }

    private void RecordToCurrent()
    {
        if (tableView == null || controller == null) return;

        // Если есть лаб-контроллер — запись через него (с обратной связью)
        if (labController != null)
        {
            switch (tableView.tableType)
            {
                case Lab5ChartTableView.TableType.Table5_1_NoLoad:
                    labController.RecordNoLoadPoint(); break;
                case Lab5ChartTableView.TableType.Table5_2_InductiveLoad:
                    labController.RecordInductiveLoadPoint(); break;
                case Lab5ChartTableView.TableType.Table5_3_External:
                    labController.RecordExternalPoint(); break;
                case Lab5ChartTableView.TableType.Table5_4_Regulating:
                    labController.RecordRegulatingPoint(); break;
                case Lab5ChartTableView.TableType.Table5_5_ShortCircuit:
                    if (controller.isShortCircuit2PhaseMode)
                        labController.RecordShortCircuit2PhasePoint();
                    else
                        labController.RecordShortCircuitPoint();
                    break;
            }
        }
        else
        {
            // Fallback: прямой вызов модели без сообщения
            switch (tableView.tableType)
            {
                case Lab5ChartTableView.TableType.Table5_1_NoLoad:
                    controller.RecordNoLoadPoint(); break;
                case Lab5ChartTableView.TableType.Table5_2_InductiveLoad:
                    controller.RecordInductiveLoadPoint(); break;
                case Lab5ChartTableView.TableType.Table5_3_External:
                    controller.RecordExternalPoint(); break;
                case Lab5ChartTableView.TableType.Table5_4_Regulating:
                    controller.RecordRegulatingPoint(); break;
                case Lab5ChartTableView.TableType.Table5_5_ShortCircuit:
                    if (controller.isShortCircuit2PhaseMode)
                        controller.RecordShortCircuit2PhasePoint();
                    else
                        controller.RecordShortCircuitPoint();
                    break;
            }
        }
    }
}
