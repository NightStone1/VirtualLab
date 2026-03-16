using System;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentManager : MonoBehaviour
{
    [SerializeField] private ElectricCircuit circuit;
    [SerializeField] private LabCalculator calculator;

    private readonly List<MeasurementPoint> points = new();
    private int nextIndex = 1;
    [ContextMenu("Test Table22 Mapping")]
    private void TestTable22Mapping()
    {
        MeasurementPoint p = CapturePoint(ExperimentSeries.PHO);
        if (p == null) return;

        LabTableMapper mapper = FindObjectOfType<LabTableMapper>();
        if (mapper == null)
        {
            Debug.LogError("LabTableMapper не найден на сцене.");
            return;
        }

        Table22Row row = mapper.BuildTable22Row(p);

        Debug.Log(
            $"Table22 -> Ug={row.Ug}, Iaq={row.Iaq}, Ifg={row.Ifg}, N={row.N}, Ur={row.Ur}, Iag={row.Iag}, " +
            $"P2g={row.P2g}, P2d={row.P2d}, M2d={row.M2d}, Omega={row.Omega}, EtaD={row.EtaD}"
        );
    }
    public IReadOnlyList<MeasurementPoint> Points => points;
    [ContextMenu("Capture PHO Point")]
    private void DebugCapturePHOPoint()
    {
        CapturePoint(ExperimentSeries.PHO);
    }
    [ContextMenu("Capture R1 Point")]
    private void DebugCaptureR1Point()
    {
        CapturePoint(ExperimentSeries.R1);
    }
    public MeasurementPoint CapturePoint(ExperimentSeries series, string note = "")
    {
        if (circuit == null)
        {
            Debug.LogError("ExperimentManager: ElectricCircuit не назначен.");
            return null;
        }

        if (calculator == null)
        {
            Debug.LogError("ExperimentManager: LabCalculator не назначен.");
            return null;
        }

        CircuitSnapshot snapshot = circuit.GetSnapshot();

        MeasurementPoint point = new MeasurementPoint
        {
            index = nextIndex++,
            series = series,

            phoPercent = snapshot.phoPercent,
            r1Percent = snapshot.r1Percent,
            r2Percent = snapshot.r2Percent,
            r3Percent = snapshot.r3Percent,

            q1Enabled = snapshot.q1Enabled,
            q2Enabled = snapshot.q2Enabled,
            q3Enabled = snapshot.q3Enabled,

            pv1Voltage = snapshot.pv1Voltage,
            pv2Voltage = snapshot.pv2Voltage,
            pa1Current = snapshot.pa1Current,
            pa2CurrentMilliAmp = snapshot.pa2CurrentMilliAmp,
            pa3CurrentMilliAmp = snapshot.pa3CurrentMilliAmp,
            pa4Current = snapshot.pa4Current,
            rpm = snapshot.rpm,

            note = note,
            timestamp = DateTime.Now.ToString("HH:mm:ss")
        };

        calculator.FillCalculatedValues(ref point);
        points.Add(point);

        Debug.Log(
    $"Точка #{point.index} сохранена. " +
    $"Серия: {point.series}, " +
    $"PHO={point.phoPercent}, R1={point.r1Percent}, R2={point.r2Percent}, R3={point.r3Percent}, " +
    $"PV1={point.pv1Voltage}, PV2={point.pv2Voltage}, " +
    $"PA1={point.pa1Current}, PA2={point.pa2CurrentMilliAmp}, PA3={point.pa3CurrentMilliAmp}, PA4={point.pa4Current}, " +
    $"RPM={point.rpm}"
);
        return point;
    }

    public void RemovePoint(int pointIndex)
    {
        points.RemoveAll(p => p.index == pointIndex);
    }

    public void ClearAll()
    {
        points.Clear();
        nextIndex = 1;
    }

    public List<MeasurementPoint> GetSeries(ExperimentSeries series)
    {
        return points.FindAll(p => p.series == series);
    }
}