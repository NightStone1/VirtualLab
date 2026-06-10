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
    [SerializeField] private bool createRuntimeHud = true;

    [Header("State")]
    [SerializeField] private Lab6Stage currentStage = Lab6Stage.Preparation;
    [SerializeField] private bool q1Enabled;
    [SerializeField] private bool q3Enabled;
    [SerializeField] private bool q4Enabled;
    [SerializeField] private bool q5Enabled;
    [SerializeField] private bool q6Enabled;
    [SerializeField] private int q2Position;
    [SerializeField] private bool brakeEnabled;
    [SerializeField] private float loadPercent;

    private readonly List<Lab6Measurement> measurements = new List<Lab6Measurement>();
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
    public float LoadPercent => loadPercent;
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
        loadPercent = Mathf.Clamp(loadPercent, 0f, Mathf.Max(0f, data.maxLoadPercent));

        if (hudView == null)
        {
            hudView = FindAnyLab6HudView();
        }

        if (standView == null)
        {
            standView = FindAnyLab6StandView();
        }

        if (hudView == null && createRuntimeHud)
        {
            hudView = CreateRuntimeHud();
        }

        if (hudView != null)
        {
            hudView.SetController(this);
        }

        RefreshViews();
    }

    private void Update()
    {
        RefreshViews();
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
        brakeEnabled = !brakeEnabled;
        SetMessage($"Тормоз ротора {(brakeEnabled ? "включён" : "выключен")}." );
    }

    public void SetLoadPercent(float value)
    {
        EnsureData();
        loadPercent = Mathf.Clamp(value, 0f, Mathf.Max(0f, data.maxLoadPercent));
        SetMessage($"Нагрузка установлена: {loadPercent:F0}%." );
    }

    public void ChangeLoadPercent(float delta)
    {
        SetLoadPercent(loadPercent + delta);
    }

    public void RecordPoint()
    {
        if (!TryValidateCurrentStage(out string error))
        {
            SetMessage("Ошибка записи: " + error, true);
            return;
        }

        Lab6Measurement point = CreateMeasurementSnapshot();
        measurements.Add(point);
        SetMessage($"Точка записана: {GetRecordedPointCount(currentStage)}/{GetRequiredPoints(currentStage)} для текущего этапа." );
        Debug.Log("Lab6 point recorded: " + point);
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
                TryAdvance(Lab6Stage.NoLoad, Lab6Stage.ShortCircuit, "Начат опыт короткого замыкания. Снизьте Q2 и включите тормоз.");
                break;
            case Lab6Stage.ShortCircuit:
                TryAdvance(Lab6Stage.ShortCircuit, Lab6Stage.Load, "Начат опыт непосредственной нагрузки.");
                break;
            case Lab6Stage.Load:
                TryAdvance(Lab6Stage.Load, Lab6Stage.Completed, "Лабораторная завершена.");
                break;
            case Lab6Stage.Completed:
                SetMessage("Лабораторная уже завершена.");
                break;
        }
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
        loadPercent = 0f;
        SetMessage("Аварийный останов: питание, двигатель, нагрузка и тормоз выключены.", true);
    }

    public int GetRecordedPointCount(Lab6Stage stage)
    {
        int count = 0;
        for (int i = 0; i < measurements.Count; i++)
        {
            if (measurements[i].stage == stage)
            {
                count++;
            }
        }

        return count;
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
                if (q2Position > 3) { error = "для опыта КЗ напряжение слишком большое: поставьте Q2 не выше 3."; return false; }
                if (!brakeEnabled) { error = "для опыта КЗ ротор должен быть заторможен."; return false; }
                error = null;
                return true;
            case Lab6Stage.Load:
                if (!q1Enabled) { error = "для опыта нагрузки включите Q1."; return false; }
                if (!Q2Enabled) { error = "для опыта нагрузки задайте напряжение Q2."; return false; }
                if (!q3Enabled) { error = "для опыта нагрузки включите Q3."; return false; }
                if (!q4Enabled) { error = "для опыта нагрузки включите Q4."; return false; }
                if (!q5Enabled) { error = "для опыта нагрузки включите Q5."; return false; }
                if (!q6Enabled) { error = "для опыта нагрузки включите Q6."; return false; }
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
        float syncSpeed = GetSynchronousSpeed();
        float limitedPosition = Mathf.Clamp(q2Position, 0, 3) / 3f;
        result.cosPhi = 0.42f;
        result.current = GetNominalCurrent() * Mathf.Lerp(0.45f, 2.4f, limitedPosition);
        result.speed = brakeEnabled ? 0f : syncSpeed * 0.35f;
        result.powerInput = Mathf.Sqrt(3f) * result.voltage * result.current * result.cosPhi;
        result.powerOutput = 0f;
        result.torque = GetNominalTorque() * Mathf.Lerp(0.25f, 1.7f, limitedPosition);
        result.efficiency = 0f;
        result.slip = brakeEnabled ? 1f : Mathf.Clamp01((syncSpeed - result.speed) / syncSpeed);
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
        result.slip = Mathf.Lerp(0.015f, 0.07f, normalizedLoad);
        result.speed = brakeEnabled ? 0f : syncSpeed * (1f - result.slip);
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
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
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
            ("Load -", () => ChangeLoadPercent(-10f)),
            ("Load +", () => ChangeLoadPercent(10f)));
        CreateButtonRow(panelObject.transform,
            ("Record", (Action)RecordPoint),
            ("Next Stage", NextStage),
            ("Emergency Stop", EmergencyStop));

        view.BindRuntimeFields(title, stage, instruction, warning, switches, measurementsText, points);
        view.SetController(this);
        return view;
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

    private static void EnsureEventSystem()
    {
        if (FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
