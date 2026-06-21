using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab5SyncGeneratorLabController : MonoBehaviour
{
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
    public bool showRuntimeHud;

    private Lab5SyncGeneratorStage currentStage = Lab5SyncGeneratorStage.Intro;
    private GameObject runtimeHudObject;
    private Lab5SyncGeneratorHud hud;
    private string lastMessage = "Начните работу с органов управления стенда.";

    public Lab5SyncGeneratorStage CurrentStage => currentStage;
    public string StageDisplayName => GetStageDisplayName();
    public string StageHint => GetStageHint();
    public string LastMessage => lastMessage;

    private void Awake()
    {
        if (model == null && autoFindModel)
            model = FindFirstObjectByType<Lab5SyncGeneratorModel>();

        if (tvInfoText == null)
        {
            foreach (var t in FindObjectsOfType<TMP_Text>())
            {
                string tn = t.gameObject.name.ToLower();
                string tp = t.transform.parent != null ? t.transform.parent.name.ToLower() : "";
                if (tn.Contains("info") || tn.Contains("tv") || tp.Contains("tv") || tp.Contains("info"))
                { tvInfoText = t; break; }
            }
        }

        CreateRuntimeHud();
    }

    private void Update()
    {
        if (model == null) return;
        HandleInput();
        RefreshLabState(false);
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.H))
            showRuntimeHud = !showRuntimeHud;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            ConfirmCurrentStage();
    }

    // ============================================================
    //  ЭТАПЫ РАБОТЫ (по методичке ЛР №5)
    // ============================================================
    public void ConfirmCurrentStage()
    {
        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.Intro:
                currentStage = Lab5SyncGeneratorStage.PowerOn;
                lastMessage = "Включите KM1 для подачи питания на приводной двигатель.";
                break;

            case Lab5SyncGeneratorStage.PowerOn:
                if (model != null && model.IsMainPowerOn)
                {
                    currentStage = Lab5SyncGeneratorStage.PrimeMoverStart;
                    lastMessage = "Отрегулируйте LLR для запуска приводного двигателя. Контролируйте частоту по PF1.";
                }
                else
                {
                    lastMessage = "Сначала включите KM1 (контактор) для подачи питания.";
                }
                break;

            case Lab5SyncGeneratorStage.PrimeMoverStart:
                if (model != null && model.isPrimeMoverRunning)
                {
                    currentStage = Lab5SyncGeneratorStage.NoLoadTest;
                    lastMessage = "Двигатель запущен. Включите Q1 (возбуждение), установите R2 и снимайте ХХХ (табл. 2).";
                }
                else
                {
                    lastMessage = "Сначала запустите двигатель (KM1 + LLR).";
                }
                break;

            case Lab5SyncGeneratorStage.NoLoadTest:
                if (model != null && model.noLoadAscending.Count + model.noLoadDescending.Count > 0)
                {
                    currentStage = Lab5SyncGeneratorStage.InductiveLoadTest;
                    lastMessage = "ХХХ снята. Включите Q2, установите R3 (индуктивную нагрузку) и снимайте индукционную нагрузочную характеристику (табл. 3).";
                }
                else
                {
                    lastMessage = "Запишите хотя бы одну точку ХХХ кнопкой «Записать» (табл. 2). Q2 должен быть выключен.";
                }
                break;

            case Lab5SyncGeneratorStage.InductiveLoadTest:
                if (model != null && model.inductiveLoadData.Count > 0)
                {
                    currentStage = Lab5SyncGeneratorStage.ExternalTest;
                    lastMessage = "Индукционная нагрузочная характеристика снята. Установите R3=0% и снимайте внешнюю характеристику U=f(Ia) при cosφ=1 (табл. 4).";
                }
                else
                {
                    lastMessage = "Запишите хотя бы одну точку индукционной нагрузочной характеристики (табл. 3). Q2 и Q1 должны быть включены, R3 > 0%.";
                }
                break;

            case Lab5SyncGeneratorStage.ExternalTest:
                if (model != null && model.externalData.Count > 0)
                {
                    currentStage = Lab5SyncGeneratorStage.RegulatingTest;
                    lastMessage = "Внешняя характеристика снята. Снимайте регулировочную характеристику Iв=f(Ia) при U=const (табл. 5).";
                }
                else
                {
                    lastMessage = "Запишите хотя бы одну точку внешней характеристики (табл. 4). Изменяйте R1 и записывайте точки.";
                }
                break;

            case Lab5SyncGeneratorStage.RegulatingTest:
                if (model != null && model.regulatingData.Count > 0)
                {
                    currentStage = Lab5SyncGeneratorStage.ShortCircuitTest;
                    lastMessage = "Регулировочная характеристика снята. Включите режим КЗ и снимайте Ik=f(Iв) для трёхфазного и двухфазного КЗ (табл. 6).";
                }
                else
                {
                    lastMessage = "Запишите хотя бы одну точку регулировочной характеристики (табл. 5).";
                }
                break;

            case Lab5SyncGeneratorStage.ShortCircuitTest:
                if ((model != null && model.shortCircuitData.Count > 0) ||
                    (model != null && model.shortCircuit2PhaseData.Count > 0))
                {
                    currentStage = Lab5SyncGeneratorStage.ReactiveTriangle;
                    lastMessage = "Характеристики КЗ сняты. Постройте реактивный треугольник и диаграмму ЭДС на планшете.";
                }
                else
                {
                    lastMessage = "Запишите хотя бы одну точку КЗ (табл. 6). Используйте режимы трёхфазного и двухфазного КЗ.";
                }
                break;

            case Lab5SyncGeneratorStage.ReactiveTriangle:
                if (model != null && model.noLoadAscending.Count > 0)
                {
                    currentStage = Lab5SyncGeneratorStage.Completed;
                    lastMessage = "Лабораторная работа выполнена. Для повторного сброса используйте кнопку «Сброс».";
                }
                else
                {
                    lastMessage = "Выполните расчёт на планшете, используя кнопку «Реактивный треугольник».";
                }
                break;

            case Lab5SyncGeneratorStage.Completed:
                lastMessage = "Лабораторная работа завершена. Для повторного выполнения используйте кнопку «Сброс».";
                break;

            case Lab5SyncGeneratorStage.Fault:
                lastMessage = "Аварийное состояние. Используйте кнопку «Сброс».";
                break;
        }
    }

    private void AutoAdvanceByState()
    {
        if (currentStage == Lab5SyncGeneratorStage.Intro && model != null && model.IsMainPowerOn)
        {
            currentStage = Lab5SyncGeneratorStage.PowerOn;
            return;
        }
        if (currentStage == Lab5SyncGeneratorStage.PowerOn && model != null && model.isPrimeMoverRunning)
        {
            currentStage = Lab5SyncGeneratorStage.PrimeMoverStart;
            return;
        }
    }

    // ============================================================
    //  УПРАВЛЕНИЕ ПЕРЕКЛЮЧАТЕЛЯМИ
    // ============================================================
    public void ToggleSB1()
    {
        if (model == null || model.SB1 == null) return;
        model.SB1.isOn = !model.SB1.isOn;
        lastMessage = model.SB1.isOn ? "SB1 нажат: стоп." : "SB1 отпущен.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void ToggleSB2()
    {
        if (model == null || model.SB2 == null) return;
        model.SB2.isOn = !model.SB2.isOn;
        lastMessage = model.SB2.isOn ? "SB2 нажат: пуск." : "SB2 отпущен.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void ToggleKM1()
    {
        if (model == null || model.KM1 == null) return;
        model.KM1.isOn = !model.KM1.isOn;
        lastMessage = model.KM1.isOn
            ? "KM1 включён: питание двигателя подано."
            : "KM1 выключен: двигатель остановлен.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void ToggleQ1()
    {
        if (model == null || model.Q1 == null) return;
        model.Q1.isOn = !model.Q1.isOn;
        lastMessage = model.Q1.isOn
            ? "Q1 включён: возбуждение подано."
            : "Q1 выключен: возбуждение отключено.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void ToggleQ2()
    {
        if (model == null || model.Q2 == null) return;
        model.Q2.isOn = !model.Q2.isOn;
        lastMessage = model.Q2.isOn
            ? "Q2 включён: нагрузка подключена."
            : "Q2 выключен: нагрузка отключена.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    // ============================================================
    //  УПРАВЛЕНИЕ РЕГУЛЯТОРАМИ (шаговое)
    // ============================================================
    public void IncreaseR1()
    {
        if (model == null || model.R1 == null) return;
        float step = Mathf.Max(1f, 100f / 20f);
        model.R1.SetPercent(Mathf.Clamp(model.R1.Percent + step, 0f, 100f));
        lastMessage = $"R1 увеличен: {model.R1.Percent:F0}%.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void DecreaseR1()
    {
        if (model == null || model.R1 == null) return;
        float step = Mathf.Max(1f, 100f / 20f);
        model.R1.SetPercent(Mathf.Clamp(model.R1.Percent - step, 0f, 100f));
        lastMessage = $"R1 уменьшен: {model.R1.Percent:F0}%.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void IncreaseR2()
    {
        if (model == null || model.R2 == null) return;
        float step = Mathf.Max(1f, 100f / 20f);
        model.R2.SetPercent(Mathf.Clamp(model.R2.Percent + step, 0f, 100f));
        lastMessage = $"R2 увеличен: {model.R2.Percent:F0}%.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void DecreaseR2()
    {
        if (model == null || model.R2 == null) return;
        float step = Mathf.Max(1f, 100f / 20f);
        model.R2.SetPercent(Mathf.Clamp(model.R2.Percent - step, 0f, 100f));
        lastMessage = $"R2 уменьшен: {model.R2.Percent:F0}%.";
        AutoAdvanceByState();
        RefreshLabState();
    }

    public void IncreaseLLR()
    {
        if (model == null || model.LLR == null) return;
        float newVal = Mathf.Clamp(model.LLR.llrValue + 5f, 0f, 250f);
        model.LLR.SetNormalizedValue(newVal / 250f, raiseEvent: true);
        lastMessage = $"LLR увеличен: {model.LLR.llrValue:F0}.";
        AutoAdvanceByState();
        RefreshLabState(false);
    }

    public void DecreaseLLR()
    {
        if (model == null || model.LLR == null) return;
        float newVal = Mathf.Clamp(model.LLR.llrValue - 5f, 0f, 250f);
        model.LLR.SetNormalizedValue(newVal / 250f, raiseEvent: true);
        lastMessage = $"LLR уменьшен: {model.LLR.llrValue:F0}.";
        AutoAdvanceByState();
        RefreshLabState(false);
    }

    public void IncreaseR3()
    {
        if (model == null || model.R3 == null) return;
        float newVal = Mathf.Clamp(model.R3.value + 5f, 0f, 100f);
        model.R3.SetNormalizedValue(newVal / 100f, raiseEvent: true);
        lastMessage = $"R3 (индуктивная нагрузка): {model.R3.value:F0}%.";
        AutoAdvanceByState();
        RefreshLabState(false);
    }

    public void DecreaseR3()
    {
        if (model == null || model.R3 == null) return;
        float newVal = Mathf.Clamp(model.R3.value - 5f, 0f, 100f);
        model.R3.SetNormalizedValue(newVal / 100f, raiseEvent: true);
        lastMessage = $"R3 (индуктивная нагрузка): {model.R3.value:F0}%.";
        AutoAdvanceByState();
        RefreshLabState(false);
    }

    // ============================================================
    //  ЗАПИСЬ ХАРАКТЕРИСТИК
    // ============================================================
    public void RecordNoLoadPoint()
    {
        if (model == null) return;
        model.RecordNoLoadPoint();
        lastMessage = "Точка ХХХ записана.";
        RefreshLabState();
    }

    public void RecordInductiveLoadPoint()
    {
        if (model == null) return;
        model.RecordInductiveLoadPoint();
        lastMessage = "Точка индукционной нагрузочной характеристики записана.";
        RefreshLabState();
    }

    public void RecordExternalPoint()
    {
        if (model == null) return;
        model.RecordExternalPoint();
        lastMessage = "Точка внешней характеристики записана.";
        RefreshLabState();
    }

    public void RecordRegulatingPoint()
    {
        if (model == null) return;
        model.RecordRegulatingPoint();
        lastMessage = "Точка регулировочной характеристики записана.";
        RefreshLabState();
    }

    public void RecordShortCircuitPoint()
    {
        if (model == null) return;
        model.RecordShortCircuitPoint();
        lastMessage = "Точка трёхфазного КЗ записана.";
        RefreshLabState();
    }

    public void RecordShortCircuit2PhasePoint()
    {
        if (model == null) return;
        model.RecordShortCircuit2PhasePoint();
        lastMessage = "Точка двухфазного КЗ записана.";
        RefreshLabState();
    }

    public void EnableShortCircuitMode()
    {
        if (model == null) return;
        model.EnableShortCircuitMode();
        lastMessage = "Режим трёхфазного КЗ активирован.";
        RefreshLabState();
    }

    public void DisableShortCircuitMode()
    {
        if (model == null) return;
        model.DisableShortCircuitMode();
        lastMessage = "Режим КЗ отключён.";
        RefreshLabState();
    }

    public void EnableShortCircuit2PhaseMode()
    {
        if (model == null) return;
        model.EnableShortCircuit2PhaseMode();
        lastMessage = "Режим двухфазного КЗ активирован.";
        RefreshLabState();
    }

    public void DisableShortCircuit2PhaseMode()
    {
        if (model == null) return;
        model.DisableShortCircuit2PhaseMode();
        lastMessage = "Режим двухфазного КЗ отключён.";
        RefreshLabState();
    }

    public void ClearAllCharacteristicData()
    {
        if (model == null) return;
        model.ClearAllCharacteristicData();
        lastMessage = "Все данные характеристик очищены.";
        RefreshLabState();
    }

    // ============================================================
    //  СБРОС
    // ============================================================
    public void ResetLab()
    {
        if (model != null) model.ResetCircuit();
        currentStage = Lab5SyncGeneratorStage.Intro;
        lastMessage = "Лабораторная работа сброшена. Начните с включения KM1.";
        RefreshLabState();
    }

    // ============================================================
    //  ОБНОВЛЕНИЕ СТРЕЛОЧНЫХ ПРИБОРОВ
    // ============================================================
    private Meter ResolveMeter(Meter local, Meter modelFallback)
    {
        return local != null ? local : modelFallback;
    }

    private void UpdateMeters()
    {
        if (model == null) return;

        bool km1On = model.IsMainPowerOn;
        bool genActive = model.isPrimeMoverRunning && km1On;
        bool q1On = model.IsExcitationOn;

        Meter m;

        m = ResolveMeter(PA1_MotorCurrent, model.PA1_MotorCurrent);
        if (m != null) m.current = km1On ? model.MotorCurrent : 0f;

        m = ResolveMeter(PV1_GeneratorVoltage, model.PV1_GeneratorVoltage);
        if (m != null) m.current = genActive ? model.GeneratorVoltage : 0f;

        m = ResolveMeter(PF1_Frequency, model.PF1_Frequency);
        if (m != null) m.current = genActive ? model.GeneratorFrequency : 0f;

        m = ResolveMeter(PA2_PhaseA, model.PA2_PhaseA);
        if (m != null) m.current = genActive ? model.PhaseACurrent : 0f;

        m = ResolveMeter(PA3_PhaseB, model.PA3_PhaseB);
        if (m != null) m.current = genActive ? model.PhaseBCurrent : 0f;

        m = ResolveMeter(PA4_PhaseC, model.PA4_PhaseC);
        if (m != null) m.current = genActive ? model.PhaseCCurrent : 0f;

        m = ResolveMeter(PA5_ExcitationCurrent, model.PA5_ExcitationCurrent);
        if (m != null) m.current = q1On ? model.ExcitationCurrentAmps : 0f;
    }

    // ============================================================
    //  ОБНОВЛЕНИЕ ИНФОРМАЦИОННОГО ДИСПЛЕЯ
    // ============================================================
    private void UpdateInfoText()
    {
        if (model == null) return;

        // Цифровой дисплей частоты (pf1_Display)
        if (pf1_Display != null)
        {
            bool genActive = model.isPrimeMoverRunning && model.IsMainPowerOn;
            pf1_Display.text = genActive
                ? $"{model.generatorFrequency:F2} Гц"
                : "---";
        }

        if (tvInfoText == null) return;

        string km1State = model.KM1 != null && model.KM1.isOn ? "ВКЛ" : "ВЫКЛ";
        string q1State  = model.Q1 != null && model.Q1.isOn  ? "ВКЛ" : "ВЫКЛ";
        string q2State  = model.Q2 != null && model.Q2.isOn  ? "ВКЛ" : "ВЫКЛ";

        tvInfoText.text =
            $"═ СТЕНД ЛР №5 ═\n" +
            $"Этап: {GetStageDisplayName()}\n" +
            $"Подсказка: {GetStageHint()}\n" +
            $"{lastMessage}\n" +
            $"\n═ СИЛОВАЯ ЦЕПЬ ═\n" +
            $"KM1={km1State} | Q2={q2State}\n" +
            $"n = {model.rotorSpeedRpm:F0} об/мин\n" +
            $"I_дв (PA1) = {model.motorCurrent:F3} А\n" +
            $"\n═ ГЕНЕРАТОР ═\n" +
            $"f (PF1) = {model.generatorFrequency:F2} Гц\n" +
            $"U (PV1) = {model.generatorVoltage:F1} В\n" +
            $"I_A (PA2) = {model.phaseACurrent:F3} А\n" +
            $"I_B (PA3) = {model.phaseBCurrent:F3} А\n" +
            $"I_C (PA4) = {model.phaseCCurrent:F3} А\n" +
            $"cos φ = {model.powerFactor:F3}\n" +
            $"\n═ ВОЗБУЖДЕНИЕ ═\n" +
            $"Q1={q1State} | T1={(model.isTransformerActive ? "✓" : "✗")}\n" +
            $"VD1-VD4={(model.isRectifierActive ? "✓" : "✗")}\n" +
            $"I_в (PA5) = {model.excitationCurrent:F3} А\n" +
            $"R2 = {model.ExcitationRheostatPercent:F0}%\n" +
            $"\n═ НАГРУЗКА ═\n" +
            $"R1 = {model.ActiveLoadPercent:F0}% (R1.1/R1.2/R1.3)";
    }

    // ============================================================
    //  HUD
    // ============================================================
    public void RefreshLabState(bool recalculateModel = true)
    {
        if (recalculateModel && model != null)
            model.RefreshCircuit();

        UpdateMeters();
        UpdateInfoText();
        UpdateHud();
    }

    private void UpdateHud()
    {
        if (hud == null)
            CreateRuntimeHud();

        if (hud == null) return;

        hud.SetHudVisible(showRuntimeHud);
        hud.SetHint(string.Empty);
        hud.SetText(showRuntimeHud ? BuildHudText() : "H — включить HUD");
    }

    private string BuildHudText()
    {
        StringBuilder builder = new StringBuilder(900);
        builder.AppendLine("Испытание синхронного генератора (ЛР №5)");
        builder.AppendLine("Этап: " + GetStageDisplayName());
        builder.AppendLine("Подсказка: " + GetStageHint());
        builder.AppendLine();
        builder.AppendLine("Сообщение: " + lastMessage);
        builder.AppendLine();
        builder.AppendLine("Точки ХХХ (табл.2): " + (model != null ? (model.noLoadAscending.Count + model.noLoadDescending.Count).ToString() : "0"));
        builder.AppendLine("Индукц. нагрузочная (табл.3): " + (model != null ? model.inductiveLoadData.Count.ToString() : "0"));
        builder.AppendLine("Внешняя х-ка (табл.4): " + (model != null ? model.externalData.Count.ToString() : "0"));
        builder.AppendLine("Регулировочная (табл.5): " + (model != null ? model.regulatingData.Count.ToString() : "0"));
        builder.AppendLine("КЗ 3-ф (табл.6): " + (model != null ? model.shortCircuitData.Count.ToString() : "0"));
        builder.AppendLine("КЗ 2-ф (табл.6): " + (model != null ? model.shortCircuit2PhaseData.Count.ToString() : "0"));
        builder.AppendLine();
        builder.AppendLine("Enter — подтвердить этап");
        builder.AppendLine("H — отключить HUD");
        return builder.ToString();
    }

    private void CreateRuntimeHud()
    {
        if (runtimeHudObject != null)
        {
            runtimeHudObject.SetActive(true);
            if (hud != null)
                hud.SetHudVisible(showRuntimeHud);
            return;
        }

        GameObject canvasObject = new GameObject("Lab5RuntimeHud", typeof(Canvas), typeof(CanvasScaler), typeof(Lab5SyncGeneratorHud));
        runtimeHudObject = canvasObject;
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (runtimeFont == null)
            runtimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

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

        hud = canvasObject.GetComponent<Lab5SyncGeneratorHud>();
        hud.SetMainText(text);
        hud.SetHudVisible(showRuntimeHud);
        hud.SetText(showRuntimeHud ? BuildHudText() : "H — включить HUD");
        runtimeHudObject.SetActive(true);
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================
    private string GetStageDisplayName()
    {
        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.Intro:              return "Введение";
            case Lab5SyncGeneratorStage.PowerOn:            return "Подача питания";
            case Lab5SyncGeneratorStage.PrimeMoverStart:    return "Запуск приводного двигателя";
            case Lab5SyncGeneratorStage.NoLoadTest:         return "Характеристика холостого хода (табл.2)";
            case Lab5SyncGeneratorStage.InductiveLoadTest:  return "Индукционная нагрузочная (табл.3)";
            case Lab5SyncGeneratorStage.ExternalTest:       return "Внешняя характеристика (табл.4)";
            case Lab5SyncGeneratorStage.RegulatingTest:     return "Регулировочная характеристика (табл.5)";
            case Lab5SyncGeneratorStage.ShortCircuitTest:   return "Характеристика КЗ (табл.6)";
            case Lab5SyncGeneratorStage.ReactiveTriangle:   return "Реактивный треугольник и диаграмма ЭДС";
            case Lab5SyncGeneratorStage.Completed:          return "Завершено";
            case Lab5SyncGeneratorStage.Fault:              return "Авария";
            default: return currentStage.ToString();
        }
    }

    private string GetStageHint()
    {
        switch (currentStage)
        {
            case Lab5SyncGeneratorStage.Intro:
                return "Включите KM1 для подачи питания на приводной двигатель.";
            case Lab5SyncGeneratorStage.PowerOn:
                return "Включите KM1 и отрегулируйте LLR для запуска двигателя.";
            case Lab5SyncGeneratorStage.PrimeMoverStart:
                return "Дождитесь выхода двигателя на номинальную скорость. Контролируйте частоту по PF1.";
            case Lab5SyncGeneratorStage.NoLoadTest:
                return "Включите Q1 (возбуждение), установите R2. При Q2 выключенном снимайте ХХХ — изменяйте R2 и нажимайте «Записать» для восходящей и нисходящей ветви.";
            case Lab5SyncGeneratorStage.InductiveLoadTest:
                return "Включите Q2. Установите R3 > 0% (индуктивная нагрузка). Изменяйте R2 и записывайте точки (табл.3).";
            case Lab5SyncGeneratorStage.ExternalTest:
                return "Установите R3 = 0% (cosφ=1). Изменяйте R1 (активную нагрузку) и записывайте точки U=f(Ia) (табл.4).";
            case Lab5SyncGeneratorStage.RegulatingTest:
                return "Поддерживайте U≈Uном, изменяйте R1 и компенсируйте R2, записывая точки Iв=f(Ia) (табл.5).";
            case Lab5SyncGeneratorStage.ShortCircuitTest:
                return "Активируйте режим КЗ на планшете. Изменяйте R2 и записывайте точки Ik=f(Iв) для 3-ф и 2-ф КЗ (табл.6).";
            case Lab5SyncGeneratorStage.ReactiveTriangle:
                return "На планшете нажмите «Реактивный треугольник» для расчёта Xσ, Fa, Xd и постройте диаграмму ЭДС.";
            case Lab5SyncGeneratorStage.Completed:
                return "Лабораторная работа завершена. Для повторного выполнения используйте кнопку «Сброс».";
            case Lab5SyncGeneratorStage.Fault:
                return "Аварийное состояние. Используйте кнопку «Сброс».";
            default:
                return string.Empty;
        }
    }
}
