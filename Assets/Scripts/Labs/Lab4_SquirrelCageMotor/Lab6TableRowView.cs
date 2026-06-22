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

public class Lab6TableRowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] cells;

    private void Awake()
    {
        EnsureCells();
    }

    public void SetCells(params string[] values)
    {
        EnsureCells();

        if (cells == null)
        {
            return;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null)
            {
                continue;
            }

            cells[i].text = values != null && i < values.Length && values[i] != null ? values[i] : string.Empty;
        }
    }

    public void Clear()
    {
        SetCells();
    }

    private void EnsureCells()
    {
        if (cells == null || cells.Length == 0)
        {
            cells = GetComponentsInChildren<TextMeshProUGUI>(true);
        }
    }
}
