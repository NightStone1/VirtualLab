using UnityEngine;

public class LabTableMapper : MonoBehaviour
{

    public Table22Row BuildTable22Row(MeasurementPoint p)
    {
        float omega = CalculateOmega(p.rpm);

        float p2g = p.pv2Voltage * p.pa4Current;
        float etaG = GeneratorEfficiencyHelper.GetEfficiency(p.pa4Current);
        float p2d = etaG > 0.0001f ? p2g / etaG : 0f;
        float m2d = omega > 0.0001f ? p2d / omega : 0f;

        // If двигателя = PA2
        float ifMotor = p.pa2CurrentMilliAmp / 1000f;

        float p1d = p.pv1Voltage * p.pa1Current + p.pv1Voltage * ifMotor;
        float etaD = p1d > 0.0001f ? p2d / p1d : 0f;
        Debug.Log(
    $"RAW Table22 | " +
    $"PV1={p.pv1Voltage}, " +
    $"PA1={p.pa1Current}, " +
    $"PA2_mA={p.pa2CurrentMilliAmp}, " +
    $"RPM={p.rpm}, " +
    $"PV2={p.pv2Voltage}, " +
    $"PA4={p.pa4Current}"
);

        Debug.Log(
            $"CALC Table22 | " +
            $"P2g={p2g}, " +
            $"etaG={etaG}, " +
            $"P2d={p2d}, " +
            $"ifMotor={ifMotor}, " +
            $"P1d={p1d}, " +
            $"etaD={etaD * 100f}%"
        );
        if (p2d > p1d * 1.02f)
        {
            Debug.LogWarning(
                $"Некорректный КПД двигателя: P2д ({p2d:F2}) > P1д ({p1d:F2}). " +
                $"Строка, вероятно, построена из неверных измерений."
            );
        }
        if (p2g > p1d + 0.01f)
        {
            Debug.LogWarning(
                $"Физически невозможные данные: P2г ({p2g:F2}) > P1д ({p1d:F2}). " +
                $"Проверь датчики PV1/PA1/PV2/PA4 и их маппинг."
            );
        }
        return new Table22Row
        {
            Ug = p.pv1Voltage,
            Iaq = p.pa1Current,
            Ifg = ifMotor,
            N = p.rpm,
            Ur = p.pv2Voltage,
            Iag = p.pa4Current,

            P2g = p2g,
            P1d = p1d,
            P2d = p2d,
            M2d = m2d,
            Omega = omega,
            EtaD = etaD * 100f
        };
    }

    public Table23Row BuildTable23Row(MeasurementPoint p)
    {
        return new Table23Row
        {
            U = p.pv1Voltage,
            N = p.rpm,
            Omega = CalculateOmega(p.rpm)
        };
    }

    public Table24Row BuildTable24Row(MeasurementPoint p)
    {
        float ifMotor = p.pa2CurrentMilliAmp / 1000f;

        return new Table24Row
        {
            If = ifMotor,
            Ia = p.pa1Current
        };
    }

    public Table25Row BuildTable25Row(MeasurementPoint p)
    {
        float ifMotor = p.pa2CurrentMilliAmp / 1000f;

        return new Table25Row
        {
            If = ifMotor,
            Omega = CalculateOmega(p.rpm)
        };
    }

    private float CalculateOmega(float rpm)
    {
        return rpm * 2f * Mathf.PI / 60f;
    }
}