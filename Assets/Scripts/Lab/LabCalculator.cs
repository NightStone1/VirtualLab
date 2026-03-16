using UnityEngine;

public class LabCalculator : MonoBehaviour
{
    public void FillCalculatedValues(ref MeasurementPoint point)
    {
        point.omega = point.rpm * 2f * Mathf.PI / 60f;

        // Временная базовая логика.
        // Позже можно заменить на формулы строго по методичке.
        point.inputPower = point.pv1Voltage * point.pa1Current;
        point.outputPower = point.pv2Voltage * point.pa4Current;

        point.torque = point.omega > 0.001f
            ? point.outputPower / point.omega
            : 0f;

        point.efficiency = point.inputPower > 0.001f
            ? point.outputPower / point.inputPower
            : 0f;
    }
}