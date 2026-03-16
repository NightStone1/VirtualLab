using System.Collections.Generic;
using UnityEngine;

public class Table25Presenter : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private Table25RowView rowPrefab;

    public void RefreshTable()
    {
        if (resultsManager == null)
        {
            Debug.LogError("Table25Presenter: LabResultsManager не назначен.");
            return;
        }

        if (rowsParent == null)
        {
            Debug.LogError("Table25Presenter: rowsParent не назначен.");
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogError("Table25Presenter: rowPrefab не назначен.");
            return;
        }

        ClearRows();

        IReadOnlyList<Table25Row> rows = resultsManager.Table25Rows;
        for (int i = 0; i < rows.Count; i++)
        {
            Table25RowView rowView = Instantiate(rowPrefab, rowsParent);
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