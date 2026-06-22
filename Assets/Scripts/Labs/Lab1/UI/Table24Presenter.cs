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

using System.Collections.Generic;
using UnityEngine;

public class Table24Presenter : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private Table24RowView rowPrefab;

    private readonly List<Table24RowView> rowViews = new List<Table24RowView>();

    public void RefreshTable()
    {
        if (resultsManager == null)
        {
            Debug.LogError("Table24Presenter: LabResultsManager �� ��������.");
            return;
        }

        if (rowsParent == null)
        {
            Debug.LogError("Table24Presenter: rowsParent �� ��������.");
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogError("Table24Presenter: rowPrefab �� ��������.");
            return;
        }

        IReadOnlyList<Table24Row> rows = resultsManager.Table24Rows;
        EnsureRowCount(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            Table24RowView rowView = rowViews[i];
            rowView.gameObject.SetActive(true);
            rowView.Bind(rows[i]);
        }

        for (int i = rows.Count; i < rowViews.Count; i++)
        {
            rowViews[i].gameObject.SetActive(false);
        }
    }

    private void EnsureRowCount(int requiredCount)
    {
        while (rowViews.Count < requiredCount)
        {
            Table24RowView rowView = Instantiate(rowPrefab, rowsParent);
            rowView.gameObject.SetActive(false);
            rowViews.Add(rowView);
        }
    }
}
