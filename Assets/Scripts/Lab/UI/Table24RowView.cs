using TMPro;
using UnityEngine;

public class Table24RowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ifText;
    [SerializeField] private TextMeshProUGUI iaText;

    public void Bind(Table24Row row)
    {
        ifText.text = row.If.ToString("F3");
        iaText.text = row.Ia.ToString("F3");
    }
}