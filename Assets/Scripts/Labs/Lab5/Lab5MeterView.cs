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

    private Vector3 offEuler = new Vector3(-180f, 90f, -50f);
    private Quaternion targetRotation;

    private void Awake()
    {
        if (controller == null && autoFindController)
            controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();
        targetRotation = Quaternion.Euler(offEuler);
    }

    private void OnEnable()
    {
        if (controller == null && autoFindController)
            controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();
    }

    private void Update()
    {
        if (controller == null) return;

        float value = GetValue();
        bool active = controller.isPrimeMoverRunning && controller.IsMainPowerOn;

        if (meterType == MeterType.PA1_MotorCurrent)
            active = controller.IsMainPowerOn;

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
