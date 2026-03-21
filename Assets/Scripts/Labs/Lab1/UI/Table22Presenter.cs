using System.Collections.Generic;
using UnityEngine;

public class Table22Presenter : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private Table22RowView rowPrefab;

    private readonly List<Table22RowView> rowViews = new List<Table22RowView>();

    public void RefreshTable()
    {
        if (resultsManager == null)
        {
            Debug.LogError("Table22Presenter: LabResultsManager �� ��������.");
            return;
        }

        if (rowsParent == null)
        {
            Debug.LogError("Table22Presenter: rowsParent �� ��������.");
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogError("Table22Presenter: rowPrefab �� ��������.");
            return;
        }

        IReadOnlyList<Table22Row> rows = resultsManager.Table22Rows;
        EnsureRowCount(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            Table22RowView rowView = rowViews[i];
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
            Table22RowView rowView = Instantiate(rowPrefab, rowsParent);
            rowView.gameObject.SetActive(false);
            rowViews.Add(rowView);
        }
    }
}
