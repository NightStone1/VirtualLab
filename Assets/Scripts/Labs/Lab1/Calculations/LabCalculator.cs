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