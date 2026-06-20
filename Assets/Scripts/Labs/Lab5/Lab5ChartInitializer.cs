using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Lab5ChartTableView), typeof(Lab5ChartButtonGenerator))]
public class Lab5ChartInitializer : MonoBehaviour
{
    public RectTransform buttonPanel;
    public TMP_Text tableText;
    public RectTransform graphPanel;
    public TMP_Text legendText;

    [ContextMenu("Initialize Chart UI")]
    public void Initialize()
    {
        SetupTableView();
        SetupButtonGenerator();
        SetupGraphView();
    }

    private void SetupTableView()
    {
        var tv = GetComponent<Lab5ChartTableView>();
        tv.controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();
        tv.autoFindController = true;
        tv.autoFindText = false;

        if (tableText == null)
        {
            var existingText = GetComponentInChildren<TMP_Text>();
            if (existingText != null && existingText.gameObject != gameObject)
                tableText = existingText;
        }

        tv.targetText = tableText;
    }

    private void SetupButtonGenerator()
    {
        var gen = GetComponent<Lab5ChartButtonGenerator>();
        gen.buttonPanel = buttonPanel;

        if (buttonPanel == null)
            gen.buttonPanel = transform as RectTransform;
    }

    private void SetupGraphView()
    {
        if (graphPanel == null)
        {
            var existingGraph = GetComponentInChildren<Lab5ChartGraphView>();
            if (existingGraph != null)
                return;
        }

        var gv = gameObject.AddComponent<Lab5ChartGraphView>();
        gv.controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();
        gv.autoFindController = true;
        gv.plotRoot = graphPanel;
        gv.legendText = legendText;
        gv.syncTableView = GetComponent<Lab5ChartTableView>();
    }
}
