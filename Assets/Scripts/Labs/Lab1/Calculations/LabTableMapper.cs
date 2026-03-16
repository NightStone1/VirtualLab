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