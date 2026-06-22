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

public class Table25RowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ifText;
    [SerializeField] private TextMeshProUGUI omegaText;

    public void Bind(Table25Row row)
    {
        if (ifText == null)
        {
            Debug.LogError("Table25RowView: ifText не назначен.");
            return;
        }

        if (omegaText == null)
        {
            Debug.LogError("Table25RowView: omegaText не назначен.");
            return;
        }

        if (row == null)
        {
            Debug.LogError("Table25RowView: row == null.");
            return;
        }

        ifText.text = row.If.ToString("F3");
        omegaText.text = row.Omega.ToString("F2");
    }
}