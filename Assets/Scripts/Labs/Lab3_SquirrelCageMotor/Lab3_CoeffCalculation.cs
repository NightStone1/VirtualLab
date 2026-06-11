using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Расчётная модель генератора постоянного тока независимого возбуждения
/// Полное соответствие лабораторной работе №1 (01.11)
/// 
/// Основные характеристики:
/// - Характеристика холостого хода (ХХХ): E = f(I_в) при I_a = 0, n = const
/// - Нагрузочная характеристика: U = f(I_в) при I_a = const, n = const
/// - Внешняя характеристика: U = f(I_a) при I_в = const, n = const
/// - Регулировочная характеристика: I_в = f(I_a) при U = const, n = const
/// - Характеристика короткого замыкания (ХКЗ): I_к = f(I_в) при U = 0, n = const
/// </summary>
public class Lab3_CoeffCalculation : MonoBehaviour
{
    // ============ ПАСПОРТНЫЕ ДАННЫЕ ГЕНЕРАТОРА (по табл. 1.1-1.6) ============

    [Header("Паспортные данные генератора")]
    [SerializeField] private float nominalVoltage = 220f;        // U_ном, В
    [SerializeField] private float nominalArmatureCurrent = 2.0f; // I_аном, А
    [SerializeField] private float nominalFieldCurrent = 0.095f;  // I_вном, А (95 мА)
    [SerializeField] private float nominalRpm = 1500f;            // n_ном, об/мин
    [SerializeField] private float armatureResistance = 12.5f;    // R_я, Ом (при 75°C)
    [SerializeField] private float fieldResistance = 2200f;       // R_в, Ом (при 75°C)

    [Header("Параметры кривой намагничивания")]
    [SerializeField] private float residualVoltage = 8f;          // Остаточная ЭДС, В (3-5% от U_ном)
    [SerializeField] private float saturationStartIf = 0.06f;     // Начало насыщения, А
    [SerializeField] private float saturationStartEmf = 180f;     // Напряжение начала насыщения, В
    [SerializeField] private float saturationFactor = 1.4f;       // Коэффициент насыщения R_u (1.25-1.66)

    [Header("Параметры реакции якоря")]
    [SerializeField] private float armatureReactionCoeff = 0.025f; // Коэффициент размагничивания (AB/BC в хар. треугольнике)

    // ============ КОНСТАНТЫ ДЛЯ РАСЧЁТОВ ============
    private const float MIN_FIELD_CURRENT = 0.001f;     // Минимальный ток возбуждения, А
    private const float MAX_FIELD_CURRENT = 0.25f;      // Максимальный ток возбуждения, А
    private const float MAX_ARM_CURRENT = 5f;           // Максимальный ток якоря, А
    private const float MIN_RPM = 200f;                 // Минимальная скорость, об/мин
    private const float MAX_RPM = 2800f;                // Максимальная скорость, об/мин

    // Кэш для характеристики холостого хода (ускоряет расчёты)
    private static List<Vector2> cachedNoLoadCurve;

    // ============ ОСНОВНОЙ МЕТОД СИМУЛЯЦИИ ============

    /// <summary>
    /// Полная симуляция работы генератора постоянного тока независимого возбуждения
    /// Соответствует схеме рис. 1.1
    /// </summary>
    /// <param name="Q1">Главный автомат (питание привода)</param>
    /// <param name="Q2">Автомат цепи якоря (подключение нагрузки)</param>
    /// <param name="Q3">Автомат цепи возбуждения (питание ОВ)</param>
    /// <param name="engineIsOn">Приводной двигатель вращается</param>
    /// <param name="supplyVoltage">Напряжение питания привода (380В номинал)</param>
    /// <param name="R1Percent">Реостат возбуждения (0-100%, 0=макс ток)</param>
    /// <param name="R2Percent">Нагрузочный реостат (0-100%, 0=макс нагрузка)</param>
    /// <param name="R3Percent">Доп. реостат (не используется в ЛР1)</param>
    /// <param name="Ia">Ток якоря I_a, А (PA1)</param>
    /// <param name="If">Ток возбуждения I_в, А (PA2)</param>
    /// <param name="Iload">Ток нагрузки I_нагр, А (PA3)</param>
    /// <param name="U">Напряжение на зажимах U, В (PV2)</param>
    /// <param name="E">ЭДС генератора E, В</param>
    /// <param name="RPM">Скорость вращения, об/мин</param>
    public static void Simulate(
        bool Q1, bool Q2, bool Q3, bool engineIsOn,
        float supplyVoltage, float R1Percent, float R2Percent, float R3Percent,
        out float Ia, out float If, out float Iload, out float U, out float E, out float RPM)
    {
        // Инициализация
        Ia = 0f;
        If = 0f;
        Iload = 0f;
        U = 0f;
        E = 0f;
        RPM = 0f;

        // Проверка питания привода
        if (!Q1 || !engineIsOn)
            return;

        // Нормализация входных параметров
        float r1Norm = Mathf.Clamp01(R1Percent / 100f);  // r1=0 → If max, r1=1 → If min
        float r2Norm = Mathf.Clamp01(R2Percent / 100f);  // r2=0 → нагрузка max, r2=1 → нагрузка min
        float supplyNorm = Mathf.Clamp(supplyVoltage, 300f, 420f);

        // Расчёт скорости вращения (асинхронный привод)
        RPM = CalculateRpm(supplyNorm);

        // Расчёт тока возбуждения (цепь ОВ)
        If = CalculateFieldCurrent(Q3, r1Norm, supplyNorm);

        // Расчёт ЭДС по характеристике холостого хода
        E = CalculateEmf(If, RPM);

        // Проверка режима короткого замыкания (U = 0)
        bool isShortCircuit = (R2Percent >= 99f) && Q2;

        if (!Q2)
        {
            // Режим холостого хода (I_a = 0) - для снятия ХХХ
            U = E;
            Ia = 0f;
            Iload = 0f;
        }
        else if (isShortCircuit)
        {
            // Режим короткого замыкания (ХКЗ) - U = 0
            SimulateShortCircuit(If, RPM, out Ia, out U);
            Iload = Ia;
        }
        else
        {
            // Нормальный нагрузочный режим
            SimulateLoadMode(If, RPM, r2Norm, Q2, Q3, out Ia, out Iload, out U, ref E);
        }

        // Применение физических ограничений
        ApplyLimits(ref Ia, ref If, ref Iload, ref U, ref E, ref RPM);
    }

    // ============ РАСЧЁТ ОТДЕЛЬНЫХ КОМПОНЕНТОВ ============

    /// <summary>
    /// Расчёт скорости вращения от напряжения питания
    /// Асинхронный двигатель с короткозамкнутым ротором
    /// </summary>
    private static float CalculateRpm(float supplyVoltage)
    {
        // Номинальная скорость при 380В
        const float nominalSupply = 380f;
        const float nominalRpm = 1500f;

        float rpm = nominalRpm * (supplyVoltage / nominalSupply);

        // Учёт скольжения (3-5%)
        float slip = 0.04f;
        rpm = rpm * (1f - slip);

        return Mathf.Clamp(rpm, MIN_RPM, MAX_RPM);
    }

    /// <summary>
    /// Расчёт тока возбуждения с учётом положения реостата R1
    /// I_в = U_в / (R_в + R_реостат)
    /// </summary>
    private static float CalculateFieldCurrent(bool Q3, float r1Norm, float supplyVoltage)
    {
        if (!Q3)
            return 0f;  // Цепь возбуждения разомкнута

        // Напряжение питания цепи возбуждения (обычно 220В от отдельного источника)
        float excitationVoltage = 220f;

        // Сопротивление реостата (0-100% → R_reo = 0 до 5000 Ом)
        float reostatResistance = r1Norm * 5000f;

        // Полное сопротивление цепи возбуждения
        float totalResistance = 2200f + reostatResistance;

        // Ток возбуждения
        float fieldCurrent = excitationVoltage / totalResistance;

        return Mathf.Clamp(fieldCurrent, MIN_FIELD_CURRENT, MAX_FIELD_CURRENT);
    }

    /// <summary>
    /// Расчёт ЭДС по характеристике холостого хода (кривая намагничивания)
    /// Соответствует рис. 1.2
    /// </summary>
    private static float CalculateEmf(float fieldCurrent, float rpm)
    {
        if (fieldCurrent <= 0f)
            return 8f;  // Остаточная ЭДС

        // Приведение к номинальной скорости
        float speedFactor = rpm / 1500f;

        // Нормированный ток возбуждения
        float ifNorm = fieldCurrent / 0.095f;

        float emfNorm;

        // Характеристика холостого хода (кривая насыщения)
        if (ifNorm <= 0.6f)
        {
            // Линейная часть (воздушный зазор)
            emfNorm = ifNorm * 1.2f;
        }
        else if (ifNorm <= 1.0f)
        {
            // Переходная область (колено)
            float t = (ifNorm - 0.6f) / 0.4f;
            emfNorm = 0.72f + t * 0.38f;
        }
        else if (ifNorm <= 1.5f)
        {
            // Область насыщения
            float t = (ifNorm - 1.0f) / 0.5f;
            emfNorm = 1.1f + t * 0.1f;
        }
        else
        {
            // Глубокое насыщение
            emfNorm = 1.2f;
        }

        float emf = 220f * emfNorm * speedFactor;

        // Остаточная ЭДС при малых токах
        if (fieldCurrent < 0.01f)
            emf = Mathf.Max(emf, 8f);

        return emf;
    }

    /// <summary>
    /// Режим короткого замыкания (ХКЗ)
    /// Снимается при закороченной якорной цепи и U = 0
    /// Таблица 1.6
    /// </summary>
    private static void SimulateShortCircuit(float fieldCurrent, float rpm, out float Ia, out float U)
    {
        // ЭДС при данном токе возбуждения
        float emf = CalculateEmf(fieldCurrent, rpm);

        // Ток короткого замыкания: I_к = E / R_я
        Ia = emf / 12.5f;

        // Напряжение равно нулю (якорь закорочен)
        U = 0f;

        // Ограничение по току
        Ia = Mathf.Clamp(Ia, 0f, MAX_ARM_CURRENT);
    }

    /// <summary>
    /// Расчёт нагрузочного режима с учётом:
    /// - Падения напряжения в якоре I_a * R_я
    /// - Реакции якоря (размагничивание)
    /// </summary>
    private static void SimulateLoadMode(float fieldCurrent, float rpm, float r2Norm,
        bool Q2, bool Q3, out float Ia, out float Iload, out float U, ref float E)
    {
        Ia = 0f;
        Iload = 0f;
        U = 0f;

        if (!Q2)
        {
            // Холостой ход
            U = E;
            return;
        }

        // Сопротивление нагрузки (0-100% → R_нагр от 0 до 200 Ом)
        float loadResistance = r2Norm * 200f + 0.1f;

        // Итерационный расчёт (учитывает реакцию якоря и падение напряжения)
        float previousU = E;

        for (int iter = 0; iter < 10; iter++)
        {
            // Ток якоря
            Ia = previousU / loadResistance;
            Ia = Mathf.Clamp(Ia, 0f, MAX_ARM_CURRENT);
            Iload = Ia;

            // Реакция якоря (размагничивание) - пропорциональна току якоря
            float demagnetizingCurrent = Ia * 0.025f;
            float effectiveFieldCurrent = Mathf.Max(MIN_FIELD_CURRENT, fieldCurrent - demagnetizingCurrent);

            // Пересчёт ЭДС с учётом реакции якоря
            float adjustedEmf = CalculateEmf(effectiveFieldCurrent, rpm);
            E = adjustedEmf;

            // Падение напряжения в якоре
            float voltageDrop = Ia * 12.5f;

            // Напряжение на зажимах
            U = adjustedEmf - voltageDrop;
            U = Mathf.Max(0f, U);

            // Проверка сходимости
            if (Mathf.Abs(U - previousU) < 0.1f)
                break;

            previousU = U;
        }
    }

    /// <summary>
    /// Применение физических ограничений
    /// </summary>
    private static void ApplyLimits(ref float Ia, ref float If, ref float Iload,
        ref float U, ref float E, ref float RPM)
    {
        Ia = Mathf.Clamp(Ia, 0f, MAX_ARM_CURRENT);
        If = Mathf.Clamp(If, MIN_FIELD_CURRENT, MAX_FIELD_CURRENT);
        Iload = Mathf.Clamp(Iload, 0f, MAX_ARM_CURRENT);
        U = Mathf.Clamp(U, 0f, 250f);
        E = Mathf.Clamp(E, 0f, 280f);
        RPM = Mathf.Clamp(RPM, MIN_RPM, MAX_RPM);
    }

    // ============ МЕТОДЫ ДЛЯ ПОСТРОЕНИЯ ХАРАКТЕРИСТИК ============

    /// <summary>
    /// Получение характеристики холостого хода (XXX)
    /// Таблица 1.2, рис. 1.2
    /// </summary>
    /// <returns>Список точек (I_в, А; E, В)</returns>
    public static List<Vector2> GetNoLoadCharacteristic()
    {
        List<Vector2> points = new List<Vector2>();

        // Ток возбуждения от 0 до 1.5 I_в_ном
        for (float ifValue = 0; ifValue <= 0.15f; ifValue += 0.005f)
        {
            float emf = CalculateEmf(ifValue, 1500f);
            points.Add(new Vector2(ifValue, emf));
        }

        return points;
    }

    /// <summary>
    /// Получение характеристики короткого замыкания (ХКЗ)
    /// Таблица 1.6
    /// </summary>
    /// <returns>Список точек (I_в, А; I_к, А)</returns>
    public static List<Vector2> GetShortCircuitCharacteristic()
    {
        List<Vector2> points = new List<Vector2>();

        for (float ifValue = 0; ifValue <= 0.12f; ifValue += 0.005f)
        {
            float emf = CalculateEmf(ifValue, 1500f);
            float ik = emf / 12.5f;
            ik = Mathf.Clamp(ik, 0f, 5f);
            points.Add(new Vector2(ifValue, ik));
        }

        return points;
    }

    /// <summary>
    /// Получение внешней характеристики U = f(I_a) при I_в = const, n = const
    /// Таблица 1.4, рис. 1.4
    /// </summary>
    /// <param name="fieldCurrent">Фиксированный ток возбуждения, А</param>
    /// <returns>Список точек (I_a, А; U, В)</returns>
    public static List<Vector2> GetExternalCharacteristic(float fieldCurrent)
    {
        List<Vector2> points = new List<Vector2>();

        // Ток якоря от 0 до 1.2 I_ном
        for (float ia = 0; ia <= 2.5f; ia += 0.1f)
        {
            float U = GetVoltage(fieldCurrent, ia, 1500f);
            points.Add(new Vector2(ia, U));
        }

        return points;
    }

    /// <summary>
    /// Получение регулировочной характеристики I_в = f(I_a) при U = const, n = const
    /// Таблица 1.5, рис. 1.5
    /// </summary>
    /// <param name="targetVoltage">Поддерживаемое напряжение, В</param>
    /// <returns>Список точек (I_a, А; I_в, А)</returns>
    public static List<Vector2> GetRegulatingCharacteristic(float targetVoltage)
    {
        List<Vector2> points = new List<Vector2>();

        // Добавляем точку холостого хода
        float ifNoLoad = GetFieldCurrentForVoltage(targetVoltage, 0f, 1500f);
        points.Add(new Vector2(0f, ifNoLoad));

        // Ток якоря от 0.2 до 1.2 I_ном
        for (float ia = 0.2f; ia <= 2.5f; ia += 0.2f)
        {
            float ifValue = GetFieldCurrentForVoltage(targetVoltage, ia, 1500f);
            points.Add(new Vector2(ia, ifValue));
        }

        return points;
    }

    /// <summary>
    /// Получение нагрузочной характеристики U = f(I_в) при I_a = const
    /// Таблица 1.3, рис. 1.3
    /// </summary>
    /// <param name="armatureCurrent">Фиксированный ток якоря, А</param>
    /// <returns>Список точек (I_в, А; U, В)</returns>
    public static List<Vector2> GetLoadCharacteristic(float armatureCurrent)
    {
        List<Vector2> points = new List<Vector2>();

        for (float ifValue = 0; ifValue <= 0.15f; ifValue += 0.005f)
        {
            float U = GetVoltage(ifValue, armatureCurrent, 1500f);
            points.Add(new Vector2(ifValue, U));
        }

        return points;
    }

    // ============ РАСЧЁТНЫЕ МЕТОДЫ ДЛЯ ХАРАКТЕРИСТИЧЕСКОГО ТРЕУГОЛЬНИКА ============

    /// <summary>
    /// Получение напряжения при заданных параметрах
    /// </summary>
    public static float GetVoltage(float fieldCurrent, float armatureCurrent, float rpm)
    {
        // Реакция якоря (размагничивание)
        float demagnetizing = armatureCurrent * 0.025f;
        float effectiveField = Mathf.Max(MIN_FIELD_CURRENT, fieldCurrent - demagnetizing);

        // ЭДС
        float emf = CalculateEmf(effectiveField, rpm);

        // Падение напряжения
        float voltageDrop = armatureCurrent * 12.5f;

        return Mathf.Max(0f, emf - voltageDrop);
    }

    /// <summary>
    /// Получение тока возбуждения для заданного напряжения (регулировочная характеристика)
    /// </summary>
    public static float GetFieldCurrentForVoltage(float targetVoltage, float armatureCurrent, float rpm)
    {
        // Бинарный поиск
        float low = MIN_FIELD_CURRENT;
        float high = MAX_FIELD_CURRENT;

        for (int iter = 0; iter < 20; iter++)
        {
            float mid = (low + high) / 2f;
            float voltage = GetVoltage(mid, armatureCurrent, rpm);

            if (voltage > targetVoltage)
                high = mid;
            else
                low = mid;
        }

        return (low + high) / 2f;
    }

    /// <summary>
    /// Расчёт характеристического треугольника (рис. 1.3)
    /// Катет BC = I_a * R_я — падение напряжения в якоре
    /// Катет AB — размагничивающее действие реакции якоря
    /// </summary>
    /// <param name="armatureCurrent">Ток якоря, А</param>
    /// <param name="voltageDrop">Падение напряжения в якоре (катет BC), В</param>
    /// <param name="demagnetizingCurrent">Размагничивающий ток (катет AB), А</param>
    public static void GetCharacteristicTriangle(float armatureCurrent,
        out float voltageDrop, out float demagnetizingCurrent)
    {
        // Катет BC
        voltageDrop = armatureCurrent * 12.5f;

        // Катет AB — по характеристике холостого хода
        // Находим, насколько нужно увеличить I_в для компенсации падения напряжения
        float slopeAtNominal = GetNoLoadSlopeAtVoltage(220f);
        demagnetizingCurrent = voltageDrop / slopeAtNominal;

        // Дополнительное размагничивание от реакции якоря
        demagnetizingCurrent += armatureCurrent * 0.025f;
    }

    /// <summary>
    /// Крутизна характеристики холостого хода в заданной точке (ΔE/ΔI_в)
    /// </summary>
    private static float GetNoLoadSlopeAtVoltage(float voltage)
    {
        // Поиск рабочей точки на ХХХ
        float ifLow = 0f;
        float ifHigh = MAX_FIELD_CURRENT;

        for (int iter = 0; iter < 20; iter++)
        {
            float ifMid = (ifLow + ifHigh) / 2f;
            float emf = CalculateEmf(ifMid, 1500f);

            if (emf > voltage)
                ifHigh = ifMid;
            else
                ifLow = ifMid;
        }

        // Крутизна в рабочей точке (производная)
        float deltaIf = 0.001f;
        float emf1 = CalculateEmf(ifLow, 1500f);
        float emf2 = CalculateEmf(ifLow + deltaIf, 1500f);

        return (emf2 - emf1) / deltaIf;
    }

    /// <summary>
    /// Расчёт коэффициента насыщения R_u = F_0 / F_0*
    /// Формула (1) из методики, рис. 1.2
    /// </summary>
    public static float GetSaturationFactor()
    {
        // F_0 — полная НС (ток возбуждения при U_ном)
        float If_nominal = GetFieldCurrentForVoltage(220f, 0f, 1500f);

        // F_0* — НС воздушного зазора (по линейной части ХХХ)
        // Берём точку на линейном участке (например, при I_в = 0.03 А)
        float linearIf = 0.03f;
        float linearEmf = CalculateEmf(linearIf, 1500f);
        float slope = linearEmf / linearIf;

        float If_airGap = 220f / slope;

        return If_nominal / If_airGap;
    }

    /// <summary>
    /// Процентное снижение напряжения по внешней характеристике
    /// Формула (8): ΔU% = (U_0 - U_ном) / U_ном * 100%
    /// </summary>
    public static float GetVoltageDropPercent(float fieldCurrent)
    {
        float U0 = GetVoltage(fieldCurrent, 0f, 1500f);
        float Un = GetVoltage(fieldCurrent, 2f, 1500f);

        return (U0 - Un) / Un * 100f;
    }

    // ============ ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ============

    /// <summary>
    /// Сброс кэша характеристик (при изменении параметров)
    /// </summary>
    public static void ClearCache()
    {
        cachedNoLoadCurve = null;
    }

    /// <summary>
    /// Получение сопротивления якоря (для отчёта)
    /// </summary>
    public static float GetArmatureResistance() => 12.5f;

    /// <summary>
    /// Получение номинальных параметров
    /// </summary>
    public static (float U, float Ia, float If, float n) GetNominalParameters()
    {
        return (220f, 2f, 0.095f, 1500f);
    }
}