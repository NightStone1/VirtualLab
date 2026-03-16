using TMPro;
using UnityEngine;

public class Table22RowView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ugText;
    [SerializeField] private TextMeshProUGUI iaqText;
    [SerializeField] private TextMeshProUGUI ifText;
    [SerializeField] private TextMeshProUGUI nText;
    [SerializeField] private TextMeshProUGUI urText;
    [SerializeField] private TextMeshProUGUI iagText;
    [SerializeField] private TextMeshProUGUI p2gText;
    [SerializeField] private TextMeshProUGUI p1dText;
    [SerializeField] private TextMeshProUGUI p2dText;
    [SerializeField] private TextMeshProUGUI m2dText;
    [SerializeField] private TextMeshProUGUI omegaText;
    [SerializeField] private TextMeshProUGUI etaDText;

    public void Bind(Table22Row row)
    {
        ugText.text = row.Ug.ToString("F2");
        iaqText.text = row.Iaq.ToString("F3");
        ifText.text = row.Ifg.ToString("F3");
        nText.text = row.N.ToString("F2");
        urText.text = row.Ur.ToString("F2");
        iagText.text = row.Iag.ToString("F3");
        p2gText.text = row.P2g.ToString("F2");
        p1dText.text = row.P1d.ToString("F2");
        p2dText.text = row.P2d.ToString("F2");
        m2dText.text = row.M2d.ToString("F3");
        omegaText.text = row.Omega.ToString("F2");
        etaDText.text = row.EtaD.ToString("F2");
    }
}