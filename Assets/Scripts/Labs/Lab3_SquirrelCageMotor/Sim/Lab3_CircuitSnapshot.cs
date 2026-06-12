using System;

[Serializable]
public struct Lab3_CircuitSnapshot  
{
    public float r1Percent;
    public float r2Percent;
    public float r3Percent;
    public bool q1Enabled;
    public bool q2Enabled;
    public bool q3Enabled;
    public float pv1Voltage;
    public float pv2Voltage;
    public float pa1Current;
    public float pa2CurrentMilliAmp;
    public float pa3CurrentMilliAmp;
    public float rpm;
}