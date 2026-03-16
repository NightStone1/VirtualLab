using TMPro;
using UnityEngine;

public class Table23RowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI uText;
    [SerializeField] private TextMeshProUGUI nText;
    [SerializeField] private TextMeshProUGUI omegaText;

    public void Bind(Table23Row row)
    {
        uText.text = row.U.ToString("F2");
        nText.text = row.N.ToString("F2");
        omegaText.text = row.Omega.ToString("F2");
    }
}