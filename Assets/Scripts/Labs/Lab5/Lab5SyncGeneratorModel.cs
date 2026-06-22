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

using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class Lab5SyncGeneratorModel : MonoBehaviour
{
    // ============================================================
    //  ПОЛНЫЙ ПЕРЕЧЕНЬ ЭЛЕМЕНТОВ СТЕНДА ЛР №5 (синхронный генератор)
    //  В соответствии с таблицей «Полный список элементов стенда/схемы»
    // ============================================================

    // ————— СЕТЬ ПИТАНИЯ (L1, L2, L3) —————
    // Визуальные элементы на схеме, в коде представлены логически.
    public bool isL1Connected = true;
    public bool isL2Connected = true;
    public bool isL3Connected = true;

    // Паспортный ток приводного двигателя (для расчёта PA1)
    public float nominalMotorCurrent = 10f;

    // ————— ОРГАНЫ УПРАВЛЕНИЯ —————
    [Header("— ОРГАНЫ УПРАВЛЕНИЯ (авто-поиск) —")]
    public SliderGor R1;                 // Активная нагрузка R1
    public SliderGor R2;                 // Реостат возбуждения R2
    public Rotator R3;                   // Индуктивная нагрузка (опционально)
    public Rotator LLR;                  // Регулятор скорости приводного двигателя

    [Header("— КНОПКИ УПРАВЛЕНИЯ КОНТАКТОРОМ —")]
    public Switch SB1;                   // Кнопка «Стоп» (размыкающая)
    public Switch SB2;                   // Кнопка «Пуск» (замыкающая)

    [Header("— КОНТАКТОР И КОММУТАЦИОННЫЕ АППАРАТЫ —")]
    public Switch KM1;                   // Катушка контактора + силовые контакты
    // Вспомогательный контакт самоподхвата KM1 — моделируется логически в коде
    public Switch Q1;                    // Двухполюсный выключатель возбуждения (Q1.1 + Q1.2)
    public Switch Q2;                    // Выключатель нагрузки

    [Header("— ДВИГАТЕЛЬ-ГЕНЕРАТОР —")]
    public Motor motor;                  // Приводной двигатель M
    // Синхронный генератор G — представлен всей моделью
    // Вал M-G — механическая связь — моделируется через rotorSpeedRpm

    [Header("— ЦЕПЬ ВОЗБУЖДЕНИЯ (AC/DC) —")]
    // T1 — трансформатор (логически: активен при Q1.isOn)
    public bool isTransformerActive;
    // VD1, VD2, VD3, VD4 — диоды мостового выпрямителя
    public bool isRectifierActive;
    // LG — обмотка возбуждения генератора (логически: через excitationCurrent)
    // R2 — реостат возбуждения (SliderGor выше)

    [Header("— НАГРУЗКА —")]
    // R1.1, R1.2, R1.3 — три ветви активной нагрузки
    public float LoadBranch1Percent;
    public float LoadBranch2Percent;
    public float LoadBranch3Percent;

    [Header("— ЦИФРОВЫЕ ДИСПЛЕИ —")]
    public TMP_Text tvInfoText;
    public TMP_Text pf1_Display;               // Цифровой дисплей частоты (PF1)

    [Header("— СТРЕЛОЧНЫЕ ПРИБОРЫ (ручное подключение) —")]
    public Meter PA1_MotorCurrent;              // Амперметр двигателя
    public Meter PV1_GeneratorVoltage;          // Вольтметр генератора
    public Meter PF1_Frequency;                 // Частотомер
    public Meter PA2_PhaseA;                    // Амперметр фазы A
    public Meter PA3_PhaseB;                    // Амперметр фазы B
    public Meter PA4_PhaseC;                    // Амперметр фазы C
    public Meter PA5_ExcitationCurrent;         // Амперметр возбуждения

    [Header("— ПАСПОРТНЫЕ ДАННЫЕ —")]
    public float nominalVoltage = 380f;
    public float nominalFrequency = 50f;
    public float nominalStatorCurrent = 10f;
    public float nominalExcitationCurrent = 1.2f;
    public float nominalPower = 5000f;
    public int polePairs = 2;

    [Header("— ПАРАМЕТРЫ ГЕНЕРАТОРА —")]
    public float statorResistance75C = 0.5f;
    public float leakageInductiveReactance = 2.5f;
    public float unsaturatedSyncReactance = 12f;
    public float saturatedSyncReactance = 8f;

    [Header("— ТЕКУЩЕЕ СОСТОЯНИЕ (измеряемые величины) —")]
    // Силовая цепь двигателя
    public float rotorSpeedRpm;           // Частота вращения n, об/мин
    public float motorCurrent;            // Ток двигателя (PA1)
    // Статор генератора
    public float generatorFrequency;      // Частота f, Гц (PF1)
    public float generatorVoltage;        // Напряжение U, В (PV1)
    public float phaseACurrent;           // Ток фазы A (PA2)
    public float phaseBCurrent;           // Ток фазы B (PA3)
    public float phaseCCurrent;           // Ток фазы C (PA4)
    // Цепь возбуждения
    public float excitationCurrent;       // Ток возбуждения Iв (PA5)
    // Коэффициент мощности
    public float powerFactor;             // cos φ

    [Header("— ФЛАГИ —")]
    public bool createRuntimeController = true;

    [Header("— RUNTIME HUD —")]
    [SerializeField] private bool showHud = true;
    [SerializeField] private bool showDebugControls = true;

    public bool isPrimeMoverRunning;
    public bool isShortCircuitMode;
    public bool isShortCircuit2PhaseMode;
    public bool hasFault;

    [Header("— СОСТОЯНИЕ ЦЕПЕЙ (вычисляется в UpdateConnectionChains) —")]
    public bool motorPowered;           // L1,L2,L3 → KM1 → M
    public bool generatorRunning;       // motorPowered && isPrimeMoverRunning
    public bool loadConnected;          // Q2.isOn
    public bool excitationACAvailable;  // L1,L2 → Q1 → T1

    [Header("— ДАННЫЕ ХАРАКТЕРИСТИК —")]
    public List<Vector2> noLoadAscending = new List<Vector2>();
    public List<Vector2> noLoadDescending = new List<Vector2>();
    public List<Vector2> inductiveLoadData = new List<Vector2>();
    public List<Vector2> externalData = new List<Vector2>();
    public List<Vector2> regulatingData = new List<Vector2>();
    public List<Vector2> shortCircuitData = new List<Vector2>();
    public List<Vector2> shortCircuit2PhaseData = new List<Vector2>();

    // ============================================================
    //  СВОЙСТВА (для доступа из MeterView и UI)
    // ============================================================
    public float ActiveLoadPercent => R1 != null ? R1.Percent : 0f;
    public float ExcitationRheostatPercent => R2 != null ? R2.Percent : 0f;
    public float InductiveLoadPercent => R3 != null ? R3.value : 0f;
    public float DriveSpeed => LLR != null ? LLR.llrValue : 0f;

    // Состояние коммутационных аппаратов
    public bool IsContactorOn       => KM1 != null && KM1.isOn;
    public bool IsMainPowerOn       => KM1 != null && KM1.isOn;   // Синоним
    public bool IsLoadOn            => Q2 != null && Q2.isOn;
    public bool IsExcitationOn      => Q1 != null && Q1.isOn;
    public bool IsSB1Pressed        => SB1 != null && SB1.isOn;
    public bool IsSB2Pressed        => SB2 != null && SB2.isOn;

    // Измеряемые величины (для Lab5MeterView)
    public float MotorCurrent       => motorCurrent;
    public float GeneratorVoltage   => generatorVoltage;
    public float GeneratorFrequency => generatorFrequency;
    public float PhaseACurrent      => phaseACurrent;
    public float PhaseBCurrent      => phaseBCurrent;
    public float PhaseCCurrent      => phaseCCurrent;
    public float ExcitationCurrentAmps => excitationCurrent;

    public bool ShowHud => showHud;
    public bool ShowDebugControls => showDebugControls;

    // Состояние цепей (для диагностики)
    public bool IsPowerCircuitActive   => IsMainPowerOn && isPrimeMoverRunning;
    public bool IsExcitationACActive   => IsExcitationOn && isL1Connected;
    public bool IsExcitationDCActive   => IsExcitationOn && IsExcitationACActive;
    public bool IsGeneratorLoaded      => IsLoadOn && isPrimeMoverRunning;

    private void Awake()
    {
        AutoFindAll();

        if (Application.isPlaying)
            EnsureSafeMotorRpmText();

        if (Application.isPlaying)
            EnsureRuntimeController();
    }

    private void EnsureSafeMotorRpmText()
    {
        if (motor == null)
            return;

        if (motor.rpmText != null && !IsUnsafeMotorRpmText(motor.rpmText))
            return;

        var rpmTextObject = new GameObject("Lab5SafeMotorRpmText", typeof(TextMeshProUGUI));
        rpmTextObject.transform.SetParent(transform, false);
        rpmTextObject.SetActive(false);
        motor.rpmText = rpmTextObject.GetComponent<TextMeshProUGUI>();
    }

    private bool IsUnsafeMotorRpmText(TMP_Text text)
    {
        if (text == null)
            return true;

        if (text == tvInfoText || text == pf1_Display)
            return true;

        string name = text.gameObject.name.ToLowerInvariant();
        string parentName = text.transform.parent != null ? text.transform.parent.name.ToLowerInvariant() : string.Empty;
        return name.Contains("table") || parentName.Contains("table") || name.Contains("button") || parentName.Contains("button") || parentName.Contains("hud");
    }

    private void EnsureRuntimeController()
    {
        if (!createRuntimeController)
            return;

        if (GetComponent<Lab5SyncGeneratorLabController>() != null)
            return;

        if (FindFirstObjectByType<Lab5SyncGeneratorLabController>() != null)
            return;

        var controller = gameObject.AddComponent<Lab5SyncGeneratorLabController>();
        controller.model = this;
        controller.showRuntimeHud = showHud;
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        SubscribeControls();
        ResetGenerator();
        InvokeRepeating(nameof(TickModel), 0f, 1f / 30f);
    }

    [ContextMenu("Auto-Find All Components")]
    public void AutoFindAll()
    {
        // Поиск слайдеров (R1 — активная нагрузка, R2 — реостат возбуждения)
        foreach (var s in FindObjectsOfType<SliderGor>())
        {
            string n = s.gameObject.name;
            if (n == "R1") R1 = s;
            else if (n == "R2") R2 = s;
        }
        if (R1 == null)
        {
            var sliders = FindObjectsOfType<SliderGor>();
            if (sliders.Length > 0) R1 = sliders[0];
            if (sliders.Length > 1) R2 = sliders[1];
        }

        // Поиск поворотных регуляторов (LLR — скорость, R3 — индуктивная нагрузка)
        foreach (var r in FindObjectsOfType<Rotator>())
        {
            string n = r.gameObject.name;
            if (n == "LLR") LLR = r;
            else if (n == "R3") R3 = r;
        }
        if (LLR == null)
        {
            foreach (var r in FindObjectsOfType<Rotator>())
                if (r.isLLR) { LLR = r; break; }
        }
        if (R3 == null && LLR != null)
        {
            foreach (var r in FindObjectsOfType<Rotator>())
                if (r != LLR) { R3 = r; break; }
        }

        // Поиск переключателей (SB1, SB2, KM1, Q1, Q2)
        foreach (var sw in FindObjectsOfType<Switch>())
        {
            string n = sw.gameObject.name;
            if      (n == "SB1" || n == "sb1") SB1 = sw;
            else if (n == "SB2" || n == "sb2") SB2 = sw;
            else if (n == "KM1" || n == "km1") KM1 = sw;
            else if (n == "Q1"  || n == "q1")  Q1  = sw;
            else if (n == "Q2"  || n == "q2")  Q2  = sw;
        }

        // Fallback: если имена не совпали, назначаем по порядку
        if (SB1 == null || SB2 == null || KM1 == null || Q1 == null || Q2 == null)
        {
            var switches = FindObjectsOfType<Switch>();
            int idx = 0;
            foreach (var sw in switches)
            {
                string n = sw.gameObject.name.ToLower();
                if (n.Contains("sb1") || n.Contains("stop"))  { if (SB1 == null) { SB1 = sw; continue; } }
                if (n.Contains("sb2") || n.Contains("start")) { if (SB2 == null) { SB2 = sw; continue; } }
                if (n.Contains("km1") || n.Contains("kontaktor") || n.Contains("contactor"))
                { if (KM1 == null) { KM1 = sw; continue; } }
                if (n.Contains("q1")  || n.Contains("exc"))   { if (Q1  == null) { Q1  = sw; continue; } }
                if (n.Contains("q2")  || n.Contains("load"))  { if (Q2  == null) { Q2  = sw; continue; } }
            }
            // Если всё ещё не назначены — по порядку
            if (SB1 == null && switches.Length > 0) SB1 = switches[0];
            if (SB2 == null && switches.Length > 1) SB2 = switches[1];
            if (KM1 == null && switches.Length > 2) KM1 = switches[2];
            if (Q1  == null && switches.Length > 3) Q1  = switches[3];
            if (Q2  == null && switches.Length > 4) Q2  = switches[4];
        }

        if (motor == null)
            motor = FindObjectOfType<Motor>();

        // tvInfoText and pf1_Display must be assigned explicitly in the Lab5 scene.
        // A global TMP search can capture runtime HUD/table/button text and corrupt displays.
    }

    private void SubscribeControls()
    {
        if (R1  != null) R1.OnValueChanged  += v => RefreshCircuit();
        if (R2  != null) R2.OnValueChanged  += v => RefreshCircuit();
        if (R3  != null) R3.OnValueChanged  += v => RefreshCircuit();
        if (LLR != null) LLR.OnValueChanged += v => RefreshCircuit();
        if (SB1 != null) SB1.OnValueChanged += v => OnSB1Changed(v);
        if (SB2 != null) SB2.OnValueChanged += v => OnSB2Changed(v);
        if (KM1 != null) KM1.OnValueChanged += v => RefreshCircuit();
        if (Q1  != null) Q1.OnValueChanged  += v => RefreshCircuit();
        if (Q2  != null) Q2.OnValueChanged  += v => RefreshCircuit();
    }

    public void RefreshCircuit()
    {
        CheckContactorSelfHold();
        SetMotorTarget();
        CheckState();
        UpdateConnectionChains();
        CalculateState();
        UpdateInfoText();
    }

    private void TickModel()
    {
        if (isPrimeMoverRunning || (motor != null && motor.CurrentRPM > 1f))
            RefreshCircuit();
    }

    private void SetMotorTarget()
    {
        if (motor == null) return;
        if (KM1 != null && KM1.isOn && LLR != null)
        {
            motor.TargetRPM = Mathf.Lerp(0f, 1500f, Mathf.Clamp01(LLR.llrValue / 250f));
        }
        else
        {
            motor.TargetRPM = 0f;
            motor.CurrentRPM = 0f;
            motor.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        }
    }

    private void CheckState()
    {
        if (KM1 == null || !KM1.isOn)
        {
            isPrimeMoverRunning = false;
            return;
        }
        if (motor != null)
            isPrimeMoverRunning = motor.CurrentRPM > 5f;
        else if (LLR != null)
            isPrimeMoverRunning = LLR.llrValue > 5f;
        else
            isPrimeMoverRunning = false;
    }

    // ============================================================
    //  ОБРАБОТЧИКИ КНОПОК УПРАВЛЕНИЯ КОНТАКТОРОМ
    //  SB1 — кнопка «Стоп» (размыкает цепь управления)
    //  SB2 — кнопка «Пуск» (замыкает цепь управления)
    //  KM1 — катушка контактора + силовые контакты
    //  Самоподхват: при включении KM1 его вспомогательный контакт
    //  шунтирует SB2, удерживая катушку под напряжением
    // ============================================================
    private void OnSB1Changed(bool isPressed)
    {
        if (isPressed)
        {
            // SB1 нажат (размыкает) → отключаем KM1
            if (KM1 != null) KM1.isOn = false;
            // Кнопка с самовозвратом — возвращаем в исходное положение
            if (SB1 != null) StartCoroutine(ResetMomentarySwitch(SB1));
        }
        RefreshCircuit();
    }

    private void OnSB2Changed(bool isPressed)
    {
        if (isPressed)
        {
            // SB2 нажат (замыкает) → включаем KM1 (самоподхват удержит)
            if (KM1 != null && !KM1.isOn) KM1.isOn = true;
            // Кнопка с самовозвратом — возвращаем в исходное положение
            if (SB2 != null) StartCoroutine(ResetMomentarySwitch(SB2));
        }
        RefreshCircuit();
    }

    private System.Collections.IEnumerator ResetMomentarySwitch(Switch sw)
    {
        yield return new WaitForSeconds(0.15f);
        if (sw != null) sw.SetStateImmediate(false);
    }

    // ============================================================
    //  САМОПОДХВАТ КОНТАКТОРА KM1
    //  Если KM1 включён и есть напряжение в цепи управления,
    //  он остаётся включённым даже после отпускания SB2.
    // ============================================================
    private void CheckContactorSelfHold()
    {
        if (KM1 == null) return;
        // Цепь управления: +L1 → SB1(НЗ) → SB2(НО) || KM1(вспом.) → KM1(катушка) → -L3
        bool controlCircuitPowered = isL1Connected && isL3Connected;

        if (KM1.isOn && (!controlCircuitPowered || (SB1 != null && SB1.isOn)))
        {
            // Пропало питание сети или нажата SB1 — KM1 отпадает
            KM1.isOn = false;
        }
    }

    // ============================================================
    //  ОБНОВЛЕНИЕ ЦЕПЕЙ СОЕДИНЕНИЙ
    //  Моделирует физические соединения по каждой цепи стенда
    // ============================================================
    private void UpdateConnectionChains()
    {
        bool mainsAvailable = isL1Connected && isL2Connected && isL3Connected;
        bool mainsForTransformer = isL1Connected && isL2Connected;  // T1 подкл. к L1 и L2

        // ——— Цепь 1: Сеть питания двигателя ———
        // L1, L2, L3 → KM1 силовые контакты → M
        // PA1 включён в разрыв одной фазы.
        bool powerContactsClosed = KM1 != null && KM1.isOn;
        motorPowered = mainsAvailable && powerContactsClosed;

        // ——— Цепь 2: Цепь управления контактором ———
        // L1 → SB1 (НЗ) → SB2 (НО) || KM1 вспом. контакт → KM1 катушка → L3
        // Логика самоподхвата реализована в CheckContactorSelfHold

        // ——— Цепь 3: PA1 — в разрыве фазы (значение в CalculateState) ———

        // ——— Цепь 4: M → вал → G (rotorSpeedRpm) ———

        // ——— Цепь 5: Статор генератора ———
        // G → PA2, PA3, PA4 → Q2 → R1
        generatorRunning = isPrimeMoverRunning && motorPowered;
        loadConnected = Q2 != null && Q2.isOn;

        // ——— Цепь 6/7: PV1, PF1 — активны при generatorRunning ———

        // ——— Цепь 8: Активная нагрузка ———
        // Q2 → R1.1/R1.2/R1.3
        if (R1 != null)
        {
            float loadPct = Mathf.Clamp01(R1.Percent / 100f);
            LoadBranch1Percent = loadPct;
            LoadBranch2Percent = loadPct * 0.95f;
            LoadBranch3Percent = loadPct * 0.98f;
        }
        else
        {
            LoadBranch1Percent = 0f;
            LoadBranch2Percent = 0f;
            LoadBranch3Percent = 0f;
        }

        // ——— Цепь 9: Цепь возбуждения AC ———
        // L1/L2 → Q1.1/Q1.2 → T1 (трансформатор на две фазы)
        bool excSwitchClosed = Q1 != null && Q1.isOn;
        excitationACAvailable = mainsForTransformer && excSwitchClosed;
        isTransformerActive = excitationACAvailable;

        // ——— Цепь 10: Выпрямитель ———
        // T1 → VD1, VD2, VD3, VD4
        isRectifierActive = isTransformerActive;

        // ——— Цепь 11: DC-цепь возбуждения ———
        // VD1-VD4(+) → R2 → LG → PA5 → обратно к мосту
        // (excitationCurrent вычисляется в CalculateState)
    }

    // ============================================================
    //  ПОЛНЫЙ РАСЧЁТ СОСТОЯНИЯ СХЕМЫ
    //  Вычисляет все измеряемые величины по цепям:
    //  PA1, PV1, PF1, PA2, PA3, PA4, PA5, cos φ
    // ============================================================
    private void CalculateState()
    {
        if (!isPrimeMoverRunning)
        {
            rotorSpeedRpm = 0f;
            generatorFrequency = 0f;
            excitationCurrent = 0f;
            generatorVoltage = 0f;
            phaseACurrent = 0f;
            phaseBCurrent = 0f;
            phaseCCurrent = 0f;
            motorCurrent = 0f;
            powerFactor = 0f;
            return;
        }

        // ——— Приводной двигатель M ———
        rotorSpeedRpm = motor != null ? motor.CurrentRPM
            : Mathf.Lerp(0f, 1500f, Mathf.Clamp01((LLR != null ? LLR.llrValue : 0f) / 250f));
        generatorFrequency = rotorSpeedRpm * polePairs / 60f;

        // Ток двигателя (PA1): зависит от скорости и нагрузки на валу.
        float speedNorm = Mathf.Clamp01(rotorSpeedRpm / 1500f);
        float genLoadFactor = (loadConnected && !isShortCircuitMode && !isShortCircuit2PhaseMode)
            ? Mathf.Clamp01((R1 != null ? R1.Percent : 0f) / 100f)
            : 0f;
        motorCurrent = motorPowered ? speedNorm * (nominalMotorCurrent * (0.3f + 0.7f * genLoadFactor)) : 0f;

        // ——— Цепь возбуждения ———
        // excitationACAvailable, isRectifierActive — из UpdateConnectionChains
        float excT = R2 != null ? Mathf.Clamp01(R2.Percent / 100f) : 0f;
        excitationCurrent = isRectifierActive
            ? Mathf.Lerp(0f, nominalExcitationCurrent * 1.5f, excT)
            : 0f;

        // ——— Статор генератора ———
        float loadT = R1 != null ? Mathf.Clamp01(R1.Percent / 100f) : 0f;
        float rawInductiveT = R3 != null ? Mathf.Clamp01(R3.value / 100f) : 0f;
        float inductiveT = rawInductiveT <= 0.05f ? 0f : rawInductiveT;

        // ЭДС по кусочно-линейной кривой насыщения (характеристика холостого хода)
        generatorVoltage = CalculateVoltageFromMagnetizationCurve(excitationCurrent);

        if (!generatorRunning)
        {
            // Сеть питания двигателя отсутствует — всё нули
            generatorVoltage = 0f;
            phaseACurrent = 0f;
            phaseBCurrent = 0f;
            phaseCCurrent = 0f;
            powerFactor = 0f;
        }
        else if (loadConnected && !isShortCircuitMode && !isShortCircuit2PhaseMode)
        {
            // Падение напряжения от активной и реактивной нагрузок складывается
            // геометрически (как ортогональные векторы), а не арифметически.
            float deltaU = Mathf.Sqrt(loadT * 120f * loadT * 120f + inductiveT * 180f * inductiveT * 180f);
            generatorVoltage = Mathf.Max(0f, generatorVoltage - deltaU);
            float baseCurrent = generatorVoltage / 50f;

            // Активная и реактивная составляющие тока — ортогональны
            float iActive = baseCurrent * loadT;      // I_a = U/R, в фазе с U
            float iReactive = baseCurrent * inductiveT; // I_p = U/XL, отстаёт на 90°
            float iTotal = Mathf.Sqrt(iActive * iActive + iReactive * iReactive);
            phaseACurrent = Mathf.Min(iTotal, nominalStatorCurrent * 1.5f);
            phaseBCurrent = phaseACurrent * 0.97f;
            phaseCCurrent = phaseACurrent * 1.02f;

            // cos φ = I_a / I_полн
            powerFactor = iTotal > 0.001f
                ? Mathf.Clamp01(iActive / iTotal)
                : 1f;
        }
        else if (isShortCircuitMode || isShortCircuit2PhaseMode)
        {
            float Ik3 = CalculateShortCircuitCurrent(excitationCurrent);
            phaseACurrent = isShortCircuit2PhaseMode ? Ik3 * 0.85f : Ik3;
            phaseBCurrent = phaseACurrent * 0.97f;
            phaseCCurrent = phaseACurrent * 1.02f;
            generatorVoltage = 0f;
            powerFactor = 0f;
        }
        else
        {
            // Холостой ход — токов нет (Q2 выключен)
            phaseACurrent = 0f;
            phaseBCurrent = 0f;
            phaseCCurrent = 0f;
            powerFactor = 1f;
        }
    }

    /// ЭДС по кусочно-линейной кривой намагничивания (ХХХ)
    /// Участки: остаточная → линейный (воздушный зазор) → колено → насыщение → глубокое насыщение
    private float CalculateVoltageFromMagnetizationCurve(float fieldCurrent)
    {
        if (fieldCurrent <= 0f) return 0f;

        float ifNorm = fieldCurrent / nominalExcitationCurrent; // I_в / I_в_ном
        float emfNorm;

        // Кривая насыщения в относительных единицах
        // ifNorm = 0    → emfNorm = 0.00
        // ifNorm = 0.5  → emfNorm = 0.65 (конец линейного участка)
        // ifNorm = 0.75 → emfNorm = 0.90 (колено)
        // ifNorm = 1.0  → emfNorm = 1.00 (номинальный режим)
        // ifNorm = 1.5  → emfNorm = 1.15 (насыщение)
        // ifNorm > 1.5  → emfNorm = 1.20 (глубокое насыщение)
        if (ifNorm <= 0.5f)
        {
            // Линейный участок (воздушный зазор): slope = 1.3
            emfNorm = 1.3f * ifNorm;
            emfNorm = Mathf.Max(emfNorm, 0f);
        }
        else if (ifNorm <= 0.75f)
        {
            // Колено (переход от линейного к насыщению)
            float t = (ifNorm - 0.5f) / 0.25f;
            emfNorm = Mathf.Lerp(0.65f, 0.90f, t);
        }
        else if (ifNorm <= 1.0f)
        {
            // Верхняя часть колена (приближение к номиналу)
            float t = (ifNorm - 0.75f) / 0.25f;
            emfNorm = Mathf.Lerp(0.90f, 1.0f, t);
        }
        else if (ifNorm <= 1.5f)
        {
            // Насыщение
            float t = (ifNorm - 1.0f) / 0.5f;
            emfNorm = Mathf.Lerp(1.0f, 1.15f, t);
        }
        else
        {
            // Глубокое насыщение
            emfNorm = 1.20f;
        }

        return emfNorm * nominalVoltage;
    }

    /// Ток короткого замыкания: I_к = f(I_в) по характеристике КЗ (линейная)
    /// Ik = (I_в / I_в_ном) * I_к_ном, где I_к_ном = 0.8·I_ном
    /// Ik < I1 (п. 5.7 методички)
    private float CalculateShortCircuitCurrent(float fieldCurrent)
    {
        float ifRatio = fieldCurrent / nominalExcitationCurrent;
        float ratedScCurrent = nominalStatorCurrent * 0.8f;
        return Mathf.Min(ifRatio * ratedScCurrent, nominalStatorCurrent * 0.95f);
    }

    private void UpdateInfoText()
    {
        if (pf1_Display != null)
        {
            bool genActive = isPrimeMoverRunning && IsMainPowerOn;
            pf1_Display.text = genActive
                ? $"{generatorFrequency:F2} Гц"
                : "---";
        }

        if (tvInfoText == null) return;

        // This world-space display is shared with the motor stand. Keep it short;
        // the runtime HUD owns all long lab/status text.
        tvInfoText.text =
            $"n = {rotorSpeedRpm:F0} об./мин.\n" +
            $"f = {generatorFrequency:F1} Гц";
    }

    public void StartMotor()
    {
        if (KM1 != null) KM1.isOn = true;
        RefreshCircuit();
    }

    public void StopMotor()
    {
        if (KM1 != null) KM1.isOn = false;
        StopMotorImmediately();
        RefreshCircuit();
    }
    public void ResetGenerator() { hasFault = false; isShortCircuitMode = false; isShortCircuit2PhaseMode = false; }

    public void RecordNoLoadPoint()
    {
        if (!isPrimeMoverRunning) { Debug.LogWarning("Двигатель не вращается"); return; }
        if (Q2 != null && Q2.isOn) { Debug.LogWarning("Для ХХХ Q2 должен быть выключен"); return; }
        float If = excitationCurrent;
        noLoadAscending.Add(new Vector2(If, generatorVoltage));
        Debug.Log($"ХХХ: I_в = {If:F3} А, E_0 = {generatorVoltage:F1} В");
    }

    public void RecordInductiveLoadPoint() { inductiveLoadData.Add(new Vector2(excitationCurrent, generatorVoltage)); }
    public void RecordExternalPoint() { externalData.Add(new Vector2(phaseACurrent, generatorVoltage)); }
    public void RecordRegulatingPoint() { regulatingData.Add(new Vector2(phaseACurrent, excitationCurrent)); }

    public void RecordShortCircuitPoint()
    {
        if (!isShortCircuitMode) { Debug.LogWarning("Режим КЗ не активирован"); return; }
        shortCircuitData.Add(new Vector2(excitationCurrent, phaseACurrent));
    }

    public void EnableShortCircuitMode() { isShortCircuitMode = true; isShortCircuit2PhaseMode = false; }
    public void DisableShortCircuitMode() { isShortCircuitMode = false; }

    public void RecordShortCircuit2PhasePoint()
    {
        if (!isShortCircuit2PhaseMode) { Debug.LogWarning("Режим двухфазного КЗ не активирован"); return; }
        shortCircuit2PhaseData.Add(new Vector2(excitationCurrent, phaseACurrent));
    }

    public void EnableShortCircuit2PhaseMode() { isShortCircuit2PhaseMode = true; isShortCircuitMode = false; }
    public void DisableShortCircuit2PhaseMode() { isShortCircuit2PhaseMode = false; }

    public void ClearAllCharacteristicData()
    {
        noLoadAscending.Clear(); noLoadDescending.Clear();
        inductiveLoadData.Clear(); externalData.Clear();
        regulatingData.Clear(); shortCircuitData.Clear();
        shortCircuit2PhaseData.Clear();
    }

    public void ResetCircuit()
    {
        if (R1 != null) R1.SetPercent(0f);
        if (R2 != null) R2.SetPercent(0f);
        if (R3 != null) { R3.SetNormalizedValue(0f); }
        if (LLR != null) LLR.SetNormalizedValue(0f);
        if (SB1 != null) SB1.SetStateImmediate(false);
        if (SB2 != null) SB2.SetStateImmediate(false);
        if (KM1 != null) KM1.SetStateImmediate(false);
        if (Q1 != null) Q1.SetStateImmediate(false);
        if (Q2 != null) Q2.SetStateImmediate(false);
        ResetGenerator();
        StopMotorImmediately();
        RefreshCircuit();
        StopMotorImmediately();
    }

    private void StopMotorImmediately()
    {
        isPrimeMoverRunning = false;
        rotorSpeedRpm = 0f;
        generatorFrequency = 0f;
        motorCurrent = 0f;
        generatorVoltage = 0f;
        phaseACurrent = 0f;
        phaseBCurrent = 0f;
        phaseCCurrent = 0f;
        excitationCurrent = 0f;
        powerFactor = 0f;

        if (motor == null) return;

        motor.TargetRPM = 0f;
        motor.CurrentRPM = 0f;
        motor.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
    }

    /// Находит угловой коэффициент начального прямолинейного участка ХХХ
    private float CalculateInitialSlope()
    {
        List<Vector2> noLoadPoints = GetSortedNoLoadPointsForCalculation();
        int count = Mathf.Min(noLoadPoints.Count, 5);
        if (count < 2) return 0f;
        float sumXY = 0f, sumXX = 0f;
        for (int i = 0; i < count; i++)
        {
            float x = noLoadPoints[i].x;
            float y = noLoadPoints[i].y;
            sumXY += x * y;
            sumXX += x * x;
        }
        if (sumXX < 0.0001f) return 0f;
        return sumXY / sumXX;
    }

    /// Линейная интерполяция в списке точек по X
    private float Interpolate(List<Vector2> points, float targetX)
    {
        if (points.Count == 0) return 0f;
        if (targetX <= points[0].x) return points[0].y;
        if (targetX >= points[points.Count - 1].x) return points[points.Count - 1].y;
        for (int i = 1; i < points.Count; i++)
        {
            if (targetX <= points[i].x)
            {
                float t = (targetX - points[i - 1].x) / (points[i].x - points[i - 1].x);
                return Mathf.Lerp(points[i - 1].y, points[i].y, t);
            }
        }
        return points[points.Count - 1].y;
    }

    private List<Vector2> GetSortedNoLoadPointsForCalculation()
    {
        List<Vector2> points = new List<Vector2>(noLoadAscending.Count + noLoadDescending.Count);
        points.AddRange(noLoadAscending);
        points.AddRange(noLoadDescending);
        points.Sort((a, b) => a.x.CompareTo(b.x));
        return points;
    }

    /// Ток возбуждения I_в по характеристике КЗ для заданного тока I_к
    public float GetShortCircuitExcitation(float targetCurrent)
    {
        List<Vector2> points = GetSortedByX(shortCircuitData);
        points.Sort((a, b) => a.y.CompareTo(b.y));
        if (points.Count < 2) return points.Count == 1 ? points[0].x : 0f;
        if (targetCurrent <= 0f) return 0f;
        if (targetCurrent <= points[0].y) return points[0].x;
        if (targetCurrent >= points[points.Count - 1].y)
            return points[points.Count - 1].x;
        for (int i = 1; i < points.Count; i++)
        {
            if (targetCurrent <= points[i].y)
            {
                float t = Mathf.InverseLerp(points[i - 1].y, points[i].y, targetCurrent);
                return Mathf.Lerp(points[i - 1].x, points[i].x, t);
            }
        }
        return points[points.Count - 1].x;
    }

    /// Точка на индукционной нагрузочной характеристике при заданном напряжении
    public Vector2 FindPointOnInductiveLoad(float targetVoltage)
    {
        return FindPointOnInductiveLoad(targetVoltage, 5f, out _);
    }

    private Vector2 FindPointOnInductiveLoad(float targetVoltage, float voltageTolerance, out string warning)
    {
        warning = string.Empty;
        if (inductiveLoadData.Count == 0) return Vector2.zero;

        List<Vector2> points = GetSortedByY(inductiveLoadData);
        Vector2 nearest = points[0];
        float nearestDelta = Mathf.Abs(points[0].y - targetVoltage);
        for (int i = 1; i < points.Count; i++)
        {
            float delta = Mathf.Abs(points[i].y - targetVoltage);
            if (delta < nearestDelta)
            {
                nearest = points[i];
                nearestDelta = delta;
            }
        }

        if (nearestDelta <= voltageTolerance)
            return nearest;

        if (points.Count >= 2)
        {
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 prev = points[i - 1];
                Vector2 curr = points[i];
                if ((prev.y <= targetVoltage && targetVoltage <= curr.y) || (curr.y <= targetVoltage && targetVoltage <= prev.y))
                {
                    float t = Mathf.InverseLerp(prev.y, curr.y, targetVoltage);
                    float x = Mathf.Lerp(prev.x, curr.x, t);
                    return new Vector2(x, targetVoltage);
                }
            }
        }

        warning = $"ПРЕДУПРЕЖДЕНИЕ: индукционная характеристика не достигает Uном = {targetVoltage:F1} В. Для расчета использована ближайшая точка U = {nearest.y:F1} В при Iв = {nearest.x:F3} А.";
        return nearest;
    }

    private List<Vector2> GetSortedByX(List<Vector2> source)
    {
        List<Vector2> points = new List<Vector2>(source);
        points.Sort((a, b) => a.x.CompareTo(b.x));
        return points;
    }

    private List<Vector2> GetSortedByY(List<Vector2> source)
    {
        List<Vector2> points = new List<Vector2>(source);
        points.Sort((a, b) => a.y.CompareTo(b.y));
        return points;
    }

    /// Пересечение прямой (point, slope) с характеристикой холостого хода
    private Vector2 FindIntersectionWithNoLoad(Vector2 pointOnLine, float slope)
    {
        List<Vector2> noLoadPoints = GetSortedNoLoadPointsForCalculation();
        if (noLoadPoints.Count < 2) return Vector2.zero;
        for (int i = 1; i < noLoadPoints.Count; i++)
        {
            Vector2 prev = noLoadPoints[i - 1];
            Vector2 curr = noLoadPoints[i];
            float lineYatPrev = pointOnLine.y + slope * (prev.x - pointOnLine.x);
            float lineYatCurr = pointOnLine.y + slope * (curr.x - pointOnLine.x);
            float dPrev = prev.y - lineYatPrev;
            float dCurr = curr.y - lineYatCurr;
            if (dPrev * dCurr <= 0f)
            {
                float t = Mathf.Abs(dPrev) / (Mathf.Abs(dPrev) + Mathf.Abs(dCurr));
                float x = Mathf.Lerp(prev.x, curr.x, t);
                float y = Mathf.Lerp(prev.y, curr.y, t);
                return new Vector2(x, y);
            }
        }
        return noLoadPoints[noLoadPoints.Count - 1];
    }

    /// Полный расчёт реактивного треугольника (п. 5.8 методички)
    /// <param name="Xσ">Индуктивное сопротивление рассеяния, Ом</param>
    /// <param name="Fa">МДС реакции якоря в масштабе тока возбуждения, А</param>
    /// <param name="Xd_unsat">Ненасыщенное синхронное индуктивное сопротивление, Ом</param>
    /// <param name="Xd_sat">Насыщенное синхронное индуктивное сопротивление, Ом</param>
    /// <param name="details">Пошаговые результаты для отображения в таблице</param>
    public void CalculateReactiveTriangle(
        out float Xσ, out float Fa, out float Xd_unsat, out float Xd_sat,
        out Dictionary<string, string> details)
    {
        details = new Dictionary<string, string>();
        Xσ = 0f; Fa = 0f; Xd_unsat = unsaturatedSyncReactance; Xd_sat = saturatedSyncReactance;

        float Ia_target = nominalStatorCurrent * 0.5f;
        details["Ia_target"] = $"{Ia_target:F3} А (0.5·I_1ном)";
        details["UsedTables"] = "Использованы таблицы: ХХХ — 5.1; индукционная нагрузочная — 5.2; трехфазное КЗ — 5.5.";

        List<Vector2> noLoadPoints = GetSortedNoLoadPointsForCalculation();
        if (noLoadPoints.Count < 2) { details["Error"] = "Недостаточно данных ХХХ для расчета таблицы 5.6."; return; }
        if (inductiveLoadData.Count == 0) { details["Error"] = "Нет данных индукционной нагрузочной характеристики для расчета таблицы 5.6."; return; }
        if (shortCircuitData.Count == 0) { details["Error"] = "Недостаточно данных трехфазного КЗ для расчета таблицы 5.6."; return; }

        // 1. Угловой коэффициент начальной прямолинейной части ХХХ
        float slope = CalculateInitialSlope();
        if (slope <= 0f) { details["Error"] = "Недостаточно данных ХХХ для определения начального участка."; return; }
        details["Slope_XXX"] = $"{slope:F2} В/А (E_0 / I_в)";

        // 2. Ток возбуждения I_кз при номинальном токе КЗ (I_к = I_1ном = 100%).
        // Методичка п.5.8: отрезок A1O1 = I_в при I_к = I_1ном (не 50%!).
        // Для линейной модели КЗ: Ik = (I_в / I_в_ном) · 0.8 · I_1ном
        // I_в(I_к=I_1ном) = I_в_ном / 0.8 (расчёт, не интерполяция — данные КЗ могут быть
        // ниже I_1 из-за ограничения Ik < I1 по п.5.7).
        float I_k3 = nominalExcitationCurrent / 0.8f;
        details["I_k3"] = $"{I_k3:F4} А (I_в при I_к = I_1ном); по данным КЗ: {GetShortCircuitExcitation(nominalStatorCurrent):F4} А";

        // 3. Точка A1 на индукционной нагрузочной характеристике при U = U_ном
        Vector2 A1 = FindPointOnInductiveLoad(nominalVoltage, 5f, out string inductiveWarning);
        if (!string.IsNullOrEmpty(inductiveWarning)) details["Warning_Inductive"] = inductiveWarning;
        details["A1"] = $"координата ({A1.x:F4} А; {A1.y:F1} В) — на индукц. нагрузочной х-ке при U = U_ном";

        // 4. Точка O1 = A1, сдвинутая влево на I_кз
        Vector2 O1 = new Vector2(A1.x - I_k3, A1.y);
        details["O1"] = $"расчетная координата ({O1.x:F4} А; {O1.y:F1} В) — A1 сдвинута на -I_кз; отрицательный X допустим как координата построения";

        // 5. Прямая O1C1 ∥ начальному участку ХХХ → поиск пересечения C1 с ХХХ
        Vector2 C1 = FindIntersectionWithNoLoad(O1, slope);
        if (C1.x <= 0f) { details["Error"] = "Не удалось найти пересечение прямой O1C1 с ХХХ"; return; }
        details["C1"] = $"координата ({C1.x:F4} А; {C1.y:F1} В) — пересечение O1C1 с ХХХ";

        // 6. Ia · Xσ — катет C1O1 в масштабе напряжения
        float deltaU_Xσ = C1.y - O1.y;
        if (deltaU_Xσ < 0f) deltaU_Xσ = 0f;
        Xσ = deltaU_Xσ / Ia_target;
        details["deltaU_Xσ"] = $"{deltaU_Xσ:F2} В (C1O1 в масштабе напряжения = Ia · Xσ)";
        details["Xσ"] = $"{Xσ:F3} Ом (Xσ = ΔU / Ia)";

        // 7. Fa — отрезок A1O1 (МДС реакции якоря)
        Fa = A1.x - O1.x;
        details["Fa"] = $"{Fa:F4} А (A1O1 — МДС реакции якоря в масштабе I_в)";

        // 8. Xd насыщенное: Xd_sat = (A1F) / Ia, где F = (I_в_O1, E_XXX(I_в_O1))
        float E_at_O1 = Interpolate(noLoadPoints, O1.x);
        float rawA1F = E_at_O1 - A1.y;
        float A1F = Mathf.Max(0f, rawA1F);
        if (A1F > 0f) Xd_sat = A1F / Ia_target;
        details["E_at_O1"] = $"{E_at_O1:F1} В (E_0 при I_в = {O1.x:F4} А по ХХХ)";
        details["A1F"] = $"{A1F:F2} В (модуль отрезка A1F; расчетная разность {rawA1F:F2} В)";
        details["Xd_sat"] = $"{Xd_sat:F3} Ом (насыщенное Xd = A1F / Ia)";

        // 9. Xd ненасыщенное — по спрямлённой ХХХ (воздушный зазор)
        Xd_unsat = CalculateUnsaturatedSyncReactance();
        details["Xd_unsat"] = $"{Xd_unsat:F3} Ом (ненасыщенное Xd = E_0 / I_к по спрямлённой ХХХ)";
        // Для сравнения — по реальной (сырой) ХХХ
        float Xd_raw = CalculateSyncReactance();
        details["Xd_raw"] = $"{Xd_raw:F3} Ом (по реальной ХХХ, с насыщением)";
    }

    /// Xd ненасыщенное по спрямлённой ХХХ (прямая воздушного зазора)
    public float CalculateUnsaturatedSyncReactance()
    {
        List<Vector2> noLoadPoints = GetSortedNoLoadPointsForCalculation();
        if (shortCircuitData.Count < 1 || noLoadPoints.Count < 2) return unsaturatedSyncReactance;

        List<Vector2> scPoints = GetSortedByX(shortCircuitData);
        var last = scPoints[scPoints.Count - 1];
        float slope = CalculateInitialSlope();
        if (slope <= 0f || last.y <= 0.001f) return unsaturatedSyncReactance;

        // E_0 по спрямлённой ХХХ: E_0 = slope * I_в (линейная зависимость)
        float E0_airgap = slope * last.x;
        return E0_airgap / last.y;
    }

    /// Xd по реальной (сырой) ХХХ — с учётом насыщения
    public float CalculateSyncReactance()
    {
        List<Vector2> noLoadPoints = GetSortedNoLoadPointsForCalculation();
        if (shortCircuitData.Count > 0 && noLoadPoints.Count > 0)
        {
            List<Vector2> scPoints = GetSortedByX(shortCircuitData);
            var last = scPoints[scPoints.Count - 1];
            float E0 = Interpolate(noLoadPoints, last.x);
            if (last.y > 0.001f && E0 > 0.1f) return E0 / last.y;
        }
        return unsaturatedSyncReactance;
    }

    public float CalculateLeakageReactance() => leakageInductiveReactance;

    /// Обратная интерполяция: поиск X по заданному Y (для ХХХ: I_в по E_0)
    private float InterpolateInverse(List<Vector2> points, float targetY)
    {
        if (points.Count == 0) return 0f;
        if (targetY <= points[0].y) return points[0].x;
        if (targetY >= points[points.Count - 1].y) return points[points.Count - 1].x;
        for (int i = 1; i < points.Count; i++)
        {
            if (targetY <= points[i].y)
            {
                float t = (targetY - points[i - 1].y) / (points[i].y - points[i - 1].y);
                return Mathf.Lerp(points[i - 1].x, points[i].x, t);
            }
        }
        return points[points.Count - 1].x;
    }

    /// Расчёт диаграммы ЭДС (векторной) по МПО (п. 5.9 методички)
    /// Вызывается после CalculateReactiveTriangle — нужны Xσ и Fa
    public void CalculateEmfVectorDiagram(
        float Xσ, float Fa,
        out Vector2 emf_E_δ, out Vector2 emf_F_δ, out Vector2 emf_Fa, out Vector2 emf_F_0, out Vector2 emf_E_0,
        out float deltaU_percent, out Dictionary<string, string> details)
    {
        details = new Dictionary<string, string>();
        emf_E_δ = emf_F_δ = emf_Fa = emf_F_0 = emf_E_0 = Vector2.zero;
        deltaU_percent = 0f;

        List<Vector2> noLoadPoints = GetSortedNoLoadPointsForCalculation();
        if (noLoadPoints.Count < 2) { details["Error"] = "Недостаточно данных ХХХ для диаграммы ЭДС"; return; }

        float U_н = nominalVoltage;
        float I_a = nominalStatorCurrent * 0.5f;
        float cosφ = 0.8f;
        float φ_rad = Mathf.Acos(cosφ);
        float φ_deg = φ_rad * Mathf.Rad2Deg;

        details["U_н"] = $"{U_н:F1} В (опорный вектор, 0°)";
        details["I_a"] = $"{I_a:F3} А (0.5·I_ном)";
        details["cosφ"] = $"{cosφ:F1} (φ = {φ_deg:F2}°, отстающий)";

        // 1. Вектор U_н: (U_н, 0)
        Vector2 v_U = new Vector2(U_н, 0f);

        // 2. Вектор I_a: под углом -φ (отстаёт от U)
        Vector2 v_Ia = new Vector2(I_a * cosφ, -I_a * Mathf.Sin(φ_rad));

        // 3. I_a · R_a — в фазе с I_a
        float Ra = statorResistance75C;
        Vector2 v_IaRa = v_Ia * (Ra);

        // 4. I_a · Xσ — опережает I_a на 90°
        float angle_IaRa = -φ_rad;
        float angle_Xσ = angle_IaRa + Mathf.PI / 2f;
        float mag_Xσ = I_a * Xσ;
        Vector2 v_IaXσ = new Vector2(mag_Xσ * Mathf.Cos(angle_Xσ), mag_Xσ * Mathf.Sin(angle_Xσ));

        // 5. E_δ = U_н + I_a·R_a + j·I_a·Xσ
        emf_E_δ = v_U + v_IaRa + v_IaXσ;
        float mag_Eδ = emf_E_δ.magnitude;
        float angle_Eδ_deg = Mathf.Atan2(emf_E_δ.y, emf_E_δ.x) * Mathf.Rad2Deg;
        details["E_δ"] = $"{mag_Eδ:F2} В (угол {angle_Eδ_deg:F2}°)";
        details["E_δ_components"] = $"проекции: U_н({v_U.x:F1};0) + Ia·Ra({v_IaRa.x:F2};{v_IaRa.y:F2}) + Ia·Xσ({v_IaXσ.x:F2};{v_IaXσ.y:F2})";

        // 6. F_δ — из ХХХ по |E_δ|, угол = ∠E_δ + 90° (опережает)
        float mag_Fδ = InterpolateInverse(noLoadPoints, mag_Eδ);
        // Если E_δ выходит за пределы ХХХ — экстраполируем по начальному участку
        if (mag_Eδ > noLoadPoints[noLoadPoints.Count - 1].y)
        {
            float slope = CalculateInitialSlope();
            if (slope > 0f) mag_Fδ = mag_Eδ / slope;
        }
        float angle_Fδ_deg = angle_Eδ_deg + 90f;
        float angle_Fδ_rad = angle_Fδ_deg * Mathf.Deg2Rad;
        emf_F_δ = new Vector2(mag_Fδ * Mathf.Cos(angle_Fδ_rad), mag_Fδ * Mathf.Sin(angle_Fδ_rad));
        details["F_δ"] = $"{mag_Fδ:F4} А (угол {angle_Fδ_deg:F2}°)";

        // 7. Fa — из реактивного треугольника, в фазе с I_a
        float angle_Fa_deg = -φ_deg;
        float angle_Fa_rad = -φ_rad;
        emf_Fa = new Vector2(Fa * Mathf.Cos(angle_Fa_rad), Fa * Mathf.Sin(angle_Fa_rad));
        details["Fa (вектор)"] = $"{Fa:F4} А (угол {angle_Fa_deg:F2}°)";

        // 8. F_0 = F_δ + Fa (геометрическая сумма)
        emf_F_0 = emf_F_δ + emf_Fa;
        float mag_F0 = emf_F_0.magnitude;
        float angle_F0_deg = Mathf.Atan2(emf_F_0.y, emf_F_0.x) * Mathf.Rad2Deg;
        details["F_0"] = $"{mag_F0:F4} А (угол {angle_F0_deg:F2}°)";

        // 9. E_0 — из ХХХ по |F_0|, угол = ∠F_0 - 90° (отстаёт)
        float mag_E0 = Interpolate(noLoadPoints, mag_F0);
        if (mag_F0 > noLoadPoints[noLoadPoints.Count - 1].x)
        {
            float slope = CalculateInitialSlope();
            if (slope > 0f) mag_E0 = mag_F0 * slope;
        }
        // Если mag_E0 < U_н (анормально), используем Xd_unsat для оценки
        if (mag_E0 < U_н) mag_E0 = U_н + I_a * CalculateSyncReactance();
        float angle_E0_deg = angle_F0_deg - 90f;
        float angle_E0_rad = angle_E0_deg * Mathf.Deg2Rad;
        emf_E_0 = new Vector2(mag_E0 * Mathf.Cos(angle_E0_rad), mag_E0 * Mathf.Sin(angle_E0_rad));
        details["E_0 (вектор)"] = $"{mag_E0:F2} В (угол {angle_E0_deg:F2}°)";

        // 10. ΔU_o = (E_0 - U_н) / U_н · 100%
        deltaU_percent = (mag_E0 - U_н) / U_н * 100f;
        details["ΔU_o"] = $"{deltaU_percent:F2}%";
    }

    public List<Vector2> GetNoLoadData()
    { var c = new List<Vector2>(noLoadAscending); c.AddRange(noLoadDescending); return c; }

    public List<Vector2> GetInductiveLoadData() => new List<Vector2>(inductiveLoadData);
    public List<Vector2> GetExternalData() => new List<Vector2>(externalData);
    public List<Vector2> GetRegulatingData() => new List<Vector2>(regulatingData);
    public List<Vector2> GetShortCircuitData() => new List<Vector2>(shortCircuitData);
    public List<Vector2> GetShortCircuit2PhaseData() => new List<Vector2>(shortCircuit2PhaseData);
}
