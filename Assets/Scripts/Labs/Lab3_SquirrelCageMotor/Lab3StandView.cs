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

using TMPro;
using UnityEngine;

public class Lab3StandView : MonoBehaviour
{
    [Header("Existing Lab3 Circuit")]
    [SerializeField] private bool autoBindFromExistingCircuit = true;
    [SerializeField] private Lab3_ElectricCircuit existingCircuit;

    [Header("Meters")]
    [SerializeField] private Meter pv1;
    [SerializeField] private Meter pv2;
    [SerializeField] private Meter pa1;
    [SerializeField] private Meter pa2;
    [SerializeField] private Meter pa3;

    [Header("Switches")]
    [SerializeField] private Lab3Switch q1;
    [SerializeField] private Lab3Switch q2;
    [SerializeField] private Lab3Switch q3;

    [Header("Regulators")]
    [SerializeField] private Lab3SliderGor r1;
    [SerializeField] private Lab3SliderGor r2;

    [Header("Drive")]
    [SerializeField] private Lab3Motor motor;
    [SerializeField] private Transform rotatingShaft;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("Text Displays")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text rpmText;

    public void BindExistingCircuit(Lab3_ElectricCircuit circuit)
    {
        if (!autoBindFromExistingCircuit)
        {
            return;
        }

        existingCircuit = circuit;
        if (existingCircuit == null)
        {
            return;
        }

        pv1 ??= existingCircuit.Pv1;
        pv2 ??= existingCircuit.Pv2;
        pa1 ??= existingCircuit.Pa1;
        pa2 ??= existingCircuit.Pa2;
        pa3 ??= existingCircuit.Pa3;
        q1 ??= existingCircuit.Q1;
        q2 ??= existingCircuit.Q2;
        q3 ??= existingCircuit.Q3;
        r1 ??= existingCircuit.R1;
        r2 ??= existingCircuit.R2;
        motor ??= existingCircuit.Motor;
    }

    public void UpdateView(Lab3Controller controller, float deltaTime)
    {
        if (controller == null)
        {
            return;
        }

        // PV1 — напряжение питающей сети L1/L2/L3 (фиксированное 380В на вводных клеммах Q1)
        SetMeterValue(pv1, controller.Q1Enabled ? 380f : 0f);
        // PV2 — напряжение на зажимах генератора G1
        SetMeterValue(pv2, controller.Voltage);
        // PA1 — ток двигателя M1 (амперметр последовательно с M1)
        SetMeterValue(pa1, controller.MotorCurrent);
        // PA2 — ток якоря генератора G1 (в цепи G1 → Q2 → PA2 → R2)
        SetMeterValue(pa2, controller.ArmatureCurrent);
        // PA3 — ток возбуждения генератора G1 (в цепи +L → Q3 → R1 → Ш1/Ш2 → PA3 → -L)
        SetMeterValue(pa3, controller.FieldCurrent * 1000f);

        // Слайдеры и выключатели НЕ перезаписываются — они управляются
        // напрямую пользователем в сцене или HUD-кнопками контроллера
        // через existingCircuit (см. ToggleQ1, ChangeR1 и т.д.).

        float rpm = controller.Omega * 60f / (2f * Mathf.PI);
        if (motor != null)
        {
            motor.TargetRPM = rpm;
        }

        if (rotatingShaft != null && rpm > 1f)
        {
            rotatingShaft.Rotate(rotationAxis.normalized, rpm * 6f * deltaTime, Space.Self);
        }

        if (statusText != null)
        {
            statusText.text = $"ЛР №3: {controller.GetStageName(controller.CurrentStage)}";
        }

        if (rpmText != null)
        {
            rpmText.text = $"n = {rpm:F0} об/мин | M1→G1";
        }
    }

    private static void SetMeterValue(Meter meter, float value)
    {
        if (meter != null)
        {
            meter.current = value;
        }
    }

    private static void SetSliderValue(Lab3SliderGor slider, float value)
    {
        if (slider != null && !Mathf.Approximately(slider.Percent, value))
        {
            slider.SetPercent(value);
        }
    }

    private static void SetSwitchState(Lab3Switch target, bool state)
    {
        if (target == null || target.isOn == state)
        {
            return;
        }

        target.isOn = state;
        target.transform.localRotation = Quaternion.Euler(state ? target.onEuler : target.offEuler);
        SetRendererColor(target.GetComponent<Renderer>(), state ? Color.green : Color.red);
        if (target.circleObject != null)
        {
            SetRendererColor(target.circleObject.GetComponent<Renderer>(), state ? Color.green : Color.red);
        }
    }

    private static void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
}
