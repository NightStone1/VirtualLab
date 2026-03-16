using System;

[Serializable]
public struct CircuitSnapshot
{
    // Положение органов управления
    public float phoPercent;
    public float r1Percent;
    public float r2Percent;
    public float r3Percent;

    // Состояние ключей
    public bool q1Enabled;
    public bool q2Enabled;
    public bool q3Enabled;

    // Первичные измеренные величины
    public float pv1Voltage;
    public float pv2Voltage;
    public float pa1Current;
    public float pa2CurrentMilliAmp;
    public float pa3CurrentMilliAmp;
    public float pa4Current;
    public float rpm;
}