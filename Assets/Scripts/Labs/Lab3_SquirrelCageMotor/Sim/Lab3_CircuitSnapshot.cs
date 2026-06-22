using System;

[Serializable]
public struct Lab3_CircuitSnapshot  
{
    public float r1Percent;            // R1 — реостат возбуждения G1 (%)
    public float r2Percent;            // R2 — нагрузочный реостат (%)
    public bool q1Enabled;             // Q1 — питание M1
    public bool q2Enabled;             // Q2 — цепь якоря G1
    public bool q3Enabled;             // Q3 — цепь возбуждения G1
    public float pv1Voltage;           // PV1 — напряжение питающей сети L1/L2/L3, В
    public float pv2Voltage;           // PV2 — напряжение генератора G1, В
    public float pa1Current;           // PA1 — ток двигателя M1, А
    public float pa2CurrentMilliAmp;   // PA2 — ток якоря G1, мА
    public float pa3CurrentMilliAmp;   // PA3 — ток возбуждения G1, мА
    public float rpm;                  // скорость вращения M1-G1, об/мин
}