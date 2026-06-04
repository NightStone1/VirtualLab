using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SyncGeneratorLabController : MonoBehaviour
{
    [SerializeField] private SynchronizationMethod synchronizationMethod = SynchronizationMethod.NeedleSynchronoscope;
    [SerializeField] private NeedleSynchronoscopeView needleSynchronoscopeView;
    [SerializeField] private LampSynchronoscopeView lampSynchronoscopeView;
    [SerializeField] private AnalogMeterView voltageMeterView;
    [SerializeField] private AnalogMeterView frequencyMeterView;
    [SerializeField] private AnalogMeterView statorCurrentMeterView;
    [SerializeField] private float speedStepRpm = 30f;
    [SerializeField] private float excitationStep = 0.1f;
    [SerializeField] private float loadStep = 0.1f;

    private readonly SyncGeneratorModel model = new SyncGeneratorModel();
    private readonly List<UCurvePoint> uCurvePoints = new List<UCurvePoint>();
    private SyncGeneratorStage currentStage = SyncGeneratorStage.Intro;
    private SyncGeneratorHud hud;
    private string lastMessage = "Нажмите Enter, чтобы начать.";

    private void Awake()
    {
        model.Reset();
        CreateRuntimeHud();
    }

    private void Update()
    {
        HandleInput();
        model.Update(Time.deltaTime);
        UpdateViews();
        UpdateHud();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetLab();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmCurrentStage();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TogglePower();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TogglePrimeMover();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            model.IncreaseSpeed(speedStepRpm);
            lastMessage = string.Format("Скорость увеличена до {0:0} об/мин.", model.rotorSpeedRpm);
            AutoAdvanceByState();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            model.DecreaseSpeed(speedStepRpm);
            lastMessage = string.Format("Скорость уменьшена до {0:0} об/мин.", model.rotorSpeedRpm);
            AutoAdvanceByState();
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            model.IncreaseExcitation(excitationStep);
            lastMessage = string.Format("Возбуждение увеличено до {0:0.00}.", model.excitationCurrent);
            AutoAdvanceByState();
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            model.DecreaseExcitation(excitationStep);
            lastMessage = string.Format("Возбуждение уменьшено до {0:0.00}.", model.excitationCurrent);
            AutoAdvanceByState();
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            ChangeLoad(loadStep);
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ChangeLoad(-loadStep);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            RecordUCurvePoint();
        }
    }

    private void ConfirmCurrentStage()
    {
        switch (currentStage)
        {
            case SyncGeneratorStage.Intro:
                currentStage = SyncGeneratorStage.PowerOn;
                lastMessage = "Включите питание клавишей 1.";
                break;

            case SyncGeneratorStage.PowerOn:
                if (model.isPowered)
                {
                    currentStage = SyncGeneratorStage.PrimeMoverStart;
                    lastMessage = "Запустите приводной двигатель клавишей 2.";
                }
                else
                {
                    lastMessage = "Питание отключено. Сначала нажмите 1.";
                }
                break;

            case SyncGeneratorStage.PrimeMoverStart:
                if (model.isPrimeMoverRunning)
                {
                    currentStage = SyncGeneratorStage.FrequencyAdjustment;
                    lastMessage = "Настройте частоту до 50 Гц клавишами 3 и 4.";
                }
                else
                {
                    lastMessage = "Приводной двигатель остановлен. Сначала нажмите 2.";
                }
                break;

            case SyncGeneratorStage.FrequencyAdjustment:
                if (IsFrequencyMatched())
                {
                    currentStage = SyncGeneratorStage.VoltageAdjustment;
                    lastMessage = "Настройте напряжение генератора до 380 В клавишами 5 и 6.";
                }
                else
                {
                    lastMessage = "Частота должна отличаться от сети не более чем на 0.5 Гц.";
                }
                break;

            case SyncGeneratorStage.VoltageAdjustment:
                if (IsVoltageMatched())
                {
                    currentStage = SyncGeneratorStage.Synchronization;
                    lastMessage = "Дождитесь фазы около 0° и нажмите Enter для подключения.";
                }
                else
                {
                    lastMessage = "Напряжение генератора должно отличаться от сети не более чем на 20 В.";
                }
                break;

            case SyncGeneratorStage.Synchronization:
                TrySynchronize();
                break;

            case SyncGeneratorStage.ConnectedToGrid:
                currentStage = SyncGeneratorStage.LoadTransfer;
                lastMessage = "Изменяйте нагрузку клавишами 7 и 8.";
                break;

            case SyncGeneratorStage.LoadTransfer:
                if (model.loadPower > 0f)
                {
                    currentStage = SyncGeneratorStage.UCurveMeasurement;
                    lastMessage = "Изменяйте возбуждение/нагрузку и записывайте точки U-кривой клавишей 9.";
                }
                else
                {
                    lastMessage = "Сначала увеличьте нагрузку клавишей 7.";
                }
                break;

            case SyncGeneratorStage.UCurveMeasurement:
                if (uCurvePoints.Count > 0)
                {
                    currentStage = SyncGeneratorStage.Completed;
                    lastMessage = "Сценарий MVP завершён. Нажмите R для сброса.";
                }
                else
                {
                    lastMessage = "Запишите хотя бы одну точку U-кривой клавишей 9.";
                }
                break;

            case SyncGeneratorStage.Completed:
                lastMessage = "Сценарий MVP завершён. Нажмите R для сброса.";
                break;

            case SyncGeneratorStage.Fault:
                lastMessage = "Аварийное состояние. Нажмите R для сброса.";
                break;
        }
    }

    private void TogglePower()
    {
        model.SetPower(!model.isPowered);
        lastMessage = model.isPowered ? "Питание включено." : "Питание отключено.";

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
            lastMessage = "Приводной двигатель остановлен.";
            currentStage = SyncGeneratorStage.PrimeMoverStart;
            return;
        }

        model.StartPrimeMover();
        lastMessage = model.isPrimeMoverRunning
            ? "Приводной двигатель запущен."
            : "Приводной двигатель нельзя запустить: питание отключено или генератор уже подключён.";
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
        lastMessage = string.Format("Нагрузка установлена: {0:0}%.", model.loadPower * 100f);
        AutoAdvanceByState();
    }

    private void TrySynchronize()
    {
        if (model.TryConnectToGrid(out string message))
        {
            currentStage = SyncGeneratorStage.ConnectedToGrid;
        }

        lastMessage = message;
    }

    private void RecordUCurvePoint()
    {
        if (!model.isConnectedToGrid)
        {
            lastMessage = "Подключите генератор к сети перед записью точек U-кривой.";
            return;
        }

        UCurvePoint point = model.CalculateUCurvePoint(model.excitationCurrent, model.loadPower);
        uCurvePoints.Add(point);

        if (currentStage == SyncGeneratorStage.LoadTransfer)
        {
            currentStage = SyncGeneratorStage.UCurveMeasurement;
        }

        lastMessage = string.Format(
            "Точка U-кривой записана: If={0:0.00}, Iст={1:0.00}, cosφ={2}.",
            point.excitationCurrent,
            point.statorCurrent,
            FormatPowerFactor(point));
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

        if (currentStage == SyncGeneratorStage.ConnectedToGrid && model.loadPower > 0f)
        {
            currentStage = SyncGeneratorStage.LoadTransfer;
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

    private void ResetLab()
    {
        model.Reset();
        currentStage = SyncGeneratorStage.Intro;
        uCurvePoints.Clear();
        lastMessage = "Лабораторная сброшена. Нажмите Enter, чтобы начать.";
    }

    private void UpdateViews()
    {
        float deltaFrequency = model.generatorFrequency - model.gridFrequency;

        if (needleSynchronoscopeView != null)
        {
            needleSynchronoscopeView.SetPhaseDifference(model.phaseDifferenceDeg, deltaFrequency);
        }

        if (lampSynchronoscopeView != null)
        {
            lampSynchronoscopeView.SetPhaseDifference(model.phaseDifferenceDeg);
        }

        if (voltageMeterView != null)
        {
            voltageMeterView.SetValue(model.generatorVoltage);
        }

        if (frequencyMeterView != null)
        {
            frequencyMeterView.SetValue(model.generatorFrequency);
        }

        if (statorCurrentMeterView != null)
        {
            statorCurrentMeterView.SetValue(model.statorCurrent);
        }
    }

    private void UpdateHud()
    {
        if (hud == null)
        {
            return;
        }

        hud.SetText(BuildHudText());
    }

    private string BuildHudText()
    {
        StringBuilder builder = new StringBuilder(1400);
        float displayedPhase = Mathf.Repeat(model.phaseDifferenceDeg, 360f);

        builder.AppendLine("Параллельная работа синхронного генератора (MVP)");
        builder.AppendLine("Этап: " + GetStageDisplayName());
        builder.AppendLine("Подсказка: " + GetStageHint());
        builder.AppendLine();
        builder.AppendLine("Клавиши: Enter подтвердить/подключить | 1 питание | 2 привод | 3/4 скорость | 5/6 возбуждение | 7/8 нагрузка | 9 записать точку U-кривой | R сброс");
        builder.AppendLine();
        builder.AppendLine("Питание: " + FormatOnOff(model.isPowered));
        builder.AppendLine("Приводной двигатель: " + (model.isPrimeMoverRunning ? "запущен" : "остановлен"));
        builder.AppendLine("Подключение к сети: " + (model.isConnectedToGrid ? "подключен" : "не подключен"));
        builder.AppendLine(string.Format("Напряжение сети: {0:0.0} В", model.gridVoltage));
        builder.AppendLine(string.Format("Напряжение генератора: {0:0.0} В", model.generatorVoltage));
        builder.AppendLine(string.Format("Частота сети: {0:0.00} Гц", model.gridFrequency));
        builder.AppendLine(string.Format("Частота генератора: {0:0.00} Гц", model.generatorFrequency));
        builder.AppendLine(string.Format("Разность фаз: {0:0.0}°", displayedPhase));
        builder.AppendLine("Окно синхронизации: " + (IsPhaseMatched(displayedPhase) ? "да" : "нет"));
        builder.AppendLine("Относительная скорость: " + GetFrequencyDirectionText());
        builder.AppendLine(string.Format("Ток возбуждения If: {0:0.00} отн. ед.", model.excitationCurrent));
        builder.AppendLine(string.Format("Нагрузка: {0:0}%", model.loadPower * 100f));
        builder.AppendLine(string.Format("Ток статора Iст: {0:0.00}", model.statorCurrent));
        builder.AppendLine(string.Format("cos φ: {0:0.00}", model.powerFactor));
        builder.AppendLine("Метод синхронизации: " + GetSynchronizationMethodDisplayName());
        builder.AppendLine();
        builder.AppendLine("Сообщение: " + lastMessage);

        if (model.hasFault)
        {
            builder.AppendLine("Ошибка: " + model.faultReason);
        }

        builder.AppendLine();
        builder.AppendLine("Точки U-кривой, последние 5:");

        int startIndex = Mathf.Max(0, uCurvePoints.Count - 5);
        if (uCurvePoints.Count == 0)
        {
            builder.AppendLine("нет");
        }

        for (int i = startIndex; i < uCurvePoints.Count; i++)
        {
            UCurvePoint point = uCurvePoints[i];
            builder.AppendLine(string.Format(
                "{0}. If={1:0.00}; Iст={2:0.00} А; Iакт={3:0.00} А; Iреакт={4:0.00} А; cosφ={5}; P={6:0}%",
                i + 1,
                point.excitationCurrent,
                point.statorCurrent,
                point.activeCurrent,
                point.reactiveCurrent,
                FormatPowerFactor(point),
                point.loadPower * 100f));
        }

        return builder.ToString();
    }

    private string GetStageHint()
    {
        switch (currentStage)
        {
            case SyncGeneratorStage.Intro:
                return "Нажмите Enter, чтобы начать MVP-сценарий.";
            case SyncGeneratorStage.PowerOn:
                return "Нажмите 1, чтобы включить питание.";
            case SyncGeneratorStage.PrimeMoverStart:
                return "Нажмите 2, чтобы запустить приводной двигатель.";
            case SyncGeneratorStage.FrequencyAdjustment:
                return "Клавишами 3/4 установите частоту генератора 49.5-50.5 Гц.";
            case SyncGeneratorStage.VoltageAdjustment:
                return "Клавишами 5/6 установите напряжение генератора 360-400 В.";
            case SyncGeneratorStage.Synchronization:
                return "Когда разность фаз около 0° или 360°, нажмите Enter для подключения.";
            case SyncGeneratorStage.ConnectedToGrid:
                return "Генератор подключен. Нажмите Enter и переходите к нагрузке.";
            case SyncGeneratorStage.LoadTransfer:
                return "Клавишами 7/8 изменяйте нагрузку, затем нажмите Enter для измерения U-кривой.";
            case SyncGeneratorStage.UCurveMeasurement:
                return "Используйте 5/6 и 7/8, нажимайте 9 для записи точек, Enter для завершения.";
            case SyncGeneratorStage.Completed:
                return "Сценарий MVP завершён. Нажмите R для сброса.";
            case SyncGeneratorStage.Fault:
                return "Аварийное состояние. Нажмите R для сброса.";
            default:
                return string.Empty;
        }
    }

    private string FormatOnOff(bool value)
    {
        return value ? "включено" : "отключено";
    }

    private string FormatPowerFactor(UCurvePoint point)
    {
        if (point.loadPower <= 0.001f || point.activeCurrent <= 0.001f)
        {
            return "—";
        }

        return string.Format("{0:0.00}", point.powerFactor);
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

    private string GetSynchronizationMethodDisplayName()
    {
        switch (synchronizationMethod)
        {
            case SynchronizationMethod.NeedleSynchronoscope:
                return "стрелочный синхроноскоп";
            case SynchronizationMethod.LampSynchronoscope:
                return "ламповый синхроноскоп";
            default:
                return synchronizationMethod.ToString();
        }
    }

    private bool IsPhaseMatched(float phaseDeg)
    {
        return phaseDeg <= 10f || phaseDeg >= 350f;
    }

    private string GetFrequencyDirectionText()
    {
        float deltaFrequency = model.generatorFrequency - model.gridFrequency;
        if (Mathf.Abs(deltaFrequency) < 0.01f)
        {
            return "частоты равны";
        }

        return deltaFrequency > 0f ? "генератор быстрее" : "генератор медленнее";
    }

    private void CreateRuntimeHud()
    {
        GameObject canvasObject = new GameObject("SyncGeneratorRuntimeHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject textObject = new GameObject("HudText", typeof(RectTransform), typeof(Text), typeof(SyncGeneratorHud));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(16f, -16f);
        rectTransform.sizeDelta = new Vector2(920f, 980f);

        Text text = textObject.GetComponent<Text>();
        text.font = GetRuntimeFont();
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        hud = textObject.GetComponent<SyncGeneratorHud>();
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
