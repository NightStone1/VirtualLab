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

[System.Serializable]
public struct UCurvePoint
{
    public float If;
    public float Istat;
    public float Iactive;
    public float Ireactive;
    public float cosPhi;
    public float excitationCurrent;
    public float statorCurrent;
    public float activeCurrent;
    public float reactiveCurrent;
    public float powerFactor;
    public float loadPower;
    public float loadPercent;
    public UCurveSeries seriesType;
}
