using System;

[Serializable]
public class Lab6Measurement
{
    public Lab6Stage stage;
    public int q2Position;
    public float voltage;
    public float loadPercent;
    public float current;
    public float powerInput;
    public float powerOutput;
    public float speed;
    public float torque;
    public float cosPhi;
    public float efficiency;
    public float slip;

    public override string ToString()
    {
        return $"{stage}: Q2={q2Position}, U={voltage:F0} V, I={current:F2} A, P1={powerInput:F0} W, P2={powerOutput:F0} W, n={speed:F0} rpm, M={torque:F2} N*m, cos={cosPhi:F2}, eta={efficiency:F2}, s={slip:P1}";
    }
}
