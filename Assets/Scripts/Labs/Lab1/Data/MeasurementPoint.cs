// Copyright (c) 2026 Бабичева Екатерина Анатольевна,
// Бибко Эдуард Александрович.
//
// Данный программный код разработан в рамках выпускной квалификационной работы
// "Виртуальный методический комплекс по дисциплине "Электрические машины"".
//
// Использование программного комплекса в учебном процессе АМТИ допускается
// в рамках подписанного акта о внедрении.
//
// Дальнейшее распространение, модификация, переработка, передача третьим лицам,
// публикация исходного кода, а также использование за пределами указанного
// внедрения допускаются только с письменного согласия авторов, если иное
// не предусмотрено отдельным соглашением.

using System;

[Serializable]
public class MeasurementPoint
{
    public int index;
    public ExperimentSeries series;

    // Положение органов управления
    public float phoPercent;
    public float r1Percent;
    public float r2Percent;
    public float r3Percent;

    // Состояние ключей
    public bool q1Enabled;
    public bool q2Enabled;
    public bool q3Enabled;

    // Первичные величины
    public float pv1Voltage;
    public float pv2Voltage;
    public float pa1Current;
    public float pa2CurrentMilliAmp;
    public float pa3CurrentMilliAmp;
    public float pa4Current;
    public float rpm;

    // Расчётные величины
    public float omega;
    public float inputPower;
    public float outputPower;
    public float torque;
    public float efficiency;

    public string note;
    public string timestamp;
}