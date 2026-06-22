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

[CreateAssetMenu(fileName = "Lab6Data", menuName = "VLab/Lab6 Data")]
public class Lab6Data : ScriptableObject
{
    public float nominalVoltage = 380f;
    public float nominalCurrent = 5f;
    public float nominalPower = 1500f;
    public float synchronousSpeed = 1500f;
    public float statorResistance = 2f;
    public float maxVoltage = 420f;
    public float maxLoadPercent = 120f;
    public int requiredNoLoadPoints = 5;
    public int requiredShortCircuitPoints = 5;
    public int requiredLoadPoints = 5;
}
