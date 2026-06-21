using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Lab3Stage
{
    Preparation,
    ResistanceMeasurement,
    CircuitSetup,
    NoLoadCharacteristic,
    LoadCharacteristic,
    ExternalCharacteristic,
    RegulationCharacteristic,
    ShortCircuitCharacteristic,
    Completed
}

[Serializable]
public struct Lab3ResistancePoint
{
    public float voltage;
    public float current;
    public float armatureResistance;
    public float hotArmatureResistance;
}

[Serializable]
public struct Lab3CharacteristicPoint
{
    public float x;
    public float y;
    public float voltage;
    public float emf;
    public float armatureCurrent;
    public float fieldCurrent;
    public float shortCircuitCurrent;
    public float omega;
}

public class Lab3Controller : MonoBehaviour
{
    private const string Lab3SceneName = "Lab3";
    private const string Lab3ScenePath = "Assets/Scenes/Lab3.unity";
    private const int MaxPointsPerTable = 5;
    private const float DuplicateTolerance = 0.001f;
    private const float NominalVoltage = 220f;
    private const float NominalOmega = 157f;
    private const float RegulationTargetVoltage = NominalVoltage;
    private const float RegulationVoltageTolerance = 5f;
    private const float ResistanceHotRa = 12.5f;

    [SerializeField] private Lab3HudView hudView;
    [SerializeField] private Lab3StandView standView;
    [SerializeField] private Lab3_ElectricCircuit existingCircuit;
    [SerializeField] private bool createRuntimeHud = true;
    [SerializeField] private bool showRuntimeHud = true;
    [SerializeField] private bool showDebugControls = true;

    [Header("State")]
    [SerializeField] private Lab3Stage currentStage = Lab3Stage.Preparation;
    [SerializeField] private bool q1Enabled;
    [SerializeField] private bool q2Enabled;
    [SerializeField] private bool q3Enabled;
    [SerializeField] private bool shortCircuitEnabled;
    [SerializeField] private bool resistanceMeasurementMode;
    [Range(0f, 100f)] [SerializeField] private float r1Position = 35f;
    [Range(0f, 100f)] [SerializeField] private float r2Position = 0f;

    [Header("Synthetic Measurements")]
    [SerializeField] private float voltage;
    [SerializeField] private float emf;
    [SerializeField] private float armatureCurrent;
    [SerializeField] private float fieldCurrent;
    [SerializeField] private float shortCircuitCurrent;
    [SerializeField] private float omega = NominalOmega;

    private readonly List<Lab3ResistancePoint> resistancePoints = new List<Lab3ResistancePoint>();
    private readonly List<Lab3CharacteristicPoint> noLoadPoints = new List<Lab3CharacteristicPoint>();
    private readonly List<Lab3CharacteristicPoint> loadPoints = new List<Lab3CharacteristicPoint>();
    private readonly List<Lab3CharacteristicPoint> externalPoints = new List<Lab3CharacteristicPoint>();
    private readonly List<Lab3CharacteristicPoint> regulationPoints = new List<Lab3CharacteristicPoint>();
    private readonly List<Lab3CharacteristicPoint> shortCircuitPoints = new List<Lab3CharacteristicPoint>();
    private readonly List<RuntimeButtonLabel> runtimeButtonLabels = new List<RuntimeButtonLabel>();

    private GameObject runtimeHudObject;
    private GameObject runtimeHudPanelObject;
    private TextMeshProUGUI runtimeHudHintText;
    private bool runtimeHudVisibleBeforePause;
    private bool runtimeHudPaused;
    private float loadReferenceIa = -1f;
    private float externalReferenceIf = -1f;
    private float regulationReferenceU = -1f;
    private string lastMessage = "Подготовьте стенд и перейдите к измерению сопротивлений.";

    public Lab3Stage CurrentStage => currentStage;
    public bool Q1Enabled => q1Enabled;
    public bool Q2Enabled => q2Enabled;
    public bool Q3Enabled => q3Enabled;
    public bool ShortCircuitEnabled => shortCircuitEnabled;
    public bool ResistanceMeasurementMode => resistanceMeasurementMode;
    public float R1Position => r1Position;
    public float R2Position => r2Position;
    public float Voltage => voltage;
    public float Emf => emf;
    public float ArmatureCurrent => armatureCurrent;
    public float FieldCurrent => fieldCurrent;
    public float ShortCircuitCurrent => shortCircuitCurrent;
    public float Omega => omega;
    public float TargetRegulationVoltage => RegulationTargetVoltage;
    public float RegulationVoltageDelta => voltage - RegulationTargetVoltage;
    public bool ShowDebugControls => showDebugControls;
    public string LastMessage => lastMessage;
    public IReadOnlyList<Lab3ResistancePoint> ResistancePoints => resistancePoints;
    public IReadOnlyList<Lab3CharacteristicPoint> NoLoadPoints => noLoadPoints;
    public IReadOnlyList<Lab3CharacteristicPoint> LoadPoints => loadPoints;
    public IReadOnlyList<Lab3CharacteristicPoint> ExternalPoints => externalPoints;
    public IReadOnlyList<Lab3CharacteristicPoint> RegulationPoints => regulationPoints;
    public IReadOnlyList<Lab3CharacteristicPoint> ShortCircuitPoints => shortCircuitPoints;

    private struct RuntimeButtonLabel
    {
        public TextMeshProUGUI text;
        public string label;

        public RuntimeButtonLabel(TextMeshProUGUI text, string label)
        {
            this.text = text;
            this.label = label;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeBootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterRuntimeBootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapActiveLab3Scene()
    {
        RegisterRuntimeBootstrap();
        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (!IsLab3Scene(scene))
        {
            return;
        }

        if (FindObjectsByType<Lab3Controller>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        GameObject root = new GameObject("Lab3Root");
        root.AddComponent<Lab3Controller>();
    }

    private static bool IsLab3Scene(Scene scene)
    {
        string scenePath = string.IsNullOrEmpty(scene.path) ? string.Empty : scene.path.Replace('\\', '/');
        return scene.name == Lab3SceneName || scenePath == Lab3ScenePath;
    }

    private void Awake()
    {
        ResolveReferences();

        if (hudView == null && createRuntimeHud)
        {
            hudView = CreateRuntimeHud();
        }

        if (hudView != null)
        {
            hudView.SetController(this);
        }

        ResetSyntheticValuesOnly();
        RefreshViews();
    }

    private void Update()
    {
        if (SyncRuntimeHudWithPause())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            SetRuntimeHudVisible(!showRuntimeHud);
        }

        RecalculateSyntheticValues();
        RefreshViews(false);
    }

    public void ToggleQ1()
    {
        q1Enabled = !q1Enabled;
        if (!q1Enabled)
        {
            q2Enabled = false;
            q3Enabled = false;
            shortCircuitEnabled = false;
        }

        SetMessage($"Q1 {(q1Enabled ? "включен" : "выключен")}.");
    }

    public void ToggleQ2()
    {
        q2Enabled = !q2Enabled;
        if (q2Enabled && currentStage == Lab3Stage.NoLoadCharacteristic)
        {
            SetMessage("Q2 включен: для холостого хода его нужно выключить.", true);
            return;
        }

        SetMessage($"Q2 {(q2Enabled ? "включен" : "выключен")}.");
    }

    public void ToggleQ3()
    {
        q3Enabled = !q3Enabled;
        SetMessage($"Q3 {(q3Enabled ? "включен" : "выключен")}.");
    }

    public void ToggleShortCircuitMode()
    {
        shortCircuitEnabled = !shortCircuitEnabled;
        if (shortCircuitEnabled)
        {
            q2Enabled = false;
        }

        SetMessage(shortCircuitEnabled ? "Режим короткого замыкания включен." : "Режим короткого замыкания выключен.");
    }

    public void ToggleResistanceMeasurementMode()
    {
        resistanceMeasurementMode = !resistanceMeasurementMode;
        SetMessage(resistanceMeasurementMode ? "Режим измерения сопротивлений включен." : "Режим измерения сопротивлений выключен.");
    }

    public void IncreaseR1() => ChangeR1(10f);
    public void DecreaseR1() => ChangeR1(-10f);
    public void IncreaseR2() => ChangeR2(10f);
    public void DecreaseR2() => ChangeR2(-10f);

    public void ChangeR1(float delta)
    {
        r1Position = Mathf.Clamp(r1Position + delta, 0f, 100f);
        SetMessage($"R1 = {r1Position:F0}%.");
    }

    public void ChangeR2(float delta)
    {
        r2Position = Mathf.Clamp(r2Position + delta, 0f, 100f);
        SetMessage($"R2 = {r2Position:F0}%.");
    }

    public void TuneRegulationVoltage()
    {
        if (currentStage != Lab3Stage.RegulationCharacteristic)
        {
            SetMessage("Tune U доступен только на этапе регулировочной характеристики.", true);
            return;
        }

        if (!q1Enabled || !q2Enabled || !q3Enabled)
        {
            SetMessage("Для Tune U включите Q1, Q2 и Q3.", true);
            return;
        }

        if (shortCircuitEnabled)
        {
            SetMessage("Для Tune U выключите SC.", true);
            return;
        }

        if (resistanceMeasurementMode)
        {
            SetMessage("Для Tune U выключите R mode.", true);
            return;
        }

        float low = 0f;
        float high = 100f;
        for (int i = 0; i < 16; i++)
        {
            float mid = (low + high) * 0.5f;
            if (CalculateVoltageForR1(mid) < RegulationTargetVoltage)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        r1Position = Mathf.Clamp((low + high) * 0.5f, 0f, 100f);
        RecalculateSyntheticValues();
        SetMessage($"Tune U: R1={r1Position:F0}%, U={voltage:F1} В, ΔU={RegulationVoltageDelta:F1} В.");
    }

    public void NextStage()
    {
        if (!CanAdvanceToNextStage(out string errorMessage))
        {
            SetMessage(errorMessage, true);
            return;
        }

        if (currentStage < Lab3Stage.Completed)
        {
            if (currentStage == Lab3Stage.ResistanceMeasurement)
            {
                resistanceMeasurementMode = false;
            }

            currentStage++;
            ResetStageReferences();
            SetMessage("Этап: " + GetStageName(currentStage) + ".");
        }
    }

    public void PreviousStage()
    {
        if (currentStage > Lab3Stage.Preparation)
        {
            currentStage--;
            ResetStageReferences();
            SetMessage("Этап: " + GetStageName(currentStage) + ".");
        }
    }

    public void RecordPoint()
    {
        RecalculateSyntheticValues();

        if (!CanRecordCurrentStage(out string error))
        {
            SetMessage(error, true);
            return;
        }

        if (GetCurrentStagePointCount() >= MaxPointsPerTable)
        {
            SetMessage("Достигнут лимит: 5 точек для текущего этапа", true);
            return;
        }

        switch (currentStage)
        {
            case Lab3Stage.ResistanceMeasurement:
                Lab3ResistancePoint resistancePoint = CreateResistancePoint();
                if (HasDuplicateResistancePoint(resistancePoint))
                {
                    SetMessage("Такая точка уже записана для текущего этапа", true);
                    return;
                }
                resistancePoints.Add(resistancePoint);
                break;
            case Lab3Stage.NoLoadCharacteristic:
                if (!TryAddCharacteristicPoint(noLoadPoints, CreateCharacteristicPoint(fieldCurrent, emf)))
                {
                    return;
                }
                break;
            case Lab3Stage.LoadCharacteristic:
                Lab3CharacteristicPoint loadPoint = CreateCharacteristicPoint(fieldCurrent, voltage);
                if (!TryAddCharacteristicPoint(loadPoints, loadPoint))
                {
                    return;
                }
                if (loadReferenceIa < 0f)
                {
                    loadReferenceIa = armatureCurrent;
                }
                break;
            case Lab3Stage.ExternalCharacteristic:
                Lab3CharacteristicPoint externalPoint = CreateCharacteristicPoint(armatureCurrent, voltage);
                if (!TryAddCharacteristicPoint(externalPoints, externalPoint))
                {
                    return;
                }
                if (externalReferenceIf < 0f)
                {
                    externalReferenceIf = fieldCurrent;
                }
                break;
            case Lab3Stage.RegulationCharacteristic:
                Lab3CharacteristicPoint regulationPoint = CreateCharacteristicPoint(armatureCurrent, fieldCurrent);
                if (!TryAddCharacteristicPoint(regulationPoints, regulationPoint))
                {
                    return;
                }
                if (regulationReferenceU < 0f)
                {
                    regulationReferenceU = voltage;
                }
                break;
            case Lab3Stage.ShortCircuitCharacteristic:
                if (!TryAddCharacteristicPoint(shortCircuitPoints, CreateCharacteristicPoint(fieldCurrent, shortCircuitCurrent)))
                {
                    return;
                }
                break;
        }

        SetMessage($"Точка записана. Точек на текущем этапе: {GetCurrentStagePointCount()}.");
        RefreshLab3ChartTables();
    }

    public void RemoveLastPointInCurrentStage()
    {
        List<Lab3CharacteristicPoint> characteristicList = GetCurrentCharacteristicList();
        if (currentStage == Lab3Stage.ResistanceMeasurement && resistancePoints.Count > 0)
        {
            resistancePoints.RemoveAt(resistancePoints.Count - 1);
            ResetStageReferences();
            SetMessage("Последняя точка сопротивлений удалена.");
            RefreshLab3ChartTables();
            return;
        }

        if (characteristicList == null || characteristicList.Count == 0)
        {
            SetMessage("В текущей таблице нет точек для удаления", true);
            return;
        }

        characteristicList.RemoveAt(characteristicList.Count - 1);
        ResetStageReferences();
        SetMessage("Последняя точка текущего этапа удалена.");
        RefreshLab3ChartTables();
    }

    public void ClearCurrentStagePoints()
    {
        if (currentStage == Lab3Stage.ResistanceMeasurement)
        {
            resistancePoints.Clear();
            ResetStageReferences();
            SetMessage("Текущая таблица очищена.");
            RefreshLab3ChartTables();
            return;
        }

        List<Lab3CharacteristicPoint> characteristicList = GetCurrentCharacteristicList();
        if (characteristicList == null)
        {
            SetMessage("На текущем этапе нет таблицы для очистки", true);
            return;
        }

        characteristicList.Clear();
        ResetStageReferences();
        SetMessage("Текущая таблица очищена.");
        RefreshLab3ChartTables();
    }

    public void ClearAllPoints()
    {
        ClearAllTablesSilently();
        ResetStageReferences();
        SetMessage("Все временные результаты Lab3 очищены.");
        RefreshLab3ChartTables();
    }

    public void ResetLab()
    {
        currentStage = Lab3Stage.Preparation;
        ClearAllTablesSilently();
        q1Enabled = false;
        q2Enabled = false;
        q3Enabled = false;
        shortCircuitEnabled = false;
        resistanceMeasurementMode = false;
        r1Position = 35f;
        r2Position = 0f;
        ResetStageReferences();
        ResetSyntheticValuesOnly();

        if (existingCircuit != null)
        {
            existingCircuit.ResetCircuit();
        }

        SetMessage("Lab3 сброшена в исходное состояние.");
        RefreshLab3ChartTables();
    }

    public int GetRecordedPointCount(Lab3Stage stage)
    {
        switch (stage)
        {
            case Lab3Stage.ResistanceMeasurement:
                return resistancePoints.Count;
            case Lab3Stage.NoLoadCharacteristic:
                return noLoadPoints.Count;
            case Lab3Stage.LoadCharacteristic:
                return loadPoints.Count;
            case Lab3Stage.ExternalCharacteristic:
                return externalPoints.Count;
            case Lab3Stage.RegulationCharacteristic:
                return regulationPoints.Count;
            case Lab3Stage.ShortCircuitCharacteristic:
                return shortCircuitPoints.Count;
            default:
        return 0;
        }
    }

    private void ClearAllTablesSilently()
    {
        resistancePoints.Clear();
        noLoadPoints.Clear();
        loadPoints.Clear();
        externalPoints.Clear();
        regulationPoints.Clear();
        shortCircuitPoints.Clear();
    }

    public List<Vector2> GetResistanceData()
    {
        List<Vector2> points = new List<Vector2>(resistancePoints.Count);
        for (int i = 0; i < resistancePoints.Count; i++)
        {
            points.Add(new Vector2(resistancePoints[i].voltage, resistancePoints[i].current));
        }

        return points;
    }

    public List<Vector2> GetNoLoadData() => BuildVectorPoints(noLoadPoints);
    public List<Vector2> GetLoadData() => BuildVectorPoints(loadPoints);
    public List<Vector2> GetExternalData() => BuildVectorPoints(externalPoints);
    public List<Vector2> GetRegulatingData() => BuildVectorPoints(regulationPoints);
    public List<Vector2> GetShortCircuitData() => BuildVectorPoints(shortCircuitPoints);

    public string GetStageName(Lab3Stage stage)
    {
        switch (stage)
        {
            case Lab3Stage.ResistanceMeasurement:
                return "2. Измерение сопротивлений обмоток генератора";
            case Lab3Stage.CircuitSetup:
                return "3. Сборка и настройка схемы";
            case Lab3Stage.NoLoadCharacteristic:
                return "4. Характеристика холостого хода";
            case Lab3Stage.LoadCharacteristic:
                return "5. Нагрузочная характеристика";
            case Lab3Stage.ExternalCharacteristic:
                return "6. Внешняя характеристика";
            case Lab3Stage.RegulationCharacteristic:
                return "7. Регулировочная характеристика";
            case Lab3Stage.ShortCircuitCharacteristic:
                return "8. Характеристика короткого замыкания";
            case Lab3Stage.Completed:
                return "9. Обработка результатов и завершение";
            default:
                return "1. Подготовка стенда и ознакомление со схемой";
        }
    }

    private void ResolveReferences()
    {
        if (existingCircuit == null)
        {
            Lab3_ElectricCircuit[] circuits = FindObjectsByType<Lab3_ElectricCircuit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            existingCircuit = circuits.Length > 0 ? circuits[0] : null;
        }

        if (hudView == null)
        {
            Lab3HudView[] huds = FindObjectsByType<Lab3HudView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            hudView = huds.Length > 0 ? huds[0] : null;
        }

        if (standView == null)
        {
            Lab3StandView[] stands = FindObjectsByType<Lab3StandView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            standView = stands.Length > 0 ? stands[0] : gameObject.AddComponent<Lab3StandView>();
        }

        if (standView != null)
        {
            standView.BindExistingCircuit(existingCircuit);
        }
    }

    private void RecalculateSyntheticValues()
    {
        omega = q1Enabled ? NominalOmega : 0f;
        fieldCurrent = q3Enabled ? Mathf.Lerp(0.02f, 1.2f, r1Position / 100f) : 0f;
        emf = q1Enabled && q3Enabled ? NominalVoltage * 1.08f * (1f - Mathf.Exp(-2.6f * fieldCurrent)) : 0f;

        if (shortCircuitEnabled)
        {
            voltage = 0f;
            armatureCurrent = q1Enabled ? fieldCurrent * 6.5f : 0f;
            shortCircuitCurrent = armatureCurrent;
            return;
        }

        armatureCurrent = q1Enabled && q2Enabled ? Mathf.Lerp(0.2f, 8.5f, r2Position / 100f) : 0f;
        shortCircuitCurrent = 0f;

        if (!q1Enabled || !q3Enabled)
        {
            voltage = 0f;
            return;
        }

        if (!q2Enabled)
        {
            voltage = emf;
            return;
        }

        voltage = Mathf.Max(0f, emf - armatureCurrent * Mathf.Lerp(3.5f, 7.5f, r2Position / 100f));

        if (currentStage == Lab3Stage.RegulationCharacteristic)
        {
            voltage = Mathf.Lerp(voltage, NominalVoltage, 0.55f);
        }
    }

    private float CalculateVoltageForR1(float candidateR1)
    {
        if (!q1Enabled || !q2Enabled || !q3Enabled || shortCircuitEnabled)
        {
            return 0f;
        }

        float candidateFieldCurrent = Mathf.Lerp(0.02f, 1.2f, Mathf.Clamp01(candidateR1 / 100f));
        float candidateEmf = NominalVoltage * 1.08f * (1f - Mathf.Exp(-2.6f * candidateFieldCurrent));
        float candidateArmatureCurrent = Mathf.Lerp(0.2f, 8.5f, r2Position / 100f);
        float candidateVoltage = Mathf.Max(0f, candidateEmf - candidateArmatureCurrent * Mathf.Lerp(3.5f, 7.5f, r2Position / 100f));
        return Mathf.Lerp(candidateVoltage, NominalVoltage, 0.55f);
    }

    public bool CanRecordCurrentStage(out string error)
    {
        switch (currentStage)
        {
            case Lab3Stage.Preparation:
                error = "На этапе подготовки измерения не записываются";
                return false;
            case Lab3Stage.ResistanceMeasurement:
                if (!resistanceMeasurementMode)
                {
                    error = "Включите R mode для измерения сопротивлений";
                    return false;
                }

                error = string.Empty;
                return true;
            case Lab3Stage.CircuitSetup:
                error = "На этапе настройки схемы измерения не записываются";
                return false;
            case Lab3Stage.NoLoadCharacteristic:
                if (resistanceMeasurementMode)
                {
                    error = "Выключите R mode перед снятием характеристики";
                    return false;
                }
                if (!q1Enabled)
                {
                    error = "Включите Q1";
                    return false;
                }
                if (!q3Enabled)
                {
                    error = "Включите Q3";
                    return false;
                }
                if (q2Enabled)
                {
                    error = "Выключите Q2 для холостого хода";
                    return false;
                }
                if (shortCircuitEnabled)
                {
                    error = "Выключите SC для холостого хода";
                    return false;
                }
                if (armatureCurrent > 0.1f)
                {
                    error = "Ia должен быть около 0";
                    return false;
                }
                break;
            case Lab3Stage.LoadCharacteristic:
                if (!ValidatePoweredLoadedMode(out error))
                {
                    return false;
                }
                if (armatureCurrent < 0.2f)
                {
                    error = "Ia слишком мал для нагрузочной характеристики.";
                    return false;
                }
                if (loadPoints.Count > 0 && Mathf.Abs(armatureCurrent - loadPoints[0].armatureCurrent) > 0.35f)
                {
                    error = $"Поддерживайте Ia условно постоянным: {loadPoints[0].armatureCurrent:F2} А";
                    return false;
                }
                break;
            case Lab3Stage.ExternalCharacteristic:
                if (!ValidatePoweredLoadedMode(out error))
                {
                    return false;
                }
                if (externalPoints.Count > 0 && Mathf.Abs(fieldCurrent - externalPoints[0].fieldCurrent) > 0.08f)
                {
                    error = $"Поддерживайте If условно постоянным: {externalPoints[0].fieldCurrent:F2} А";
                    return false;
                }
                break;
            case Lab3Stage.RegulationCharacteristic:
                if (!ValidatePoweredLoadedMode(out error))
                {
                    return false;
                }
                if (Mathf.Abs(voltage - RegulationTargetVoltage) > RegulationVoltageTolerance)
                {
                    error = "Поддерживайте U около целевого значения. Нажмите Tune U или подстройте R1.";
                    return false;
                }
                break;
            case Lab3Stage.ShortCircuitCharacteristic:
                if (resistanceMeasurementMode)
                {
                    error = "Выключите R mode перед снятием характеристики короткого замыкания";
                    return false;
                }
                if (!q1Enabled)
                {
                    error = "Включите Q1";
                    return false;
                }
                if (!shortCircuitEnabled)
                {
                    error = "Включите режим SC";
                    return false;
                }
                if (q2Enabled)
                {
                    error = "Выключите Q2 для характеристики короткого замыкания";
                    return false;
                }
                if (voltage > 1f)
                {
                    error = "U должно быть около 0";
                    return false;
                }
                break;
            default:
                error = "На этом этапе измерения не записываются";
                return false;
        }

        error = string.Empty;
        return true;
    }

    public bool CanAdvanceToNextStage(out string error)
    {
        switch (currentStage)
        {
            case Lab3Stage.Preparation:
                error = string.Empty;
                return true;
            case Lab3Stage.CircuitSetup:
                if (!q1Enabled || !q3Enabled)
                {
                    error = "Нельзя перейти дальше: включите Q1 и Q3";
                    return false;
                }
                error = string.Empty;
                return true;
            case Lab3Stage.Completed:
                error = "Лабораторная уже завершена";
                return false;
            default:
                if (GetRecordedPointCount(currentStage) < MaxPointsPerTable)
                {
                    error = "Нельзя перейти дальше: запишите 5 точек текущего этапа";
                    return false;
                }
                error = string.Empty;
                return true;
        }
    }

    private bool ValidatePoweredLoadedMode(out string error)
    {
        if (resistanceMeasurementMode)
        {
            error = "Выключите R mode перед снятием характеристики";
            return false;
        }

        if (!q1Enabled)
        {
            error = "Включите Q1";
            return false;
        }

        if (!q2Enabled)
        {
            error = "Включите Q2";
            return false;
        }

        if (!q3Enabled)
        {
            error = "Включите Q3";
            return false;
        }

        if (shortCircuitEnabled)
        {
            error = "Выключите SC";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private Lab3ResistancePoint CreateResistancePoint()
    {
        float testCurrent = Mathf.Max(0.2f, Mathf.Lerp(0.2f, 2.2f, r2Position / 100f));
        float testVoltage = testCurrent * Mathf.Lerp(8.5f, 12.5f, r1Position / 100f);
        return new Lab3ResistancePoint
        {
            voltage = testVoltage,
            current = testCurrent,
            armatureResistance = testVoltage / testCurrent,
            hotArmatureResistance = ResistanceHotRa
        };
    }

    private bool TryAddCharacteristicPoint(List<Lab3CharacteristicPoint> target, Lab3CharacteristicPoint point)
    {
        if (HasDuplicateCharacteristicPoint(target, point.x))
        {
            SetMessage("Такая точка уже записана для текущего этапа", true);
            return false;
        }

        target.Add(point);
        return true;
    }

    private bool HasDuplicateResistancePoint(Lab3ResistancePoint point)
    {
        for (int i = 0; i < resistancePoints.Count; i++)
        {
            if (Mathf.Abs(resistancePoints[i].voltage - point.voltage) <= DuplicateTolerance &&
                Mathf.Abs(resistancePoints[i].current - point.current) <= DuplicateTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDuplicateCharacteristicPoint(List<Lab3CharacteristicPoint> points, float x)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (Mathf.Abs(points[i].x - x) <= DuplicateTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private Lab3CharacteristicPoint CreateCharacteristicPoint(float x, float y)
    {
        return new Lab3CharacteristicPoint
        {
            x = x,
            y = y,
            voltage = voltage,
            emf = emf,
            armatureCurrent = armatureCurrent,
            fieldCurrent = fieldCurrent,
            shortCircuitCurrent = shortCircuitCurrent,
            omega = omega
        };
    }

    private List<Lab3CharacteristicPoint> GetCurrentCharacteristicList()
    {
        switch (currentStage)
        {
            case Lab3Stage.NoLoadCharacteristic:
                return noLoadPoints;
            case Lab3Stage.LoadCharacteristic:
                return loadPoints;
            case Lab3Stage.ExternalCharacteristic:
                return externalPoints;
            case Lab3Stage.RegulationCharacteristic:
                return regulationPoints;
            case Lab3Stage.ShortCircuitCharacteristic:
                return shortCircuitPoints;
            default:
                return null;
        }
    }

    private int GetCurrentStagePointCount()
    {
        return GetRecordedPointCount(currentStage);
    }

    private void ResetStageReferences()
    {
        loadReferenceIa = loadPoints.Count > 0 ? loadPoints[0].armatureCurrent : -1f;
        externalReferenceIf = externalPoints.Count > 0 ? externalPoints[0].fieldCurrent : -1f;
        regulationReferenceU = regulationPoints.Count > 0 ? regulationPoints[0].voltage : -1f;
    }

    private void ResetSyntheticValuesOnly()
    {
        voltage = 0f;
        emf = 0f;
        armatureCurrent = 0f;
        fieldCurrent = 0f;
        shortCircuitCurrent = 0f;
        omega = NominalOmega;
        RecalculateSyntheticValues();
    }

    private void SetMessage(string message, bool warning = false)
    {
        lastMessage = message;
        if (warning)
        {
            Debug.LogWarning("Lab3: " + message);
        }
        else
        {
            Debug.Log("Lab3: " + message);
        }

        RefreshViews();
    }

    private void RefreshViews(bool refreshHud = true)
    {
        RecalculateSyntheticValues();

        if (standView != null)
        {
            standView.UpdateView(this, Time.deltaTime);
        }

        if (refreshHud && hudView != null)
        {
            hudView.Refresh(this);
        }
        else if (hudView != null)
        {
            hudView.Refresh(this);
        }

        RefreshRuntimeButtonLabels();
    }

    private void RefreshLab3ChartTables()
    {
        Lab3ChartTableView[] tables = FindObjectsByType<Lab3ChartTableView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tables.Length; i++)
        {
            if (tables[i] != null)
            {
                tables[i].mvpController = this;
                tables[i].Refresh();
            }
        }
    }

    private static List<Vector2> BuildVectorPoints(List<Lab3CharacteristicPoint> source)
    {
        List<Vector2> points = new List<Vector2>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            points.Add(new Vector2(source[i].x, source[i].y));
        }

        return points;
    }

    private Lab3HudView CreateRuntimeHud()
    {
        EnsureEventSystem();
        runtimeButtonLabels.Clear();

        GameObject canvasObject = new GameObject("Lab3RuntimeHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
        panelRect.sizeDelta = new Vector2(680f, -24f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.74f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        Lab3HudView view = panelObject.AddComponent<Lab3HudView>();
        TextMeshProUGUI title = CreateHudText(panelObject.transform, "Title", 23f, FontStyles.Bold);
        TextMeshProUGUI stage = CreateHudText(panelObject.transform, "Stage", 19f, FontStyles.Bold);
        TextMeshProUGUI instruction = CreateHudText(panelObject.transform, "Instruction", 16f, FontStyles.Normal);
        TextMeshProUGUI state = CreateHudText(panelObject.transform, "State", 16f, FontStyles.Normal);
        TextMeshProUGUI points = CreateHudText(panelObject.transform, "Points", 16f, FontStyles.Normal);
        TextMeshProUGUI message = CreateHudText(panelObject.transform, "Message", 16f, FontStyles.Bold);
        message.color = new Color(1f, 0.78f, 0.25f, 1f);
        runtimeHudHintText = CreateRuntimeHudHint(canvasObject.transform);

        if (showDebugControls)
        {
            CreateButtonRow(panelObject.transform,
                ("Prev", PreviousStage),
                ("Next", NextStage),
                ("Record", RecordPoint),
                ("Remove", RemoveLastPointInCurrentStage),
                ("Clear", ClearCurrentStagePoints),
                ("Reset", ResetLab));
            CreateButtonRow(panelObject.transform,
                ("Q1", ToggleQ1),
                ("Q2", ToggleQ2),
                ("Q3", ToggleQ3),
                ("SC", ToggleShortCircuitMode),
                ("R mode", ToggleResistanceMeasurementMode),
                ("Tune U", TuneRegulationVoltage));
            CreateButtonRow(panelObject.transform,
                ("R1-", DecreaseR1),
                ("R1+", IncreaseR1),
                ("R2-", DecreaseR2),
                ("R2+", IncreaseR2));
        }

        view.BindRuntimeFields(title, stage, instruction, state, points, message);
        view.SetController(this);
        ConfigureRuntimeHudRaycasts(canvasObject);
        SetRuntimeHudVisible(showRuntimeHud);
        return view;
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
            {
                return true;
            }

            runtimeHudPaused = false;
            SetRuntimeHudVisible(runtimeHudVisibleBeforePause);
        }

        return false;
    }

    private void ApplyRuntimeHudVisibility()
    {
        bool isVisible = showRuntimeHud && !runtimeHudPaused;

        if (runtimeHudPanelObject != null)
        {
            runtimeHudPanelObject.SetActive(isVisible);
        }

        if (runtimeHudHintText != null)
        {
            runtimeHudHintText.gameObject.SetActive(!runtimeHudPaused);
            runtimeHudHintText.text = showRuntimeHud ? "H - скрыть Lab3 HUD" : "H - Lab3 HUD";
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
        rect.sizeDelta = new Vector2(260f, 32f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "H - Lab3 HUD";
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
        {
            CreateHudButton(rowObject.transform, buttons[i].label, buttons[i].action);
        }
    }

    private void CreateHudButton(Transform parent, string label, Action action)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.32f, 0.95f);
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => action());

        LayoutElement buttonLayout = buttonObject.GetComponent<LayoutElement>();
        buttonLayout.minHeight = 34f;

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
        runtimeButtonLabels.Add(new RuntimeButtonLabel(text, label));
    }

    private void RefreshRuntimeButtonLabels()
    {
        for (int i = 0; i < runtimeButtonLabels.Count; i++)
        {
            RuntimeButtonLabel binding = runtimeButtonLabels[i];
            if (binding.text != null && binding.text.text != binding.label)
            {
                binding.text.text = binding.label;
            }
        }
    }

    private static void ConfigureRuntimeHudRaycasts(GameObject root)
    {
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
