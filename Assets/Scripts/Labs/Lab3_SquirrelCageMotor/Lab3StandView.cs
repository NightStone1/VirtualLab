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
    [SerializeField] private Switch q1;
    [SerializeField] private Switch q2;
    [SerializeField] private Switch q3;

    [Header("Regulators")]
    [SerializeField] private SliderGor r1;
    [SerializeField] private SliderGor r2;

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

        SetMeterValue(pv1, controller.Q1Enabled ? 220f : 0f);
        SetMeterValue(pv2, controller.Voltage);
        SetMeterValue(pa1, controller.ArmatureCurrent);
        SetMeterValue(pa2, controller.FieldCurrent * 1000f);
        SetMeterValue(pa3, controller.ShortCircuitEnabled ? controller.ShortCircuitCurrent : controller.ArmatureCurrent);

        SetSwitchState(q1, controller.Q1Enabled);
        SetSwitchState(q2, controller.Q2Enabled);
        SetSwitchState(q3, controller.Q3Enabled);
        SetSliderValue(r1, controller.R1Position);
        SetSliderValue(r2, controller.R2Position);

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
            statusText.text = $"Lab3: {controller.GetStageName(controller.CurrentStage)}";
        }

        if (rpmText != null)
        {
            rpmText.text = $"n = {rpm:F0} об/мин";
        }
    }

    private static void SetMeterValue(Meter meter, float value)
    {
        if (meter != null)
        {
            meter.current = value;
        }
    }

    private static void SetSliderValue(SliderGor slider, float value)
    {
        if (slider != null && !Mathf.Approximately(slider.Percent, value))
        {
            slider.SetPercent(value);
        }
    }

    private static void SetSwitchState(Switch target, bool state)
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
