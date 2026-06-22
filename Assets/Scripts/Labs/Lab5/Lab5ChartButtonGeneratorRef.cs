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

using TMPro;
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
    public bool isRemoveCurrentPoint;
    public bool isNextStage;
    public bool isResetCircuit;
    public bool isEnableShortCircuit;
    public bool isDisableShortCircuit;
    public bool isToggleShortCircuit;
    public bool isEnableShortCircuit2Phase;
    public bool isDisableShortCircuit2Phase;
    public bool isToggleShortCircuit2Phase;

    private void Awake()
    {
        AutoConfigureFromObjectName();
    }

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
            tableView.SwitchToTable((Lab5ChartTableView.TableType)(int)switchToTable);

        if (isRecordToCurrentTable)
            RunControllerAction(c => c.RecordCurrentPoint());

        if (isClearAll || isRemoveCurrentPoint)
            RunControllerAction(c => c.RemoveCurrentPoint());

        if (isNextStage)
            RunControllerAction(c => c.ConfirmCurrentStage());

        if (isResetCircuit)
            RunControllerAction(c => c.ResetLab());

        if (isEnableShortCircuit || isDisableShortCircuit || isToggleShortCircuit)
            RunControllerAction(c => c.ToggleShortCircuitMode());

        if (isEnableShortCircuit2Phase || isDisableShortCircuit2Phase || isToggleShortCircuit2Phase)
            RunControllerAction(c => c.ToggleShortCircuit2PhaseMode());

        if (tableView != null)
            tableView.Refresh();
        if (graphView != null)
            graphView.Refresh();
    }

    private void RunControllerAction(System.Action<Lab5SyncGeneratorLabController> action)
    {
        if (labController == null)
        {
            Debug.LogWarning("Lab5 TV button: Lab5SyncGeneratorLabController not found; action skipped.");
            return;
        }

        action(labController);
    }

    private void AutoConfigureFromObjectName()
    {
        string n = gameObject.name;
        if (n.Contains("T5_1") || n.Contains("Т5_1")) SetTable(TargetTable.Table5_1_NoLoad);
        else if (n.Contains("T5_2") || n.Contains("Т5_2")) SetTable(TargetTable.Table5_2_InductiveLoad);
        else if (n.Contains("T5_3") || n.Contains("Т5_3")) SetTable(TargetTable.Table5_3_External);
        else if (n.Contains("T5_4") || n.Contains("Т5_4")) SetTable(TargetTable.Table5_4_Regulating);
        else if (n.Contains("T5_5") || n.Contains("Т5_5")) SetTable(TargetTable.Table5_5_ShortCircuit);
        else if (n.Contains("T5_6") || n.Contains("Т5_6")) SetTable(TargetTable.Table5_6_ReactiveTriangle);
        else if (n.Contains("Записать")) SetAction(record: true);
        else if (n.Contains("Удалить")) SetAction(remove: true);
        else if (n.Contains("Следующий")) SetAction(next: true);
        else if (n.Contains("Сброс")) SetAction(reset: true);
        else if (n.Contains("3ф") && n.Contains("КЗ"))
        {
            SetAction(toggleShortCircuit: true);
            SetButtonText("3ф КЗ");
        }
        else if (n.Contains("2ф") && n.Contains("КЗ"))
        {
            SetAction(toggleShortCircuit2Phase: true);
            SetButtonText("2ф КЗ");
        }
    }

    private void SetTable(TargetTable targetTable)
    {
        ClearActions();
        isSwitchToTable = true;
        switchToTable = targetTable;
    }

    private void SetAction(bool record = false, bool remove = false, bool next = false, bool reset = false, bool toggleShortCircuit = false, bool toggleShortCircuit2Phase = false)
    {
        ClearActions();
        isRecordToCurrentTable = record;
        isRemoveCurrentPoint = remove;
        isNextStage = next;
        isResetCircuit = reset;
        isToggleShortCircuit = toggleShortCircuit;
        isToggleShortCircuit2Phase = toggleShortCircuit2Phase;
    }

    private void ClearActions()
    {
        isSwitchToTable = false;
        isRecordToCurrentTable = false;
        isClearAll = false;
        isRemoveCurrentPoint = false;
        isNextStage = false;
        isResetCircuit = false;
        isEnableShortCircuit = false;
        isDisableShortCircuit = false;
        isToggleShortCircuit = false;
        isEnableShortCircuit2Phase = false;
        isDisableShortCircuit2Phase = false;
        isToggleShortCircuit2Phase = false;
    }

    private void SetButtonText(string value)
    {
        var tmp = GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = value;
            return;
        }

        var text = GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (text != null)
            text.text = value;
    }
}
