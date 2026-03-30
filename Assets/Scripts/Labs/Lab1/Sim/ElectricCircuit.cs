using System.Collections;
using UnityEngine;

public class ElectricCircuit : MonoBehaviour
{
    [Header("Controls")]
    public Slider R1, R2;
    public Rotator R3, LLR;
    public Switch Q1, Q2, Q3;

    [Header("Meters")]
    public Meter Pv1, Pv2, Pa1, Pa2, Pa3, Pa4;
    public Meter info_Pv1, info_Pv2, info_Pa1, info_Pa2, info_Pa3, info_Pa4;

    [Header("Motor")]
    public Motor Motor;
    public float rotationSpeed = 2f;

    private readonly Vector3 offEuler = new Vector3(-180f, 0f, -49f);

    private Vector3 onEuler_Pv1, onEuler_Pv2, onEuler_Pa1, onEuler_Pa2, onEuler_Pa3, onEuler_Pa4;

    private float U_Pv1, U_Pv2;
    private float A_Pa1, A_Pa2, A_Pa3, A_Pa4;
    private float R1_value, R2_value, R3_value, LLR_value;
    private float RPM;

    private bool engineIsOn;
    private Coroutine pv1RotationRoutine;
    private Coroutine pv2RotationRoutine;
    private Coroutine pa1RotationRoutine;
    private Coroutine pa2RotationRoutine;
    private Coroutine pa3RotationRoutine;
    private Coroutine pa4RotationRoutine;

    public float PV1Value => U_Pv1;
    public float PV2Value => U_Pv2;
    public float PA1Value => A_Pa1;
    public float PA2ValueMilliAmp => A_Pa2 * 1000f;
    public float PA3ValueMilliAmp => A_Pa3 * 1000f;
    public float PA4Value => A_Pa4;
    public float RPMValue => RPM;

    public float PHOValue => LLR_value;
    public float R1Percent => R1_value;
    public float R2Percent => R2_value;
    public float R3Percent => R3_value;

    public bool Q1Enabled => Q1.isOn;
    public bool Q2Enabled => Q2.isOn;
    public bool Q3Enabled => Q3.isOn;
    public bool EngineIsOn => engineIsOn;

    public CircuitSnapshot GetSnapshot()
    {
        return new CircuitSnapshot
        {
            phoPercent = LLR_value,
            r1Percent = R1_value,
            r2Percent = R2_value,
            r3Percent = R3_value,

            q1Enabled = Q1.isOn,
            q2Enabled = Q2.isOn,
            q3Enabled = Q3.isOn,

            pv1Voltage = U_Pv1,
            pv2Voltage = U_Pv2,
            pa1Current = A_Pa1,
            pa2CurrentMilliAmp = A_Pa2 * 1000f,
            pa3CurrentMilliAmp = A_Pa3 * 1000f,
            pa4Current = A_Pa4,
            rpm = RPM
        };
    }

    private void Start()
    {
        R1.OnValueChanged += OnR1Changed;
        R2.OnValueChanged += OnR2Changed;
        R3.OnValueChanged += OnR3Changed;
        LLR.OnValueChanged += OnLLRChanged;

        Q1.OnValueChanged += OnQ1Changed;
        Q2.OnValueChanged += OnQ2Changed;
        Q3.OnValueChanged += OnQ3Changed;

        GameManager.Instance.SetState(GameState.Playing);

        RefreshCircuit();
    }

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
        A_Pa4 = 0f;
        RPM = 0f;
        engineIsOn = false;

        RefreshCircuit();
    }

    private void RefreshCircuit()
    {
        CheckEngine();
        RecalculateState();
        ApplyInfoMeters();
        UpdateMeterTargetAngles();
        SetAllMeters();
    }

    private void CheckEngine()
    {
        engineIsOn = LLR_value > 50f && Q1.isOn && Q2.isOn;
    }

    private void RecalculateState()
    {
        CoeffCalculation.Simulate(
            Q1.isOn,
            Q2.isOn,
            Q3.isOn,
            engineIsOn,
            U_Pv1,
            R1_value,
            R2_value,
            R3_value,
            out A_Pa1,
            out A_Pa2,
            out A_Pa3,
            out A_Pa4,
            out U_Pv2,
            out RPM
        );
        float ifMotor = A_Pa2;
        float p1d = U_Pv1 * (A_Pa1 + ifMotor);
        float p2g = U_Pv2 * A_Pa4;

        if (p2g > p1d + 0.01f)
        {
            Debug.LogWarning(
                $"MODEL INVALID | P2g ({p2g:F2}) > P1d ({p1d:F2}) | " +
                $"PV1={U_Pv1:F2}, PA1={A_Pa1:F3}, PA2={A_Pa2:F3}, " +
                $"PV2={U_Pv2:F2}, PA4={A_Pa4:F3}, RPM={RPM:F1}, " +
                $"R1={R1_value:F2}, R2={R2_value:F2}, R3={R3_value:F2}"
            );
        }

        Motor.TargetRPM = RPM;
    }

    private void ApplyInfoMeters()
    {
        info_Pa1.current = A_Pa1;
        info_Pa2.current = A_Pa2 * 1000f;
        info_Pa3.current = A_Pa3 * 1000f;
        info_Pa4.current = A_Pa4;
        info_Pv1.current = U_Pv1;
        info_Pv2.current = U_Pv2;
    }

    private void UpdateMeterTargetAngles()
    {
        onEuler_Pv1 = BuildMeterAngle(U_Pv1, 250f);
        onEuler_Pv2 = BuildMeterAngle(U_Pv2, 250f);
        onEuler_Pa1 = BuildMeterAngle(A_Pa1, 5f);
        onEuler_Pa2 = BuildMeterAngle(A_Pa2 * 1000f, 250f);
        onEuler_Pa3 = BuildMeterAngle(A_Pa3 * 1000f, 250f);
        onEuler_Pa4 = BuildMeterAngle(A_Pa4, 5f);
    }

    private Vector3 BuildMeterAngle(float currentValue, float maxValue)
    {
        float angle = Mathf.Lerp(-49f, -131f, Mathf.Clamp01(currentValue / maxValue));
        return new Vector3(-180f, 0f, angle);
    }

    private void SetAllMeters()
    {
        StartMeterRotation(ref pv1RotationRoutine, Q1.isOn, onEuler_Pv1, Pv1);
        StartMeterRotation(ref pv2RotationRoutine, Q1.isOn && Q2.isOn && engineIsOn, onEuler_Pv2, Pv2);
        StartMeterRotation(ref pa1RotationRoutine, Q1.isOn && Q2.isOn && engineIsOn, onEuler_Pa1, Pa1);
        StartMeterRotation(ref pa2RotationRoutine, Q1.isOn && Q2.isOn && engineIsOn, onEuler_Pa2, Pa2);
        StartMeterRotation(ref pa3RotationRoutine, Q1.isOn && Q2.isOn && engineIsOn, onEuler_Pa3, Pa3);
        StartMeterRotation(ref pa4RotationRoutine, Q1.isOn && Q2.isOn && Q3.isOn && engineIsOn, onEuler_Pa4, Pa4);
    }

    private void StartMeterRotation(ref Coroutine routine, bool toOn, Vector3 onEuler, Meter meter)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(RotateMeter(toOn, onEuler, meter));
    }

    private void OnR1Changed(float value)
    {
        R1_value = value;
        RefreshCircuit();
    }

    private void OnR2Changed(float value)
    {
        R2_value = value;
        RefreshCircuit();
    }

    private void OnR3Changed(float value)
    {
        R3_value = value;
        RefreshCircuit();
    }

    private void OnLLRChanged(float value)
    {
        U_Pv1 = Mathf.Clamp(value, 0f, 250f);
        LLR_value = U_Pv1;
        RefreshCircuit();
    }

    private void OnQ1Changed(bool value) => RefreshCircuit();
    private void OnQ2Changed(bool value) => RefreshCircuit();
    private void OnQ3Changed(bool value) => RefreshCircuit();

    private IEnumerator RotateMeter(bool toOn, Vector3 onEuler, Meter meter)
    {
        Quaternion startRot = meter.transform.localRotation;
        Quaternion endRot = Quaternion.Euler(toOn ? onEuler : offEuler);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            meter.transform.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        meter.transform.localRotation = endRot;
    }

    private void OnDisable()
    {
        R1.OnValueChanged -= OnR1Changed;
        R2.OnValueChanged -= OnR2Changed;
        R3.OnValueChanged -= OnR3Changed;
        LLR.OnValueChanged -= OnLLRChanged;

        Q1.OnValueChanged -= OnQ1Changed;
        Q2.OnValueChanged -= OnQ2Changed;
        Q3.OnValueChanged -= OnQ3Changed;
    }
}
