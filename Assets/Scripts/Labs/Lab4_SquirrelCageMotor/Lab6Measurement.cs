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
    public float za;
    public float zb;
    public float zc;
    public float zAverage;

    public override string ToString()
    {
        if (stage == Lab6Stage.ResistanceMeasurement)
        {
            return $"{stage}: Za={za:F2} Ohm, Zb={zb:F2} Ohm, Zc={zc:F2} Ohm, Zavg={zAverage:F2} Ohm";
        }

        return $"{stage}: Q2={q2Position}, U={voltage:F0} V, I={current:F2} A, P1={powerInput:F0} W, P2={powerOutput:F0} W, n={speed:F0} rpm, M={torque:F2} N*m, cos={cosPhi:F2}, eta={efficiency:F2}, s={slip:P1}";
    }
}
