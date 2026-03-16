using System;
using UnityEngine;

public class CoeffCalculation : MonoBehaviour
{
    public static void Simulate(bool Q1, bool Q2, bool Q3, bool engineIsOn,
        float PNO, float R1, float R2, float R3,
        out float PA1, out float PA2, out float PA3, out float PA4, out float PV2, out float RPM)
    {
        float r1 = R1 / 100f;
        float r2 = R2 / 100f;
        float r3 = R3 / 100f;
        bool loadOn = Q1 && Q2 && Q3 && engineIsOn;
        // âûêëþ÷åíî
        if (!Q1 || !Q2 || !engineIsOn)
        {
            PA1 = PA2 = PA3 = PA4 = PV2 = RPM = 0f;
            return;
        }
        // ===== ÁÀÇÀ ÏÎ PNO =====
        float baseRPM = 0f;
        float basePA1 = 0.8f;
        float basePA2 = 0f;
        float basePA3 = 0f;
        float basePV2 = 0f;
        if (PNO>=50 && PNO<=100) 
        {
            float t = (PNO - 50) / 50f;
            baseRPM = Mathf.Lerp(750, 1500, t);
            basePA2 = Mathf.Lerp(0.05f, 0.08f, t);
            basePA3 = Mathf.Lerp(0.015f, 0.027f, t);
            basePV2 = Mathf.Lerp(25, 70, t);
        }
        if (PNO > 100)
        {
            float t = (PNO - 100) / 150f;
            baseRPM = Mathf.Lerp(1500, 2750, t);
            basePA2 = Mathf.Lerp(0.08f, 0.123f, t);
            basePA3 = Mathf.Lerp(0.027f, 0.075f, t);
            basePV2 = Mathf.Lerp(70, 250, t);
        }
        // ===== PA1 =====
        PA1 = basePA1
            * Mathf.Lerp(1f, 0.64f, r1)
            * (loadOn ? Mathf.Lerp(1f, 1.25f, r3) : 1f);

        // ===== PA2 =====
        PA2 = basePA2 * Mathf.Lerp(1f, 1.83f, r1);

        // ===== PA3 =====
        PA3 = basePA3 * Mathf.Lerp(1f, 1.25f, r2); //1.42857f


        // ===== PA4 =====
        float basePA4_R3 = Mathf.Lerp(0.5f, 2.0f, r3);
        float pnoFactor = PNO / 150f;
        PA4 = loadOn ? basePA4_R3 * pnoFactor : 0f;


        // ===== PV2 =====
        PV2 = basePV2 * Mathf.Lerp(1f, 1.79f, r2);
        if (loadOn)
            PV2 *= Mathf.Lerp(0.71428f, 0.001f, r3 * r3);

        // ===== RPM =====
        RPM = baseRPM
            * Mathf.Lerp(1f, 0.721053f, r1)
            * (loadOn ? Mathf.Lerp(0.8f, 0.47368f, r3) : 1f); //0.8f => 0.75f


        // ===== CLAMP =====
        PA1 = Mathf.Clamp(PA1, 0f, 5f);
        PA2 = Mathf.Clamp(PA2, 0f, 0.25f);
        PA3 = Mathf.Clamp(PA3, 0f, 0.25f);
        PA4 = Mathf.Clamp(PA4, 0f, 5f);
        PV2 = Mathf.Clamp(PV2, 0f, 250f);
        RPM = Mathf.Clamp(RPM, 0f, 2750f);
        return;
        //if (!loadOn)
        //{
        //    RPM = baseRPM * Mathf.Lerp(1, 0.721053f, r1);
        //    PA1 = PA2 = PA3 = PA4 = PV2 = 0f;
        //    return;
        //}
    }
}
