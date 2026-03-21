using System.Collections.Generic;
using UnityEngine;

public class Table25Presenter : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private Table25RowView rowPrefab;

    private readonly List<Table25RowView> rowViews = new List<Table25RowView>();

    public void RefreshTable()
    {
        if (resultsManager == null)
        {
            Debug.LogError("Table25Presenter: LabResultsManager �� ��������.");
            return;
        }

        if (rowsParent == null)
        {
            Debug.LogError("Table25Presenter: rowsParent �� ��������.");
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogError("Table25Presenter: rowPrefab �� ��������.");
            return;
        }

        IReadOnlyList<Table25Row> rows = resultsManager.Table25Rows;
        EnsureRowCount(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            Table25RowView rowView = rowViews[i];
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
            Table25RowView rowView = Instantiate(rowPrefab, rowsParent);
            rowView.gameObject.SetActive(false);
            rowViews.Add(rowView);
        }
    }
}
