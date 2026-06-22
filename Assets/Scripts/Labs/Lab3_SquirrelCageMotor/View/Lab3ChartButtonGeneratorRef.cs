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

using UnityEngine;
using UnityEngine.UI;

public class Lab3ChartButtonGeneratorRef : MonoBehaviour
{
    public enum TargetTable
    {
        Table3_1_Resistance,
        Table3_2_NoLoad,
        Table3_3_Load,
        Table3_4_External,
        Table3_5_Regulating,
        Table3_6_ShortCircuit
    }

    public Lab3ChartTableView tableView;
    public Lab3ChartGraphView graphView;
    public Lab3_ElectricCircuit controller;
    public Lab3Controller mvpController;

    public bool isSwitchToTable;
    public TargetTable switchToTable;
    public bool isRecordToCurrentTable;
    public bool isRemoveLast;
    public bool isNextStage;
    public bool isClearAll;
    public bool isResetCircuit;
    public bool isToggleShortCircuit;
    public bool isEnableShortCircuit;
    public bool isDisableShortCircuit;
    public bool isToggleResistanceMode;
    public bool isTuneU;

    private bool listenerRegistered;

    private void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null && !listenerRegistered)
        {
            btn.onClick.AddListener(OnClick);
            listenerRegistered = true;
        }
    }

    public void ResolveReferences()
    {
        if (tableView == null)
            tableView = FindFirstObjectByType<Lab3ChartTableView>();
        if (graphView == null)
            graphView = FindFirstObjectByType<Lab3ChartGraphView>();
        if (controller == null)
            controller = FindFirstObjectByType<Lab3_ElectricCircuit>();
        if (mvpController == null)
            mvpController = FindFirstObjectByType<Lab3Controller>();
    }

    public void OnClick()
    {
        ResolveReferences();

        if (isSwitchToTable && tableView != null)
            tableView.tableType = (Lab3ChartTableView.TableType)(int)switchToTable;

        if (isSwitchToTable && mvpController != null)
            mvpController.SwitchToTable((int)switchToTable);

        if (isRecordToCurrentTable)
        {
            if (mvpController != null)
                mvpController.RecordPoint();
            else
                RecordToCurrent();
        }

        if (isRemoveLast && mvpController != null)
            mvpController.RemoveLastPointInCurrentStage();

        if (isNextStage && mvpController != null)
            mvpController.NextStage();

        if (isClearAll && mvpController != null)
            mvpController.ClearCurrentStagePoints();
        else if (isClearAll && controller != null)
            controller.ClearAllCharacteristicData();

        if (isResetCircuit && mvpController != null)
            mvpController.ResetLab();
        else if (isResetCircuit && controller != null)
            controller.ResetCircuit();

        if (isToggleShortCircuit && mvpController != null)
            mvpController.ToggleShortCircuitMode();
        else if (isEnableShortCircuit && mvpController != null && !mvpController.ShortCircuitEnabled)
            mvpController.ToggleShortCircuitMode();
        else if (isEnableShortCircuit && controller != null)
            controller.EnableShortCircuitMode();

        if (isDisableShortCircuit && mvpController != null && mvpController.ShortCircuitEnabled)
            mvpController.ToggleShortCircuitMode();
        else if (isDisableShortCircuit && controller != null)
            controller.DisableShortCircuitMode();

        if (isToggleResistanceMode && mvpController != null)
            mvpController.ToggleResistanceMeasurementMode();

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
            case Lab3ChartTableView.TableType.Table3_1_Resistance:
                tableView.RecordCurrentPoint();
                break;
            case Lab3ChartTableView.TableType.Table3_2_NoLoad:
                controller.RecordNoLoadPoint();
                break;
            case Lab3ChartTableView.TableType.Table3_3_Load:
                controller.RecordLoadPoint();
                break;
            case Lab3ChartTableView.TableType.Table3_4_External:
                controller.RecordExternalPoint();
                break;
            case Lab3ChartTableView.TableType.Table3_5_Regulating:
                controller.RecordRegulatingPoint();
                break;
            case Lab3ChartTableView.TableType.Table3_6_ShortCircuit:
                controller.RecordShortCircuitPoint();
                break;
        }
    }
}
