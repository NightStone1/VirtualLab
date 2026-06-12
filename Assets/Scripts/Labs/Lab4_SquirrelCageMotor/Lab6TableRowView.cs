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
