using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class Lab5SyncGeneratorModel : MonoBehaviour
{
    [Header("— ОРГАНЫ УПРАВЛЕНИЯ (авто-поиск) —")]
    public SliderGor R1;
    public SliderGor R2;
    public Rotator R3;
    public Rotator LLR;
    public Switch KM1;
    public Switch Q2;
    public Switch Q1;

    [Header("— ДВИГАТЕЛЬ —")]
    public Motor motor;

    [Header("— ЦИФРОВЫЕ ДИСПЛЕИ —")]
    public TMP_Text tvInfoText;

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

    [Header("— ТЕКУЩЕЕ СОСТОЯНИЕ —")]
    public float rotorSpeedRpm;
    public float generatorFrequency;
    public float excitationCurrent;
    public float generatorVoltage;
    public float statorCurrent;
    public float powerFactor;

    [Header("— ФЛАГИ —")]
    public bool isPrimeMoverRunning;
    public bool isShortCircuitMode;
    public bool isShortCircuit2PhaseMode;
    public bool hasFault;

    [Header("— ДАННЫЕ ХАРАКТЕРИСТИК —")]
    public List<Vector2> noLoadAscending = new List<Vector2>();
    public List<Vector2> noLoadDescending = new List<Vector2>();
    public List<Vector2> inductiveLoadData = new List<Vector2>();
    public List<Vector2> externalData = new List<Vector2>();
    public List<Vector2> regulatingData = new List<Vector2>();
    public List<Vector2> shortCircuitData = new List<Vector2>();
    public List<Vector2> shortCircuit2PhaseData = new List<Vector2>();

    public float ActiveLoadPercent => R1 != null ? R1.Percent : 0f;
    public float ExcitationRheostatPercent => R2 != null ? R2.Percent : 0f;
    public float InductiveLoadPercent => R3 != null ? R3.value : 0f;
    public float DriveSpeed => LLR != null ? LLR.llrValue : 0f;
    public bool IsMainPowerOn => KM1 != null && KM1.isOn;
    public bool IsLoadOn => Q2 != null && Q2.isOn;
    public bool IsExcitationOn => Q1 != null && Q1.isOn;
    public float MotorCurrent => rotorSpeedRpm / 10f;
    public float GeneratorVoltage => generatorVoltage;
    public float GeneratorFrequency => generatorFrequency;
    public float PhaseACurrent => statorCurrent;
    public float PhaseBCurrent => statorCurrent * 0.95f;
    public float PhaseCCurrent => (PhaseACurrent + PhaseBCurrent) * 0.5f;
    public float ExcitationCurrentAmps => excitationCurrent;

    private void Awake()
    {
        AutoFindAll();
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

        foreach (var sw in FindObjectsOfType<Switch>())
        {
            string n = sw.gameObject.name;
            if (n == "Q1") KM1 = sw;
            else if (n == "Q2") Q2 = sw;
            else if (n == "Q3") Q1 = sw;
        }
        if (KM1 == null)
        {
            var switches = FindObjectsOfType<Switch>();
            if (switches.Length > 0) KM1 = switches[0];
            if (switches.Length > 1) Q2 = switches[1];
            if (switches.Length > 2) Q1 = switches[2];
        }

        if (motor == null)
            motor = FindObjectOfType<Motor>();

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
    }

    private void SubscribeControls()
    {
        if (R1 != null)  R1.OnValueChanged += v => RefreshCircuit();
        if (R2 != null)  R2.OnValueChanged += v => RefreshCircuit();
        if (R3 != null)  R3.OnValueChanged += v => RefreshCircuit();
        if (LLR != null) LLR.OnValueChanged += v => RefreshCircuit();
        if (KM1 != null) KM1.OnValueChanged += v => RefreshCircuit();
        if (Q2 != null)  Q2.OnValueChanged += v => RefreshCircuit();
        if (Q1 != null)  Q1.OnValueChanged += v => RefreshCircuit();
    }

    private void RefreshCircuit()
    {
        CheckState();
        SetMotorTarget();
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
            motor.TargetRPM = Mathf.Lerp(0f, 1500f, Mathf.Clamp01(LLR.llrValue / 250f));
        else
            motor.TargetRPM = 0f;
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

    private void CalculateState()
    {
        if (!isPrimeMoverRunning)
        {
            rotorSpeedRpm = 0f;
            generatorFrequency = 0f;
            excitationCurrent = 0f;
            generatorVoltage = 0f;
            statorCurrent = 0f;
            powerFactor = 0f;
            return;
        }

        rotorSpeedRpm = motor != null ? motor.CurrentRPM
            : Mathf.Lerp(0f, 1500f, Mathf.Clamp01((LLR != null ? LLR.llrValue : 0f) / 250f));
        generatorFrequency = rotorSpeedRpm * polePairs / 60f;

        bool excEnabled = Q1 != null && Q1.isOn;
        float excT = R2 != null ? Mathf.Clamp01(R2.Percent / 100f) : 0f;
        excitationCurrent = excEnabled
            ? Mathf.Lerp(0f, nominalExcitationCurrent * 1.5f, excT)
            : 0f;

        float loadT = R1 != null ? Mathf.Clamp01(R1.Percent / 100f) : 0f;
        float inductiveT = R3 != null ? Mathf.Clamp01(R3.value / 100f) : 0f;

        // ЭДС по кусочно-линейной кривой насыщения (характеристика холостого хода)
        generatorVoltage = CalculateVoltageFromMagnetizationCurve(excitationCurrent);

        if (Q2 != null && Q2.isOn && !isShortCircuitMode && !isShortCircuit2PhaseMode)
        {
            generatorVoltage = Mathf.Max(0f, generatorVoltage - loadT * 120f - inductiveT * 180f);
            float baseCurrent = generatorVoltage / 50f;
            statorCurrent = Mathf.Min(baseCurrent * (loadT + inductiveT * 0.5f), nominalStatorCurrent * 1.5f);
            powerFactor = Mathf.Lerp(0.99f, 0.3f, inductiveT);
        }
        else if (isShortCircuitMode || isShortCircuit2PhaseMode)
        {
            float Ik3 = CalculateShortCircuitCurrent(excitationCurrent);
            statorCurrent = isShortCircuit2PhaseMode ? Ik3 * 0.866f : Ik3;
            generatorVoltage = 0f;
            powerFactor = 0f;
        }
        else
        {
            statorCurrent = 0f;
            powerFactor = 1f;
        }
    }

    /// ЭДС по кусочно-линейной кривой намагничивания (ХХХ)
    /// Участки: остаточная → линейный (воздушный зазор) → колено → насыщение → глубокое насыщение
    private float CalculateVoltageFromMagnetizationCurve(float fieldCurrent)
    {
        if (fieldCurrent <= 0f) return 8f; // остаточная ЭДС

        float ifNorm = fieldCurrent / nominalExcitationCurrent; // I_в / I_в_ном
        float emfNorm;

        // Кривая насыщения в относительных единицах
        // ifNorm = 0    → emfNorm = 0.02 (остаточная)
        // ifNorm = 0.5  → emfNorm = 0.65 (конец линейного участка)
        // ifNorm = 0.75 → emfNorm = 0.90 (колено)
        // ifNorm = 1.0  → emfNorm = 1.00 (номинальный режим)
        // ifNorm = 1.5  → emfNorm = 1.15 (насыщение)
        // ifNorm > 1.5  → emfNorm = 1.20 (глубокое насыщение)
        if (ifNorm <= 0.5f)
        {
            // Линейный участок (воздушный зазор): slope = 1.3
            emfNorm = 1.3f * ifNorm;
            emfNorm = Mathf.Max(emfNorm, 0.02f);
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
    /// Ik = (I_в / I_в_ном) * I_к_ном, где I_к_ном = 0.8·I_ном (Ik < I1 по методичке)
    private float CalculateShortCircuitCurrent(float fieldCurrent)
    {
        float ifRatio = fieldCurrent / nominalExcitationCurrent;
        float ratedScCurrent = nominalStatorCurrent * 0.8f;
        return ifRatio * ratedScCurrent;
    }

    private void UpdateInfoText()
    {
        if (tvInfoText == null) return;
        tvInfoText.text = $"n = {rotorSpeedRpm:F0} об/мин\n" +
                          $"f = {generatorFrequency:F2} Гц\n" +
                          $"U = {generatorVoltage:F1} В\n" +
                          $"I_a = {statorCurrent:F3} А\n" +
                          $"I_в = {excitationCurrent:F3} А\n" +
                          $"cos φ = {powerFactor:F3}";
    }

    public void StartMotor() { if (KM1 != null) KM1.isOn = true; }
    public void StopMotor() { if (KM1 != null) KM1.isOn = false; }
    public void ResetGenerator() { hasFault = false; isShortCircuitMode = false; isShortCircuit2PhaseMode = false; }

    public void RecordNoLoadPoint()
    {
        if (!isPrimeMoverRunning) { Debug.LogWarning("Двигатель не вращается"); return; }
        if (Q2 != null && Q2.isOn) { Debug.LogWarning("Для ХХХ Q2 должен быть выключен"); return; }
        float If = excitationCurrent;
        if (noLoadAscending.Count > 0 && If < noLoadAscending[noLoadAscending.Count - 1].x)
            noLoadDescending.Add(new Vector2(If, generatorVoltage));
        else
            noLoadAscending.Add(new Vector2(If, generatorVoltage));
        Debug.Log($"ХХХ: I_в = {If:F3} А, E_0 = {generatorVoltage:F1} В");
    }

    public void RecordInductiveLoadPoint() { inductiveLoadData.Add(new Vector2(excitationCurrent, generatorVoltage)); }
    public void RecordExternalPoint() { externalData.Add(new Vector2(statorCurrent, generatorVoltage)); }
    public void RecordRegulatingPoint() { regulatingData.Add(new Vector2(statorCurrent, excitationCurrent)); }

    public void RecordShortCircuitPoint()
    {
        if (!isShortCircuitMode) { Debug.LogWarning("Режим КЗ не активирован"); return; }
        shortCircuitData.Add(new Vector2(excitationCurrent, statorCurrent));
    }

    public void EnableShortCircuitMode() { isShortCircuitMode = true; isShortCircuit2PhaseMode = false; }
    public void DisableShortCircuitMode() { isShortCircuitMode = false; }

    public void RecordShortCircuit2PhasePoint()
    {
        if (!isShortCircuit2PhaseMode) { Debug.LogWarning("Режим двухфазного КЗ не активирован"); return; }
        shortCircuit2PhaseData.Add(new Vector2(excitationCurrent, statorCurrent));
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
        if (R3 != null) { R3.value = 0f; R3.llrValue = 0f; }
        if (LLR != null) LLR.llrValue = 0f;
        if (KM1 != null) KM1.isOn = false;
        if (Q2 != null) Q2.isOn = false;
        if (Q1 != null) Q1.isOn = false;
        ResetGenerator();
        RefreshCircuit();
    }

    /// Находит угловой коэффициент начального прямолинейного участка ХХХ
    private float CalculateInitialSlope()
    {
        int count = Mathf.Min(noLoadAscending.Count, 5);
        if (count < 2) return 0f;
        float sumXY = 0f, sumXX = 0f;
        for (int i = 0; i < count; i++)
        {
            float x = noLoadAscending[i].x;
            float y = noLoadAscending[i].y;
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

    /// Ток возбуждения I_в по характеристике КЗ для заданного тока I_к
    public float GetShortCircuitExcitation(float targetCurrent)
    {
        if (shortCircuitData.Count < 2) return 0f;
        if (targetCurrent <= 0f) return 0f;
        if (targetCurrent <= shortCircuitData[0].y) return shortCircuitData[0].x;
        if (targetCurrent >= shortCircuitData[shortCircuitData.Count - 1].y)
            return shortCircuitData[shortCircuitData.Count - 1].x;
        for (int i = 1; i < shortCircuitData.Count; i++)
        {
            if (targetCurrent <= shortCircuitData[i].y)
            {
                float t = (targetCurrent - shortCircuitData[i - 1].y) / (shortCircuitData[i].y - shortCircuitData[i - 1].y);
                return Mathf.Lerp(shortCircuitData[i - 1].x, shortCircuitData[i].x, t);
            }
        }
        return shortCircuitData[shortCircuitData.Count - 1].x;
    }

    /// Точка на индукционной нагрузочной характеристике при заданном напряжении
    public Vector2 FindPointOnInductiveLoad(float targetVoltage)
    {
        if (inductiveLoadData.Count < 2) return Vector2.zero;
        if (targetVoltage <= inductiveLoadData[0].y) return inductiveLoadData[0];
        if (targetVoltage >= inductiveLoadData[inductiveLoadData.Count - 1].y)
            return inductiveLoadData[inductiveLoadData.Count - 1];
        for (int i = 1; i < inductiveLoadData.Count; i++)
        {
            if (targetVoltage <= inductiveLoadData[i].y)
            {
                float t = (targetVoltage - inductiveLoadData[i - 1].y) / (inductiveLoadData[i].y - inductiveLoadData[i - 1].y);
                float x = Mathf.Lerp(inductiveLoadData[i - 1].x, inductiveLoadData[i].x, t);
                return new Vector2(x, targetVoltage);
            }
        }
        return inductiveLoadData[inductiveLoadData.Count - 1];
    }

    /// Пересечение прямой (point, slope) с характеристикой холостого хода
    private Vector2 FindIntersectionWithNoLoad(Vector2 pointOnLine, float slope)
    {
        if (noLoadAscending.Count < 2) return Vector2.zero;
        for (int i = 1; i < noLoadAscending.Count; i++)
        {
            Vector2 prev = noLoadAscending[i - 1];
            Vector2 curr = noLoadAscending[i];
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
        return noLoadAscending[noLoadAscending.Count - 1];
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

        // 1. Угловой коэффициент начальной прямолинейной части ХХХ
        float slope = CalculateInitialSlope();
        if (slope <= 0f) { details["Error"] = "Недостаточно данных ХХХ для определения начального участка"; return; }
        details["Slope_XXX"] = $"{slope:F2} В/А (E_0 / I_в)";

        // 2. Ток возбуждения I_кз по характеристике КЗ при I_a = 0.5·I_1ном
        float I_k3 = GetShortCircuitExcitation(Ia_target);
        if (I_k3 <= 0f) { details["Error"] = "Недостаточно данных КЗ для I_a = " + Ia_target.ToString("F3") + " А"; return; }
        details["I_k3"] = $"{I_k3:F4} А (I_в при I_к = {Ia_target:F3} А)";

        // 3. Точка A1 на индукционной нагрузочной характеристике при U = U_ном
        Vector2 A1 = FindPointOnInductiveLoad(nominalVoltage);
        if (A1.x <= 0f) { details["Error"] = "Недостаточно данных индукционной нагрузочной х-ки для U = " + nominalVoltage.ToString("F1") + " В"; return; }
        details["A1"] = $"({A1.x:F4} А; {A1.y:F1} В) — на индукц. нагрузочной х-ке при U = U_ном";

        // 4. Точка O1 = A1, сдвинутая влево на I_кз
        Vector2 O1 = new Vector2(A1.x - I_k3, A1.y);
        details["O1"] = $"({O1.x:F4} А; {O1.y:F1} В) — A1 сдвинута на -I_кз";

        // 5. Прямая O1C1 ∥ начальному участку ХХХ → поиск пересечения C1 с ХХХ
        Vector2 C1 = FindIntersectionWithNoLoad(O1, slope);
        if (C1.x <= 0f) { details["Error"] = "Не удалось найти пересечение прямой O1C1 с ХХХ"; return; }
        details["C1"] = $"({C1.x:F4} А; {C1.y:F1} В) — пересечение O1C1 с ХХХ";

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
        float E_at_O1 = Interpolate(noLoadAscending, O1.x);
        float A1F = E_at_O1 - A1.y;
        if (A1F > 0f) Xd_sat = A1F / Ia_target;
        details["E_at_O1"] = $"{E_at_O1:F1} В (E_0 при I_в = {O1.x:F4} А по ХХХ)";
        details["A1F"] = $"{A1F:F2} В (E_0(I_в_O1) - U_ном)";
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
        if (shortCircuitData.Count < 1 || noLoadAscending.Count < 2) return unsaturatedSyncReactance;

        var last = shortCircuitData[shortCircuitData.Count - 1];
        float slope = CalculateInitialSlope();
        if (slope <= 0f || last.y <= 0.001f) return unsaturatedSyncReactance;

        // E_0 по спрямлённой ХХХ: E_0 = slope * I_в (линейная зависимость)
        float E0_airgap = slope * last.x;
        return E0_airgap / last.y;
    }

    /// Xd по реальной (сырой) ХХХ — с учётом насыщения
    public float CalculateSyncReactance()
    {
        if (shortCircuitData.Count > 0 && noLoadAscending.Count > 0)
        {
            var last = shortCircuitData[shortCircuitData.Count - 1];
            float E0 = 0f;
            foreach (var p in noLoadAscending)
                if (p.x >= last.x) { E0 = p.y; break; }
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

        if (noLoadAscending.Count < 2) { details["Error"] = "Недостаточно данных ХХХ для диаграммы ЭДС"; return; }

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
        details["E_δ_components"] = $"U_н({v_U.x:F1};0) + Ia·Ra({v_IaRa.x:F2};{v_IaRa.y:F2}) + Ia·Xσ({v_IaXσ.x:F2};{v_IaXσ.y:F2})";

        // 6. F_δ — из ХХХ по |E_δ|, угол = ∠E_δ + 90° (опережает)
        float mag_Fδ = InterpolateInverse(noLoadAscending, mag_Eδ);
        // Если E_δ выходит за пределы ХХХ — экстраполируем по начальному участку
        if (mag_Eδ > noLoadAscending[noLoadAscending.Count - 1].y)
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
        float mag_E0 = Interpolate(noLoadAscending, mag_F0);
        if (mag_F0 > noLoadAscending[noLoadAscending.Count - 1].x)
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
