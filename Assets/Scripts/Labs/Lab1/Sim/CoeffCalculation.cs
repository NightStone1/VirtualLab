using UnityEngine;

public static class CoeffCalculation
{
    private const float MaxP2gRatio = 0.55f; // мягкий ограничитель

    public static void Simulate(
        bool Q1, bool Q2, bool Q3, bool engineIsOn,
        float PNO, float R1, float R2, float R3,
        out float PA1, out float PA2, out float PA3, out float PA4, out float PV2, out float RPM)
    {
        PA1 = PA2 = PA3 = PA4 = PV2 = RPM = 0f;

        if (!Q1 || !Q2 || !engineIsOn)
            return;

        float pho = Mathf.Clamp(PNO, 50f, 230f);
        float r1 = Mathf.Clamp01(R1 / 100f);
        float r2 = Mathf.Clamp01(R2 / 100f);
        float r3 = Mathf.Clamp01(R3 / 100f);

        if (!Q3)
        {
            SimulateNoLoad(pho, r1, r2, out PA1, out PA2, out PA3, out PA4, out PV2, out RPM);
        }
        else
        {
            SimulateLoaded(pho, r1, r2, r3, out PA1, out PA2, out PA3, out PA4, out PV2, out RPM);
        }

        PA1 = Mathf.Clamp(PA1, 0f, 5f);
        PA2 = Mathf.Clamp(PA2, 0f, 0.25f);
        PA3 = Mathf.Clamp(PA3, 0f, 0.25f);
        PA4 = Mathf.Clamp(PA4, 0f, 5f);
        PV2 = Mathf.Clamp(PV2, 0f, 250f);
        RPM = Mathf.Clamp(RPM, 0f, 2750f);
    }

    private static void SimulateNoLoad(
        float pho, float r1, float r2,
        out float pa1, out float pa2, out float pa3, out float pa4, out float pv2, out float rpm)
    {
        float pa1Base = EvalPHO_PA1(pho);
        float pa2Base = EvalPHO_PA2(pho);
        float pa3Base = EvalPHO_PA3(pho);
        float pv2Base = EvalPHO_PV2(pho);
        float rpmBase = EvalPHO_RPM(pho);

        float kR1_PA1 = SafeRatio(EvalR1_PA1(r1), EvalR1_PA1(0f));
        float kR1_PA2 = SafeRatio(EvalR1_PA2(r1), EvalR1_PA2(0f));
        float kR1_RPM = SafeRatio(EvalR1_RPM(r1), EvalR1_RPM(0f));

        float kR2_PV2 = SafeRatio(EvalR2_PV2(r2), EvalR2_PV2(0f));
        float kR2_PA3 = SafeRatio(EvalR2_PA3(r2), EvalR2_PA3(0f));

        pa1 = pa1Base * kR1_PA1;
        pa2 = pa2Base * kR1_PA2;
        pa3 = pa3Base * kR2_PA3;
        pv2 = pv2Base * kR2_PV2;
        rpm = rpmBase * kR1_RPM;
        pa4 = 0f;
    }

    private static void SimulateLoaded(
        float pho, float r1, float r2, float r3,
        out float pa1, out float pa2, out float pa3, out float pa4, out float pv2, out float rpm)
    {
        // 1. Ненагруженная база для текущего PHO/R1/R2
        SimulateNoLoad(pho, r1, r2, out float pa1NoLoad, out float pa2NoLoad, out float pa3NoLoad, out _, out float pv2NoLoad, out float rpmNoLoad);

        // 2. Нагруженная абсолютная таблица R3 снята при PHO = 150
        float pa1R3 = EvalR3_PA1(r3);
        float pa2R3 = EvalR3_PA2(r3);
        float pa3R3 = EvalR3_PA3(r3);
        float pa4R3 = EvalR3_PA4(r3);
        float pv2R3 = EvalR3_PV2(r3);
        float rpmR3 = EvalR3_RPM(r3);

        // 3. База для PHO=150 без нагрузки
        float pa1Base150 = EvalPHO_PA1(150f);
        float pa2Base150 = EvalPHO_PA2(150f);
        float pa3Base150 = EvalPHO_PA3(150f);
        float pv2Base150 = EvalPHO_PV2(150f);
        float rpmBase150 = EvalPHO_RPM(150f);

        // 4. Коэффициенты перехода "ненагруженный 150" -> "нагруженный R3"
        float kLoad_PA1 = SafeRatio(pa1R3, pa1Base150);
        float kLoad_PA2 = SafeRatio(pa2R3, pa2Base150);
        float kLoad_PA3 = SafeRatio(pa3R3, pa3Base150);
        float kLoad_PV2 = SafeRatio(pv2R3, pv2Base150);
        float kLoad_RPM = SafeRatio(rpmR3, rpmBase150);

        // 5. Применяем нагрузку к текущему состоянию
        pa1 = pa1NoLoad * kLoad_PA1;
        pa2 = pa2NoLoad * kLoad_PA2;
        pa3 = pa3NoLoad * kLoad_PA3;
        pv2 = pv2NoLoad * kLoad_PV2;
        rpm = rpmNoLoad * kLoad_RPM;

        // 6. PA4 переносим по масштабу текущего состояния
        float stateScalePV2 = SafeRatio(pv2NoLoad, pv2Base150);
        float stateScaleRPM = SafeRatio(rpmNoLoad, rpmBase150);
        float stateScale = 0.5f * stateScalePV2 + 0.5f * stateScaleRPM;
        pa4 = pa4R3 * stateScale;

        // 7. Ограничение по энергии
        float ug = pho;
        float p1d = ug * (pa1 + pa2);
        float p2g = pv2 * pa4;
        float maxP2g = MaxP2gRatio * p1d;

        if (p2g > maxP2g && p2g > 0.0001f)
        {
            float scale = Mathf.Sqrt(maxP2g / p2g);
            pv2 *= scale;
            pa4 *= scale;
        }
    }

    // ---------------- PHO ----------------
    private static float EvalPHO_PA1(float pho)
    {
        return 0.8f;
    }

    private static float EvalPHO_PA2(float pho)
    {
        float[] x = { 50f, 100f, 150f, 200f, 230f };
        float[] y = { 0.050f, 0.080f, 0.095f, 0.110f, 0.123f };
        return EvalPiecewise(x, y, pho);
    }

    private static float EvalPHO_PA3(float pho)
    {
        float[] x = { 50f, 100f, 150f, 200f, 230f };
        float[] y = { 0.015f, 0.027f, 0.045f, 0.062f, 0.075f };
        return EvalPiecewise(x, y, pho);
    }

    private static float EvalPHO_PV2(float pho)
    {
        float[] x = { 50f, 100f, 150f, 200f, 230f };
        float[] y = { 25f, 70f, 140f, 210f, 250f };
        return EvalPiecewise(x, y, pho);
    }

    private static float EvalPHO_RPM(float pho)
    {
        float[] x = { 50f, 100f, 150f, 200f, 230f };
        float[] y = { 750f, 1500f, 1900f, 2300f, 2450f };
        return EvalPiecewise(x, y, pho);
    }

    // ---------------- R1 ----------------
    private static float EvalR1_PA1(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 0.80f, 0.60f, 0.55f, 0.54f, 0.51f };
        return EvalPiecewise01(x, y, r);
    }

    private static float EvalR1_PA2(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 0.090f, 0.110f, 0.115f, 0.125f, 0.165f };
        return EvalPiecewise01(x, y, r);
    }

    private static float EvalR1_RPM(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 1900f, 1740f, 1680f, 1540f, 1360f };
        return EvalPiecewise01(x, y, r);
    }

    // ---------------- R2 ----------------
    private static float EvalR2_PV2(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 140f, 155f, 175f, 215f, 250f };
        return EvalPiecewise01(x, y, r);
    }

    private static float EvalR2_PA3(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 0.045f, 0.050f, 0.060f, 0.070f, 0.105f };
        return EvalPiecewise01(x, y, r);
    }

    // ---------------- R3 absolute loaded table ----------------
    private static float EvalR3_PV2(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 100f, 90f, 77f, 60f, 0.5f };
        return EvalPiecewise01(x, y, r);
    }

    private static float EvalR3_PA1(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 1.45f, 1.50f, 1.675f, 1.90f, 2.75f };
        return EvalPiecewise01(x, y, r);
    }

    private static float EvalR3_PA2(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 0.090f, 0.090f, 0.090f, 0.090f, 0.090f };
        return EvalPiecewise01(x, y, r);
    }

    private static float EvalR3_PA3(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 0.045f, 0.045f, 0.040f, 0.040f, 0.040f };
        return EvalPiecewise01(x, y, r);
    }

    private static float EvalR3_PA4(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 0.50f, 0.55f, 0.75f, 1.00f, 2.00f };
        return EvalPiecewise01(x, y, r);
    }

    private static float EvalR3_RPM(float r)
    {
        float[] x = { 0f, 0.25f, 0.50f, 0.75f, 1f };
        float[] y = { 1520f, 1440f, 1350f, 1260f, 900f };
        return EvalPiecewise01(x, y, r);
    }

    // ---------------- Helpers ----------------
    private static float SafeRatio(float value, float basis)
    {
        return basis > 0.0001f ? value / basis : 1f;
    }

    private static float EvalPiecewise01(float[] x, float[] y, float t01)
    {
        return EvalPiecewise(x, y, Mathf.Clamp01(t01));
    }

    private static float EvalPiecewise(float[] x, float[] y, float value)
    {
        if (x == null || y == null || x.Length != y.Length || x.Length == 0)
            return 0f;

        if (value <= x[0])
            return y[0];

        int last = x.Length - 1;
        if (value >= x[last])
            return y[last];

        for (int i = 0; i < last; i++)
        {
            if (value >= x[i] && value <= x[i + 1])
            {
                float t = Mathf.InverseLerp(x[i], x[i + 1], value);
                return Mathf.Lerp(y[i], y[i + 1], t);
            }
        }

        return y[last];
    }
}