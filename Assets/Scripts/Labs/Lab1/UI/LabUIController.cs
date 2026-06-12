using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LabUIController : MonoBehaviour
{
    private const string RuntimeRemoveLastButtonName = "RemoveLastCurrentTableRuntime";
    private const string RuntimeResetLabButtonName = "ResetLabRuntime";

    [SerializeField] private LabResultsManager resultsManager;
    [SerializeField] private ElectricCircuit circuit;
    [SerializeField] private TextMeshProUGUI currentModeText;
    [SerializeField] private bool createRuntimeRemoveLastButton = true;

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

    [Header("Hint Overlay")]
    [SerializeField] private HintOverlayController hintOverlay;

    private void Start()
    {
        ResolveReferences();
        RefreshModeText();
        RefreshVisibleTables();
        RefreshVisiblePanels();
        EnsureRuntimeRemoveLastButton();
        EnsureRuntimeResetLabButton();
    }

    public void RemoveLastRow()
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: resultsManager не назначен.");
            return;
        }

        bool removed = resultsManager.RemoveLastRowInCurrentMode();

        if (!removed)
        {
            Debug.Log(resultsManager.LastMessage);
            return;
        }

        Debug.Log(resultsManager.LastMessage);
        RefreshVisibleTables();

        if (hintOverlay != null)
            hintOverlay.RefreshForCurrentMode();
    }

    public void OpenHint()
    {
        if (hintOverlay == null)
        {
            Debug.LogWarning("LabUIController: hintOverlay не назначен.");
            return;
        }

        hintOverlay.Show();
    }

    public void CloseHint()
    {
        if (hintOverlay == null)
        {
            Debug.LogWarning("LabUIController: hintOverlay не назначен.");
            return;
        }

        hintOverlay.Hide();
    }

    public void CaptureCurrentPoint()
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: LabResultsManager не назначен.");
            return;
        }

        Debug.Log("UI button clicked -> capture current lab mode point");
        resultsManager.TryCaptureCurrentModePoint();
        Debug.Log(resultsManager.LastMessage);
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
        Debug.Log(resultsManager.LastMessage);
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
        Debug.Log(resultsManager.LastMessage);
        RefreshVisibleTables();
    }

    public void ResetLab()
    {
        ResolveReferences();

        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: LabResultsManager не назначен.");
            return;
        }

        resultsManager.ResetLabResults();
        if (circuit != null)
        {
            circuit.ResetCircuit();
        }
        else
        {
            Debug.LogWarning("LabUIController: ElectricCircuit не найден, органы управления стенда не сброшены.");
        }

        Debug.Log(resultsManager.LastMessage);
        RefreshModeText();
        RefreshVisibleTables();
        RefreshVisiblePanels();

        if (hintOverlay != null)
            hintOverlay.RefreshForCurrentMode();
    }

    private void ResolveReferences()
    {
        if (circuit == null)
        {
            ElectricCircuit[] circuits = FindObjectsByType<ElectricCircuit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            circuit = circuits.Length > 0 ? circuits[0] : null;
        }
    }

    private void EnsureRuntimeRemoveLastButton()
    {
        if (!createRuntimeRemoveLastButton || FindRuntimeRemoveLastButton() != null)
        {
            return;
        }

        GameObject templateObject = GameObject.Find("ClearCurrentTable");
        if (templateObject == null || templateObject.transform.parent == null)
        {
            return;
        }

        RectTransform templateRect = templateObject.GetComponent<RectTransform>();
        Image templateImage = templateObject.GetComponent<Image>();

        GameObject buttonObject = new GameObject(RuntimeRemoveLastButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(templateObject.transform.parent, false);
        buttonObject.transform.SetSiblingIndex(templateObject.transform.GetSiblingIndex() + 1);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (templateRect != null)
        {
            rect.anchorMin = templateRect.anchorMin;
            rect.anchorMax = templateRect.anchorMax;
            rect.pivot = templateRect.pivot;
            rect.sizeDelta = templateRect.sizeDelta;
            rect.anchoredPosition = templateRect.anchoredPosition;
        }
        else
        {
            rect.sizeDelta = new Vector2(160f, 30f);
        }

        Image image = buttonObject.GetComponent<Image>();
        if (templateImage != null)
        {
            image.sprite = templateImage.sprite;
            image.type = templateImage.type;
            image.color = templateImage.color;
        }

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(RemoveLastRow);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "Удалить точку";
        text.fontSize = 14f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;
    }

    private void EnsureRuntimeResetLabButton()
    {
        if (!createRuntimeRemoveLastButton || FindRuntimeButton(RuntimeResetLabButtonName) != null)
        {
            return;
        }

        GameObject templateObject = FindRuntimeRemoveLastButton();
        if (templateObject == null)
        {
            templateObject = GameObject.Find("ClearAllTables");
        }

        if (templateObject == null || templateObject.transform.parent == null)
        {
            return;
        }

        CreateRuntimeButton(templateObject, RuntimeResetLabButtonName, "Сброс", ResetLab);
    }

    private static GameObject FindRuntimeRemoveLastButton()
    {
        return FindRuntimeButton(RuntimeRemoveLastButtonName);
    }

    private static GameObject FindRuntimeButton(string buttonName)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == buttonName)
            {
                return buttons[i].gameObject;
            }
        }

        return null;
    }

    private static GameObject CreateRuntimeButton(GameObject templateObject, string objectName, string label, UnityEngine.Events.UnityAction action)
    {
        RectTransform templateRect = templateObject.GetComponent<RectTransform>();
        Image templateImage = templateObject.GetComponent<Image>();

        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(templateObject.transform.parent, false);
        buttonObject.transform.SetSiblingIndex(templateObject.transform.GetSiblingIndex() + 1);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (templateRect != null)
        {
            rect.anchorMin = templateRect.anchorMin;
            rect.anchorMax = templateRect.anchorMax;
            rect.pivot = templateRect.pivot;
            rect.sizeDelta = templateRect.sizeDelta;
            rect.anchoredPosition = templateRect.anchoredPosition;
        }
        else
        {
            rect.sizeDelta = new Vector2(160f, 30f);
        }

        Image image = buttonObject.GetComponent<Image>();
        if (templateImage != null)
        {
            image.sprite = templateImage.sprite;
            image.type = templateImage.type;
            image.color = templateImage.color;
        }

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 14f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;

        return buttonObject;
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

    public void OnModeChanged(LabMode mode)
    {
        if (resultsManager == null)
        {
            Debug.LogError("LabUIController: resultsManager не назначен.");
            return;
        }

        resultsManager.SetMode(mode);

        if (hintOverlay != null)
            hintOverlay.RefreshForCurrentMode();
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
