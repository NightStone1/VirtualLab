using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lab6Controller : MonoBehaviour
{
    private const int MaxQ2Position = 7;

    [SerializeField] private Lab6Data data;
    [SerializeField] private Lab6HudView hudView;
    [SerializeField] private Lab6StandView standView;
    [SerializeField] private Lab6ResultsView resultsView;
    [SerializeField] private bool createRuntimeHud = true;
    [SerializeField] private bool showRuntimeHud = true;
    [SerializeField] private bool showDebugControls;
    [SerializeField] private bool enableDebugLogs;

    [Header("State")]
    [SerializeField] private Lab6Stage currentStage = Lab6Stage.Preparation;
    [SerializeField] private bool q1Enabled;
    [SerializeField] private bool q3Enabled;
    [SerializeField] private bool q4Enabled;
    [SerializeField] private bool q5Enabled;
    [SerializeField] private bool q6Enabled;
    [SerializeField] private int q2Position;
    [SerializeField] private bool brakeEnabled;
    [SerializeField] private int loadStep;
    [SerializeField] private float loadPercent;

    private readonly List<Lab6Measurement> measurements = new List<Lab6Measurement>();
    private readonly List<Lab6Measurement> noLoadMeasurements = new List<Lab6Measurement>();
    private readonly List<Lab6Measurement> shortCircuitMeasurements = new List<Lab6Measurement>();
    private readonly List<Lab6Measurement> loadMeasurements = new List<Lab6Measurement>();
    private readonly List<Lab6Measurement> resistanceMeasurements = new List<Lab6Measurement>();
    private GameObject runtimeHudObject;
    private GameObject runtimeHudPanelObject;
    private TextMeshProUGUI runtimeHudHintText;
    private Lab6Measurement currentMeasurement;
    private string lastMessage = "Подготовка лабораторной. Нажмите Next Stage для начала опыта ХХ.";

    public Lab6Data Data
    {
        get
        {
            EnsureData();
            return data;
        }
    }
    public Lab6Stage CurrentStage => currentStage;
    public bool Q1Enabled => q1Enabled;
    public bool Q3Enabled => q3Enabled;
    public bool Q4Enabled => q4Enabled;
    public bool Q5Enabled => q5Enabled;
    public bool Q6Enabled => q6Enabled;
    public int Q2Position => q2Position;
    public bool Q2Enabled => q2Position > 0;
    public bool BrakeEnabled => brakeEnabled;
    public int LoadStep => loadStep;
    public float LoadPercent => loadPercent;
    public bool ShowDebugControls => showDebugControls;
    public float Voltage
    {
        get
        {
            EnsureData();
            return Mathf.Lerp(0f, Mathf.Max(0f, data.maxVoltage), Mathf.Clamp01(q2Position / (float)MaxQ2Position));
        }
    }
    public Lab6Measurement CurrentMeasurement => currentMeasurement;
    public string LastMessage => lastMessage;
    public int RecordedPointCount => measurements.Count;
    public IReadOnlyList<Lab6Measurement> NoLoadMeasurements => noLoadMeasurements;
    public IReadOnlyList<Lab6Measurement> ShortCircuitMeasurements => shortCircuitMeasurements;
    public IReadOnlyList<Lab6Measurement> LoadMeasurements => loadMeasurements;
    public IReadOnlyList<Lab6Measurement> ResistanceMeasurements => resistanceMeasurements;
    public WindingConnection CurrentWindingConnection => GetWindingConnection(currentStage);
    public string WindingConnectionText => GetWindingConnectionText(CurrentWindingConnection);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapLab6Scene()
    {
        if (SceneManager.GetActiveScene().name != "Lab6_SquirrelCageMotor")
        {
            return;
        }

        if (FindObjectsByType<Lab6Controller>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        GameObject root = new GameObject("Lab6Root");
        root.AddComponent<Lab6Controller>();
        Debug.Log("Lab6: created runtime Lab6Root because scene has no Lab6Controller.");
    }

    private void Awake()
    {
        EnsureData();
        q2Position = Mathf.Clamp(q2Position, 0, MaxQ2Position);
        loadStep = Mathf.Clamp(loadStep, 0, 4);
        loadPercent = GetLoadPercentForStep(loadStep);

        if (hudView == null)
        {
            hudView = FindAnyLab6HudView();
        }

        if (standView == null)
        {
            standView = FindAnyLab6StandView();
        }

        if (resultsView == null)
        {
            resultsView = FindAnyLab6ResultsView();
        }

        if (hudView == null && createRuntimeHud)
        {
            hudView = CreateRuntimeHud();
        }

        if (hudView != null)
        {
            hudView.SetController(this);
        }

        if (resultsView != null)
        {
            resultsView.BindController(this);
        }

        ApplyAutomaticBrakeForStage();
        RefreshViews();
        RefreshResultsView();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            SetRuntimeHudVisible(!showRuntimeHud);
        }

        RefreshStandViewOnly();
    }

    public void ToggleQ1()
    {
        q1Enabled = !q1Enabled;
        if (!q1Enabled)
        {
            q2Position = 0;
            q3Enabled = false;
            q4Enabled = false;
            q5Enabled = false;
            q6Enabled = false;
            brakeEnabled = false;
            loadStep = 0;
            loadPercent = 0f;
        }

        SetMessage($"Q1 {(q1Enabled ? "включён" : "выключен")}." );
    }

    public void ToggleQ3()
    {
        q3Enabled = !q3Enabled;
        SetMessage($"Q3 {(q3Enabled ? "включён" : "выключен")}." );
    }

    public void ToggleQ4()
    {
        q4Enabled = !q4Enabled;
        SetMessage($"Q4 {(q4Enabled ? "включён" : "выключен")}." );
    }

    public void ToggleQ5()
    {
        q5Enabled = !q5Enabled;
        SetMessage($"Q5 двигатель {(q5Enabled ? "включён" : "выключен")}." );
    }

    public void ToggleQ6()
    {
        q6Enabled = !q6Enabled;
        SetMessage($"Q6 {(q6Enabled ? "включён" : "выключен")}." );
    }

    public void SetQ2Position(int position)
    {
        q2Position = Mathf.Clamp(position, 0, MaxQ2Position);
        SetMessage($"Q2 установлен в позицию {q2Position}/{MaxQ2Position}, U={Voltage:F0} В." );
    }

    public void IncreaseQ2()
    {
        SetQ2Position(q2Position + 1);
    }

    public void DecreaseQ2()
    {
        SetQ2Position(q2Position - 1);
    }

    public void ToggleBrake()
    {
        if (!showDebugControls)
        {
            SetMessage("Тормоз ротора управляется автоматически учебным сценарием.");
            return;
        }

        brakeEnabled = !brakeEnabled;
        SetMessage($"Тормоз ротора {(brakeEnabled ? "включён" : "выключен")}." );
    }

    public void SetLoadPercent(float value)
    {
        EnsureData();
        SetLoadStep(Mathf.RoundToInt(Mathf.Clamp(value, 0f, 100f) / 25f));
    }

    public void ChangeLoadPercent(float delta)
    {
        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        SetLoadStep(loadStep + (delta > 0f ? 1 : -1));
    }

    public void SetLoadStep(int step)
    {
        int previousStep = loadStep;
        loadStep = Mathf.Clamp(step, 0, 4);
        loadPercent = GetLoadPercentForStep(loadStep);
        if (previousStep != loadStep && enableDebugLogs)
        {
            Debug.Log($"Lab6 load step changed: step={loadStep}, load={loadPercent:F0}%");
        }

        SetMessage($"Ступень R нагрузки: {loadStep}, нагрузка {loadPercent:F0}%." );
        RefreshResultsView();
    }

    public void ToggleLoadStep(int step)
    {
        int clampedStep = Mathf.Clamp(step, 0, 4);
        if (clampedStep <= 0 || loadStep == clampedStep)
        {
            SetLoadStep(0);
            return;
        }

        SetLoadStep(clampedStep);
    }

    public void RecordPoint()
    {
        if (!TryValidateCurrentStage(out string error))
        {
            SetMessage("Ошибка записи: " + error, true);
            return;
        }

        if (GetRecordedPointCount(currentStage) >= GetRequiredPoints(currentStage))
        {
            SetMessage("Для текущего этапа уже записано нужное количество точек.", true);
            return;
        }

        Lab6Measurement point = CreateMeasurementSnapshot();
        if (HasDuplicatePoint(point))
        {
            SetMessage("Такая точка уже записана для текущего этапа.", true);
            return;
        }

        measurements.Add(point);
        AddMeasurementToStageList(point);
        SetMessage($"Точка записана: {GetRecordedPointCount(currentStage)}/{GetRequiredPoints(currentStage)} для текущего этапа." );
        Debug.Log("Lab6 point recorded: " + point);
        RefreshResultsView();
    }

    public void NextStage()
    {
        switch (currentStage)
        {
            case Lab6Stage.Preparation:
                currentStage = Lab6Stage.NoLoad;
                SetMessage("Начат опыт холостого хода.");
                break;
            case Lab6Stage.NoLoad:
                TryAdvanceToShortCircuit();
                break;
            case Lab6Stage.ShortCircuit:
                TryAdvanceFromShortCircuitToLoad();
                break;
            case Lab6Stage.Load:
                TryAdvance(Lab6Stage.Load, Lab6Stage.ResistanceMeasurement, "Начат этап измерения сопротивлений обмоток.");
                break;
            case Lab6Stage.ResistanceMeasurement:
                TryAdvance(Lab6Stage.ResistanceMeasurement, Lab6Stage.Completed, "Лабораторная завершена.");
                break;
            case Lab6Stage.Completed:
                SetMessage("Лабораторная уже завершена.");
                break;
        }

        RefreshResultsView();
    }

    public void EmergencyStop()
    {
        q1Enabled = false;
        q2Position = 0;
        q3Enabled = false;
        q4Enabled = false;
        q5Enabled = false;
        q6Enabled = false;
        brakeEnabled = false;
        loadStep = 0;
        loadPercent = 0f;
        SetMessage("Аварийный останов: питание, двигатель, нагрузка и тормоз выключены.", true);
        RefreshResultsView();
    }

    public void RemoveLastPointInCurrentStage()
    {
        List<Lab6Measurement> stageList = GetMutableStageMeasurements(currentStage);
        if (stageList == null)
        {
            SetMessage("Для текущего этапа удаление точек недоступно.", true);
            return;
        }

        if (stageList.Count == 0)
        {
            SetMessage("В текущем этапе нет записанных точек для удаления.", true);
            return;
        }

        Lab6Measurement removed = stageList[stageList.Count - 1];
        stageList.RemoveAt(stageList.Count - 1);
        measurements.Remove(removed);
        SetMessage("Последняя точка текущего этапа удалена.");
        RefreshResultsView();
    }

    public void ResetLab()
    {
        currentStage = Lab6Stage.Preparation;
        q1Enabled = false;
        q2Position = 0;
        q3Enabled = false;
        q4Enabled = false;
        q5Enabled = false;
        q6Enabled = false;
        brakeEnabled = false;
        loadStep = 0;
        loadPercent = 0f;
        measurements.Clear();
        noLoadMeasurements.Clear();
        shortCircuitMeasurements.Clear();
        loadMeasurements.Clear();
        resistanceMeasurements.Clear();
        lastMessage = "Подготовка лабораторной. Нажмите Next Stage для начала опыта ХХ.";

        RefreshViews();
        RefreshResultsView();
        Debug.Log("Lab6: laboratory was fully reset.");
    }

    public int GetRecordedPointCount(Lab6Stage stage)
    {
        List<Lab6Measurement> stageList = GetMutableStageMeasurements(stage);
        return stageList != null ? stageList.Count : 0;
    }

    private void AddMeasurementToStageList(Lab6Measurement point)
    {
        switch (point.stage)
        {
            case Lab6Stage.NoLoad:
                noLoadMeasurements.Add(point);
                break;
            case Lab6Stage.ShortCircuit:
                shortCircuitMeasurements.Add(point);
                break;
            case Lab6Stage.Load:
                loadMeasurements.Add(point);
                break;
            case Lab6Stage.ResistanceMeasurement:
                resistanceMeasurements.Add(point);
                break;
        }
    }

    private List<Lab6Measurement> GetMutableStageMeasurements(Lab6Stage stage)
    {
        switch (stage)
        {
            case Lab6Stage.NoLoad:
                return noLoadMeasurements;
            case Lab6Stage.ShortCircuit:
                return shortCircuitMeasurements;
            case Lab6Stage.Load:
                return loadMeasurements;
            case Lab6Stage.ResistanceMeasurement:
                return resistanceMeasurements;
            default:
                return null;
        }
    }

    private void TryAdvance(Lab6Stage requiredStage, Lab6Stage nextStage, string successMessage)
    {
        int count = GetRecordedPointCount(requiredStage);
        int required = GetRequiredPoints(requiredStage);
        if (count < required)
        {
            SetMessage($"Переход запрещён: нужно {required} точек, записано {count}.", true);
            return;
        }

        currentStage = nextStage;
        SetMessage(successMessage);
    }

    private void TryAdvanceToShortCircuit()
    {
        int count = GetRecordedPointCount(Lab6Stage.NoLoad);
        int required = GetRequiredPoints(Lab6Stage.NoLoad);
        if (count < required)
        {
            SetMessage($"Переход запрещён: нужно {required} точек, записано {count}.", true);
            return;
        }

        currentStage = Lab6Stage.ShortCircuit;
        brakeEnabled = true;
        SetMessage("Начат опыт короткого замыкания. Ротор заторможен автоматически.");
    }

    private void TryAdvanceFromShortCircuitToLoad()
    {
        int count = GetRecordedPointCount(Lab6Stage.ShortCircuit);
        int required = GetRequiredPoints(Lab6Stage.ShortCircuit);
        if (count < required)
        {
            SetMessage($"Переход запрещён: нужно {required} точек, записано {count}.", true);
            return;
        }

        brakeEnabled = false;
        loadStep = 0;
        loadPercent = 0f;
        currentStage = Lab6Stage.Load;
        SetMessage("Начат опыт непосредственной нагрузки. Тормоз ротора отключён. Установите нагрузку реостатом R.");
    }

    private void ApplyAutomaticBrakeForStage()
    {
        brakeEnabled = currentStage == Lab6Stage.ShortCircuit;
    }

    private bool TryValidateCurrentStage(out string error)
    {
        switch (currentStage)
        {
            case Lab6Stage.NoLoad:
                if (!q1Enabled) { error = "для холостого хода включите Q1."; return false; }
                if (!Q2Enabled) { error = "для холостого хода задайте напряжение Q2."; return false; }
                if (!q5Enabled) { error = "для холостого хода включите Q5."; return false; }
                if (!q6Enabled) { error = "для холостого хода включите Q6."; return false; }
                error = null;
                return true;
            case Lab6Stage.ShortCircuit:
                if (!q1Enabled) { error = "для опыта КЗ включите Q1."; return false; }
                if (!q5Enabled) { error = "для опыта КЗ включите Q5."; return false; }
                if (!Q2Enabled) { error = "для опыта КЗ задайте низкое напряжение Q2."; return false; }
                if (!brakeEnabled) { error = "для опыта КЗ ротор должен быть заторможен."; return false; }
                if (GetShortCircuitCurrent() > GetNominalCurrent() * 1.2f + 0.001f) { error = "Ток КЗ превышает 1.2 Iн. Уменьшите положение РНТ."; return false; }
                error = null;
                return true;
            case Lab6Stage.Load:
                if (!q1Enabled) { error = "для опыта нагрузки включите Q1."; return false; }
                if (!Q2Enabled) { error = "для опыта нагрузки задайте напряжение Q2."; return false; }
                if (!q3Enabled) { error = "для опыта нагрузки включите Q3."; return false; }
                if (!q4Enabled) { error = "для опыта нагрузки включите Q4."; return false; }
                if (!q5Enabled) { error = "для опыта нагрузки включите Q5."; return false; }
                if (!q6Enabled) { error = "для опыта нагрузки включите Q6."; return false; }
                if (brakeEnabled) { error = "Отключите тормоз ротора перед опытом нагрузки."; return false; }
                error = null;
                return true;
            case Lab6Stage.ResistanceMeasurement:
                error = null;
                return true;
            case Lab6Stage.Completed:
                error = "лабораторная уже завершена.";
                return false;
            default:
                error = "сначала перейдите к первому этапу кнопкой Next Stage.";
                return false;
        }
    }

    private int GetRequiredPoints(Lab6Stage stage)
    {
        switch (stage)
        {
            case Lab6Stage.NoLoad:
                return Mathf.Max(0, Data.requiredNoLoadPoints);
            case Lab6Stage.ShortCircuit:
                return Mathf.Max(0, Data.requiredShortCircuitPoints);
            case Lab6Stage.Load:
                return Mathf.Max(0, Data.requiredLoadPoints);
            case Lab6Stage.ResistanceMeasurement:
                return 1;
            default:
                return 0;
        }
    }

    private void RefreshViews()
    {
        currentMeasurement = CreateMeasurementSnapshot();

        if (hudView != null)
        {
            hudView.Refresh(this);
        }

        if (standView != null)
        {
            standView.UpdateView(this, currentMeasurement, Time.deltaTime);
        }
    }

    private void RefreshStandViewOnly()
    {
        if (standView == null)
        {
            return;
        }

        currentMeasurement = CreateMeasurementSnapshot();
        standView.UpdateView(this, currentMeasurement, Time.deltaTime);
    }

    private Lab6Measurement CreateMeasurementSnapshot()
    {
        EnsureData();

        Lab6Measurement result = new Lab6Measurement
        {
            stage = currentStage,
            q2Position = q2Position,
            voltage = q1Enabled && Q2Enabled ? Voltage : 0f,
            loadPercent = loadPercent
        };

        if (currentStage == Lab6Stage.ResistanceMeasurement)
        {
            FillResistance(result);
            return SanitizeMeasurement(result);
        }

        if (!q1Enabled || !Q2Enabled || !q5Enabled)
        {
            return SanitizeMeasurement(result);
        }

        switch (currentStage)
        {
            case Lab6Stage.ShortCircuit:
                FillShortCircuit(result);
                break;
            case Lab6Stage.Load:
                FillLoad(result);
                break;
            default:
                FillNoLoad(result);
                break;
        }

        return SanitizeMeasurement(result);
    }

    private void FillNoLoad(Lab6Measurement result)
    {
        float syncSpeed = GetSynchronousSpeed();
        float nominalCurrent = GetNominalCurrent();
        float vNorm = Mathf.Clamp01(result.voltage / Mathf.Max(1f, data.nominalVoltage));
        result.cosPhi = Mathf.Lerp(0.18f, 0.32f, vNorm);
        result.current = nominalCurrent * Mathf.Lerp(0.16f, 0.28f, vNorm);
        result.speed = brakeEnabled ? 0f : syncSpeed * Mathf.Lerp(0.975f, 0.992f, vNorm);
        result.powerInput = Mathf.Sqrt(3f) * result.voltage * result.current * result.cosPhi;
        result.powerOutput = 0f;
        result.torque = 0f;
        result.efficiency = 0f;
        result.slip = result.speed > 0f ? Mathf.Clamp01((syncSpeed - result.speed) / syncSpeed) : 1f;
    }

    private void FillShortCircuit(Lab6Measurement result)
    {
        float limitedPosition = Mathf.Clamp(q2Position, 0, 7) / 5f;
        result.cosPhi = 0.42f;
        result.current = GetShortCircuitCurrent();
        result.speed = brakeEnabled ? 0f : GetSynchronousSpeed() * 0.35f;
        result.powerInput = Mathf.Sqrt(3f) * result.voltage * result.current * result.cosPhi;
        result.powerOutput = 0f;
        result.torque = GetNominalTorque() * Mathf.Clamp(limitedPosition, 0.15f, 1.6f);
        result.efficiency = 0f;
        result.slip = brakeEnabled ? 1f : Mathf.Clamp01((GetSynchronousSpeed() - result.speed) / GetSynchronousSpeed());
    }

    private void FillResistance(Lab6Measurement result)
    {
        float statorResistance = Mathf.Max(0f, data.statorResistance);
        result.za = statorResistance * 0.98f;
        result.zb = statorResistance * 1.01f;
        result.zc = statorResistance;
        result.zAverage = (result.za + result.zb + result.zc) / 3f;
    }

    private void FillLoad(Lab6Measurement result)
    {
        float syncSpeed = GetSynchronousSpeed();
        float nominalCurrent = GetNominalCurrent();
        float maxLoadFraction = Mathf.Max(0f, data.maxLoadPercent) / 100f;
        float loadFraction = Mathf.Clamp(loadPercent / 100f, 0f, maxLoadFraction);
        float normalizedLoad = Mathf.Clamp01(loadFraction / Mathf.Max(0.01f, maxLoadFraction));
        result.cosPhi = Mathf.Lerp(0.4f, 0.84f, Mathf.Clamp01(loadFraction));
        result.powerOutput = Mathf.Max(0f, data.nominalPower) * loadFraction;
        result.efficiency = loadFraction <= 0.01f
            ? 0f
            : Mathf.Clamp(Mathf.Lerp(0.55f, 0.84f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(loadFraction / 0.7f))) - Mathf.Max(0f, loadFraction - 1f) * 0.08f, 0f, 0.86f);
        result.powerInput = result.efficiency > 0.01f
            ? result.powerOutput / result.efficiency
            : Mathf.Sqrt(3f) * result.voltage * nominalCurrent * 0.2f * 0.25f;
        result.current = Mathf.Max(nominalCurrent * 0.25f, result.powerInput / Mathf.Max(1f, Mathf.Sqrt(3f) * result.voltage * Mathf.Max(0.25f, result.cosPhi)));
        result.slip = Mathf.Lerp(0.01f, 0.06f, normalizedLoad);
        result.speed = syncSpeed * Mathf.Clamp(1f - result.slip, 0.9f, 0.995f);
        result.torque = result.speed > 1f
            ? result.powerOutput / (2f * Mathf.PI * result.speed / 60f)
            : 0f;
    }

    private float GetNominalTorque()
    {
        return Mathf.Max(0f, data.nominalPower) / Mathf.Max(1f, 2f * Mathf.PI * GetSynchronousSpeed() / 60f);
    }

    private float GetNominalCurrent()
    {
        return Mathf.Max(0f, data.nominalCurrent);
    }

    private float GetSynchronousSpeed()
    {
        return Mathf.Max(1f, data.synchronousSpeed);
    }

    private float GetShortCircuitCurrent()
    {
        return GetNominalCurrent() * 1.2f * (Mathf.Clamp(q2Position, 0, MaxQ2Position) / 5f);
    }

    private static float GetLoadPercentForStep(int step)
    {
        return Mathf.Clamp(step, 0, 4) * 25f;
    }

    private bool HasDuplicatePoint(Lab6Measurement point)
    {
        List<Lab6Measurement> stageList = GetMutableStageMeasurements(point.stage);
        if (stageList == null)
        {
            return false;
        }

        for (int i = 0; i < stageList.Count; i++)
        {
            Lab6Measurement existing = stageList[i];
            if (existing == null)
            {
                continue;
            }

            switch (point.stage)
            {
                case Lab6Stage.NoLoad:
                case Lab6Stage.ShortCircuit:
                    if (existing.q2Position == point.q2Position || Mathf.Abs(existing.voltage - point.voltage) <= 0.5f)
                    {
                        return true;
                    }
                    break;
                case Lab6Stage.Load:
                    if (Mathf.Abs(existing.loadPercent - point.loadPercent) <= 0.1f)
                    {
                        return true;
                    }
                    break;
                case Lab6Stage.ResistanceMeasurement:
                    return true;
            }
        }

        return false;
    }

    private static WindingConnection GetWindingConnection(Lab6Stage stage)
    {
        switch (stage)
        {
            case Lab6Stage.NoLoad:
            case Lab6Stage.ShortCircuit:
                return WindingConnection.Delta;
            case Lab6Stage.Load:
                return WindingConnection.Star;
            case Lab6Stage.ResistanceMeasurement:
                return WindingConnection.Measurement;
            default:
                return WindingConnection.None;
        }
    }

    private static string GetWindingConnectionText(WindingConnection connection)
    {
        switch (connection)
        {
            case WindingConnection.Delta:
                return "Соединение обмоток: Δ";
            case WindingConnection.Star:
                return "Соединение обмоток: Y";
            case WindingConnection.Measurement:
                return "Режим измерения сопротивлений обмоток";
            default:
                return "Соединение обмоток: не требуется";
        }
    }

    private Lab6Measurement SanitizeMeasurement(Lab6Measurement measurement)
    {
        measurement.q2Position = Mathf.Clamp(measurement.q2Position, 0, MaxQ2Position);
        measurement.voltage = SafeNonNegative(measurement.voltage);
        measurement.loadPercent = SafeNonNegative(measurement.loadPercent);
        measurement.current = SafeNonNegative(measurement.current);
        measurement.powerInput = SafeNonNegative(measurement.powerInput);
        measurement.powerOutput = SafeNonNegative(measurement.powerOutput);
        measurement.speed = SafeNonNegative(measurement.speed);
        measurement.torque = SafeNonNegative(measurement.torque);
        measurement.cosPhi = Mathf.Clamp01(SafeNonNegative(measurement.cosPhi));
        measurement.efficiency = Mathf.Clamp01(SafeNonNegative(measurement.efficiency));
        measurement.slip = Mathf.Clamp01(SafeNonNegative(measurement.slip));
        measurement.za = SafeNonNegative(measurement.za);
        measurement.zb = SafeNonNegative(measurement.zb);
        measurement.zc = SafeNonNegative(measurement.zc);
        measurement.zAverage = SafeNonNegative(measurement.zAverage);
        return measurement;
    }

    private static float SafeNonNegative(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }

    private void EnsureData()
    {
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<Lab6Data>();
        }
    }

    private static Lab6HudView FindAnyLab6HudView()
    {
        Lab6HudView[] views = FindObjectsByType<Lab6HudView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return views.Length > 0 ? views[0] : null;
    }

    private static Lab6StandView FindAnyLab6StandView()
    {
        Lab6StandView[] views = FindObjectsByType<Lab6StandView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return views.Length > 0 ? views[0] : null;
    }

    private static Lab6ResultsView FindAnyLab6ResultsView()
    {
        Lab6ResultsView[] views = FindObjectsByType<Lab6ResultsView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return views.Length > 0 ? views[0] : null;
    }

    private void RefreshResultsView()
    {
        if (resultsView != null)
        {
            resultsView.RefreshAll();
        }
    }

    private void SetMessage(string message, bool warning = false)
    {
        lastMessage = message;
        if (warning)
        {
            Debug.LogWarning("Lab6: " + message);
        }
        else
        {
            Debug.Log("Lab6: " + message);
        }

        RefreshViews();
    }

    private Lab6HudView CreateRuntimeHud()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Lab6RuntimeHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        runtimeHudObject = canvasObject;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        runtimeHudPanelObject = panelObject;
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(12f, -12f);
        panelRect.sizeDelta = new Vector2(620f, -24f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        Lab6HudView view = panelObject.AddComponent<Lab6HudView>();
        TextMeshProUGUI title = CreateHudText(panelObject.transform, "Title", 24f, FontStyles.Bold);
        TextMeshProUGUI stage = CreateHudText(panelObject.transform, "Stage", 20f, FontStyles.Bold);
        TextMeshProUGUI instruction = CreateHudText(panelObject.transform, "Instruction", 17f, FontStyles.Normal);
        TextMeshProUGUI warning = CreateHudText(panelObject.transform, "Warning", 17f, FontStyles.Bold);
        warning.color = new Color(1f, 0.78f, 0.25f, 1f);
        TextMeshProUGUI switches = CreateHudText(panelObject.transform, "Switches", 16f, FontStyles.Normal);
        TextMeshProUGUI measurementsText = CreateHudText(panelObject.transform, "Measurements", 16f, FontStyles.Normal);
        TextMeshProUGUI points = CreateHudText(panelObject.transform, "Points", 16f, FontStyles.Normal);
        runtimeHudHintText = CreateRuntimeHudHint(canvasObject.transform);

        if (showDebugControls)
        {
            CreateButtonRow(panelObject.transform,
                ("Q1", (Action)ToggleQ1),
                ("Q2 -", DecreaseQ2),
                ("Q2 +", IncreaseQ2),
                ("Q5", ToggleQ5),
                ("Q6", ToggleQ6));
            CreateButtonRow(panelObject.transform,
                ("Q3", (Action)ToggleQ3),
                ("Q4", ToggleQ4),
                ("Brake", ToggleBrake),
                ("Load -", () => ChangeLoadPercent(-25f)),
                ("Load +", () => ChangeLoadPercent(25f)));
            CreateButtonRow(panelObject.transform,
                ("Record", (Action)RecordPoint),
                ("Remove Last", RemoveLastPointInCurrentStage),
                ("Next Stage", NextStage),
                ("Emergency Stop", EmergencyStop));
        }

        view.BindRuntimeFields(title, stage, instruction, warning, switches, measurementsText, points);
        view.SetController(this);
        ConfigureRuntimeHudRaycasts(canvasObject);
        SetRuntimeHudVisible(showRuntimeHud);
        return view;
    }

    private void SetRuntimeHudVisible(bool visible)
    {
        showRuntimeHud = visible;
        if (runtimeHudPanelObject != null)
        {
            runtimeHudPanelObject.SetActive(showRuntimeHud);
        }

        if (runtimeHudHintText != null)
        {
            runtimeHudHintText.text = showRuntimeHud ? "H — скрыть помощь" : "H — помощь";
        }
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
        rect.sizeDelta = new Vector2(220f, 32f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "H — помощь";
        text.fontSize = 16f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void CreateButtonRow(Transform parent, params (string label, Action action)[] buttons)
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
        {
            CreateHudButton(rowObject.transform, buttons[i].label, buttons[i].action);
        }
    }

    private static void CreateHudButton(Transform parent, string label, Action action)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.32f, 0.95f);
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => action());

        LayoutElement buttonLayout = buttonObject.GetComponent<LayoutElement>();
        buttonLayout.minHeight = 36f;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 15f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void ConfigureRuntimeHudRaycasts(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].raycastTarget = false;
            }
        }

        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null)
            {
                continue;
            }

            selectable.interactable = true;

            if (selectable.targetGraphic != null)
            {
                selectable.targetGraphic.raycastTarget = true;
            }

            Graphic graphic = selectable.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
