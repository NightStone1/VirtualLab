using TMPro;
using UnityEngine;

public class Table25RowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ifText;
    [SerializeField] private TextMeshProUGUI omegaText;

    public void Bind(Table25Row row)
    {
        if (ifText == null)
        {
            Debug.LogError("Table25RowView: ifText не назначен.");
            return;
        }

        if (omegaText == null)
        {
            Debug.LogError("Table25RowView: omegaText не назначен.");
            return;
        }

        if (row == null)
        {
            Debug.LogError("Table25RowView: row == null.");
            return;
        }

        ifText.text = row.If.ToString("F3");
        omegaText.text = row.Omega.ToString("F2");
    }
}