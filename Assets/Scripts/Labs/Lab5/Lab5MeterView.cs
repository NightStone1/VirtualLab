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

using UnityEngine;

public class Lab5MeterView : MonoBehaviour
{
    public enum MeterType
    {
        PA1_MotorCurrent,
        PV1_GeneratorVoltage,
        PF1_Frequency,
        PA2_PhaseA,
        PA3_PhaseB,
        PA4_PhaseC,
        PA5_ExcitationCurrent
    }

    public MeterType meterType;
    public Lab5SyncGeneratorModel controller;
    public bool autoFindController = true;
    public float maxValue = 250f;
    public float rotationSpeed = 2f;

    // Если на объекте есть компонент Meter, обновляем и его
    private Meter meterComponent;

    private Vector3 offEuler = new Vector3(-180f, 90f, -50f);
    private Quaternion targetRotation;

    private void Awake()
    {
        if (controller == null && autoFindController)
            controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();

        meterComponent = GetComponent<Meter>();
        targetRotation = Quaternion.Euler(offEuler);

        // Авто-определение типа по имени объекта, если не назначен вручную
        AutoDetectMeterType();
        AutoSetMaxValue();
    }

    private void OnEnable()
    {
        if (controller == null && autoFindController)
            controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();
    }

    private void AutoDetectMeterType()
    {
        string n = gameObject.name.ToLower();
        string p = transform.parent != null ? transform.parent.name.ToLower() : "";

        // Проверяем и имя объекта, и имя родителя
        bool In(string s) => n.Contains(s) || p.Contains(s);

        if      (In("pa1") || n.Contains("motorcurrent") || n.Contains("двигател")) meterType = MeterType.PA1_MotorCurrent;
        else if (In("pv1") || n.Contains("voltage")       || n.Contains("напряжен")) meterType = MeterType.PV1_GeneratorVoltage;
        else if (In("pf1") || n.Contains("freq")          || n.Contains("частот"))   meterType = MeterType.PF1_Frequency;
        else if (In("pa2") || In("phasea") || n.Contains("фаза a") || n.Contains("фазаа")) meterType = MeterType.PA2_PhaseA;
        else if (In("pa3") || In("phaseb") || n.Contains("фаза b") || n.Contains("фазаб")) meterType = MeterType.PA3_PhaseB;
        else if (In("pa4") || In("phasec") || n.Contains("фаза c") || n.Contains("фазав")) meterType = MeterType.PA4_PhaseC;
        else if (In("pa5") || In("exc")    || n.Contains("возбужд")) meterType = MeterType.PA5_ExcitationCurrent;
    }

    private void AutoSetMaxValue()
    {
        switch (meterType)
        {
            case MeterType.PA1_MotorCurrent:       maxValue = 15f;    break;
            case MeterType.PV1_GeneratorVoltage:   maxValue = 500f;   break;
            case MeterType.PF1_Frequency:          maxValue = 60f;    break;
            case MeterType.PA2_PhaseA:             maxValue = 15f;    break;
            case MeterType.PA3_PhaseB:             maxValue = 15f;    break;
            case MeterType.PA4_PhaseC:             maxValue = 15f;    break;
            case MeterType.PA5_ExcitationCurrent:  maxValue = 2.5f;  break;
        }
    }

    private void Update()
    {
        if (controller == null) return;

        float value = GetValue();
        bool active = ShouldBeActive();

        if (active && value > 0.01f)
        {
            float angle = Mathf.Lerp(-50f, -131f, Mathf.Clamp01(value / maxValue));
            targetRotation = Quaternion.Euler(-180f, 90f, angle);
        }
        else
        {
            targetRotation = Quaternion.Euler(offEuler);
        }

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);

        // Обновляем Meter компонент, если он есть (для совместимости)
        if (meterComponent != null)
            meterComponent.current = value;
    }

    private bool ShouldBeActive()
    {
        if (controller == null) return false;

        switch (meterType)
        {
            case MeterType.PA1_MotorCurrent:
                // Ток двигателя — активен при включённом контакторе
                return controller.IsMainPowerOn;

            case MeterType.PA5_ExcitationCurrent:
                // Ток возбуждения — активен при включённом Q1
                return controller.IsExcitationOn;

            case MeterType.PV1_GeneratorVoltage:
            case MeterType.PF1_Frequency:
            case MeterType.PA2_PhaseA:
            case MeterType.PA3_PhaseB:
            case MeterType.PA4_PhaseC:
                // Остальные приборы активны при вращении двигателя
                return controller.isPrimeMoverRunning && controller.IsMainPowerOn;

            default:
                return false;
        }
    }

    private float GetValue()
    {
        if (controller == null) return 0f;

        switch (meterType)
        {
            case MeterType.PA1_MotorCurrent: return controller.MotorCurrent;
            case MeterType.PV1_GeneratorVoltage: return controller.GeneratorVoltage;
            case MeterType.PF1_Frequency: return controller.GeneratorFrequency;
            case MeterType.PA2_PhaseA: return controller.PhaseACurrent;
            case MeterType.PA3_PhaseB: return controller.PhaseBCurrent;
            case MeterType.PA4_PhaseC: return controller.PhaseCCurrent;
            case MeterType.PA5_ExcitationCurrent: return controller.ExcitationCurrentAmps;
        }
        return 0f;
    }
}
