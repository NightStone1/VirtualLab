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
    private const float NominalVoltage = 220f;
    private const float NominalOmega = 157f;
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
    public float R1Position => r1Position;
    public float R2Position => r2Position;
    public float Voltage => voltage;
    public float Emf => emf;
    public float ArmatureCurrent => armatureCurrent;
    public float FieldCurrent => fieldCurrent;
    public float ShortCircuitCurrent => shortCircuitCurrent;
    public float Omega => omega;
    public bool ShowDebugControls => showDebugControls;
    public string LastMessage => lastMessage;
    public IReadOnlyList<Lab3ResistancePoint> ResistancePoints => resistancePoints;
    public IReadOnlyList<Lab3CharacteristicPoint> NoLoadPoints => noLoadPoints;
    public IReadOnlyList<Lab3CharacteristicPoint> LoadPoints => loadPoints;
    public IReadOnlyList<Lab3CharacteristicPoint> ExternalPoints => externalPoints;
    public IReadOnlyList<Lab3CharacteristicPoint> RegulationPoints => regulationPoints;
    public IReadOnlyList<Lab3CharacteristicPoint> ShortCircuitPoints => shortCircuitPoints;

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
            q2Enabled = true;
        }

        SetMessage(shortCircuitEnabled ? "Режим короткого замыкания включен." : "Режим короткого замыкания выключен.");
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

    public void NextStage()
    {
        if (currentStage < Lab3Stage.Completed)
        {
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

        if (!TryValidateCurrentStage(out string error))
        {
            SetMessage("Нельзя записать точку: " + error, true);
            return;
        }

        switch (currentStage)
        {
            case Lab3Stage.ResistanceMeasurement:
                RecordResistancePoint();
                break;
            case Lab3Stage.NoLoadCharacteristic:
                noLoadPoints.Add(CreateCharacteristicPoint(fieldCurrent, emf));
                break;
            case Lab3Stage.LoadCharacteristic:
                if (loadReferenceIa < 0f)
                {
                    loadReferenceIa = armatureCurrent;
                }
                loadPoints.Add(CreateCharacteristicPoint(fieldCurrent, voltage));
                break;
            case Lab3Stage.ExternalCharacteristic:
                if (externalReferenceIf < 0f)
                {
                    externalReferenceIf = fieldCurrent;
                }
                externalPoints.Add(CreateCharacteristicPoint(armatureCurrent, voltage));
                break;
            case Lab3Stage.RegulationCharacteristic:
                if (regulationReferenceU < 0f)
                {
                    regulationReferenceU = voltage;
                }
                regulationPoints.Add(CreateCharacteristicPoint(armatureCurrent, fieldCurrent));
                break;
            case Lab3Stage.ShortCircuitCharacteristic:
                shortCircuitPoints.Add(CreateCharacteristicPoint(fieldCurrent, shortCircuitCurrent));
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
            SetMessage("Последняя точка сопротивлений удалена.");
            RefreshLab3ChartTables();
            return;
        }

        if (characteristicList == null || characteristicList.Count == 0)
        {
            SetMessage("На текущем этапе нет записанных точек.", true);
            return;
        }

        characteristicList.RemoveAt(characteristicList.Count - 1);
        SetMessage("Последняя точка текущего этапа удалена.");
        RefreshLab3ChartTables();
    }

    public void ClearAllPoints()
    {
        resistancePoints.Clear();
        noLoadPoints.Clear();
        loadPoints.Clear();
        externalPoints.Clear();
        regulationPoints.Clear();
        shortCircuitPoints.Clear();
        ResetStageReferences();
        SetMessage("Все временные результаты Lab3 очищены.");
        RefreshLab3ChartTables();
    }

    public void ResetLab()
    {
        currentStage = Lab3Stage.Preparation;
        ClearAllPoints();
        q1Enabled = false;
        q2Enabled = false;
        q3Enabled = false;
        shortCircuitEnabled = false;
        r1Position = 35f;
        r2Position = 0f;
        ResetSyntheticValuesOnly();

        if (existingCircuit != null)
        {
            existingCircuit.ResetCircuit();
        }

        SetMessage("Lab3 сброшена в исходное состояние.");
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

    private bool TryValidateCurrentStage(out string error)
    {
        switch (currentStage)
        {
            case Lab3Stage.ResistanceMeasurement:
                error = string.Empty;
                return true;
            case Lab3Stage.NoLoadCharacteristic:
                if (!q1Enabled || !q3Enabled || q2Enabled || armatureCurrent > 0.1f)
                {
                    error = "для ХХ нужны Q1 on, Q3 on, Q2 off и Ia около 0.";
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
                if (loadReferenceIa >= 0f && Mathf.Abs(armatureCurrent - loadReferenceIa) > 0.35f)
                {
                    error = $"для нагрузочной характеристики Ia должен быть условно постоянен ({loadReferenceIa:F2} А).";
                    return false;
                }
                break;
            case Lab3Stage.ExternalCharacteristic:
                if (!ValidatePoweredLoadedMode(out error))
                {
                    return false;
                }
                if (externalReferenceIf >= 0f && Mathf.Abs(fieldCurrent - externalReferenceIf) > 0.08f)
                {
                    error = $"для внешней характеристики If должен быть условно постоянен ({externalReferenceIf:F2} А).";
                    return false;
                }
                break;
            case Lab3Stage.RegulationCharacteristic:
                if (!ValidatePoweredLoadedMode(out error))
                {
                    return false;
                }
                if (regulationReferenceU >= 0f && Mathf.Abs(voltage - regulationReferenceU) > 12f)
                {
                    error = $"для регулировочной характеристики U должен поддерживаться около {regulationReferenceU:F0} В.";
                    return false;
                }
                break;
            case Lab3Stage.ShortCircuitCharacteristic:
                if (!q1Enabled || !shortCircuitEnabled || voltage > 1f)
                {
                    error = "для КЗ нужны Q1 on, активный режим КЗ и U около 0.";
                    return false;
                }
                break;
            default:
                error = "на этом этапе запись точки не предусмотрена.";
                return false;
        }

        error = string.Empty;
        return true;
    }

    private bool ValidatePoweredLoadedMode(out string error)
    {
        if (!q1Enabled || !q2Enabled || !q3Enabled)
        {
            error = "нужны Q1 on, Q2 on и Q3 on.";
            return false;
        }

        if (shortCircuitEnabled)
        {
            error = "отключите режим короткого замыкания.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void RecordResistancePoint()
    {
        float testCurrent = Mathf.Max(0.2f, Mathf.Lerp(0.2f, 2.2f, r2Position / 100f));
        float testVoltage = testCurrent * Mathf.Lerp(8.5f, 12.5f, r1Position / 100f);
        resistancePoints.Add(new Lab3ResistancePoint
        {
            voltage = testVoltage,
            current = testCurrent,
            armatureResistance = testVoltage / testCurrent,
            hotArmatureResistance = ResistanceHotRa
        });
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
                ("Clear", ClearAllPoints),
                ("Reset", ResetLab));
            CreateButtonRow(panelObject.transform,
                ("Q1", ToggleQ1),
                ("Q2", ToggleQ2),
                ("Q3", ToggleQ3),
                ("SC", ToggleShortCircuitMode));
            CreateButtonRow(panelObject.transform,
                ("R1 -", DecreaseR1),
                ("R1 +", IncreaseR1),
                ("R2 -", DecreaseR2),
                ("R2 +", IncreaseR2));
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
