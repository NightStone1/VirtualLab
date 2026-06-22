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

public class AnalogMeterView : MonoBehaviour
{
    public float minValue;
    public float maxValue = 1f;
    public float minAngle = -90f;
    public float maxAngle = 90f;
    public Transform needle;

    public void SetValue(float value)
    {
        if (needle == null)
        {
            return;
        }

        float t = Mathf.InverseLerp(minValue, maxValue, value);
        float angle = Mathf.Lerp(minAngle, maxAngle, t);
        needle.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
