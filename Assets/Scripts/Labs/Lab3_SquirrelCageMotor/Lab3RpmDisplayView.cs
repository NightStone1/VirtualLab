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

using TMPro;
using UnityEngine;

public class Lab3RpmDisplayView : MonoBehaviour
{
    public Lab3_ElectricCircuit controller;
    public bool autoFindController = true;
    public TMP_Text targetText;
    public bool autoFindText = true;

    private void Awake()
    {
        if (controller == null && autoFindController)
            controller = FindFirstObjectByType<Lab3_ElectricCircuit>();

        if (targetText == null && autoFindText)
        {
            targetText = GetComponent<TMP_Text>();
            if (targetText == null)
                targetText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void Update()
    {
        if (targetText == null)
            return;

        if (controller == null)
        {
            targetText.text = "Нет данных";
            return;
        }

        float rpm = controller.RPMValue;
        targetText.text = $"n = {rpm:F0} об/мин";
    }
}
