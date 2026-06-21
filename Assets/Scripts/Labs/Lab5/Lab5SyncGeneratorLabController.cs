using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Lab5SyncGeneratorLabController : MonoBehaviour
{
    private const int RequiredPoints = 5;
    private const float DuplicateTolerance = 0.001f;
    private const float FrequencyTarget = 50f;
    private const float FrequencyTolerance = 5f;
    private const float RegulatingVoltageTolerance = 45f;

    [Header("— МОДЕЛЬ —")]
    public Lab5SyncGeneratorModel model;
    public bool autoFindModel = true;

    [Header("— СТРЕЛОЧНЫЕ ПРИБОРЫ (ручное подключение) —")]
    public Meter PA1_MotorCurrent;
    public Meter PV1_GeneratorVoltage;
    public Meter PF1_Frequency;
    public Meter PA2_PhaseA;
    public Meter PA3_PhaseB;
    public Meter PA4_PhaseC;
    public Meter PA5_ExcitationCurrent;

    [Header("— ЦИФРОВЫЕ ДИСПЛЕИ —")]
    public TMP_Text tvInfoText;
    public TMP_Text pf1_Display;

    [Header("— HUD —")]
    public bool showRuntimeHud = true;

    private Lab5SyncGeneratorStage currentStage = Lab5SyncGeneratorStage.Intro;
    private GameObject runtimeHudObject;
    private RectTransform hudPanelRoot;
    private RectTransform debugControlsRoot;
    private RectTransform graphPanel;
    private Lab5SyncGeneratorHud hud;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI stageText;
    private TextMeshProUGUI instructionText;
    private TextMeshProUGUI stateText;
    private TextMeshProUGUI pointsText;
    private TextMeshProUGUI messageText;
    private TextMeshProUGUI runtimeHudHintText;
    private Lab5ChartTableView tableView;
    private Lab5ChartGraphView graphView;
    private bool runtimeHudVisibleBeforePause;
    private bool runtimeHudPaused;
    private string lastMessage = "Ознакомьтесь со схемой установки синхронного генератора.";

    public Lab5SyncGeneratorStage CurrentStage => currentStage;
    public string StageDisplayName => GetStageDisplayName();
    public string StageHint => GetStageHint();
    public string LastMessage => lastMessage;

    private void Awake()
    {
        ResolveReferences();
        if (IsRuntimeHudEnabled())
            CreateRuntimeHud();
    }

    private void Update()
    {
        if (model == null) return;

        if (SyncRuntimeHudWithPause())
            return;

        HandleInput();
        RefreshLabState(false);
    }

    private void ResolveReferences()
    {
        if (model == null && autoFindModel)
            model = FindFirstObjectByType<Lab5SyncGeneratorModel>();

        if (model == null)
            return;

        if (tvInfoText == null)
            tvInfoText = model.tvInfoText;
        if (pf1_Display == null)
            pf1_Display = model.pf1_Display;

        if (tableView == null)
            tableView = FindFirstObjectByType<Lab5ChartTableView>();
        if (graphView == null)
            graphView = FindFirstObjectByType<Lab5ChartGraphView>();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.H) && IsRuntimeHudEnabled())
            SetRuntimeHudVisible(!showRuntimeHud);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            ConfirmCurrentStage();
    }

    public void PreviousStage()
    {
        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.PowerOn: currentStage = Lab5SyncGeneratorStage.Intro; break;
            case Lab5SyncGeneratorStage.NoLoadTest: currentStage = Lab5SyncGeneratorStage.PowerOn; break;
            case Lab5SyncGeneratorStage.InductiveLoadTest: currentStage = Lab5SyncGeneratorStage.NoLoadTest; break;
            case Lab5SyncGeneratorStage.ExternalTest: currentStage = Lab5SyncGeneratorStage.InductiveLoadTest; break;
            case Lab5SyncGeneratorStage.RegulatingTest: currentStage = Lab5SyncGeneratorStage.ExternalTest; break;
            case Lab5SyncGeneratorStage.ShortCircuitTest: currentStage = Lab5SyncGeneratorStage.RegulatingTest; break;
            case Lab5SyncGeneratorStage.ReactiveTriangle: currentStage = Lab5SyncGeneratorStage.ShortCircuitTest; break;
            case Lab5SyncGeneratorStage.Completed: currentStage = Lab5SyncGeneratorStage.ReactiveTriangle; break;
        }

        lastMessage = "Переход к предыдущему этапу.";
        SyncTableToCurrentStage();
        RefreshLabState();
    }

    public void ConfirmCurrentStage()
    {
        if (!CanAdvanceToNextStage(out string errorMessage))
        {
            lastMessage = "Нельзя перейти дальше: " + errorMessage;
            RefreshLabState();
            return;
        }

        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.Intro:
                currentStage = Lab5SyncGeneratorStage.PowerOn;
                lastMessage = "Включите KM1 и увеличьте LLR до частоты около 50 Гц.";
                break;
            case Lab5SyncGeneratorStage.PowerOn:
            case Lab5SyncGeneratorStage.PrimeMoverStart:
                currentStage = Lab5SyncGeneratorStage.NoLoadTest;
                lastMessage = "Переход к ХХХ: Q2 выключен, Q1 включен, изменяйте R2.";
                break;
            case Lab5SyncGeneratorStage.NoLoadTest:
                currentStage = Lab5SyncGeneratorStage.InductiveLoadTest;
                lastMessage = "Переход к индукционной нагрузочной характеристике: включите Q2 и задайте R3.";
                break;
            case Lab5SyncGeneratorStage.InductiveLoadTest:
                currentStage = Lab5SyncGeneratorStage.ExternalTest;
                lastMessage = "Переход к внешней характеристике: R3 держите около 0%, изменяйте R1.";
                break;
            case Lab5SyncGeneratorStage.ExternalTest:
                currentStage = Lab5SyncGeneratorStage.RegulatingTest;
                lastMessage = "Переход к регулировочной характеристике: поддерживайте U около номинального.";
                break;
            case Lab5SyncGeneratorStage.RegulatingTest:
                currentStage = Lab5SyncGeneratorStage.ShortCircuitTest;
                lastMessage = "Переход к КЗ: включайте только один режим КЗ и изменяйте R2.";
                break;
            case Lab5SyncGeneratorStage.ShortCircuitTest:
                currentStage = Lab5SyncGeneratorStage.ReactiveTriangle;
                lastMessage = "Проверьте расчет реактивного треугольника и диаграммы ЭДС.";
                break;
            case Lab5SyncGeneratorStage.ReactiveTriangle:
                currentStage = Lab5SyncGeneratorStage.Completed;
                lastMessage = "Лабораторная работа завершена.";
                break;
            case Lab5SyncGeneratorStage.Completed:
                lastMessage = "Работа уже завершена. Reset выполнит полный сброс.";
                break;
        }

        SyncTableToCurrentStage();
        RefreshLabState();
    }

    public bool CanAdvanceToNextStage(out string errorMessage)
    {
        errorMessage = string.Empty;
        if (model == null)
        {
            errorMessage = "модель Lab5 не найдена";
            return false;
        }

        model.RefreshCircuit();

        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.Intro:
                return true;
            case Lab5SyncGeneratorStage.PowerOn:
            case Lab5SyncGeneratorStage.PrimeMoverStart:
                if (!IsDriveReady())
                {
                    errorMessage = "запустите двигатель и установите частоту 45-55 Гц";
                    return false;
                }
                return true;
            case Lab5SyncGeneratorStage.NoLoadTest:
                return RequireCount(model.noLoadAscending.Count + model.noLoadDescending.Count, "ХХХ", out errorMessage);
            case Lab5SyncGeneratorStage.InductiveLoadTest:
                return RequireCount(model.inductiveLoadData.Count, "индукционной нагрузочной характеристики", out errorMessage);
            case Lab5SyncGeneratorStage.ExternalTest:
                return RequireCount(model.externalData.Count, "внешней характеристики", out errorMessage);
            case Lab5SyncGeneratorStage.RegulatingTest:
                return RequireCount(model.regulatingData.Count, "регулировочной характеристики", out errorMessage);
            case Lab5SyncGeneratorStage.ShortCircuitTest:
                if (model.shortCircuitData.Count < RequiredPoints || model.shortCircuit2PhaseData.Count < RequiredPoints)
                {
                    errorMessage = "запишите по 5 точек для 3ф и 2ф КЗ";
                    return false;
                }
                return true;
            case Lab5SyncGeneratorStage.ReactiveTriangle:
                if (model.noLoadAscending.Count < 2 || model.inductiveLoadData.Count < 2 || model.shortCircuitData.Count < 1)
                    lastMessage = "Для точного расчета нужны ХХХ, индукционная нагрузочная и КЗ; для MVP переход разрешен.";
                return true;
            default:
                return true;
        }
    }

    private bool RequireCount(int count, string name, out string errorMessage)
    {
        if (count >= RequiredPoints)
        {
            errorMessage = string.Empty;
            return true;
        }

        errorMessage = "запишите 5 точек " + name;
        return false;
    }

    public bool CanRecordCurrentStage(out string errorMessage)
    {
        return CanRecordStage(currentStage, out errorMessage);
    }

    private bool CanRecordStage(Lab5SyncGeneratorStage stage, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (model == null)
        {
            errorMessage = "модель Lab5 не найдена";
            return false;
        }

        model.RefreshCircuit();

        switch (stage)
        {
            case Lab5SyncGeneratorStage.NoLoadTest:
                if (!CheckDriveAndExcitation(out errorMessage)) return false;
                if (model.IsLoadOn) { errorMessage = "для ХХХ выключите Q2"; return false; }
                if (model.isShortCircuitMode || model.isShortCircuit2PhaseMode) { errorMessage = "выключите режимы КЗ"; return false; }
                if (model.noLoadAscending.Count + model.noLoadDescending.Count >= RequiredPoints) { errorMessage = "лимит ХХХ: 5 точек"; return false; }
                if (HasDuplicateX(model.noLoadAscending, model.excitationCurrent) || HasDuplicateX(model.noLoadDescending, model.excitationCurrent)) { errorMessage = "точка с таким If уже есть"; return false; }
                return true;

            case Lab5SyncGeneratorStage.InductiveLoadTest:
                if (!CheckDriveAndExcitation(out errorMessage)) return false;
                if (!model.IsLoadOn) { errorMessage = "включите Q2"; return false; }
                if (model.InductiveLoadPercent <= 1f) { errorMessage = "подключите индуктивную нагрузку R3"; return false; }
                if (model.isShortCircuitMode || model.isShortCircuit2PhaseMode) { errorMessage = "выключите режимы КЗ"; return false; }
                if (model.inductiveLoadData.Count >= RequiredPoints) { errorMessage = "лимит нагрузочной характеристики: 5 точек"; return false; }
                if (HasDuplicateX(model.inductiveLoadData, model.excitationCurrent)) { errorMessage = "точка с таким If уже есть"; return false; }
                return true;

            case Lab5SyncGeneratorStage.ExternalTest:
                if (!CheckDriveAndExcitation(out errorMessage)) return false;
                if (!model.IsLoadOn) { errorMessage = "включите Q2"; return false; }
                if (model.InductiveLoadPercent > 5f) { errorMessage = "для cosφ≈1 уменьшите R3 до 0%"; return false; }
                if (model.isShortCircuitMode || model.isShortCircuit2PhaseMode) { errorMessage = "выключите режимы КЗ"; return false; }
                if (model.externalData.Count >= RequiredPoints) { errorMessage = "лимит внешней характеристики: 5 точек"; return false; }
                if (HasDuplicateX(model.externalData, model.phaseACurrent)) { errorMessage = "точка с таким Ia уже есть"; return false; }
                return true;

            case Lab5SyncGeneratorStage.RegulatingTest:
                if (!CheckDriveAndExcitation(out errorMessage)) return false;
                if (!model.IsLoadOn) { errorMessage = "включите Q2"; return false; }
                if (model.isShortCircuitMode || model.isShortCircuit2PhaseMode) { errorMessage = "выключите режимы КЗ"; return false; }
                if (Mathf.Abs(model.generatorVoltage - model.nominalVoltage) > RegulatingVoltageTolerance) { errorMessage = "поддерживайте U около номинального или нажмите Tune U"; return false; }
                if (model.regulatingData.Count >= RequiredPoints) { errorMessage = "лимит регулировочной характеристики: 5 точек"; return false; }
                if (HasDuplicateX(model.regulatingData, model.phaseACurrent)) { errorMessage = "точка с таким Ia уже есть"; return false; }
                return true;

            case Lab5SyncGeneratorStage.ShortCircuitTest:
                if (!model.isPrimeMoverRunning) { errorMessage = "запустите приводной двигатель"; return false; }
                if (!model.IsExcitationOn) { errorMessage = "включите Q1"; return false; }
                if (model.isShortCircuitMode == model.isShortCircuit2PhaseMode) { errorMessage = "включите ровно один режим КЗ: 3ф или 2ф"; return false; }
                if (model.generatorVoltage > 1f) { errorMessage = "в режиме КЗ напряжение должно быть около 0"; return false; }
                if (model.phaseACurrent > model.nominalStatorCurrent) { errorMessage = "ток КЗ превышает номинальный предел"; return false; }
                if (model.isShortCircuit2PhaseMode)
                {
                    if (model.shortCircuit2PhaseData.Count >= RequiredPoints) { errorMessage = "лимит 2ф КЗ: 5 точек"; return false; }
                    if (HasDuplicateX(model.shortCircuit2PhaseData, model.excitationCurrent)) { errorMessage = "точка 2ф КЗ с таким If уже есть"; return false; }
                }
                else
                {
                    if (model.shortCircuitData.Count >= RequiredPoints) { errorMessage = "лимит 3ф КЗ: 5 точек"; return false; }
                    if (HasDuplicateX(model.shortCircuitData, model.excitationCurrent)) { errorMessage = "точка 3ф КЗ с таким If уже есть"; return false; }
                }
                return true;
        }

        errorMessage = "на этом этапе запись точек не требуется";
        return false;
    }

    private bool CheckDriveAndExcitation(out string errorMessage)
    {
        if (!IsDriveReady())
        {
            errorMessage = "запустите двигатель и установите частоту 45-55 Гц";
            return false;
        }
        if (!model.IsExcitationOn)
        {
            errorMessage = "включите Q1";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private bool IsDriveReady()
    {
        return model != null && model.isPrimeMoverRunning && Mathf.Abs(model.generatorFrequency - FrequencyTarget) <= FrequencyTolerance;
    }

    private bool HasDuplicateX(List<Vector2> points, float x)
    {
        for (int i = 0; i < points.Count; i++)
            if (Mathf.Abs(points[i].x - x) <= DuplicateTolerance)
                return true;
        return false;
    }

    public void RecordCurrentPoint()
    {
        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.NoLoadTest: RecordNoLoadPoint(); break;
            case Lab5SyncGeneratorStage.InductiveLoadTest: RecordInductiveLoadPoint(); break;
            case Lab5SyncGeneratorStage.ExternalTest: RecordExternalPoint(); break;
            case Lab5SyncGeneratorStage.RegulatingTest: RecordRegulatingPoint(); break;
            case Lab5SyncGeneratorStage.ShortCircuitTest:
                if (model != null && model.isShortCircuit2PhaseMode) RecordShortCircuit2PhasePoint(); else RecordShortCircuitPoint();
                break;
            default:
                lastMessage = "На текущем этапе запись точки не требуется.";
                RefreshLabState();
                break;
        }
    }

    public void RecordNoLoadPoint()
    {
        if (!TryBeginRecord(Lab5SyncGeneratorStage.NoLoadTest)) return;
        model.RecordNoLoadPoint();
        lastMessage = "Точка ХХХ записана.";
        AfterDataChanged();
    }

    public void RecordInductiveLoadPoint()
    {
        if (!TryBeginRecord(Lab5SyncGeneratorStage.InductiveLoadTest)) return;
        model.RecordInductiveLoadPoint();
        lastMessage = "Точка индукционной нагрузочной характеристики записана.";
        AfterDataChanged();
    }

    public void RecordExternalPoint()
    {
        if (!TryBeginRecord(Lab5SyncGeneratorStage.ExternalTest)) return;
        model.RecordExternalPoint();
        lastMessage = "Точка внешней характеристики записана.";
        AfterDataChanged();
    }

    public void RecordRegulatingPoint()
    {
        if (!TryBeginRecord(Lab5SyncGeneratorStage.RegulatingTest)) return;
        model.RecordRegulatingPoint();
        lastMessage = "Точка регулировочной характеристики записана.";
        AfterDataChanged();
    }

    public void RecordShortCircuitPoint()
    {
        if (!TryBeginRecord(Lab5SyncGeneratorStage.ShortCircuitTest)) return;
        if (!model.isShortCircuitMode)
        {
            lastMessage = "Для записи 3ф КЗ включите режим 3ф КЗ.";
            RefreshLabState();
            return;
        }
        model.RecordShortCircuitPoint();
        lastMessage = "Точка трёхфазного КЗ записана.";
        AfterDataChanged();
    }

    public void RecordShortCircuit2PhasePoint()
    {
        if (!TryBeginRecord(Lab5SyncGeneratorStage.ShortCircuitTest)) return;
        if (!model.isShortCircuit2PhaseMode)
        {
            lastMessage = "Для записи 2ф КЗ включите режим 2ф КЗ.";
            RefreshLabState();
            return;
        }
        model.RecordShortCircuit2PhasePoint();
        lastMessage = "Точка двухфазного КЗ записана.";
        AfterDataChanged();
    }

    private bool TryBeginRecord(Lab5SyncGeneratorStage requiredStage)
    {
        if (currentStage != requiredStage)
        {
            currentStage = requiredStage;
            lastMessage = "Переключен этап для записи выбранной таблицы.";
        }

        if (!CanRecordStage(requiredStage, out string errorMessage))
        {
            lastMessage = "Точка не записана: " + errorMessage;
            RefreshLabState();
            return false;
        }

        return true;
    }

    public void RemoveCurrentPoint()
    {
        if (model == null) return;

        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.NoLoadTest:
                if (RemoveLast(model.noLoadDescending) || RemoveLast(model.noLoadAscending)) lastMessage = "Удалена последняя точка ХХХ."; else lastMessage = "В ХХХ нет точек для удаления.";
                break;
            case Lab5SyncGeneratorStage.InductiveLoadTest:
                if (RemoveLast(model.inductiveLoadData)) lastMessage = "Удалена последняя точка нагрузочной характеристики."; else lastMessage = "В таблице нет точек.";
                break;
            case Lab5SyncGeneratorStage.ExternalTest:
                if (RemoveLast(model.externalData)) lastMessage = "Удалена последняя точка внешней характеристики."; else lastMessage = "В таблице нет точек.";
                break;
            case Lab5SyncGeneratorStage.RegulatingTest:
                if (RemoveLast(model.regulatingData)) lastMessage = "Удалена последняя точка регулировочной характеристики."; else lastMessage = "В таблице нет точек.";
                break;
            case Lab5SyncGeneratorStage.ShortCircuitTest:
                if (model.isShortCircuit2PhaseMode)
                    lastMessage = RemoveLast(model.shortCircuit2PhaseData) ? "Удалена последняя точка 2ф КЗ." : "В 2ф КЗ нет точек.";
                else
                    lastMessage = RemoveLast(model.shortCircuitData) ? "Удалена последняя точка 3ф КЗ." : "В 3ф КЗ нет точек.";
                break;
            default:
                lastMessage = "На этом этапе нет текущей таблицы для удаления.";
                break;
        }

        AfterDataChanged();
    }

    private bool RemoveLast(List<Vector2> points)
    {
        if (points.Count == 0) return false;
        points.RemoveAt(points.Count - 1);
        return true;
    }

    public void ClearCurrentCharacteristic()
    {
        if (model == null) return;

        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.NoLoadTest:
                model.noLoadAscending.Clear(); model.noLoadDescending.Clear(); lastMessage = "Очищена ХХХ."; break;
            case Lab5SyncGeneratorStage.InductiveLoadTest:
                model.inductiveLoadData.Clear(); lastMessage = "Очищена индукционная нагрузочная характеристика."; break;
            case Lab5SyncGeneratorStage.ExternalTest:
                model.externalData.Clear(); lastMessage = "Очищена внешняя характеристика."; break;
            case Lab5SyncGeneratorStage.RegulatingTest:
                model.regulatingData.Clear(); lastMessage = "Очищена регулировочная характеристика."; break;
            case Lab5SyncGeneratorStage.ShortCircuitTest:
                if (model.isShortCircuitMode) { model.shortCircuitData.Clear(); lastMessage = "Очищены точки 3ф КЗ."; }
                else if (model.isShortCircuit2PhaseMode) { model.shortCircuit2PhaseData.Clear(); lastMessage = "Очищены точки 2ф КЗ."; }
                else { model.shortCircuitData.Clear(); model.shortCircuit2PhaseData.Clear(); lastMessage = "Очищены точки КЗ."; }
                break;
            default:
                lastMessage = "На этом этапе нет текущей таблицы для очистки.";
                break;
        }

        AfterDataChanged();
    }

    public void ClearCharacteristicForTable(Lab5ChartTableView.TableType tableType)
    {
        if (model == null) return;

        switch (tableType)
        {
            case Lab5ChartTableView.TableType.Table5_1_NoLoad:
                model.noLoadAscending.Clear();
                model.noLoadDescending.Clear();
                currentStage = Lab5SyncGeneratorStage.NoLoadTest;
                lastMessage = "Очищена таблица 5.1: ХХХ.";
                break;
            case Lab5ChartTableView.TableType.Table5_2_InductiveLoad:
                model.inductiveLoadData.Clear();
                currentStage = Lab5SyncGeneratorStage.InductiveLoadTest;
                lastMessage = "Очищена таблица 5.2: индукционная нагрузочная характеристика.";
                break;
            case Lab5ChartTableView.TableType.Table5_3_External:
                model.externalData.Clear();
                currentStage = Lab5SyncGeneratorStage.ExternalTest;
                lastMessage = "Очищена таблица 5.3: внешняя характеристика.";
                break;
            case Lab5ChartTableView.TableType.Table5_4_Regulating:
                model.regulatingData.Clear();
                currentStage = Lab5SyncGeneratorStage.RegulatingTest;
                lastMessage = "Очищена таблица 5.4: регулировочная характеристика.";
                break;
            case Lab5ChartTableView.TableType.Table5_5_ShortCircuit:
                model.shortCircuitData.Clear();
                model.shortCircuit2PhaseData.Clear();
                currentStage = Lab5SyncGeneratorStage.ShortCircuitTest;
                lastMessage = "Очищена таблица 5.5: характеристики КЗ.";
                break;
            case Lab5ChartTableView.TableType.Table5_6_ReactiveTriangle:
                currentStage = Lab5SyncGeneratorStage.ReactiveTriangle;
                lastMessage = "Таблица 5.6 расчетная, экспериментальные точки не очищались.";
                break;
        }

        AfterDataChanged();
    }

    public void ClearAllCharacteristicData()
    {
        ClearCurrentCharacteristic();
    }

    public void ResetLab()
    {
        if (model != null)
        {
            model.ClearAllCharacteristicData();
            model.ResetCircuit();
        }

        currentStage = Lab5SyncGeneratorStage.Intro;
        lastMessage = "Полный сброс Lab5 выполнен.";
        SyncTableToCurrentStage();
        AfterDataChanged();
    }

    public void ToggleKM1()
    {
        if (model == null || model.KM1 == null) return;
        model.KM1.isOn = !model.KM1.isOn;
        lastMessage = model.KM1.isOn ? "KM1 включен: питание двигателя подано." : "KM1 выключен: двигатель остановлен.";
        RefreshLabState();
    }

    public void ToggleQ1()
    {
        if (model == null || model.Q1 == null) return;
        model.Q1.isOn = !model.Q1.isOn;
        lastMessage = model.Q1.isOn ? "Q1 включен: возбуждение подано." : "Q1 выключен: возбуждение отключено.";
        RefreshLabState();
    }

    public void ToggleQ2()
    {
        if (model == null || model.Q2 == null) return;
        model.Q2.isOn = !model.Q2.isOn;
        lastMessage = model.Q2.isOn ? "Q2 включен: нагрузка подключена." : "Q2 выключен: нагрузка отключена.";
        RefreshLabState();
    }

    public void ToggleShortCircuitMode()
    {
        if (model == null) return;
        if (model.isShortCircuitMode) model.DisableShortCircuitMode(); else model.EnableShortCircuitMode();
        lastMessage = model.isShortCircuitMode ? "Режим 3ф КЗ включен." : "Режим 3ф КЗ выключен.";
        RefreshLabState();
    }

    public void ToggleShortCircuit2PhaseMode()
    {
        if (model == null) return;
        if (model.isShortCircuit2PhaseMode) model.DisableShortCircuit2PhaseMode(); else model.EnableShortCircuit2PhaseMode();
        lastMessage = model.isShortCircuit2PhaseMode ? "Режим 2ф КЗ включен." : "Режим 2ф КЗ выключен.";
        RefreshLabState();
    }

    public void EnableShortCircuitMode()
    {
        if (model == null) return;
        model.EnableShortCircuitMode();
        lastMessage = "Режим 3ф КЗ включен.";
        RefreshLabState();
    }

    public void DisableShortCircuitMode()
    {
        if (model == null) return;
        model.DisableShortCircuitMode();
        lastMessage = "Режим 3ф КЗ выключен.";
        RefreshLabState();
    }

    public void EnableShortCircuit2PhaseMode()
    {
        if (model == null) return;
        model.EnableShortCircuit2PhaseMode();
        lastMessage = "Режим 2ф КЗ включен.";
        RefreshLabState();
    }

    public void DisableShortCircuit2PhaseMode()
    {
        if (model == null) return;
        model.DisableShortCircuit2PhaseMode();
        lastMessage = "Режим 2ф КЗ выключен.";
        RefreshLabState();
    }

    public void IncreaseR1() { AdjustR1(5f); }
    public void DecreaseR1() { AdjustR1(-5f); }
    public void IncreaseR2() { AdjustR2(5f); }
    public void DecreaseR2() { AdjustR2(-5f); }
    public void IncreaseR3() { AdjustR3(5f); }
    public void DecreaseR3() { AdjustR3(-5f); }
    public void IncreaseLLR() { AdjustLLR(5f); }
    public void DecreaseLLR() { AdjustLLR(-5f); }

    private void AdjustR1(float delta)
    {
        if (model == null || model.R1 == null) return;
        model.R1.SetPercent(Mathf.Clamp(model.R1.Percent + delta, 0f, 100f));
        lastMessage = $"R1 = {model.R1.Percent:F0}%.";
        RefreshLabState();
    }

    private void AdjustR2(float delta)
    {
        if (model == null || model.R2 == null) return;
        model.R2.SetPercent(Mathf.Clamp(model.R2.Percent + delta, 0f, 100f));
        lastMessage = $"R2 = {model.R2.Percent:F0}%.";
        RefreshLabState();
    }

    private void AdjustR3(float delta)
    {
        if (model == null || model.R3 == null) return;
        float newValue = Mathf.Clamp(model.R3.value + delta, 0f, 100f);
        model.R3.SetNormalizedValue(newValue / 100f, raiseEvent: true);
        lastMessage = $"R3 = {model.R3.value:F0}%.";
        RefreshLabState(false);
    }

    private void AdjustLLR(float delta)
    {
        if (model == null || model.LLR == null) return;
        float newValue = Mathf.Clamp(model.LLR.llrValue + delta, 0f, 250f);
        model.LLR.SetNormalizedValue(newValue / 250f, raiseEvent: true);
        lastMessage = $"LLR = {model.LLR.llrValue:F0}.";
        RefreshLabState(false);
    }

    public void TuneVoltage()
    {
        if (model == null || model.R2 == null) return;
        if (currentStage != Lab5SyncGeneratorStage.RegulatingTest)
        {
            lastMessage = "Tune U доступен на регулировочной характеристике.";
            RefreshLabState();
            return;
        }

        float low = 0f;
        float high = 100f;
        for (int i = 0; i < 12; i++)
        {
            float mid = (low + high) * 0.5f;
            model.R2.SetPercent(mid);
            model.RefreshCircuit();
            if (model.generatorVoltage < model.nominalVoltage) low = mid; else high = mid;
        }

        lastMessage = $"Tune U: R2={model.R2.Percent:F0}%, U={model.generatorVoltage:F1} В.";
        RefreshLabState();
    }

    public void RefreshLabState(bool recalculateModel = true)
    {
        ResolveReferences();
        if (recalculateModel && model != null)
            model.RefreshCircuit();

        UpdateMeters();
        UpdateInfoText();
        UpdateHud();
    }

    private void UpdateMeters()
    {
        if (model == null) return;

        bool km1On = model.IsMainPowerOn;
        bool genActive = model.isPrimeMoverRunning && km1On;
        bool q1On = model.IsExcitationOn;

        SetMeter(PA1_MotorCurrent != null ? PA1_MotorCurrent : model.PA1_MotorCurrent, km1On ? model.MotorCurrent : 0f);
        SetMeter(PV1_GeneratorVoltage != null ? PV1_GeneratorVoltage : model.PV1_GeneratorVoltage, genActive ? model.GeneratorVoltage : 0f);
        SetMeter(PF1_Frequency != null ? PF1_Frequency : model.PF1_Frequency, genActive ? model.GeneratorFrequency : 0f);
        SetMeter(PA2_PhaseA != null ? PA2_PhaseA : model.PA2_PhaseA, genActive ? model.PhaseACurrent : 0f);
        SetMeter(PA3_PhaseB != null ? PA3_PhaseB : model.PA3_PhaseB, genActive ? model.PhaseBCurrent : 0f);
        SetMeter(PA4_PhaseC != null ? PA4_PhaseC : model.PA4_PhaseC, genActive ? model.PhaseCCurrent : 0f);
        SetMeter(PA5_ExcitationCurrent != null ? PA5_ExcitationCurrent : model.PA5_ExcitationCurrent, q1On ? model.ExcitationCurrentAmps : 0f);
    }

    private void SetMeter(Meter meter, float value)
    {
        if (meter != null)
            meter.current = value;
    }

    private void UpdateInfoText()
    {
        if (model == null) return;

        if (pf1_Display != null)
            pf1_Display.text = model.isPrimeMoverRunning && model.IsMainPowerOn ? $"{model.generatorFrequency:F2} Гц" : "---";

        if (tvInfoText == null) return;

        tvInfoText.text =
            $"n = {model.rotorSpeedRpm:F0} об./мин.\n" +
            $"f = {model.generatorFrequency:F1} Гц";
    }

    private bool IsRuntimeHudEnabled()
    {
        return model == null ? showRuntimeHud : model.ShowHud;
    }

    private bool IsDebugControlsEnabled()
    {
        return model == null || model.ShowDebugControls;
    }

    private void SetRuntimeHudVisible(bool visible)
    {
        showRuntimeHud = visible;
        ApplyRuntimeHudVisibility();
    }

    private bool SyncRuntimeHudWithPause()
    {
        bool isPaused = GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;
        bool isPlaying = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;

        if (isPaused)
        {
            if (!runtimeHudPaused)
            {
                runtimeHudVisibleBeforePause = showRuntimeHud;
                runtimeHudPaused = true;
                ApplyRuntimeHudVisibility();
            }

            return true;
        }

        if (runtimeHudPaused)
        {
            if (!isPlaying)
                return true;

            runtimeHudPaused = false;
            SetRuntimeHudVisible(runtimeHudVisibleBeforePause);
        }

        return false;
    }

    private void ApplyRuntimeHudVisibility()
    {
        bool isHudEnabled = IsRuntimeHudEnabled();
        bool isVisible = isHudEnabled && showRuntimeHud && !runtimeHudPaused;

        if (hudPanelRoot != null)
            hudPanelRoot.gameObject.SetActive(isVisible);
        if (debugControlsRoot != null)
            debugControlsRoot.gameObject.SetActive(isVisible && IsDebugControlsEnabled());
        if (graphPanel != null)
            graphPanel.gameObject.SetActive(isVisible && IsGraphVisibleForStage());
        if (runtimeHudHintText != null)
        {
            runtimeHudHintText.gameObject.SetActive(isHudEnabled && !runtimeHudPaused);
            runtimeHudHintText.text = showRuntimeHud ? "H - скрыть Lab5 HUD" : "H - Lab5 HUD";
        }
    }

    private void UpdateHud()
    {
        if (!IsRuntimeHudEnabled())
        {
            ApplyRuntimeHudVisibility();
            return;
        }

        if (hud == null)
            CreateRuntimeHud();

        RefreshRuntimeHudTexts();
        ApplyRuntimeHudVisibility();

        if (hud == null) return;
        hud.SetHudVisible(showRuntimeHud && !runtimeHudPaused);
    }

    private void RefreshRuntimeHudTexts()
    {
        if (model == null) return;

        SetHudText(titleText, "ЛР №5. Испытание синхронного генератора");
        SetHudText(stageText, "Этап: " + GetStageDisplayName());
        SetHudText(instructionText, GetStageHint());
        SetHudText(stateText,
            $"KM1={OnOff(model.IsMainPowerOn)}, Q1={OnOff(model.IsExcitationOn)}, Q2={OnOff(model.IsLoadOn)}, КЗ3={OnOff(model.isShortCircuitMode)}, КЗ2={OnOff(model.isShortCircuit2PhaseMode)}\n" +
            $"R1={model.ActiveLoadPercent:F0}%, R2={model.ExcitationRheostatPercent:F0}%, R3={model.InductiveLoadPercent:F0}%, LLR={model.DriveSpeed:F0}\n" +
            $"f={model.generatorFrequency:F1} Гц, U={model.generatorVoltage:F1} В, Ia={model.phaseACurrent:F2} А, If={model.excitationCurrent:F3} А, cosφ={model.powerFactor:F2}");
        SetHudText(pointsText,
            $"Текущая: {GetCurrentTableTitle()}  {GetCurrentPointCount()}/{GetCurrentPointLimitText()}\n" +
            $"ХХХ {model.noLoadAscending.Count + model.noLoadDescending.Count}/5; Инд {model.inductiveLoadData.Count}/5; Внеш {model.externalData.Count}/5\n" +
            $"Рег {model.regulatingData.Count}/5; КЗ3 {model.shortCircuitData.Count}/5; КЗ2 {model.shortCircuit2PhaseData.Count}/5");
        SetHudText(messageText, lastMessage + "\nEnter - Next, H - скрыть/показать HUD");
    }

    private static void SetHudText(TextMeshProUGUI target, string value)
    {
        if (target == null) return;

        target.text = value;
        target.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private void CreateRuntimeHud()
    {
        if (runtimeHudObject != null) return;
        if (!IsRuntimeHudEnabled()) return;

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Lab5RuntimeHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Lab5SyncGeneratorHud));
        runtimeHudObject = canvasObject;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        hudPanelRoot = CreatePanel(canvasObject.transform, "HudPanel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(560f, 0f));
        hudPanelRoot.GetComponent<Image>().color = new Color(0.03f, 0.035f, 0.045f, 0.86f);
        VerticalLayoutGroup panelLayout = hudPanelRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(12, 12, 12, 12);
        panelLayout.spacing = 6f;
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;
        ContentSizeFitter fitter = hudPanelRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateHudText(hudPanelRoot, "Title", 20f, FontStyles.Bold);
        stageText = CreateHudText(hudPanelRoot, "Stage", 17f, FontStyles.Bold);
        instructionText = CreateHudText(hudPanelRoot, "Instruction", 15f, FontStyles.Normal);
        stateText = CreateHudText(hudPanelRoot, "State", 15f, FontStyles.Normal);
        pointsText = CreateHudText(hudPanelRoot, "Points", 15f, FontStyles.Normal);
        messageText = CreateHudText(hudPanelRoot, "Message", 15f, FontStyles.Bold);
        messageText.color = new Color(1f, 0.78f, 0.25f, 1f);

        if (IsDebugControlsEnabled())
        {
            debugControlsRoot = CreateDebugControlsRoot(hudPanelRoot);
            CreateButtonRow(debugControlsRoot, ("Prev", PreviousStage), ("Next", ConfirmCurrentStage), ("Record", RecordCurrentPoint), ("Remove", RemoveCurrentPoint), ("Clear", ClearCurrentCharacteristic), ("Reset", ResetLab));
            CreateButtonRow(debugControlsRoot, ("KM1", ToggleKM1), ("Q1", ToggleQ1), ("Q2", ToggleQ2), ("3ф КЗ", ToggleShortCircuitMode), ("2ф КЗ", ToggleShortCircuit2PhaseMode), ("Tune U", TuneVoltage));
            CreateButtonRow(debugControlsRoot, ("R1-", DecreaseR1), ("R1+", IncreaseR1), ("R2-", DecreaseR2), ("R2+", IncreaseR2), ("R3-", DecreaseR3), ("R3+", IncreaseR3));
            CreateButtonRow(debugControlsRoot, ("LLR-", DecreaseLLR), ("LLR+", IncreaseLLR));
        }

        runtimeHudHintText = CreateRuntimeHudHint(canvasObject.transform);

        graphPanel = CreatePanel(canvasObject.transform, "RuntimeGraph", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -560f), new Vector2(560f, 150f));
        Image graphImage = graphPanel.GetComponent<Image>();
        graphImage.color = new Color(0.03f, 0.035f, 0.045f, 0.78f);

        graphView = graphPanel.gameObject.AddComponent<Lab5ChartGraphView>();
        graphView.controller = model;
        graphView.syncTableView = tableView;
        graphView.plotRoot = graphPanel;

        hud = canvasObject.GetComponent<Lab5SyncGeneratorHud>();
        hud.BindRuntimeFields(titleText, stageText, instructionText, stateText, pointsText, messageText);
        RefreshRuntimeHudTexts();
        ConfigureRuntimeHudRaycasts(canvasObject);
        graphPanel.gameObject.SetActive(IsGraphVisibleForStage());
        ApplyRuntimeHudVisibility();
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        return rect;
    }

    private static RectTransform CreateDebugControlsRoot(Transform parent)
    {
        GameObject root = new GameObject("DebugControls", typeof(RectTransform), typeof(VerticalLayoutGroup));
        root.transform.SetParent(parent, false);
        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        return root.GetComponent<RectTransform>();
    }

    private static TextMeshProUGUI CreateHudText(Transform parent, string name, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        LayoutElement layout = textObject.GetComponent<LayoutElement>();
        layout.minHeight = fontSize + 8f;
        return text;
    }

    private static TextMeshProUGUI CreateRuntimeHudHint(Transform parent)
    {
        GameObject textObject = new GameObject("HelpHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f, -8f);
        rect.sizeDelta = new Vector2(260f, 32f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "H - Lab5 HUD";
        text.fontSize = 16f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void CreateButtonRow(Transform parent, params (string label, Action action)[] buttons)
    {
        GameObject rowObject = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowObject.transform.SetParent(parent, false);
        HorizontalLayoutGroup row = rowObject.GetComponent<HorizontalLayoutGroup>();
        row.spacing = 6f;
        row.childControlHeight = true;
        row.childControlWidth = true;
        row.childForceExpandHeight = false;
        row.childForceExpandWidth = true;

        for (int i = 0; i < buttons.Length; i++)
            CreateHudButton(rowObject.transform, buttons[i].label, buttons[i].action);
    }

    private static void CreateHudButton(Transform parent, string label, Action action)
    {
        GameObject buttonObject = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.22f, 0.32f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.minHeight = 34f;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 14f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void ConfigureRuntimeHudRaycasts(GameObject root)
    {
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null) continue;

            selectable.interactable = true;
            if (selectable.targetGraphic != null)
                selectable.targetGraphic.raycastTarget = true;

            Graphic graphic = selectable.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = true;
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private bool IsGraphVisibleForStage()
    {
        return currentStage == Lab5SyncGeneratorStage.NoLoadTest
            || currentStage == Lab5SyncGeneratorStage.InductiveLoadTest
            || currentStage == Lab5SyncGeneratorStage.ExternalTest
            || currentStage == Lab5SyncGeneratorStage.RegulatingTest
            || currentStage == Lab5SyncGeneratorStage.ShortCircuitTest;
    }

    private void AfterDataChanged()
    {
        SyncTableToCurrentStage();
        RefreshLabState();
        if (tableView != null) tableView.Refresh();
        if (graphView != null) graphView.Refresh();
    }

    private void SyncTableToCurrentStage()
    {
        ResolveReferences();
        if (tableView == null) return;

        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.NoLoadTest: tableView.tableType = Lab5ChartTableView.TableType.Table5_1_NoLoad; break;
            case Lab5SyncGeneratorStage.InductiveLoadTest: tableView.tableType = Lab5ChartTableView.TableType.Table5_2_InductiveLoad; break;
            case Lab5SyncGeneratorStage.ExternalTest: tableView.tableType = Lab5ChartTableView.TableType.Table5_3_External; break;
            case Lab5SyncGeneratorStage.RegulatingTest: tableView.tableType = Lab5ChartTableView.TableType.Table5_4_Regulating; break;
            case Lab5SyncGeneratorStage.ShortCircuitTest: tableView.tableType = Lab5ChartTableView.TableType.Table5_5_ShortCircuit; break;
            case Lab5SyncGeneratorStage.ReactiveTriangle: tableView.tableType = Lab5ChartTableView.TableType.Table5_6_ReactiveTriangle; break;
        }

        if (graphView != null)
            graphView.syncTableView = tableView;
    }

    private string GetCurrentTableTitle()
    {
        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.NoLoadTest: return "5.1 E0=f(If)";
            case Lab5SyncGeneratorStage.InductiveLoadTest: return "5.2 U=f(If)";
            case Lab5SyncGeneratorStage.ExternalTest: return "5.3 U=f(Ia)";
            case Lab5SyncGeneratorStage.RegulatingTest: return "5.4 If=f(Ia)";
            case Lab5SyncGeneratorStage.ShortCircuitTest: return "5.5 Ik=f(If)";
            case Lab5SyncGeneratorStage.ReactiveTriangle: return "5.6 расчет";
            default: return "нет";
        }
    }

    private int GetCurrentPointCount()
    {
        if (model == null) return 0;
        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.NoLoadTest: return model.noLoadAscending.Count + model.noLoadDescending.Count;
            case Lab5SyncGeneratorStage.InductiveLoadTest: return model.inductiveLoadData.Count;
            case Lab5SyncGeneratorStage.ExternalTest: return model.externalData.Count;
            case Lab5SyncGeneratorStage.RegulatingTest: return model.regulatingData.Count;
            case Lab5SyncGeneratorStage.ShortCircuitTest: return model.isShortCircuit2PhaseMode ? model.shortCircuit2PhaseData.Count : model.shortCircuitData.Count;
            default: return 0;
        }
    }

    private string GetCurrentPointLimitText()
    {
        return currentStage == Lab5SyncGeneratorStage.ShortCircuitTest ? "5 на выбранную ветвь" : "5";
    }

    private string OnOff(bool value)
    {
        return value ? "ON" : "OFF";
    }

    private string GetStageDisplayName()
    {
        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.Intro: return "Подготовка установки";
            case Lab5SyncGeneratorStage.PowerOn:
            case Lab5SyncGeneratorStage.PrimeMoverStart: return "Пуск приводного двигателя";
            case Lab5SyncGeneratorStage.NoLoadTest: return "Характеристика холостого хода";
            case Lab5SyncGeneratorStage.InductiveLoadTest: return "Индукционная нагрузочная характеристика";
            case Lab5SyncGeneratorStage.ExternalTest: return "Внешняя характеристика";
            case Lab5SyncGeneratorStage.RegulatingTest: return "Регулировочная характеристика";
            case Lab5SyncGeneratorStage.ShortCircuitTest: return "Характеристики короткого замыкания";
            case Lab5SyncGeneratorStage.ReactiveTriangle: return "Реактивный треугольник и диаграмма ЭДС";
            case Lab5SyncGeneratorStage.Completed: return "Завершение работы";
            case Lab5SyncGeneratorStage.Fault: return "Авария";
            default: return currentStage.ToString();
        }
    }

    private string GetStageHint()
    {
        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.Intro:
                return "Ознакомьтесь со схемой. Проверьте исходное состояние органов управления. Нажмите Next.";
            case Lab5SyncGeneratorStage.PowerOn:
            case Lab5SyncGeneratorStage.PrimeMoverStart:
                return "Включите KM1 и увеличьте LLR до частоты около 50 Гц. После достижения частоты нажмите Next.";
            case Lab5SyncGeneratorStage.NoLoadTest:
                return "Оставьте Q2 выключенным. Включите Q1. Изменяйте R2 и запишите 5 точек E0=f(If).";
            case Lab5SyncGeneratorStage.InductiveLoadTest:
                return "Включите Q1 и Q2. Подключите R3. Изменяйте R2 и запишите 5 точек U=f(If).";
            case Lab5SyncGeneratorStage.ExternalTest:
                return "Включите Q1 и Q2. Установите R3 около 0%. Держите If условно постоянным, изменяйте R1 и запишите 5 точек U=f(Ia).";
            case Lab5SyncGeneratorStage.RegulatingTest:
                return "Изменяйте R1, подстраивайте R2 или Tune U для U≈Uн. Запишите 5 точек If=f(Ia).";
            case Lab5SyncGeneratorStage.ShortCircuitTest:
                return "Включите Q1 и один режим КЗ. Изменяйте R2 и запишите по 5 точек Ik=f(If) для 3ф и 2ф КЗ.";
            case Lab5SyncGeneratorStage.ReactiveTriangle:
                return "Проверьте расчет Xσ, Fa, Xd и диаграммы ЭДС в таблице 5.6.";
            case Lab5SyncGeneratorStage.Completed:
                return "Работа завершена. Проверьте таблицы и графики. Reset выполняет полный сброс.";
            case Lab5SyncGeneratorStage.Fault:
                return "Аварийное состояние. Используйте Reset.";
            default:
                return string.Empty;
        }
    }
}
