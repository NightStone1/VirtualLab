using System.Collections.Generic;
using UnityEngine;

public class Table23Presenter : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private Table23RowView rowPrefab;

    public void RefreshTable()
    {
        if (resultsManager == null)
        {
            Debug.LogError("Table23Presenter: LabResultsManager не назначен.");
            return;
        }

        if (rowsParent == null)
        {
            Debug.LogError("Table23Presenter: rowsParent не назначен.");
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogError("Table23Presenter: rowPrefab не назначен.");
            return;
        }

        ClearRows();

        IReadOnlyList<Table23Row> rows = resultsManager.Table23Rows;
        for (int i = 0; i < rows.Count; i++)
        {
            Table23RowView rowView = Instantiate(rowPrefab, rowsParent);
            rowView.Bind(rows[i]);
        }
    }

    private void ClearRows()
    {
        for (int i = rowsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(rowsParent.GetChild(i).gameObject);
        }
    }
}