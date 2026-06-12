using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lab1HudView : MonoBehaviour
{
    private const string Lab1SceneName = "Lab1";
    private const string Lab1ScenePath = "Assets/Scenes/Lab1.unity";
    private const float StartupVoltageMin = 20f;
    private const float StartupVoltageMax = 100f;
    private const float NominalVoltageMin = 200f;
    private const float NominalVoltageMax = 230f;
    private const float AccelerationRpmThreshold = 500f;
    private const float WorkingRpmMin = 1800f;
    private const float WorkingRpmMax = 2050f;
    private const int MinTable22Points = 5;
    private const int MinTable23Points = 5;
    private const int MinTable24Points = 5;
    private const int MinTable25Points = 5;

    [SerializeField] private ElectricCircuit circuit;
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private bool showRuntimeHud = true;

    [Header("Runtime UI")]
    [SerializeField] private GameObject runtimeHudObject;
    [SerializeField] private GameObject runtimeHudPanelObject;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI measurementsText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI resultText;

    private struct GuidanceInfo
    {
        public Lab1GuidanceStage stage;
        public string stageTitle;
        public string instruction;
        public string result;

        public GuidanceInfo(Lab1GuidanceStage stage, string stageTitle, string instruction, string result)
        {
            this.stage = stage;
            this.stageTitle = stageTitle;
            this.instruction = instruction;
            this.result = result;
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
    private static void BootstrapActiveLab1Scene()
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
        if (!IsLab1Scene(scene))
        {
            return;
        }

        if (RuntimeHudAlreadyExists())
        {
            return;
        }

        GameObject root = new GameObject("Lab1HudRoot");
        root.AddComponent<Lab1HudView>();
    }

    private static bool IsLab1Scene(Scene scene)
    {
        string scenePath = string.IsNullOrEmpty(scene.path) ? string.Empty : scene.path.Replace('\\', '/');
        return scene.name == Lab1SceneName || scenePath == Lab1ScenePath;
    }

    private static bool RuntimeHudAlreadyExists()
    {
        if (FindObjectsByType<Lab1HudView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
        {
            return true;
        }

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null)
            {
                continue;
            }

            if (target.name == "Lab1HudRoot" || target.name == "Lab1RuntimeHud")
            {
                return true;
            }
        }

        return false;
    }

    private void Awake()
    {
        ResolveReferences();

        if (runtimeHudObject == null)
        {
            CreateRuntimeHud();
        }

        SetRuntimeHudVisible(showRuntimeHud);
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            SetRuntimeHudVisible(!showRuntimeHud);
        }

        Refresh();
    }

    public void SetSources(ElectricCircuit circuitSource, LabResultsManager resultsSource)
    {
        circuit = circuitSource;
        resultsManager = resultsSource;
        Refresh();
    }

    private void ResolveReferences()
    {
        if (circuit == null)
        {
            ElectricCircuit[] circuits = FindObjectsByType<ElectricCircuit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            circuit = circuits.Length > 0 ? circuits[0] : null;
        }

        if (resultsManager == null)
        {
            LabResultsManager[] managers = FindObjectsByType<LabResultsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            resultsManager = managers.Length > 0 ? managers[0] : null;
        }
    }

    private void Refresh()
    {
        ResolveReferences();

        GuidanceInfo guidance = BuildGuidanceInfo();
        LabMode mode = resultsManager != null ? resultsManager.CurrentMode : LabMode.None;

        SetText(titleText, "Лабораторная 1. Испытание двигателя постоянного тока параллельного возбуждения");
        SetText(stageText, Section("Этап", guidance.stageTitle));
        SetText(instructionText, Section("Инструкция", guidance.instruction));
        SetText(stateText, Section("Состояние", GetStateText(mode)));
        SetText(measurementsText, Section("Измерения", GetMeasurementsText()));
        SetText(progressText, Section("Прогресс", GetProgressText(mode)));
        SetText(resultText, Section("Результат", guidance.result));
    }

    // Порядок условий ниже является методическим приоритетом: HUD только подсказывает следующий шаг и не управляет стендом.
    private GuidanceInfo BuildGuidanceInfo()
    {
        if (circuit == null || resultsManager == null)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.WaitingForData,
                "Ожидание данных",
                "Ожидание инициализации лабораторной работы.",
                "HUD продолжит поиск объектов сцены автоматически.");
        }

        if (GetRowCount(LabMode.Table22_Working) >= MinTable22Points &&
            GetRowCount(LabMode.Table23_OmegaFromU) >= MinTable23Points &&
            GetRowCount(LabMode.Table24_IfFromIa) >= MinTable24Points &&
            GetRowCount(LabMode.Table25_OmegaFromIf) >= MinTable25Points)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.Completed,
                "Завершение",
                "Основные измерения выполнены. Проверьте таблицы и переходите к построению графиков.",
                "Все таблицы заполнены: 2.2, 2.3, 2.4, 2.5.");
        }

        bool q1 = IsSwitchOn(circuit.Q1);
        bool q2 = IsSwitchOn(circuit.Q2);
        float pv1 = circuit.PV1Value;
        float rpm = circuit.RPMValue;
        bool table22InProgress = resultsManager.CurrentMode == LabMode.Table22_Working && GetRowCount(LabMode.Table22_Working) > 0;

        if (!q1)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.Preparation,
                "Подготовка установки",
                "Включите Q1 для подачи питания на установку.",
                "Питание установки ещё не включено.");
        }

        if (!q2)
        {
            if (pv1 < StartupVoltageMin)
            {
                return new GuidanceInfo(
                    Lab1GuidanceStage.StartupPreparation,
                    "Подготовка к пуску",
                    "Установите РНО так, чтобы PV1 было в пусковом диапазоне: меньше 100 В, но больше 0 В.",
                    "Пусковое напряжение ещё не задано.");
            }

            if (pv1 > StartupVoltageMax)
            {
                return new GuidanceInfo(
                    Lab1GuidanceStage.StartupPreparation,
                    "Подготовка к пуску",
                    "Перед пуском уменьшите РНО: PV1 должно быть меньше 100 В.",
                    "Пусковое напряжение слишком большое.");
            }

            return new GuidanceInfo(
                Lab1GuidanceStage.ReadyToStart,
                "Пуск двигателя",
                "PV1 находится в пусковом диапазоне. Проверьте, что R1 = 0%: для текущей модели это максимальное возбуждение двигателя. Затем включите Q2.",
                "Пусковые условия выполнены — включите Q2.");
        }

        if (pv1 < StartupVoltageMin)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.StartupVoltageMissing,
                "Пусковое напряжение не задано",
                "Q2 включён, но напряжение на двигателе слишком мало. Установите РНО так, чтобы PV1 было в пусковом диапазоне.",
                "Двигатель не разгонится без напряжения.");
        }

        if (rpm < AccelerationRpmThreshold)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.Acceleration,
                "Разгон двигателя",
                "Дождитесь разгона двигателя. После появления устойчивого вращения плавно увеличивайте РНО.",
                "Ожидается устойчивое вращение двигателя.");
        }

        GuidanceInfo? completedCurrentTableGuidance = BuildCompletedCurrentTableGuidanceInfo();
        if (completedCurrentTableGuidance.HasValue)
        {
            return completedCurrentTableGuidance.Value;
        }

        switch (resultsManager.CurrentMode)
        {
            case LabMode.Table23_OmegaFromU:
                return BuildTable23GuidanceInfo();
            case LabMode.Table24_IfFromIa:
                return BuildTable24GuidanceInfo();
            case LabMode.Table25_OmegaFromIf:
                return BuildTable25GuidanceInfo();
        }

        if (!IsNominalVoltage(pv1))
        {
            string direction = pv1 < NominalVoltageMin ? "увеличивайте" : "уменьшайте";

            return new GuidanceInfo(
                Lab1GuidanceStage.NominalMode,
                "Вывод на номинальное напряжение",
                $"Плавно {direction} РНО, чтобы вывести PV1 в рабочий диапазон 200–230 В.",
                "Напряжение ещё не в рабочем диапазоне.");
        }

        if (table22InProgress)
        {
            return BuildTable22GuidanceInfo();
        }

        if (!IsWorkingSpeed(rpm))
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.SpeedAdjustment,
                "Первичная настройка скорости",
                "Проверьте РНО и R1. Перед первой точкой таблицы 2.2 рекомендуется R1 = 0%; для текущей модели рабочие обороты ожидаются около 1900 об/мин.",
                "Перед первой точкой нужно получить устойчивые рабочие обороты.");
        }

        switch (resultsManager.CurrentMode)
        {
            case LabMode.Table22_Working:
                return BuildTable22GuidanceInfo();

            default:
                return new GuidanceInfo(
                    Lab1GuidanceStage.ReadyForCharacteristics,
                    "Готово к снятию характеристик",
                    "Выберите таблицу 2.2 и начните с точки холостого хода.",
                    "Рабочий режим достигнут — можно переходить к измерениям.");
        }
    }

    private string GetStateText(LabMode mode)
    {
        string modeText = resultsManager != null ? GetModeName(mode) : "режим: н/д";

        if (circuit == null)
        {
            return "ElectricCircuit: н/д\n" + modeText;
        }

        return $"Q1={OnOff(IsSwitchOn(circuit.Q1))}, Q2={OnOff(IsSwitchOn(circuit.Q2))}, Q3={OnOff(IsSwitchOn(circuit.Q3))}\n" +
               $"Двигатель: {(circuit.EngineIsOn ? "разгон/работа" : "остановлен")}\n" +
               $"РНО={Format(circuit.PHOValue, "F0")} В, R1={Format(circuit.R1Percent, "F0")}%, R2={Format(circuit.R2Percent, "F0")}%, R3={Format(circuit.R3Percent, "F0")}%\n" +
               modeText;
    }

    private string GetMeasurementsText()
    {
        if (circuit == null)
        {
            return "PV1=н/д, PV2=н/д\nPA1=н/д, PA2=н/д, PA3=н/д, PA4=н/д\nRPM=н/д";
        }

        return $"PV1={Format(circuit.PV1Value, "F1")} В, PV2={Format(circuit.PV2Value, "F1")} В\n" +
               $"PA1={Format(circuit.PA1Value, "F2")} А, PA2={Format(circuit.PA2ValueMilliAmp, "F1")} мА\n" +
               $"PA3={Format(circuit.PA3ValueMilliAmp, "F1")} мА, PA4={Format(circuit.PA4Value, "F2")} А\n" +
               $"RPM={Format(circuit.RPMValue, "F0")} об/мин";
    }

    private string GetProgressText(LabMode mode)
    {
        if (resultsManager == null)
        {
            return "LabResultsManager: н/д\nТекущая таблица: н/д";
        }

        return $"Текущая таблица: {GetTableName(mode)}\n" +
               $"Точек в текущей таблице: {GetRowCount(mode)}\n" +
               $"2.2: {GetRowCount(LabMode.Table22_Working)}, 2.3: {GetRowCount(LabMode.Table23_OmegaFromU)}, " +
               $"2.4: {GetRowCount(LabMode.Table24_IfFromIa)}, 2.5: {GetRowCount(LabMode.Table25_OmegaFromIf)}";
    }

    private static string GetModeName(LabMode mode)
    {
        return "Режим: " + GetTableName(mode);
    }

    private static string GetTableName(LabMode mode)
    {
        switch (mode)
        {
            case LabMode.Table22_Working:
                return "Таблица 2.2 — рабочие характеристики";
            case LabMode.Table23_OmegaFromU:
                return "Таблица 2.3 — ω = f(U)";
            case LabMode.Table24_IfFromIa:
                return "Таблица 2.4 — If = f(Ia)";
            case LabMode.Table25_OmegaFromIf:
                return "Таблица 2.5 — ω = f(If)";
            default:
                return "н/д";
        }
    }

    private int GetRowCount(LabMode mode)
    {
        if (resultsManager == null)
        {
            return 0;
        }

        switch (mode)
        {
            case LabMode.Table22_Working:
                return resultsManager.Table22Rows != null ? resultsManager.Table22Rows.Count : 0;
            case LabMode.Table23_OmegaFromU:
                return resultsManager.Table23Rows != null ? resultsManager.Table23Rows.Count : 0;
            case LabMode.Table24_IfFromIa:
                return resultsManager.Table24Rows != null ? resultsManager.Table24Rows.Count : 0;
            case LabMode.Table25_OmegaFromIf:
                return resultsManager.Table25Rows != null ? resultsManager.Table25Rows.Count : 0;
            default:
                return 0;
        }
    }

    private bool HasRows(LabMode mode)
    {
        return GetRowCount(mode) > 0;
    }

    private GuidanceInfo? BuildCompletedCurrentTableGuidanceInfo()
    {
        if (resultsManager == null)
        {
            return null;
        }

        switch (resultsManager.CurrentMode)
        {
            case LabMode.Table22_Working:
                if (GetRowCount(LabMode.Table22_Working) >= MinTable22Points)
                {
                    return new GuidanceInfo(
                        Lab1GuidanceStage.WorkingCharacteristics,
                        "Таблица 2.2 заполнена",
                        "Рабочие характеристики сняты. Перейдите к таблице 2.3.",
                        "Таблица 2.2 заполнена: 5/5 точек. Следующий шаг — таблица 2.3.");
                }

                break;

            case LabMode.Table23_OmegaFromU:
                if (GetRowCount(LabMode.Table23_OmegaFromU) >= MinTable23Points)
                {
                    return new GuidanceInfo(
                        Lab1GuidanceStage.OmegaFromU,
                        "Таблица 2.3 заполнена",
                        "Регулировочная характеристика ω = f(U) снята. Перейдите к таблице 2.4.",
                        "Таблица 2.3 заполнена: 5/5 точек. Следующий шаг — таблица 2.4.");
                }

                break;

            case LabMode.Table24_IfFromIa:
                if (GetRowCount(LabMode.Table24_IfFromIa) >= MinTable24Points)
                {
                    return new GuidanceInfo(
                        Lab1GuidanceStage.IfFromIa,
                        "Таблица 2.4 заполнена",
                        "Регулировочная характеристика If = f(Ia) снята. Перейдите к таблице 2.5.",
                        "Таблица 2.4 заполнена: 5/5 точек. Следующий шаг — таблица 2.5.");
                }

                break;

            case LabMode.Table25_OmegaFromIf:
                if (GetRowCount(LabMode.Table25_OmegaFromIf) >= MinTable25Points)
                {
                    return new GuidanceInfo(
                        Lab1GuidanceStage.OmegaFromIf,
                        "Таблица 2.5 заполнена",
                        "Таблица 2.5 заполнена, но не все предыдущие таблицы имеют по 5 точек. Вернитесь к первой незаполненной таблице.",
                        "Следующий шаг — " + GetFirstIncompleteTableName() + ".");
                }

                break;
        }

        return null;
    }

    private string GetFirstIncompleteTableName()
    {
        if (GetRowCount(LabMode.Table22_Working) < MinTable22Points)
        {
            return "таблица 2.2";
        }

        if (GetRowCount(LabMode.Table23_OmegaFromU) < MinTable23Points)
        {
            return "таблица 2.3";
        }

        if (GetRowCount(LabMode.Table24_IfFromIa) < MinTable24Points)
        {
            return "таблица 2.4";
        }

        return "таблица 2.5";
    }

    private GuidanceInfo BuildTable23GuidanceInfo()
    {
        int pointCount = GetRowCount(LabMode.Table23_OmegaFromU);
        bool q3Enabled = circuit != null && IsSwitchOn(circuit.Q3);

        if (q3Enabled)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.OmegaFromU,
                pointCount == 0 ? "Таблица 2.3 — подготовка" : "Таблица 2.3 — изменение напряжения",
                "Для рекомендуемого режима таблицы 2.3 выключите Q3. Эта таблица снимается без нагрузки: R1 = 0%, R2 не изменяйте, меняется только PV1.",
                "Выключите Q3 и меняйте только РНО/PV1.");
        }

        if (pointCount == 0)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.OmegaFromU,
                "Таблица 2.3 — подготовка",
                "Для таблицы 2.3 выключите Q3, оставьте R1 = 0% и не изменяйте R2. Меняйте только РНО/PV1 и записывайте RPM как результат.",
                "Подготовьте первый режим по напряжению и запишите точку.");
        }

        return new GuidanceInfo(
            Lab1GuidanceStage.OmegaFromU,
            "Таблица 2.3 — изменение напряжения",
            "Измените РНО/PV1 на новое значение и запишите следующую точку. Q3 держите выключенным, R1 = 0%, R2 и R3 не изменяйте; RPM является результатом.",
            "Нужно получить 5 разных значений напряжения.");
    }

    private GuidanceInfo BuildTable24GuidanceInfo()
    {
        int pointCount = GetRowCount(LabMode.Table24_IfFromIa);
        bool q3Enabled = circuit != null && IsSwitchOn(circuit.Q3);

        if (!q3Enabled)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.IfFromIa,
                "Таблица 2.4 — нет нагрузки",
                "Для таблицы 2.4 нужна изменяемая нагрузка: включите Q3, задайте R3 ≈ 20–30% и начните с R1 = 100%. R2 не изменяйте.",
                "Q3 выключен — нагрузка не подключена.");
        }

        if (pointCount == 0)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.IfFromIa,
                "Таблица 2.4 — подготовка",
                "Установите PV1 ≈ 220 В, включите Q3, задайте R3 ≈ 20–30%, установите R1 = 100% и не изменяйте R2. После стабилизации запишите первую точку.",
                "Первая точка задаст ориентир скорости для этой таблицы.");
        }

        return new GuidanceInfo(
            Lab1GuidanceStage.IfFromIa,
            "Таблица 2.4 — удержание скорости",
            "Увеличьте R3, затем уменьшайте R1, чтобы вернуть RPM примерно к скорости первой точки. PV1 держите около 220 В, R2 не изменяйте. После стабилизации запишите точку.",
            "Меняется PA1, а скорость нужно удерживать примерно постоянной.");
    }

    private GuidanceInfo BuildTable25GuidanceInfo()
    {
        int pointCount = GetRowCount(LabMode.Table25_OmegaFromIf);
        bool q3Enabled = circuit != null && IsSwitchOn(circuit.Q3);

        if (q3Enabled)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.OmegaFromIf,
                pointCount == 0 ? "Таблица 2.5 — подготовка" : "Таблица 2.5 — изменение возбуждения",
                "Для рекомендуемого режима таблицы 2.5 выключите Q3. Эта характеристика снимается при постоянной минимальной нагрузке; меняется только R1.",
                "Выключите Q3 перед записью точек таблицы 2.5.");
        }

        if (pointCount == 0)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.OmegaFromIf,
                "Таблица 2.5 — подготовка",
                "Для таблицы 2.5 выключите Q3, держите PV1 примерно 220 В и не изменяйте R2. Меняйте только R1 и записывайте RPM как результат.",
                "Подготовьте первый режим возбуждения и запишите точку.");
        }

        return new GuidanceInfo(
            Lab1GuidanceStage.OmegaFromIf,
            "Таблица 2.5 — изменение возбуждения",
            "Измените R1 (0%, 25%, 50%, 75%, 100%), дождитесь стабилизации RPM и запишите точку. PV1 держите около 220 В, Q3 выключен, R2 и R3 не изменяйте.",
            "Нужно получить 5 разных значений тока возбуждения.");
    }

    private GuidanceInfo BuildTable22GuidanceInfo()
    {
        int pointCount = GetRowCount(LabMode.Table22_Working);
        bool q3Enabled = circuit != null && IsSwitchOn(circuit.Q3);

        if (pointCount == 0)
        {
            if (q3Enabled)
            {
                return new GuidanceInfo(
                    Lab1GuidanceStage.WorkingCharacteristics,
                    "Таблица 2.2 — холостой ход",
                    "Для первой точки снимите холостой ход: выключите Q3. Перед записью держите PV1 200–230 В, R1 = 0%, R2 не изменяйте.",
                    "Первая точка должна соответствовать холостому ходу; HUD не запрещает запись, только подсказывает методику.");
            }

            return new GuidanceInfo(
                Lab1GuidanceStage.WorkingCharacteristics,
                "Таблица 2.2 — холостой ход",
                "Запишите первую точку холостого хода: Q3 выключен, R1 = 0%, PV1 200–230 В. R2 не изменяйте; PV1 и PA2 должны быть устойчивыми.",
                "После точки холостого хода включите Q3 и снимайте нагрузочные точки.");
        }

        if (!q3Enabled)
        {
            return new GuidanceInfo(
                Lab1GuidanceStage.WorkingCharacteristics,
                "Таблица 2.2 — подключение нагрузки",
                "Включите Q3 и установите ненулевую нагрузку R3, например 20–25%. Не записывайте точку сразу при нулевой нагрузке, если режим почти не изменился.",
                "Нагрузка ещё не подключена — включите Q3 и задайте R3 ≈ 20–25%.");
        }

        return new GuidanceInfo(
            Lab1GuidanceStage.WorkingCharacteristics,
            "Таблица 2.2 — нагрузочные точки",
            "Увеличивайте R3 ступенями (примерно 40%, 60%, 80%) и записывайте точки. Падение RPM допустимо; старайтесь поддерживать PV1 и PA2 постоянными, R2 не изменяйте.",
            "После изменения R3 запишите следующую точку.");
    }

    private void CreateRuntimeHud()
    {
        GameObject canvasObject = new GameObject("Lab1RuntimeHud", typeof(Canvas), typeof(CanvasScaler));
        runtimeHudObject = canvasObject;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        runtimeHudPanelObject = panelObject;
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(12f, -12f);
        panelRect.sizeDelta = new Vector2(620f, 0f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        panelImage.raycastTarget = false;

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = panelObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateHudText(panelObject.transform, "Title", 24f, FontStyles.Bold);
        stageText = CreateHudText(panelObject.transform, "Stage", 18f, FontStyles.Normal);
        instructionText = CreateHudText(panelObject.transform, "Instruction", 16f, FontStyles.Normal);
        stateText = CreateHudText(panelObject.transform, "State", 15f, FontStyles.Normal);
        measurementsText = CreateHudText(panelObject.transform, "Measurements", 15f, FontStyles.Normal);
        progressText = CreateHudText(panelObject.transform, "Progress", 15f, FontStyles.Normal);
        resultText = CreateHudText(panelObject.transform, "Result", 15f, FontStyles.Bold);
        resultText.color = new Color(1f, 0.82f, 0.32f, 1f);
        hintText = CreateRuntimeHudHint(canvasObject.transform);

        ConfigureRuntimeHudRaycasts(canvasObject);
    }

    private void SetRuntimeHudVisible(bool visible)
    {
        showRuntimeHud = visible;

        if (runtimeHudPanelObject != null)
        {
            runtimeHudPanelObject.SetActive(showRuntimeHud);
        }

        if (hintText != null)
        {
            hintText.text = showRuntimeHud ? "H — скрыть HUD" : "H — включить HUD";
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
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        LayoutElement layout = textObject.GetComponent<LayoutElement>();
        layout.minHeight = fontSize + 6f;

        return text;
    }

    private static TextMeshProUGUI CreateRuntimeHudHint(Transform parent)
    {
        GameObject textObject = new GameObject("HudHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f, -8f);
        rect.sizeDelta = new Vector2(260f, 32f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "H — включить HUD";
        text.fontSize = 16f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private static void ConfigureRuntimeHudRaycasts(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
            {
                graphics[i].raycastTarget = false;
            }
        }
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target == null)
        {
            return;
        }

        target.text = string.IsNullOrEmpty(value) ? "н/д" : value;
        target.gameObject.SetActive(true);
    }

    private static string Section(string title, string body)
    {
        return $"<b>{title}</b>\n{(string.IsNullOrEmpty(body) ? "н/д" : body)}";
    }

    private static bool IsSwitchOn(Switch target)
    {
        return target != null && target.isOn;
    }

    private static bool IsNominalVoltage(float voltage)
    {
        return voltage >= NominalVoltageMin && voltage <= NominalVoltageMax;
    }

    private static bool IsWorkingSpeed(float rpm)
    {
        return rpm >= WorkingRpmMin && rpm <= WorkingRpmMax;
    }

    private static string OnOff(bool value)
    {
        return value ? "вкл" : "выкл";
    }

    private static string Format(float value, string format)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? "н/д" : value.ToString(format);
    }
}
