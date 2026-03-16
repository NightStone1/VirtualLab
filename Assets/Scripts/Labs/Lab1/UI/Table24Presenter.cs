using System.Collections.Generic;
using UnityEngine;

public class Table24Presenter : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private Table24RowView rowPrefab;

    public void RefreshTable()
    {
        if (resultsManager == null)
        {
            Debug.LogError("Table24Presenter: LabResultsManager не назначен.");
            return;
        }

        if (rowsParent == null)
        {
            Debug.LogError("Table24Presenter: rowsParent не назначен.");
            return;
        }

        if (rowPrefab == null)
        {
            Debug.LogError("Table24Presenter: rowPrefab не назначен.");
            return;
        }

        ClearRows();

        IReadOnlyList<Table24Row> rows = resultsManager.Table24Rows;
        for (int i = 0; i < rows.Count; i++)
        {
            Table24RowView rowView = Instantiate(rowPrefab, rowsParent);
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