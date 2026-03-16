using UnityEngine;
using TMPro;

public class LabUIController : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private TextMeshProUGUI currentModeText;

    [Header("Presenters")]
    [SerializeField] private Table22Presenter table22Presenter;
    [SerializeField] private Table23Presenter table23Presenter;
    [SerializeField] private Table24Presenter table24Presenter;
    [SerializeField] private Table25Presenter table25Presenter;

    [Header("Table Panels")]
    [SerializeField] private GameObject table22Panel;
    [SerializeField] private GameObject table23Panel;
    [SerializeField] private GameObject table24Panel;
    [SerializeField] private GameObject table25Panel;

    private void Start()
    {
        RefreshModeText();
        RefreshVisibleTables();
        RefreshVisiblePanels();
    }

    public void CaptureCurrentPoint()
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: LabResultsManager не назначен.");
            return;
        }

        Debug.Log("UI button clicked -> capture current lab mode point");
        resultsManager.CaptureCurrentModePoint();
        RefreshVisibleTables();
    }

    public void SetModeTable22()
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: LabResultsManager не назначен.");
            return;
        }

        resultsManager.SetModeTable22();
        RefreshModeText();
        RefreshVisibleTables();
        RefreshVisiblePanels();
    }

    public void SetModeTable23()
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: LabResultsManager не назначен.");
            return;
        }

        resultsManager.SetModeTable23();
        RefreshModeText();
        RefreshVisibleTables();
        RefreshVisiblePanels();
    }

    public void SetModeTable24()
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: LabResultsManager не назначен.");
            return;
        }

        resultsManager.SetModeTable24();
        RefreshModeText();
        RefreshVisibleTables();
        RefreshVisiblePanels();
    }

    public void SetModeTable25()
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: LabResultsManager не назначен.");
            return;
        }

        resultsManager.SetModeTable25();
        RefreshModeText();
        RefreshVisibleTables();
        RefreshVisiblePanels();
    }

    public void ClearCurrentTable()
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: LabResultsManager не назначен.");
            return;
        }

        resultsManager.ClearCurrentMode();
        RefreshVisibleTables();
    }

    public void ClearAllTables()
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: LabResultsManager не назначен.");
            return;
        }

        resultsManager.ClearAllTables();
        RefreshVisibleTables();
    }

    private void RefreshModeText()
    {
        if (currentModeText == null || resultsManager == null)
            return;

        currentModeText.text = GetModeDisplayName(resultsManager.CurrentMode);
    }

    private void RefreshVisibleTables()
    {
        if (table22Presenter != null)
            table22Presenter.RefreshTable();

        if (table23Presenter != null)
            table23Presenter.RefreshTable();

        if (table24Presenter != null)
            table24Presenter.RefreshTable();

        if (table25Presenter != null)
            table25Presenter.RefreshTable();
    }

    private void RefreshVisiblePanels()
    {
        if (resultsManager == null)
            return;

        if (table22Panel != null)
            table22Panel.SetActive(resultsManager.CurrentMode == LabMode.Table22_Working);

        if (table23Panel != null)
            table23Panel.SetActive(resultsManager.CurrentMode == LabMode.Table23_OmegaFromU);

        if (table24Panel != null)
            table24Panel.SetActive(resultsManager.CurrentMode == LabMode.Table24_IfFromIa);

        if (table25Panel != null)
            table25Panel.SetActive(resultsManager.CurrentMode == LabMode.Table25_OmegaFromIf);
    }

    private string GetModeDisplayName(LabMode mode)
    {
        return mode switch
        {
            LabMode.Table22_Working => "Текущий режим: Таблица 2.2 — Рабочие характеристики",
            LabMode.Table23_OmegaFromU => "Текущий режим: Таблица 2.3 — ω = f(U)",
            LabMode.Table24_IfFromIa => "Текущий режим: Таблица 2.4 — If = f(Ia)",
            LabMode.Table25_OmegaFromIf => "Текущий режим: Таблица 2.5 — ω = f(If)",
            _ => "Текущий режим: не выбран"
        };
    }
}