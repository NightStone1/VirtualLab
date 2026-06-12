using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Lab3_ElectricCircuit : MonoBehaviour
{
    [Header("Органы управления")]
    public SliderGor R1;                 // Реостат в цепи возбуждения
    public SliderGor R2;                 // Нагрузочный реостат (цепь якоря)
    public SliderGor R3;                 // Дополнительный реостат
    public Rotator LLR;                  // Регулятор напряжения питания (аналог изменения скорости)

    [Header("Автоматы (ключи)")]
    public Switch Q1;                    // Ввод питания
    public Switch Q2;                    // Цепь якоря
    public Switch Q3;                    // Цепь возбуждения

    [Header("Измерительные приборы (аналоговые)")]
    public Meter Pv1;                    // Вольтметр на входе (напряжение питания)
    public Meter Pv2;                    // Вольтметр на выходе генератора U
    public Meter Pa1;                    // Амперметр в цепи якоря I_a
    public Meter Pa2;                    // Амперметр в цепи возбуждения I_f (мА)
    public Meter Pa3;                    // Амперметр в цепи нагрузки

    [Header("Информационные дисплеи (цифровые)")]
    public Meter info_Pv1;
    public Meter info_Pv2;
    public Meter info_Pa1;
    public Meter info_Pa2;
    public Meter info_Pa3;
    public TMP_Text tvInfoText;

    [Header("Приводной двигатель")]
    public Lab3Motor Motor;

    [Header("Паспортные данные генератора (из расчётной модели)")]
    [SerializeField] private float nominalVoltage = 220f;          // U_ном, В
    [SerializeField] private float nominalSpeed = 1500f;           // n_ном, об/мин

    // Базовое положение стрелок (выключено)
    private readonly Vector3 offEuler = new Vector3(-180f, 90f, -50f);

    // Целевые углы для стрелок
    private Vector3 targetEuler_Pv1, targetEuler_Pv2, targetEuler_Pa1, targetEuler_Pa2, targetEuler_Pa3;

    // Измеренные величины
    private float U_Pv1;          // Напряжение питания (аналог ЭДС)
    private float U_Pv2;          // Напряжение на зажимах генератора
    private float A_Pa1;          // Ток якоря I_a, А
    private float A_Pa2;          // Ток возбуждения I_в, А
    private float A_Pa3;          // Ток нагрузки, А
    private float E_emf;          // ЭДС генератора (добавлено)

    // Положения регуляторов (0-100%)
    private float R1_value;       // Сопротивление в цепи возбуждения
    private float R2_value;       // Сопротивление нагрузки
    private float R3_value;       // Доп. реостат
    private float LLR_value;      // Напряжение питания (скорость)

    private float RPM;            // Текущая скорость вращения
    private bool engineIsOn;      // Работает ли привод


    // Сохранённые данные для характеристик
    private List<Vector2> noLoadData = new List<Vector2>();      // (I_в, E)
    private List<Vector2> loadData = new List<Vector2>();        // (I_в, U) при I_a = const
    private List<Vector2> externalData = new List<Vector2>();    // (I_a, U) при I_в = const
    private List<Vector2> regulatingData = new List<Vector2>();  // (I_a, I_в) при U = const
    private List<Vector2> shortCircuitData = new List<Vector2>(); // (I_в, I_к) при U = 0

    private bool isShortCircuitMode = false;   // Режим короткого замыкания

    // ============ ПУБЛИЧНЫЕ СВОЙСТВА ============

    public float PV1Value => U_Pv1;
    public float PV2Value => U_Pv2;
    public float PA1Value => A_Pa1;
    public float PA2Value => A_Pa2;
    public float PA3Value => A_Pa3;
    public float PA2ValueMilliAmp => A_Pa2 * 1000f;
    public float PA3ValueMilliAmp => A_Pa3 * 1000f;
    public float RPMValue => RPM;
    public float LLRValue => LLR_value;
    public float R1Percent => R1_value;
    public float R2Percent => R2_value;
    public float R3Percent => R3_value;

    public bool Q1Enabled => Q1 != null && Q1.isOn;
    public bool Q2Enabled => Q2 != null && Q2.isOn;
    public bool Q3Enabled => Q3 != null && Q3.isOn;
    public bool EngineIsOn => engineIsOn;

    public float NominalVoltage => nominalVoltage;
    public float NominalSpeed => nominalSpeed;

    public bool IsShortCircuitMode => isShortCircuitMode;

    // ============ ОСНОВНЫЕ МЕТОДЫ ============

    private void Start()
    {
        // Подписка на события
        if (R1 != null) R1.OnValueChanged += OnR1Changed;
        if (R2 != null) R2.OnValueChanged += OnR2Changed;
        if (R3 != null) R3.OnValueChanged += OnR3Changed;
        if (LLR != null) LLR.OnValueChanged += OnLLRChanged;

        if (Q1 != null) Q1.OnValueChanged += OnQ1Changed;
        if (Q2 != null) Q2.OnValueChanged += OnQ2Changed;
        if (Q3 != null) Q3.OnValueChanged += OnQ3Changed;

        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Playing);

        RefreshCircuit();

        // Инициализация стрелок в нулевое положение (без анимации)
        var initRot = Quaternion.Euler(offEuler);
        if (Pv1 != null) Pv1.transform.localRotation = initRot;
        if (Pv2 != null) Pv2.transform.localRotation = initRot;
        if (Pa1 != null) Pa1.transform.localRotation = initRot;
        if (Pa2 != null) Pa2.transform.localRotation = initRot;
        if (Pa3 != null) Pa3.transform.localRotation = initRot;

        Debug.Log("=== Генератор постоянного тока независимого возбуждения ===");
        var (Unom, Ianom, IInom, nnom) = Lab3_CoeffCalculation.GetNominalParameters();
        Debug.Log($"U_ном = {Unom} В, I_аном = {Ianom} А, I_вном = {IInom} А, n_ном = {nnom} об/мин");
        Debug.Log($"R_я = {Lab3_CoeffCalculation.GetArmatureResistance()} Ом (при 75°C)");
    }

    /// Полный сброс схемы
    public void ResetCircuit()
    {
        R1_value = 0f;
        R2_value = 0f;
        R3_value = 0f;
        LLR_value = 0f;

        U_Pv1 = 0f;
        U_Pv2 = 0f;
        A_Pa1 = 0f;
        A_Pa2 = 0f;
        A_Pa3 = 0f;
        RPM = 0f;
        engineIsOn = false;
        isShortCircuitMode = false;

        // Сброс данных характеристик
        noLoadData.Clear();
        loadData.Clear();
        externalData.Clear();
        regulatingData.Clear();
        shortCircuitData.Clear();

        RefreshCircuit();
        Debug.Log("Схема сброшена в исходное состояние");
    }

    /// Аварийное отключение (отключает все автоматы)
    public void EmergencyStop()
    {
        if (Q1 != null && Q1.isOn) SetSwitchState(Q1, false);
        if (Q2 != null && Q2.isOn) SetSwitchState(Q2, false);
        if (Q3 != null && Q3.isOn) SetSwitchState(Q3, false);

        isShortCircuitMode = false;
        RefreshCircuit();
        Debug.LogWarning("!!! АВАРИЙНОЕ ОТКЛЮЧЕНИЕ !!! Автоматы Q1, Q2, Q3 выключены");
    }


    /// Включение режима короткого замыкания (ХКЗ)
    public void EnableShortCircuitMode()
    {
        if (Q1 == null || Q2 == null || Q3 == null) return;

        // Условия для ХКЗ по методике: U = 0, якорь закорочен
        SetSwitchState(Q1, true);           // Питание привода
        SetSwitchState(Q2, true);           // Цепь якоря замкнута
        SetSwitchState(Q3, true);           // Цепь возбуждения включена

        // Устанавливаем нагрузку на минимум (короткое замыкание)
        if (R2 != null)
        {
            // Вариант: прямой вызов обработчика (если R2_value - это поле)
            R2_value = 100f;  // Устанавливаем значение поля
            OnR2Changed(R2_value);  // Вызываем обработчик вручную
        }

        isShortCircuitMode = true;
        RefreshCircuit();
        Debug.Log("=== Режим короткого замыкания (ХКЗ) ===");
        Debug.Log("Снимаем зависимость I_к = f(I_в) при U = 0");
    }

    /// Выход из режима короткого замыкания
    public void DisableShortCircuitMode()
    {
        isShortCircuitMode = false;
        RefreshCircuit();
        Debug.Log("Выход из режима короткого замыкания");
    }


    /// Запись точки характеристики холостого хода (E = f(I_в) при I_a = 0)

    public void RecordNoLoadPoint()
    {
        if (!engineIsOn)
        {
            Debug.LogWarning("Двигатель не вращается. Невозможно снять ХХХ");
            return;
        }

        if (Q2 != null && Q2.isOn)
        {
            Debug.LogWarning("Для ХХХ цепь якоря должна быть разомкнута (Q2 выключен)");
            return;
        }

        float fieldCurrent = A_Pa2;
        float emf = U_Pv2;  // При I_a = 0, U = E

        noLoadData.Add(new Vector2(fieldCurrent, emf));
        Debug.Log($"ХХХ: I_в = {fieldCurrent:F3} А, E = {emf:F1} В");
    }


    /// Запись точки нагрузочной характеристики (U = f(I_в) при I_a = const)
    public void RecordLoadPoint()
    {
        if (!engineIsOn)
        {
            Debug.LogWarning("Двигатель не вращается");
            return;
        }

        loadData.Add(new Vector2(A_Pa2, U_Pv2));
        Debug.Log($"НХ (I_a = {A_Pa1:F2} А): I_в = {A_Pa2:F3} А, U = {U_Pv2:F1} В");
    }

    /// Запись точки внешней характеристики (U = f(I_a) при I_в = const)
    public void RecordExternalPoint()
    {
        externalData.Add(new Vector2(A_Pa1, U_Pv2));
        Debug.Log($"Внешняя х-ка (I_в = {A_Pa2:F3} А): I_a = {A_Pa1:F2} А, U = {U_Pv2:F1} В");
    }

    /// <summary>
    /// Запись точки регулировочной характеристики (I_в = f(I_a) при U = const)
    /// </summary>
    public void RecordRegulatingPoint()
    {
        regulatingData.Add(new Vector2(A_Pa1, A_Pa2));
        Debug.Log($"Регулировочная х-ка (U = {U_Pv2:F1} В): I_a = {A_Pa1:F2} А, I_в = {A_Pa2:F3} А");
    }

    /// Запись точки характеристики короткого замыкания (I_к = f(I_в) при U = 0)

    public void RecordShortCircuitPoint()
    {
        if (!isShortCircuitMode)
        {
            Debug.LogWarning("Режим короткого замыкания не активирован. Используйте EnableShortCircuitMode()");
            return;
        }

        shortCircuitData.Add(new Vector2(A_Pa2, A_Pa1));
        Debug.Log($"ХКЗ: I_в = {A_Pa2:F3} А, I_к = {A_Pa1:F2} А");
    }


    /// Очистка всех записанных данных характеристик

    public void ClearAllCharacteristicData()
    {
        noLoadData.Clear();
        loadData.Clear();
        externalData.Clear();
        regulatingData.Clear();
        shortCircuitData.Clear();
        Debug.Log("Все данные характеристик очищены");
    }

 
    /// Получение копии данных ХХХ

    public List<Vector2> GetNoLoadData() => new List<Vector2>(noLoadData);


    /// Получение копии данных ХКЗ

    public List<Vector2> GetShortCircuitData() => new List<Vector2>(shortCircuitData);

    /// <summary>
    /// Расчёт коэффициента насыщения по формуле R_u = F_0 / F_0*
    /// </summary>
    public float GetSaturationFactor()
    {
        if (noLoadData.Count < 2)
        {
            Debug.LogWarning("Недостаточно данных ХХХ для расчёта коэффициента насыщения");
            return 0f;
        }

        // Находим точку на ХХХ, соответствующую номинальному напряжению
        Vector2? nominalPoint = null;
        foreach (var point in noLoadData)
        {
            if (point.y >= nominalVoltage)
            {
                nominalPoint = point;
                break;
            }
        }

        if (!nominalPoint.HasValue)
        {
            Debug.LogWarning("На ХХХ нет точки с напряжением >= номинального");
            return 0f;
        }

        float If_nominal = nominalPoint.Value.x;

        // Строим прямую ОВ (воздушный зазор) через начало координат и точку насыщения
        // Для упрощения: используем первую точку ХХХ для определения наклона прямой
        Vector2 firstPoint = noLoadData[0];
        float slope = firstPoint.y / firstPoint.x;  // E / I_в

        // Находим F_0* (точка B) — ток возбуждения для номинальной ЭДС по прямой воздушного зазора
        float If_airGap = nominalVoltage / slope;

        // Коэффициент насыщения
        float saturationFactor = If_nominal / If_airGap;

        Debug.Log($"Коэффициент насыщения: R_u = {saturationFactor:F3}");
        Debug.Log($"  F_0 (I_в при U_ном) = {If_nominal:F3} А");
        Debug.Log($"  F_0* (по прямой возд. зазора) = {If_airGap:F3} А");

        string saturationLevel;
        if (saturationFactor < 1.25f) saturationLevel = "слабонасыщенная";
        else if (saturationFactor < 1.66f) saturationLevel = "средненасыщенная";
        else saturationLevel = "сильнонасыщенная";
        Debug.Log($"  Оценка: {saturationLevel} машина");

        return saturationFactor;
    }

    /// <summary>
    /// Расчёт характеристического треугольника и размагничивающего действия реакции якоря
    /// </summary>
    public float GetDemagnetizingEffect(float armatureCurrent)
    {
        if (noLoadData.Count < 2)
        {
            Debug.LogWarning("Недостаточно данных ХХХ");
            return 0f;
        }

        // Падение напряжения в якоре: ΔU = I_a * R_a (R_a = 12.5 Ом из расчётной модели)
        float voltageDrop = armatureCurrent * Lab3_CoeffCalculation.GetArmatureResistance();

        // По ХХХ находим, на сколько нужно увеличить I_в для компенсации падения напряжения
        // Упрощённо: используем среднюю крутизну ХХХ в рабочей точке
        float avgSlope = 0f;
        for (int i = 1; i < noLoadData.Count; i++)
        {
            avgSlope += (noLoadData[i].y - noLoadData[i - 1].y) / (noLoadData[i].x - noLoadData[i - 1].x);
        }
        avgSlope /= (noLoadData.Count - 1);

        float deltaIf = voltageDrop / avgSlope;

        Debug.Log($"Характеристический треугольник при I_a = {armatureCurrent:F2} А:");
        Debug.Log($"  Катет BC (ΔU) = {voltageDrop:F2} В");
        Debug.Log($"  Катет AB (размагничивание) = {deltaIf:F3} А");

        return deltaIf;
    }

    /// <summary>
    /// Расчёт процентного снижения напряжения (формула 8 из методики)
    /// </summary>
    public float GetVoltageDropPercent()
    {
        // Нужно найти U_0 (напряжение при I_a = 0) из внешней характеристики
        float U0 = 0f;
        foreach (var point in externalData)
        {
            if (point.x < 0.01f)  // I_a ≈ 0
            {
                U0 = point.y;
                break;
            }
        }

        if (U0 == 0f && externalData.Count > 0)
        {
            // Экстраполяция
            U0 = externalData[0].y * 1.05f;
        }

        float Un = nominalVoltage;
        float deltaU = (U0 - Un) / Un * 100f;

        Debug.Log($"Процентное снижение напряжения: ΔU% = {deltaU:F1}%");
        Debug.Log($"  U_0 = {U0:F1} В, U_ном = {Un:F1} В");

        return deltaU;
    }

    /// <summary>
    /// Экспорт всех характеристик в CSV-формат
    /// </summary>
    public string ExportCharacteristicsToCSV()
    {
        string csv = "=== ХАРАКТЕРИСТИКА ХОЛОСТОГО ХОДА ===\n";
        csv += "I_в,А;E,В\n";
        foreach (var p in noLoadData)
            csv += $"{p.x:F4};{p.y:F2}\n";

        csv += "\n=== ХАРАКТЕРИСТИКА КОРОТКОГО ЗАМЫКАНИЯ ===\n";
        csv += "I_в,А;I_к,А\n";
        foreach (var p in shortCircuitData)
            csv += $"{p.x:F4};{p.y:F2}\n";

        csv += "\n=== ВНЕШНЯЯ ХАРАКТЕРИСТИКА ===\n";
        csv += "I_a,А;U,В\n";
        foreach (var p in externalData)
            csv += $"{p.x:F2};{p.y:F2}\n";

        csv += "\n=== РЕГУЛИРОВОЧНАЯ ХАРАКТЕРИСТИКА ===\n";
        csv += "I_a,А;I_в,А\n";
        foreach (var p in regulatingData)
            csv += $"{p.x:F2};{p.y:F4}\n";

        return csv;
    }

    /// <summary>
    /// Получение полного снимка состояния схемы
    /// </summary>
    public Lab3_CircuitSnapshot GetSnapshot()
    {
        Lab3_CircuitSnapshot snapshot = new Lab3_CircuitSnapshot();
        snapshot.r1Percent = R1_value;
        snapshot.r2Percent = R2_value;
        snapshot.r3Percent = R3_value;
        snapshot.q1Enabled = Q1 != null && Q1.isOn;
        snapshot.q2Enabled = Q2 != null && Q2.isOn;
        snapshot.q3Enabled = Q3 != null && Q3.isOn;
        snapshot.pv1Voltage = U_Pv1;
        snapshot.pv2Voltage = U_Pv2;
        snapshot.pa1Current = A_Pa1;

        // ИСПРАВЛЕНО: используем правильные имена полей из Lab3_CircuitSnapshot
        snapshot.pa2CurrentMilliAmp = A_Pa2 * 1000f;  // было snapshot.pa2Current
        snapshot.pa3CurrentMilliAmp = A_Pa3 * 1000f;  // было snapshot.pa3Current

        snapshot.rpm = RPM;
        return snapshot;
    }
    private void Update()
    {
        float speed = Time.deltaTime * 5f;
        if (Pv1 != null) Pv1.transform.localRotation = Quaternion.Slerp(Pv1.transform.localRotation, Quaternion.Euler(targetEuler_Pv1), speed);
        if (Pv2 != null) Pv2.transform.localRotation = Quaternion.Slerp(Pv2.transform.localRotation, Quaternion.Euler(targetEuler_Pv2), speed);
        if (Pa1 != null) Pa1.transform.localRotation = Quaternion.Slerp(Pa1.transform.localRotation, Quaternion.Euler(targetEuler_Pa1), speed);
        if (Pa2 != null) Pa2.transform.localRotation = Quaternion.Slerp(Pa2.transform.localRotation, Quaternion.Euler(targetEuler_Pa2), speed);
        if (Pa3 != null) Pa3.transform.localRotation = Quaternion.Slerp(Pa3.transform.localRotation, Quaternion.Euler(targetEuler_Pa3), speed);
    }
    // ============ ПРИВАТНЫЕ МЕТОДЫ ============

    private void SetSwitchState(Switch sw, bool state)
    {
        if (sw == null) return;

        sw.isOn = state;

        Quaternion targetRot = Quaternion.Euler(state ? sw.onEuler : sw.offEuler);
        sw.transform.localRotation = targetRot;

        Color targetColor = state ? Color.green : Color.red;
        Renderer swRenderer = sw.GetComponent<Renderer>();
        if (swRenderer != null)
            swRenderer.material.color = targetColor;

        if (sw.circleObject != null)
        {
            Renderer circleRenderer = sw.circleObject.GetComponent<Renderer>();
            if (circleRenderer != null)
                circleRenderer.material.color = targetColor;
        }
    }

    private void RefreshCircuit()
    {
        CheckEngine();
        RecalculateState();
        ApplyInfoMeters();
        UpdateMeterTargetAngles();
    }

    private void CheckEngine()
    {
        bool q1State = Q1 != null && Q1.isOn;
        bool q2State = Q2 != null && Q2.isOn;

        // Для короткого замыкания двигатель может работать при Q2 включённом
        if (isShortCircuitMode)
            engineIsOn = q1State && LLR_value > 5f;
        else
            engineIsOn = q1State && q2State && LLR_value > 20f;
    }

    private void RecalculateState()
    {
        // Обновляем U_Pv1 для обоих режимов
        U_Pv1 = Mathf.Lerp(0f, 420f, LLR_value / 100f);

        // Нормальный режим работы генератора - ИСПРАВЛЕННЫЙ ВЫЗОВ
        // Теперь с 6 выходными параметрами: Ia, If, Iload, U, E, RPM
        // Нормальный режим работы генератора
        Lab3_CoeffCalculation.Simulate(
            Q1 != null && Q1.isOn,
            Q2 != null && Q2.isOn,
            Q3 != null && Q3.isOn,
            engineIsOn,
            U_Pv1,
            R1_value,
            R2_value,
            R3_value,
            out A_Pa1,
            out A_Pa2,
            out A_Pa3,
            out U_Pv2,
            out E_emf,      // используем поле класса
            out RPM
        );

        // Проверка баланса мощностей (диагностика)
        float p1d = U_Pv1 * (A_Pa1 + A_Pa2);
        float p2g = U_Pv2 * A_Pa3;

        if (p2g > p1d + 0.1f && engineIsOn && !isShortCircuitMode)
        {
            Debug.LogWarning($"Баланс мощностей нарушен: P2 = {p2g:F2} > P1 = {p1d:F2}");
        }

        if (Motor != null)
            Motor.TargetRPM = RPM;
    }

    private void ApplyInfoMeters()
    {
        if (Pv1 != null) Pv1.current = U_Pv1;
        if (Pv2 != null) Pv2.current = U_Pv2;
        if (Pa1 != null) Pa1.current = A_Pa1;
        if (Pa2 != null) Pa2.current = A_Pa2 * 1000f;
        if (Pa3 != null) Pa3.current = A_Pa3 * 1000f;
        if (info_Pa1 != null) info_Pa1.current = A_Pa1;
        if (info_Pa2 != null) info_Pa2.current = A_Pa2 * 1000f;
        if (info_Pa3 != null) info_Pa3.current = A_Pa3 * 1000f;
        if (info_Pv1 != null) info_Pv1.current = U_Pv1;
        if (info_Pv2 != null) info_Pv2.current = U_Pv2;
        if (tvInfoText != null)
            tvInfoText.text = $"n = {RPM:F0} об/мин\nLLR = {LLR_value:F0}%";
    }

    private void UpdateMeterTargetAngles()
    {
        bool q1 = Q1 != null && Q1.isOn;
        bool q2 = Q2 != null && Q2.isOn;
        bool q3 = Q3 != null && Q3.isOn;
        bool circuitActive = q1 && q2 && engineIsOn && !isShortCircuitMode;
        bool shortCircuitActive = isShortCircuitMode && q1 && q2;

        targetEuler_Pv1 = q1 ? BuildMeterAngle(U_Pv1, 450f) : offEuler;
        targetEuler_Pv2 = (circuitActive || shortCircuitActive) ? BuildMeterAngle(U_Pv2, 300f) : offEuler;
        targetEuler_Pa1 = (circuitActive || shortCircuitActive) ? BuildMeterAngle(A_Pa1, 15f) : offEuler;
        targetEuler_Pa2 = ((circuitActive || shortCircuitActive) && q3) ? BuildMeterAngle(A_Pa2 * 1000f, 300f) : offEuler;
        targetEuler_Pa3 = circuitActive ? BuildMeterAngle(A_Pa3 * 1000f, 300f) : offEuler;
    }

    private Vector3 BuildMeterAngle(float currentValue, float maxValue)
    {
        float angle = Mathf.Lerp(-49f, -131f, Mathf.Clamp01(currentValue / maxValue));
        return new Vector3(-180f, 90f, angle);
    }

   
    // ============ ОБРАБОТЧИКИ ============

    private void OnR1Changed(float percent)
    {
        R1_value = percent;
        RefreshCircuit();
        Debug.Log($"R1 (реостат возбуждения): {R1_value:F0}% -> I_в = {A_Pa2 * 1000f:F1} мА");
    }

    private void OnR2Changed(float percent)
    {
        R2_value = percent;
        RefreshCircuit();
        if (!isShortCircuitMode)
            Debug.Log($"R2 (нагрузка): {R2_value:F0}% -> I_a = {A_Pa1:F2} А, U = {U_Pv2:F1} В");
        else
            Debug.Log($"R2 (режим КЗ): {R2_value:F0}% -> I_к = {A_Pa1:F2} А");
    }

    private void OnR3Changed(float value)
    {
        R3_value = value;
        RefreshCircuit();
        Debug.Log($"R3 (доп. реостат): {R3_value:F0}%");
    }

    private void OnLLRChanged(float value)
    {
        LLR_value = value;
        RefreshCircuit();
        float supplyVoltage = Mathf.Lerp(0f, 420f, value / 100f);
        float speed = Mathf.Lerp(0f, nominalSpeed, value / 100f);
        Debug.Log($"LLR: {value:F0}% ({supplyVoltage:F0} В, n = {speed:F0} об/мин)");
    }

    private void OnQ1Changed(bool value)
    {
        if (value && isShortCircuitMode)
        {
            // При включении Q1 в режиме КЗ автоматически включаем Q2 и Q3
            if (Q2 != null && !Q2.isOn) SetSwitchState(Q2, true);
            if (Q3 != null && !Q3.isOn) SetSwitchState(Q3, true);
        }
        RefreshCircuit();
        Debug.Log(value ? "Q1 ВКЛЮЧЕН - Питание привода подано" : "Q1 ВЫКЛЮЧЕН - Привод остановлен");
    }

    private void OnQ2Changed(bool value)
    {
        RefreshCircuit();
        if (!isShortCircuitMode)
            Debug.Log(value ? "Q2 ВКЛЮЧЕН - Цепь якоря замкнута" : "Q2 ВЫКЛЮЧЕН - Цепь якоря разомкнута (I_a = 0)");
        else
            Debug.Log(value ? "Q2 ВКЛЮЧЕН - Якорь закорочен (режим КЗ)" : "Q2 ВЫКЛЮЧЕН - Короткое замыкание снято");
    }

    private void OnQ3Changed(bool value)
    {
        RefreshCircuit();
        Debug.Log(value ? "Q3 ВКЛЮЧЕН - Цепь возбуждения под напряжением" : "Q3 ВЫКЛЮЧЕН - Ток возбуждения = 0");
    }

    private void OnDisable()
    {
        if (R1 != null) R1.OnValueChanged -= OnR1Changed;
        if (R2 != null) R2.OnValueChanged -= OnR2Changed;
        if (R3 != null) R3.OnValueChanged -= OnR3Changed;
        if (LLR != null) LLR.OnValueChanged -= OnLLRChanged;
        if (Q1 != null) Q1.OnValueChanged -= OnQ1Changed;
        if (Q2 != null) Q2.OnValueChanged -= OnQ2Changed;
        if (Q3 != null) Q3.OnValueChanged -= OnQ3Changed;
    }
}
