using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum UCurvePowerSeries
{
    None,
    P0,
    HalfPn,
    Pn,
    OutOfRange
}

public class SyncGeneratorLabController : MonoBehaviour
{
    public event Action OnUCurveChanged;

    [SerializeField] private SynchronizationMethod synchronizationMethod = SynchronizationMethod.Both;
    [SerializeField] private NeedleSynchronoscopeView needleSynchronoscopeView;
    [SerializeField] private LampSynchronoscopeView lampSynchronoscopeView;
    [SerializeField] private AnalogMeterView voltageMeterView;
    [SerializeField] private AnalogMeterView gridFrequencyMeterView;
    [SerializeField] private AnalogMeterView frequencyMeterView;
    [SerializeField] private AnalogMeterView statorCurrentMeterView;

    [Header("Meters")]
    public Meter PHz1;
    public Meter PHz2;
    public Meter PvGrid;
    public Meter PvDrive;
    public Meter PaDrive;
    public Meter PvGenerator;
    public Meter PaExcitation;
    public Meter PvExcitation;
    public Meter PvExcitationLow;
    public Meter PaStator;
    public Meter PLoad;

    [Header("Runtime HUD")]
    public bool showRuntimeHud = false;

    [Header("Control Settings")]
    [SerializeField] private float minDriveSpeedRpm = 2700f;
    [SerializeField] private float maxDriveSpeedRpm = 3300f;
    [SerializeField] private float speedStepRpm = 5f;
    [SerializeField] private float excitationStep = 0.1f;
    [SerializeField] private float loadStep = 0.1f;
    [SerializeField] private float driveRegulatorCurveExponent = 1.5f;
    [SerializeField] private bool autoCloseQ2AtPhaseMatch = true;

    [Header("U-Curve Power Series Auto Detection")]
    public bool autoDetectUCurvePowerSeries = true;
    [Range(0f, 100f)] public float p0MinPercent = 0f;
    [Range(0f, 100f)] public float p0MaxPercent = 20f;
    [Range(0f, 100f)] public float halfPowerMinPercent = 40f;
    [Range(0f, 100f)] public float halfPowerMaxPercent = 60f;
    [Range(0f, 100f)] public float nominalPowerMinPercent = 80f;
    [Range(0f, 100f)] public float nominalPowerMaxPercent = 100f;

    [Header("U-Curve Points")]
    [Range(1, 10)] public int requiredPointsPerPowerSeries = 5;

    [Header("U-Curve Duplicate Protection")]
    public bool preventNearDuplicateExcitationPoints = true;
    [Range(0f, 1f)] public float minExcitationDeltaForSameSeries = 0.03f;

    [Header("Stage Auto Transition")]
    [Range(0f, 100f)] public float minLoadPercentForMeasurementStage = 5f;
    public bool autoEnterUCurveMeasurementStage = true;

    [Header("Resettable Controls")]
    public Lab4AxisSliderControl[] resettableAxisSliderControls;
    public Lab4AxisRotatorControl[] resettableAxisRotatorControls;
    public Lab4LinearSliderControl[] resettableLinearSliderControls;
    public Lab4CommonSliderAdapter[] resettableCommonSliderAdapters;
    public bool logMissingResetControls;

    private readonly SyncGeneratorModel model = new SyncGeneratorModel();
    private readonly List<UCurvePoint> noLoadUCurvePoints = new List<UCurvePoint>();
    private readonly List<UCurvePoint> halfLoadUCurvePoints = new List<UCurvePoint>();
    private readonly List<UCurvePoint> fullLoadUCurvePoints = new List<UCurvePoint>();
    private UCurveSeries currentUCurveSeries = UCurveSeries.NoLoad;
    private bool q1Enabled;
    private bool q5Enabled;
    private bool q2SynchronizationArmed;
    private float driveRegulatorNormalized;
    private float driveRegulatorAtConnection;
    private float r1Normalized;
    private SyncGeneratorStage currentStage = SyncGeneratorStage.Intro;
    private GameObject runtimeHudObject;
    private SyncGeneratorHud hud;
    private string lastMessage = "Начните работу с органов управления стенда.";

    public bool IsQ1Enabled => q1Enabled;
    public bool IsQ2Enabled => model.isConnectedToGrid || q2SynchronizationArmed;
    public bool IsQ3Enabled => model.isPrimeMoverRunning;
    public bool IsQ4Enabled => model.isPowered;
    public bool IsQ5Enabled => q5Enabled;
    public bool IsPrimeMoverRunning => model.isPrimeMoverRunning;
    public bool IsConnectedToGrid => model.isConnectedToGrid;
    public float GridVoltage => model.gridVoltage;
    public float GeneratorVoltage => model.generatorVoltage;
    public float GridFrequency => model.gridFrequency;
    public float GeneratorFrequency => model.generatorFrequency;
    public float RotorSpeedRpm => model.rotorSpeedRpm;
    public float PhaseDifferenceDeg => Mathf.Repeat(model.phaseDifferenceDeg, 360f);
    public float ExcitationCurrent => model.excitationCurrent;
    public float LoadPower => model.loadPower;
    public float StatorCurrent => model.statorCurrent;
    public float PowerFactor => model.powerFactor;
    public bool IsSynchronizationWindowOpen => IsPhaseMatched(PhaseDifferenceDeg);
    public UCurveSeries CurrentUCurveSeries => currentUCurveSeries;
    public float CurrentLoadPercent => GetCurrentLoadPercent();
    public UCurvePowerSeries CurrentDetectedUCurvePowerSeries => model.isConnectedToGrid
        ? DetectUCurvePowerSeries(CurrentLoadPercent)
        : UCurvePowerSeries.None;
    public int CurrentUCurvePointCount => CurrentUCurvePoints.Count;
    private List<UCurvePoint> CurrentUCurvePoints => currentUCurveSeries switch
    {
        UCurveSeries.HalfLoad => halfLoadUCurvePoints,
        UCurveSeries.FullLoad => fullLoadUCurvePoints,
        _ => noLoadUCurvePoints
    };

    private List<UCurvePoint> GetMutableUCurvePoints(UCurveSeries series)
    {
        return series switch
        {
            UCurveSeries.HalfLoad => halfLoadUCurvePoints,
            UCurveSeries.FullLoad => fullLoadUCurvePoints,
            _ => noLoadUCurvePoints
        };
    }

    public IReadOnlyList<UCurvePoint> GetUCurvePoints(UCurveSeries series)
    {
        return series switch
        {
            UCurveSeries.HalfLoad => halfLoadUCurvePoints,
            UCurveSeries.FullLoad => fullLoadUCurvePoints,
            _ => noLoadUCurvePoints
        };
    }

    public void ClearUCurveSeries(UCurveSeries series)
    {
        GetMutableUCurvePoints(series).Clear();
        lastMessage = "Серия U-кривой очищена: " + GetUCurveSeriesDisplayName(series) + ".";
        NotifyUCurveChanged();
        RefreshLabState();
    }

    public void ClearAllUCurvePoints()
    {
        noLoadUCurvePoints.Clear();
        halfLoadUCurvePoints.Clear();
        fullLoadUCurvePoints.Clear();
        lastMessage = "Все точки U-кривых очищены.";
        NotifyUCurveChanged();
        RefreshLabState();
    }

    private void Awake()
    {
        model.Reset();
        CreateRuntimeHud();

        RefreshLabState(false);
    }

    private void Update()
    {
        HandleInput();
        model.Update(Time.deltaTime);
        UpdatePendingSynchronization();
        RefreshLabState(false);
    }

    public void RefreshLabState(bool recalculateModel = true)
    {
        if (recalculateModel)
        {
            UpdateDriveFromRegulator();
            UpdateExcitationFromR1();
            model.Update(0f);
        }

        UpdateMeters();
        UpdateViews();
        UpdateHud();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            SetRuntimeHudVisible(!showRuntimeHud);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmCurrentStage();
        }
    }

    public void ToggleQ1()
    {
        q1Enabled = !q1Enabled;

        if (!q1Enabled)
        {
            model.SetPower(false);
            q5Enabled = false;
            q2SynchronizationArmed = false;
            driveRegulatorAtConnection = 0f;
            currentStage = SyncGeneratorStage.PowerOn;
        }

        lastMessage = q1Enabled
            ? "Q1 включён: цепь установки подготовлена."
            : "Q1 выключен: работа схемы запрещена.";
        RefreshLabState();
    }

    public void ToggleQ2()
    {
        if (model.isConnectedToGrid)
        {
            model.isConnectedToGrid = false;
            model.loadPower = 0f;
            q2SynchronizationArmed = false;
            driveRegulatorAtConnection = 0f;
            currentStage = SyncGeneratorStage.Synchronization;
            lastMessage = "Q2 выключен: генератор отключён от сети.";
            RefreshLabState();
            return;
        }

        if (q2SynchronizationArmed)
        {
            q2SynchronizationArmed = false;
            lastMessage = "Q2 выключен: ожидание синхронизации отменено.";
            RefreshLabState();
            return;
        }

        TrySynchronize();
        RefreshLabState();
    }

    public void ToggleQ3()
    {
        if (!q1Enabled)
        {
            lastMessage = "Q3 нельзя включить: сначала включите Q1, подготовку схемы.";
            RefreshLabState();
            return;
        }

        if (!model.isPowered)
        {
            lastMessage = "Q3 нельзя включить: сначала включите Q4, питание стенда.";
            RefreshLabState();
            return;
        }

        TogglePrimeMover();
        RefreshLabState();
    }

    public void ToggleQ4()
    {
        TogglePower();

        if (!model.isPowered)
        {
            q5Enabled = false;
            q2SynchronizationArmed = false;
            driveRegulatorAtConnection = 0f;
        }

        RefreshLabState();
    }

    public void ToggleQ5()
    {
        if (!model.isPowered)
        {
            lastMessage = "Q5 нельзя включить: сначала включите Q4, питание стенда.";
            RefreshLabState();
            return;
        }

        q5Enabled = !q5Enabled;
        UpdateExcitationFromR1();
        lastMessage = q5Enabled
            ? "Q5 включён: цепь возбуждения готова. Настройте R1."
            : "Q5 выключен: возбуждение генератора отключено.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void IncreaseR1()
    {
        if (!model.isPowered)
        {
            lastMessage = "R1 недоступен: питание Q4 отключено.";
            RefreshLabState();
            return;
        }

        if (!q5Enabled)
        {
            lastMessage = "R1 недоступен: сначала включите Q5, возбуждение генератора.";
            RefreshLabState();
            return;
        }

        model.IncreaseExcitation(excitationStep);
        r1Normalized = Mathf.Clamp01(model.excitationCurrent / 1.5f);
        q5Enabled = true;
        lastMessage = "R1 увеличен: контролируйте напряжение генератора по вольтметру.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void DecreaseR1()
    {
        if (!model.isPowered)
        {
            lastMessage = "R1 недоступен: питание Q4 отключено.";
            RefreshLabState();
            return;
        }

        if (!q5Enabled)
        {
            lastMessage = "R1 недоступен: сначала включите Q5, возбуждение генератора.";
            RefreshLabState();
            return;
        }

        model.DecreaseExcitation(excitationStep);
        r1Normalized = Mathf.Clamp01(model.excitationCurrent / 1.5f);
        lastMessage = "R1 уменьшен: контролируйте напряжение генератора по вольтметру.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void SetR1Normalized(float value)
    {
        float normalized = Mathf.Clamp01(value);
        r1Normalized = normalized;

        if (!model.isPowered)
        {
            UpdateExcitationFromR1();
            lastMessage = "Положение R1 установлено. Включите Q4 и Q5, чтобы подать возбуждение.";
            RefreshLabState();
            return;
        }

        if (!q5Enabled)
        {
            UpdateExcitationFromR1();
            lastMessage = "Положение R1 установлено. Включите Q5, чтобы подать возбуждение.";
            RefreshLabState();
            return;
        }

        UpdateExcitationFromR1();
        lastMessage = "R1 установлен: контролируйте напряжение генератора по вольтметру.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void IncreaseDriveRegulator()
    {
        if (!model.isConnectedToGrid)
        {
            if (!q1Enabled || !model.isPowered)
            {
                lastMessage = "Регулятор привода недоступен: включите Q1 и Q4.";
                RefreshLabState();
                return;
            }

            driveRegulatorNormalized = Mathf.Clamp01(driveRegulatorNormalized + SpeedStepToDriveRegulatorStep());
            if (model.isPrimeMoverRunning)
            {
                model.rotorSpeedRpm = MapDriveRegulatorToSpeed(driveRegulatorNormalized);
                lastMessage = "РНО увеличен: контролируйте частоту генератора по PHz2.";
            }
            else
            {
                lastMessage = "Положение РНО увеличено. Включите Q3, чтобы подать напряжение на привод.";
            }
        }
        else
        {
            driveRegulatorNormalized = Mathf.Clamp01(driveRegulatorNormalized + loadStep);
            ApplyLoadPowerFromDriveRegulator();
            lastMessage = "РНО увеличен: контролируйте передачу активной мощности по приборам стенда.";
        }

        AutoAdvanceByState();
        RefreshLabState();
    }

    public void DecreaseDriveRegulator()
    {
        if (!model.isConnectedToGrid)
        {
            if (!q1Enabled || !model.isPowered)
            {
                lastMessage = "Регулятор привода недоступен: включите Q1 и Q4.";
                RefreshLabState();
                return;
            }

            driveRegulatorNormalized = Mathf.Clamp01(driveRegulatorNormalized - SpeedStepToDriveRegulatorStep());
            if (model.isPrimeMoverRunning)
            {
                model.rotorSpeedRpm = MapDriveRegulatorToSpeed(driveRegulatorNormalized);
                lastMessage = "РНО уменьшен: контролируйте частоту генератора по PHz2.";
            }
            else
            {
                lastMessage = "Положение РНО уменьшено. Включите Q3, чтобы подать напряжение на привод.";
            }
        }
        else
        {
            driveRegulatorNormalized = Mathf.Clamp01(driveRegulatorNormalized - loadStep);
            ApplyLoadPowerFromDriveRegulator();
            lastMessage = "РНО уменьшен: контролируйте передачу активной мощности по приборам стенда.";
        }

        AutoAdvanceByState();
        RefreshLabState();
    }

    public void SetDriveRegulatorNormalized(float value)
    {
        float normalized = Mathf.Clamp01(value);
        driveRegulatorNormalized = normalized;

        if (!model.isConnectedToGrid)
        {
            if (!q1Enabled || !model.isPowered)
            {
                lastMessage = "Регулятор привода недоступен: включите Q1 и Q4.";
                RefreshLabState();
                return;
            }

            if (model.isPrimeMoverRunning)
            {
                model.rotorSpeedRpm = MapDriveRegulatorToSpeed(normalized);
                lastMessage = "РНО установлен: контролируйте частоту генератора по PHz2.";
            }
            else
            {
                lastMessage = "Положение РНО установлено. Включите Q3, чтобы подать напряжение на привод.";
            }
        }
        else
        {
            ApplyLoadPowerFromDriveRegulator();
            lastMessage = "РНО установлен: контролируйте передачу активной мощности по приборам стенда.";
        }

        AutoAdvanceByState();
        RefreshLabState();
    }

    public void RecordMeasurement()
    {
        RecordUCurvePoint();
        RefreshLabState();
    }

    private void ConfirmCurrentStage()
    {
        switch (currentStage)
        {
            case SyncGeneratorStage.Intro:
                currentStage = SyncGeneratorStage.PowerOn;
                lastMessage = "Включите Q1 для подготовки схемы.";
                break;

            case SyncGeneratorStage.PowerOn:
                if (q1Enabled && model.isPowered)
                {
                    currentStage = SyncGeneratorStage.PrimeMoverStart;
                    lastMessage = "Включите Q3 для запуска приводного двигателя.";
                }
                else
                {
                    lastMessage = "Включите Q1 и Q4 перед запуском стенда.";
                }
                break;

            case SyncGeneratorStage.PrimeMoverStart:
                if (model.isPrimeMoverRunning)
                {
                    currentStage = SyncGeneratorStage.FrequencyAdjustment;
                    lastMessage = "Настройте частоту генератора около 50 Гц по частотомеру PHz2.";
                }
                else
                {
                    lastMessage = "Сначала запустите привод Q3.";
                }
                break;

            case SyncGeneratorStage.FrequencyAdjustment:
                if (IsFrequencyMatched())
                {
                    currentStage = SyncGeneratorStage.VoltageAdjustment;
                    lastMessage = "Настройте напряжение генератора около 380 В по вольтметру 0–500 В.";
                }
                else
                {
                    lastMessage = "Настройте частоту генератора около 50 Гц.";
                }
                break;

            case SyncGeneratorStage.VoltageAdjustment:
                if (IsVoltageMatched())
                {
                    currentStage = SyncGeneratorStage.Synchronization;
                    lastMessage = "Нажмите Q2 и дождитесь совпадения фаз по синхроскопу и лампам.";
                }
                else
                {
                    lastMessage = "Настройте напряжение генератора около 380 В.";
                }
                break;

            case SyncGeneratorStage.Synchronization:
                TrySynchronize();
                break;

            case SyncGeneratorStage.ConnectedToGrid:
                currentStage = SyncGeneratorStage.LoadTransfer;
                lastMessage = "Увеличьте РНО для передачи активной мощности.";
                break;

            case SyncGeneratorStage.LoadTransfer:
                if (model.loadPower > 0f)
                {
                    currentStage = SyncGeneratorStage.UCurveMeasurement;
                    lastMessage = "Активная мощность передана. Можно записывать точки U-кривой. Настройте РНО в диапазон P0, 0.5Pн или Pн, изменяйте R1 и нажимайте «Записать точку».";
                }
                else
                {
                    lastMessage = "Сначала увеличьте РНО после подключения к сети.";
                }
                break;

            case SyncGeneratorStage.UCurveMeasurement:
                if (IsUCurveMeasurementComplete())
                {
                    currentStage = SyncGeneratorStage.Completed;
                    lastMessage = "Лабораторная работа завершена. Для повторного выполнения используйте кнопку «Сброс» на планшете.";
                }
                else
                {
                    lastMessage = "Запишите хотя бы одну точку U-кривой кнопкой \"Записать\".";
                }
                break;

            case SyncGeneratorStage.Completed:
                lastMessage = "Лабораторная работа завершена. Для повторного выполнения используйте кнопку «Сброс» на планшете.";
                break;

            case SyncGeneratorStage.Fault:
                lastMessage = "Аварийное состояние. Используйте кнопку \"Сброс\" на планшете.";
                break;
        }
    }

    private void TogglePower()
    {
        model.SetPower(!model.isPowered);
        lastMessage = model.isPowered ? "Q4 включён: питание стенда подано." : "Q4 выключен: питание стенда отключено.";

        if (!model.isPowered)
        {
            currentStage = SyncGeneratorStage.PowerOn;
        }

        AutoAdvanceByState();
    }

    private void TogglePrimeMover()
    {
        if (model.isPrimeMoverRunning)
        {
            model.StopPrimeMover();
            driveRegulatorAtConnection = 0f;
            lastMessage = "Q3 выключен: приводной двигатель остановлен.";
            currentStage = SyncGeneratorStage.PrimeMoverStart;
            return;
        }

        model.StartPrimeMover();
        if (model.isPrimeMoverRunning)
        {
            model.rotorSpeedRpm = MapDriveRegulatorToSpeed(driveRegulatorNormalized);
        }

        lastMessage = model.isPrimeMoverRunning
            ? "Q3 включён: приводной двигатель запущен."
            : "Q3 нельзя включить: питание отключено или генератор уже подключён.";
        AutoAdvanceByState();
    }

    private void ChangeLoad(float delta)
    {
        if (!model.isConnectedToGrid)
        {
            lastMessage = "Нагрузку можно изменять только после подключения к сети.";
            return;
        }

        model.SetLoadPower(model.loadPower + delta);
        lastMessage = "Нагрузка изменена: контролируйте приборы стенда.";
        AutoAdvanceByState();
    }

    private void TrySynchronize()
    {
        if (!TryGetQ2Readiness(out string readinessMessage))
        {
            lastMessage = readinessMessage;
            RefreshLabState();
            return;
        }

        if (!IsVoltageMatched())
        {
            lastMessage = "Q2 нельзя включить: напряжение не в допуске. Настройте напряжение генератора около 380 В.";
            RefreshLabState();
            return;
        }

        if (!IsFrequencyMatched())
        {
            lastMessage = "Q2 нельзя включить: частота не в допуске. Настройте частоту генератора около 50 Гц.";
            RefreshLabState();
            return;
        }

        if (IsPhaseMatched(PhaseDifferenceDeg))
        {
            ConnectGeneratorToGrid();
            return;
        }

        q2SynchronizationArmed = true;
        currentStage = SyncGeneratorStage.Synchronization;
        lastMessage = autoCloseQ2AtPhaseMatch
            ? "Q2 включён: ожидание совпадения фаз."
            : "Q2 включён: ожидание совпадения фаз, автоподключение отключено.";
        RefreshLabState();
    }

    private void UpdatePendingSynchronization()
    {
        if (!q2SynchronizationArmed || model.isConnectedToGrid || !autoCloseQ2AtPhaseMatch)
        {
            return;
        }

        if (!TryGetQ2Readiness(out string readinessMessage))
        {
            q2SynchronizationArmed = false;
            lastMessage = "Ожидание синхронизации отменено: " + readinessMessage;
            RefreshLabState();
            return;
        }

        if (!IsVoltageMatched())
        {
            q2SynchronizationArmed = false;
            lastMessage = "Ожидание синхронизации отменено: напряжение не в допуске.";
            RefreshLabState();
            return;
        }

        if (!IsFrequencyMatched())
        {
            q2SynchronizationArmed = false;
            lastMessage = "Ожидание синхронизации отменено: частота не в допуске.";
            RefreshLabState();
            return;
        }

        if (IsPhaseMatched(PhaseDifferenceDeg))
        {
            ConnectGeneratorToGrid();
        }
    }

    private bool TryGetQ2Readiness(out string message)
    {
        if (!q1Enabled)
        {
            message = "Q2 нельзя включить: сначала включите Q1, подготовку схемы.";
            return false;
        }

        if (!model.isPowered)
        {
            message = "Q2 нельзя включить: сначала включите Q4, питание стенда.";
            return false;
        }

        if (!model.isPrimeMoverRunning)
        {
            message = "Q2 нельзя включить: сначала включите Q3, приводной двигатель.";
            return false;
        }

        if (!q5Enabled)
        {
            message = "Q2 нельзя включить: сначала включите Q5, возбуждение генератора.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void ConnectGeneratorToGrid()
    {
        model.isConnectedToGrid = true;
        model.generatorFrequency = model.gridFrequency;
        model.generatorVoltage = model.gridVoltage;
        model.phaseDifferenceDeg = 0f;
        driveRegulatorAtConnection = driveRegulatorNormalized;
        model.SetLoadPower(0f);
        q2SynchronizationArmed = false;
        currentStage = SyncGeneratorStage.ConnectedToGrid;
        lastMessage = "Генератор подключён к сети. Увеличьте РНО для передачи активной мощности.";
        RefreshLabState();
    }

    private void ApplyLoadPowerFromDriveRegulator()
    {
        float denominator = Mathf.Max(0.0001f, 1f - driveRegulatorAtConnection);
        float normalizedLoad = Mathf.Clamp01((driveRegulatorNormalized - driveRegulatorAtConnection) / denominator);
        model.SetLoadPower(normalizedLoad);
    }

    private void RecordUCurvePoint()
    {
        if (!model.isConnectedToGrid)
        {
            lastMessage = "Нельзя записать точку: генератор ещё не подключён параллельно сети.";
            return;
        }

        if (!q5Enabled)
        {
            lastMessage = "Нельзя записывать точку: включите Q5, возбуждение генератора.";
            return;
        }

        float loadPercent = GetCurrentLoadPercent();
        UCurvePowerSeries detectedSeries = autoDetectUCurvePowerSeries
            ? DetectUCurvePowerSeries(loadPercent)
            : ToPowerSeries(currentUCurveSeries);

        if (detectedSeries == UCurvePowerSeries.OutOfRange || detectedSeries == UCurvePowerSeries.None)
        {
            lastMessage = "Нельзя записать точку: активная мощность вне диапазонов P0, 0.5Pн или Pн. Настройте РНО. Текущая мощность: "
                + FormatPercent(loadPercent) + ".";
            return;
        }

        UCurveSeries targetSeries = ToUCurveSeries(detectedSeries);
        List<UCurvePoint> targetPoints = GetMutableUCurvePoints(targetSeries);
        int requiredCount = Mathf.Max(1, requiredPointsPerPowerSeries);
        string seriesName = GetPowerSeriesDisplayName(detectedSeries);

        if (targetPoints.Count >= requiredCount)
        {
            lastMessage = "Нельзя записать точку: для серии " + seriesName + " уже записано "
                + targetPoints.Count + "/" + requiredCount + " точек.";
            return;
        }

        if (preventNearDuplicateExcitationPoints && HasNearDuplicateExcitation(targetPoints, model.excitationCurrent))
        {
            lastMessage = "Нельзя записать точку: в серии " + seriesName
                + " уже есть точка с близким током возбуждения. Измените R1.";
            return;
        }

        currentUCurveSeries = targetSeries;
        EnsureUCurveMeasurementStageForRecording();

        UCurvePoint point = model.CalculateUCurvePoint(model.excitationCurrent, model.loadPower);
        point.seriesType = targetSeries;
        targetPoints.Add(point);

        lastMessage = "Точка серии " + seriesName + " записана. P = " + FormatPercent(loadPercent)
            + ", If = " + model.excitationCurrent.ToString("F2") + ".";

        if (IsUCurveMeasurementComplete())
        {
            currentStage = SyncGeneratorStage.Completed;
            lastMessage += " Лабораторная работа завершена. Для повторного выполнения используйте кнопку «Сброс» на планшете.";
        }

        NotifyUCurveChanged();
    }

    public UCurvePowerSeries DetectUCurvePowerSeries(float loadPercent)
    {
        float clampedPercent = Mathf.Clamp(loadPercent, 0f, 100f);

        if (IsPercentInRange(clampedPercent, p0MinPercent, p0MaxPercent))
        {
            return UCurvePowerSeries.P0;
        }

        if (IsPercentInRange(clampedPercent, halfPowerMinPercent, halfPowerMaxPercent))
        {
            return UCurvePowerSeries.HalfPn;
        }

        if (IsPercentInRange(clampedPercent, nominalPowerMinPercent, nominalPowerMaxPercent))
        {
            return UCurvePowerSeries.Pn;
        }

        return UCurvePowerSeries.OutOfRange;
    }

    private bool IsPercentInRange(float percent, float minPercent, float maxPercent)
    {
        float lower = Mathf.Min(minPercent, maxPercent);
        float upper = Mathf.Max(minPercent, maxPercent);
        return percent >= lower && percent <= upper;
    }

    private bool HasNearDuplicateExcitation(IReadOnlyList<UCurvePoint> points, float excitationCurrent)
    {
        float minDelta = Mathf.Max(0f, minExcitationDeltaForSameSeries);
        for (int i = 0; i < points.Count; i++)
        {
            if (Mathf.Abs(points[i].If - excitationCurrent) < minDelta)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureUCurveMeasurementStageForRecording()
    {
        if (currentStage == SyncGeneratorStage.ConnectedToGrid || currentStage == SyncGeneratorStage.LoadTransfer)
        {
            currentStage = SyncGeneratorStage.UCurveMeasurement;
        }
    }

    private string FormatPercentRange(float minPercent, float maxPercent)
    {
        return FormatPercentValue(minPercent) + "–" + FormatPercentValue(maxPercent) + "%";
    }

    private string FormatPercent(float percent)
    {
        return FormatPercentValue(percent) + "%";
    }

    private string FormatPercentValue(float percent)
    {
        return Mathf.RoundToInt(percent).ToString();
    }

    private float GetCurrentLoadPercent()
    {
        return Mathf.Clamp01(model.loadPower) * 100f;
    }

    private UCurveSeries ToUCurveSeries(UCurvePowerSeries series)
    {
        switch (series)
        {
            case UCurvePowerSeries.HalfPn:
                return UCurveSeries.HalfLoad;
            case UCurvePowerSeries.Pn:
                return UCurveSeries.FullLoad;
            default:
                return UCurveSeries.NoLoad;
        }
    }

    private UCurvePowerSeries ToPowerSeries(UCurveSeries series)
    {
        switch (series)
        {
            case UCurveSeries.HalfLoad:
                return UCurvePowerSeries.HalfPn;
            case UCurveSeries.FullLoad:
                return UCurvePowerSeries.Pn;
            default:
                return UCurvePowerSeries.P0;
        }
    }

    public void SetUCurveSeries(UCurveSeries series)
    {
        currentUCurveSeries = series;
        lastMessage = autoDetectUCurvePowerSeries
            ? "Ручной выбор серии больше не требуется: серия U-кривой определяется автоматически по активной мощности."
            : "Выбрана серия U-кривой: " + GetUCurveSeriesLoadText(series) + ".";
        NotifyUCurveChanged();
        RefreshLabState();
    }

    private void AutoAdvanceByState()
    {
        if (currentStage == SyncGeneratorStage.Intro && model.isPowered)
        {
            currentStage = SyncGeneratorStage.PrimeMoverStart;
            return;
        }

        if (currentStage == SyncGeneratorStage.PowerOn && model.isPowered)
        {
            currentStage = SyncGeneratorStage.PrimeMoverStart;
            return;
        }

        if (currentStage == SyncGeneratorStage.PrimeMoverStart && model.isPrimeMoverRunning)
        {
            currentStage = SyncGeneratorStage.FrequencyAdjustment;
            return;
        }

        if (currentStage == SyncGeneratorStage.FrequencyAdjustment && IsFrequencyMatched())
        {
            currentStage = SyncGeneratorStage.VoltageAdjustment;
            return;
        }

        if (currentStage == SyncGeneratorStage.VoltageAdjustment && IsVoltageMatched())
        {
            currentStage = SyncGeneratorStage.Synchronization;
            return;
        }

        if (!model.isConnectedToGrid)
        {
            return;
        }

        float loadPercent = GetCurrentLoadPercent();
        if (autoEnterUCurveMeasurementStage
            && (currentStage == SyncGeneratorStage.ConnectedToGrid || currentStage == SyncGeneratorStage.LoadTransfer)
            && loadPercent >= minLoadPercentForMeasurementStage)
        {
            currentStage = SyncGeneratorStage.UCurveMeasurement;
            lastMessage = "Активная мощность передана. Можно записывать точки U-кривой. Настройте РНО в диапазон P0, 0.5Pн или Pн, изменяйте R1 и нажимайте «Записать точку».";
            return;
        }

        if (currentStage == SyncGeneratorStage.ConnectedToGrid && model.loadPower > 0f)
        {
            currentStage = SyncGeneratorStage.LoadTransfer;
            return;
        }

        if (currentStage == SyncGeneratorStage.UCurveMeasurement && IsUCurveMeasurementComplete())
        {
            currentStage = SyncGeneratorStage.Completed;
        }
    }

    private bool IsFrequencyMatched()
    {
        return Mathf.Abs(model.generatorFrequency - model.gridFrequency) <= 0.5f;
    }

    private bool IsVoltageMatched()
    {
        return Mathf.Abs(model.generatorVoltage - model.gridVoltage) <= 20f;
    }

    private float MapDriveRegulatorToSpeed(float normalized)
    {
        float clamped = Mathf.Clamp01(normalized);
        float centerSpeed = model.gridFrequency * 60f;
        float lowerRange = Mathf.Max(0f, centerSpeed - minDriveSpeedRpm);
        float upperRange = Mathf.Max(0f, maxDriveSpeedRpm - centerSpeed);

        if (clamped < 0.5f)
        {
            float t = Mathf.Clamp01((0.5f - clamped) * 2f);
            float curved = Mathf.Pow(t, Mathf.Max(1f, driveRegulatorCurveExponent));
            return centerSpeed - lowerRange * curved;
        }

        if (clamped > 0.5f)
        {
            float t = Mathf.Clamp01((clamped - 0.5f) * 2f);
            float curved = Mathf.Pow(t, Mathf.Max(1f, driveRegulatorCurveExponent));
            return centerSpeed + upperRange * curved;
        }

        return centerSpeed;
    }

    private float SpeedToDriveRegulatorNormalized(float speedRpm)
    {
        float centerSpeed = model.gridFrequency * 60f;
        float exponent = Mathf.Max(1f, driveRegulatorCurveExponent);

        if (speedRpm < centerSpeed)
        {
            float lowerRange = Mathf.Max(0.0001f, centerSpeed - minDriveSpeedRpm);
            float t = Mathf.Pow(Mathf.Clamp01((centerSpeed - speedRpm) / lowerRange), 1f / exponent);
            return Mathf.Clamp01(0.5f - t * 0.5f);
        }

        if (speedRpm > centerSpeed)
        {
            float upperRange = Mathf.Max(0.0001f, maxDriveSpeedRpm - centerSpeed);
            float t = Mathf.Pow(Mathf.Clamp01((speedRpm - centerSpeed) / upperRange), 1f / exponent);
            return Mathf.Clamp01(0.5f + t * 0.5f);
        }

        return 0.5f;
    }

    private float SpeedStepToDriveRegulatorStep()
    {
        float speedRange = Mathf.Max(0.0001f, maxDriveSpeedRpm - minDriveSpeedRpm);
        return Mathf.Abs(speedStepRpm) / speedRange;
    }

    private void UpdateDriveFromRegulator()
    {
        if (model.isConnectedToGrid)
        {
            model.rotorSpeedRpm = model.gridFrequency * 60f;
            model.generatorFrequency = model.gridFrequency;
            return;
        }

        if (!model.isPowered || !model.isPrimeMoverRunning)
        {
            model.rotorSpeedRpm = 0f;
            model.generatorFrequency = 0f;
            return;
        }

        model.rotorSpeedRpm = MapDriveRegulatorToSpeed(driveRegulatorNormalized);
        model.generatorFrequency = model.rotorSpeedRpm / 60f;
    }

    private void UpdateExcitationFromR1()
    {
        model.excitationCurrent = r1Normalized * 1.5f;
        model.excitationEnabled = model.isPowered && q5Enabled && model.excitationCurrent > 0f;
    }

    private void NotifyUCurveChanged()
    {
        OnUCurveChanged?.Invoke();
    }

    private void ResetPhysicalControlsToZero()
    {
        int resetCount = 0;

        if (resettableAxisSliderControls != null)
        {
            for (int i = 0; i < resettableAxisSliderControls.Length; i++)
            {
                Lab4AxisSliderControl control = resettableAxisSliderControls[i];
                if (control == null)
                {
                    continue;
                }

                control.SetValueFromController(0f);
                resetCount++;
            }
        }

        if (resettableAxisRotatorControls != null)
        {
            for (int i = 0; i < resettableAxisRotatorControls.Length; i++)
            {
                Lab4AxisRotatorControl control = resettableAxisRotatorControls[i];
                if (control == null)
                {
                    continue;
                }

                control.SetValueFromController(0f);
                resetCount++;
            }
        }

        if (resettableLinearSliderControls != null)
        {
            for (int i = 0; i < resettableLinearSliderControls.Length; i++)
            {
                Lab4LinearSliderControl control = resettableLinearSliderControls[i];
                if (control == null)
                {
                    continue;
                }

                control.SetValueFromController(0f);
                resetCount++;
            }
        }

        if (resettableCommonSliderAdapters != null)
        {
            for (int i = 0; i < resettableCommonSliderAdapters.Length; i++)
            {
                Lab4CommonSliderAdapter control = resettableCommonSliderAdapters[i];
                if (control == null)
                {
                    continue;
                }

                control.SetValueFromController(0f);
                resetCount++;
            }
        }

        if (logMissingResetControls && resetCount == 0)
        {
            Debug.LogWarning("Lab4 reset did not find assigned R1/RNO physical controls to reset.", this);
        }
    }

    public void ResetLab()
    {
        model.Reset();
        q1Enabled = false;
        q5Enabled = false;
        q2SynchronizationArmed = false;
        driveRegulatorNormalized = 0f;
        driveRegulatorAtConnection = 0f;
        r1Normalized = 0f;
        ResetPhysicalControlsToZero();
        currentStage = SyncGeneratorStage.Intro;
        noLoadUCurvePoints.Clear();
        halfLoadUCurvePoints.Clear();
        fullLoadUCurvePoints.Clear();
        NotifyUCurveChanged();
        lastMessage = "Лабораторная сброшена. Начните работу с Q1.";
        RefreshLabState();
    }

    private void UpdateViews()
    {
        bool frequencyMatched = IsFrequencyMatched();
        bool voltageMatched = IsVoltageMatched();
        bool phaseMatched = IsPhaseMatched(PhaseDifferenceDeg);
        bool circuitReadyForSynchronization = q1Enabled && model.isPowered && model.isPrimeMoverRunning && q5Enabled;
        bool synchronizationWindowOpen = circuitReadyForSynchronization && !model.isConnectedToGrid && frequencyMatched && voltageMatched && phaseMatched;

        if (needleSynchronoscopeView != null && UsesNeedleSynchronoscope())
        {
            needleSynchronoscopeView.SetSynchronizationState(
                model.phaseDifferenceDeg,
                model.generatorFrequency,
                model.gridFrequency,
                model.generatorVoltage,
                model.gridVoltage,
                voltageMatched,
                frequencyMatched,
                phaseMatched,
                model.isConnectedToGrid,
                circuitReadyForSynchronization);
        }

        if (lampSynchronoscopeView != null && UsesLampSynchronoscope())
        {
            lampSynchronoscopeView.SetSynchronizationState(
                model.phaseDifferenceDeg,
                model.generatorFrequency,
                model.gridFrequency,
                model.generatorVoltage,
                model.gridVoltage,
                voltageMatched,
                frequencyMatched,
                synchronizationWindowOpen,
                model.isConnectedToGrid,
                circuitReadyForSynchronization);
        }

        if (voltageMeterView != null)
        {
            voltageMeterView.SetValue(model.generatorVoltage);
        }

        if (frequencyMeterView != null)
        {
            frequencyMeterView.SetValue(model.generatorFrequency);
        }

        if (gridFrequencyMeterView != null)
        {
            gridFrequencyMeterView.SetValue(model.gridFrequency);
        }

        if (statorCurrentMeterView != null)
        {
            statorCurrentMeterView.SetValue(model.statorCurrent);
        }
    }

    private void UpdateMeters()
    {
        bool driveEnergized = model.isPowered && model.isPrimeMoverRunning;
        bool excitationEnergized = model.isPowered && q5Enabled;

        if (PHz1 != null)
        {
            PHz1.current = model.isPowered ? model.gridFrequency : 0f;
        }

        if (PHz2 != null)
        {
            PHz2.current = driveEnergized
                ? (model.isConnectedToGrid ? model.gridFrequency : model.generatorFrequency)
                : 0f;
        }

        if (PvGrid != null)
        {
            PvGrid.current = 0f;
        }

        if (PvDrive != null)
        {
            PvDrive.current = driveEnergized ? 220f * driveRegulatorNormalized : 0f;
        }

        if (PaDrive != null)
        {
            PaDrive.current = driveEnergized
                ? 5f + 35f * driveRegulatorNormalized + 10f * model.loadPower
                : 0f;
        }

        if (PvGenerator != null)
        {
            PvGenerator.current = excitationEnergized ? model.generatorVoltage : 0f;
        }

        if (PaExcitation != null)
        {
            PaExcitation.current = excitationEnergized ? model.excitationCurrent : 0f;
        }

        if (PvExcitation != null)
        {
            PvExcitation.current = excitationEnergized ? 220f * r1Normalized : 0f;
        }

        if (PvExcitationLow != null)
        {
            PvExcitationLow.current = excitationEnergized ? 50f * r1Normalized : 0f;
        }

        if (PaStator != null)
        {
            PaStator.current = model.isConnectedToGrid ? model.statorCurrent : 0f;
        }

        if (PLoad != null)
        {
            PLoad.current = model.isConnectedToGrid ? model.loadPower * 2.0f : 0f;
        }
    }

    private void UpdateHud()
    {
        if (hud == null)
        {
            CreateRuntimeHud();
        }

        if (hud == null)
        {
            return;
        }

        hud.SetHudVisible(showRuntimeHud);
        hud.SetHint(string.Empty);
        hud.SetText(showRuntimeHud ? BuildHudText() : "H — включить HUD");
    }

    private void SetRuntimeHudVisible(bool visible)
    {
        showRuntimeHud = visible;

        if (hud == null)
        {
            CreateRuntimeHud();
        }

        if (runtimeHudObject != null)
        {
            runtimeHudObject.SetActive(true);
        }

        UpdateHud();
    }

    private string BuildHudText()
    {
        StringBuilder builder = new StringBuilder(1200);

        builder.AppendLine("Параллельная работа синхронного генератора с сетью");
        builder.AppendLine("Этап: " + GetStageDisplayName());
        builder.AppendLine("Действие: " + GetStageHint());
        builder.AppendLine("Условия: " + GetStageConditionsText());
        builder.AppendLine(GetCurrentPowerSeriesHudText());
        builder.AppendLine("Прогресс: " + GetUCurvePointSummary());
        builder.AppendLine();
        builder.AppendLine("Сообщение: " + lastMessage);

        if (model.hasFault)
        {
            builder.AppendLine("Ошибка: " + model.faultReason);
        }
        builder.AppendLine("Синхронизация: " + GetSynchronizationStateText());
        builder.AppendLine();
        builder.AppendLine("H — скрыть HUD");

        return builder.ToString();
    }

    private string GetUCurvePointSummary()
    {
        int requiredCount = Mathf.Max(1, requiredPointsPerPowerSeries);
        int total = noLoadUCurvePoints.Count + halfLoadUCurvePoints.Count + fullLoadUCurvePoints.Count;
        int requiredTotal = requiredCount * 3;
        return string.Format(
            "P0 {0}/{3}, 0.5Pн {1}/{3}, Pн {2}/{3}. Всего {4}/{5}",
            noLoadUCurvePoints.Count,
            halfLoadUCurvePoints.Count,
            fullLoadUCurvePoints.Count,
            requiredCount,
            total,
            requiredTotal);
    }

    public string GetCurrentPowerSeriesStatusText()
    {
        if (!model.isConnectedToGrid)
        {
            return "Серия U-кривой: недоступна до подключения генератора к сети";
        }

        float loadPercent = GetCurrentLoadPercent();
        UCurvePowerSeries series = DetectUCurvePowerSeries(loadPercent);
        return "Текущая мощность: " + FormatPercent(loadPercent)
            + "\nСерия U-кривой: " + GetPowerSeriesDisplayName(series);
    }

    private string GetCurrentPowerSeriesHudText()
    {
        if (!model.isConnectedToGrid)
        {
            return "Серия U-кривой: недоступна до подключения генератора к сети";
        }

        float loadPercent = GetCurrentLoadPercent();
        UCurvePowerSeries series = DetectUCurvePowerSeries(loadPercent);
        return "Текущая мощность: " + FormatPercent(loadPercent)
            + "\nТекущая серия U-кривой: " + GetPowerSeriesDisplayName(series);
    }

    private string GetSynchronizationStateText()
    {
        if (model.isConnectedToGrid)
        {
            return "генератор подключён к сети";
        }

        if (!q1Enabled)
        {
            return "сначала включите Q1";
        }

        if (!model.isPowered)
        {
            return "сначала включите Q4";
        }

        if (!model.isPrimeMoverRunning)
        {
            return "сначала запустите привод Q3";
        }

        if (!q5Enabled)
        {
            return "сначала включите возбуждение Q5";
        }

        if (!IsFrequencyMatched())
        {
            return "настройте частоту генератора около 50 Гц";
        }

        if (!IsVoltageMatched())
        {
            return "настройте напряжение генератора около 380 В";
        }

        if (q2SynchronizationArmed)
        {
            return "Q2 включён: ожидание совпадения фаз";
        }

        if (IsPhaseMatched(PhaseDifferenceDeg))
        {
            return "фазовое окно открыто, можно включать Q2";
        }

        return "дождитесь совпадения фаз по лампам и синхроскопу";
    }

    private string GetStageConditionsText()
    {
        switch (currentStage)
        {
            case SyncGeneratorStage.Intro:
            case SyncGeneratorStage.PowerOn:
                return "начните с Q1, затем подайте питание Q4";
            case SyncGeneratorStage.PrimeMoverStart:
                return "Q1 и Q4 должны быть включены";
            case SyncGeneratorStage.FrequencyAdjustment:
                return "Q1, Q4 и Q3 должны быть включены; частоту контролируйте по PHz2";
            case SyncGeneratorStage.VoltageAdjustment:
                return "Q5 должен быть включён; напряжение контролируйте по вольтметру 0–500 В";
            case SyncGeneratorStage.Synchronization:
                return "Q1, Q3, Q4, Q5 включены; U/f готовы; включите Q2 в фазовом окне";
            case SyncGeneratorStage.ConnectedToGrid:
            case SyncGeneratorStage.LoadTransfer:
                return "генератор подключён параллельно сети; увеличьте РНО для передачи активной мощности";
            case SyncGeneratorStage.UCurveMeasurement:
                return "генератор подключён параллельно сети; меняйте R1 и записывайте точки при выбранной РНО мощности";
            case SyncGeneratorStage.Completed:
                return "все серии U-кривой заполнены";
            case SyncGeneratorStage.Fault:
                return "устраните причину аварии и используйте кнопку «Сброс»";
            default:
                return string.Empty;
        }
    }

    public string GetPowerSeriesDisplayName(UCurvePowerSeries series)
    {
        switch (series)
        {
            case UCurvePowerSeries.P0:
                return "P0";
            case UCurvePowerSeries.HalfPn:
                return "0.5Pн";
            case UCurvePowerSeries.Pn:
                return "Pн";
            case UCurvePowerSeries.OutOfRange:
                return "вне допустимого диапазона";
            default:
                return "недоступна до подключения генератора к сети";
        }
    }

    private string GetUCurveSeriesDisplayName(UCurveSeries series)
    {
        switch (series)
        {
            case UCurveSeries.HalfLoad:
                return "0.5Pн";
            case UCurveSeries.FullLoad:
                return "Pн";
            default:
                return "P0";
        }
    }

    private string GetUCurveSeriesLoadText(UCurveSeries series)
    {
        switch (series)
        {
            case UCurveSeries.HalfLoad:
                return "0.5Pн";
            case UCurveSeries.FullLoad:
                return "Pн";
            default:
                return "P0";
        }
    }

    private bool IsUCurveMeasurementComplete()
    {
        int requiredCount = Mathf.Max(1, requiredPointsPerPowerSeries);
        return noLoadUCurvePoints.Count >= requiredCount
            && halfLoadUCurvePoints.Count >= requiredCount
            && fullLoadUCurvePoints.Count >= requiredCount;
    }

    private string GetStageHint()
    {
        switch (currentStage)
        {
            case SyncGeneratorStage.Intro:
                return "Включите Q1 для подготовки схемы.";
            case SyncGeneratorStage.PowerOn:
                return q1Enabled ? "Включите Q4 для подачи питания на стенд." : "Включите Q1 для подготовки схемы.";
            case SyncGeneratorStage.PrimeMoverStart:
                return "Включите Q3 для запуска приводного двигателя.";
            case SyncGeneratorStage.FrequencyAdjustment:
                return "Отрегулируйте РНО: добейтесь частоты генератора около 50 Гц по PHz2.";
            case SyncGeneratorStage.VoltageAdjustment:
                return q5Enabled
                    ? "Изменяйте R1: настройте напряжение генератора около 380 В по вольтметру 0–500 В."
                    : "Включите Q5 для подачи возбуждения генератора.";
            case SyncGeneratorStage.Synchronization:
                return "Включите Q2: дождитесь совпадения фаз по лампам и синхроскопу.";
            case SyncGeneratorStage.ConnectedToGrid:
                return "Генератор подключён. Увеличьте РНО для передачи активной мощности.";
            case SyncGeneratorStage.LoadTransfer:
                return "Увеличьте РНО, чтобы передать активную мощность генератору.";
            case SyncGeneratorStage.UCurveMeasurement:
                return "Настройте РНО в один из диапазонов P0, 0.5Pн или Pн, изменяйте R1 и нажимайте «Записать точку».";
            case SyncGeneratorStage.Completed:
                return "Лабораторная работа завершена. Для повторного выполнения используйте кнопку «Сброс» на планшете.";
            case SyncGeneratorStage.Fault:
                return "Аварийное состояние. Используйте кнопку \"Сброс\" на планшете.";
            default:
                return string.Empty;
        }
    }

    private string GetStageDisplayName()
    {
        switch (currentStage)
        {
            case SyncGeneratorStage.Intro:
                return "Введение";
            case SyncGeneratorStage.PowerOn:
                return "Включение питания";
            case SyncGeneratorStage.PrimeMoverStart:
                return "Запуск приводного двигателя";
            case SyncGeneratorStage.FrequencyAdjustment:
                return "Настройка частоты";
            case SyncGeneratorStage.VoltageAdjustment:
                return "Настройка напряжения";
            case SyncGeneratorStage.Synchronization:
                return "Синхронизация";
            case SyncGeneratorStage.ConnectedToGrid:
                return "Подключено к сети";
            case SyncGeneratorStage.LoadTransfer:
                return "Передача нагрузки";
            case SyncGeneratorStage.UCurveMeasurement:
                return "Измерение U-кривой";
            case SyncGeneratorStage.Completed:
                return "Завершено";
            case SyncGeneratorStage.Fault:
                return "Авария";
            default:
                return currentStage.ToString();
        }
    }

    private bool UsesNeedleSynchronoscope()
    {
        return synchronizationMethod == SynchronizationMethod.NeedleSynchronoscope
            || synchronizationMethod == SynchronizationMethod.Both;
    }

    private bool UsesLampSynchronoscope()
    {
        return synchronizationMethod == SynchronizationMethod.LampSynchronoscope
            || synchronizationMethod == SynchronizationMethod.Both;
    }

    private bool IsPhaseMatched(float phaseDeg)
    {
        return phaseDeg <= 10f || phaseDeg >= 350f;
    }

    private void CreateRuntimeHud()
    {
        if (runtimeHudObject != null)
        {
            runtimeHudObject.SetActive(true);
            if (hud != null)
            {
                hud.SetHudVisible(showRuntimeHud);
            }

            return;
        }

        GameObject canvasObject = new GameObject("SyncGeneratorRuntimeHud", typeof(Canvas), typeof(CanvasScaler), typeof(SyncGeneratorHud));
        runtimeHudObject = canvasObject;
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font runtimeFont = GetRuntimeFont();

        GameObject textObject = new GameObject("HudText", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(16f, -16f);
        rectTransform.sizeDelta = new Vector2(920f, 980f);

        Text text = textObject.GetComponent<Text>();
        text.font = runtimeFont;
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        GameObject hintObject = new GameObject("HudHintText", typeof(RectTransform), typeof(Text));
        hintObject.transform.SetParent(canvasObject.transform, false);

        RectTransform hintRectTransform = hintObject.GetComponent<RectTransform>();
        hintRectTransform.anchorMin = new Vector2(0f, 1f);
        hintRectTransform.anchorMax = new Vector2(0f, 1f);
        hintRectTransform.pivot = new Vector2(0f, 1f);
        hintRectTransform.anchoredPosition = new Vector2(16f, -16f);
        hintRectTransform.sizeDelta = new Vector2(280f, 40f);

        Text hintText = hintObject.GetComponent<Text>();
        hintText.font = runtimeFont;
        hintText.fontSize = 20;
        hintText.color = Color.white;
        hintText.alignment = TextAnchor.UpperLeft;
        hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
        hintText.verticalOverflow = VerticalWrapMode.Overflow;
        hintText.raycastTarget = false;

        hud = canvasObject.GetComponent<SyncGeneratorHud>();
        hud.SetMainText(text);
        hud.SetHintText(hintText);
        hud.SetHudVisible(showRuntimeHud);
        hud.SetText(showRuntimeHud ? BuildHudText() : "H — включить HUD");
        runtimeHudObject.SetActive(true);
    }

    private Font GetRuntimeFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }
}
