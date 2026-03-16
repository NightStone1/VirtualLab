using UnityEngine;

public static class MeasurementPointExtensions
{
    public static float GetValue(this MeasurementPoint p, GraphField field)
    {
        return field switch
        {
            //GraphField.PHO => p.PHO,
            //GraphField.R1 => p.R1,
            //GraphField.R2 => p.R2,
            //GraphField.R3 => p.R3,
            //GraphField.PV1 => p.PV1,
            //GraphField.PV2 => p.PV2,
            //GraphField.PA1 => p.PA1,
            //GraphField.PA2 => p.PA2,
            //GraphField.PA3 => p.PA3,
            //GraphField.PA4 => p.PA4,
            //GraphField.RPM => p.RPM,
            //GraphField.Omega => p.Omega,
            //GraphField.InputPower => p.InputPower,
            //GraphField.OutputPower => p.OutputPower,
            //GraphField.Efficiency => p.Efficiency,
            //GraphField.Torque => p.Torque,
            //_ => 0f
        };
    }
}