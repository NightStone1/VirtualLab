using System.Collections.Generic;
using UnityEngine;

public class Table22Presenter : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private Table22RowView rowPrefab;

    public void RefreshTable()
    {
        if (resultsManager == null)
        {
            Debug.LogError("Table22Presenter: LabResultsManager не назначен.");
            return;
        }

        if (rowsParent == null)
        {
            Debug.LogError("Table22Presenter: rowsParent не назначен.");
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogError("Table22Presenter: rowPrefab не назначен.");
            return;
        }

        ClearRows();

        IReadOnlyList<Table22Row> rows = resultsManager.Table22Rows;
        for (int i = 0; i < rows.Count; i++)
        {
            Table22RowView rowView = Instantiate(rowPrefab, rowsParent);
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