using System.Collections.Generic;
using UnityEngine;

public class Table23Presenter : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private Table23RowView rowPrefab;

    private readonly List<Table23RowView> rowViews = new List<Table23RowView>();

    public void RefreshTable()
    {
        if (resultsManager == null)
        {
            Debug.LogError("Table23Presenter: LabResultsManager �� ��������.");
            return;
        }

        if (rowsParent == null)
        {
            Debug.LogError("Table23Presenter: rowsParent �� ��������.");
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogError("Table23Presenter: rowPrefab �� ��������.");
            return;
        }

        IReadOnlyList<Table23Row> rows = resultsManager.Table23Rows;
        EnsureRowCount(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            Table23RowView rowView = rowViews[i];
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
            Table23RowView rowView = Instantiate(rowPrefab, rowsParent);
            rowView.gameObject.SetActive(false);
            rowViews.Add(rowView);
        }
    }
}
