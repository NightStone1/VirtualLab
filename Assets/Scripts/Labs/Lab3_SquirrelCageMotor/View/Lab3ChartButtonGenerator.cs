using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab3ChartButtonGenerator : MonoBehaviour
{
    public RectTransform buttonPanel;
    public bool generateOnStart = false;

    [Header("Префаб кнопки (опционально)")]
    public GameObject buttonPrefab;

    [Header("Настройки")]
    public Vector2 buttonSize = new Vector2(180f, 30f);
    public float spacing = 4f;

    [ContextMenu("Создать кнопки")]
    public void GenerateButtons()
    {
        if (buttonPanel == null)
            buttonPanel = transform as RectTransform;

        if (buttonPanel == null)
        {
            Debug.LogWarning("Lab3ChartButtonGenerator: buttonPanel не назначен");
            return;
        }

        SetupLayout();
        RemoveGeneratedChildren();

        var tableTypes = new (string label, Lab3ChartTableView.TableType type)[]
        {
            ("Т1.1 Сопротивления", Lab3ChartTableView.TableType.Table3_1_Resistance),
            ("Т1.2 ХХ",            Lab3ChartTableView.TableType.Table3_2_NoLoad),
            ("Т1.3 Нагрузочная",   Lab3ChartTableView.TableType.Table3_3_Load),
            ("Т1.4 Внешняя",       Lab3ChartTableView.TableType.Table3_4_External),
            ("Т1.5 Регулировочная", Lab3ChartTableView.TableType.Table3_5_Regulating),
            ("Т1.6 КЗ",            Lab3ChartTableView.TableType.Table3_6_ShortCircuit),
        };

        foreach (var (label, type) in tableTypes)
            CreateSwitchButton(label, type);

        AddSeparator();
        CreateActionButton("Записать точку", "record");
        CreateActionButton("Удалить точку", "remove");
        CreateActionButton("Следующий этап", "next");
        CreateActionButton("Очистить таблицу", "clear");
        CreateActionButton("Сброс схемы", "reset");

        AddSeparator();
        CreateActionButton("SC вкл/выкл", "sc_toggle");
        CreateActionButton("R mode", "r_mode");
        CreateActionButton("Tune U", "tune_u");
    }

    private void Start()
    {
        if (generateOnStart)
            GenerateButtons();
    }

    private void SetupLayout()
    {
        buttonPanel.sizeDelta = new Vector2(190f, buttonPanel.sizeDelta.y);

        var vlg = buttonPanel.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
            vlg = buttonPanel.gameObject.AddComponent<VerticalLayoutGroup>();

        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = spacing;
        vlg.padding = new RectOffset(5, 5, 5, 5);

        var csf = buttonPanel.GetComponent<ContentSizeFitter>();
        if (csf == null)
            csf = buttonPanel.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var parent = buttonPanel.parent as RectTransform;
        if (parent != null)
        {
            var pcsf = parent.GetComponent<ContentSizeFitter>();
            if (pcsf == null)
                pcsf = parent.gameObject.AddComponent<ContentSizeFitter>();
            pcsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void RemoveGeneratedChildren()
    {
        for (int i = buttonPanel.childCount - 1; i >= 0; i--)
        {
            var child = buttonPanel.GetChild(i);
            if (child.name.StartsWith("Btn_") || child.name == "Separator" || child.GetComponent<Lab3ChartButtonGeneratorRef>() != null)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    private void CreateSwitchButton(string label, Lab3ChartTableView.TableType type)
    {
        var go = CreateButtonBase("Btn_" + label.Replace(" ", "_").Replace(".", "_"), label);

        var gen = go.AddComponent<Lab3ChartButtonGeneratorRef>();
        gen.tableView = GetComponent<Lab3ChartTableView>();
        gen.graphView = FindFirstObjectByType<Lab3ChartGraphView>();
        gen.controller = FindFirstObjectByType<Lab3_ElectricCircuit>();
        gen.mvpController = FindFirstObjectByType<Lab3Controller>();
        gen.isSwitchToTable = true;
        gen.switchToTable = (Lab3ChartButtonGeneratorRef.TargetTable)(int)type;
    }

    private void CreateActionButton(string label, string action)
    {
        var go = CreateButtonBase("Btn_" + label.Replace(" ", "_").Replace(".", "_"), label);

        var gen = go.AddComponent<Lab3ChartButtonGeneratorRef>();
        gen.tableView = GetComponent<Lab3ChartTableView>();
        gen.graphView = FindFirstObjectByType<Lab3ChartGraphView>();
        gen.controller = FindFirstObjectByType<Lab3_ElectricCircuit>();
        gen.mvpController = FindFirstObjectByType<Lab3Controller>();

        switch (action)
        {
            case "record": gen.isRecordToCurrentTable = true; break;
            case "remove": gen.isRemoveLast = true; break;
            case "next": gen.isNextStage = true; break;
            case "clear":  gen.isClearAll = true; break;
            case "reset":  gen.isResetCircuit = true; break;
            case "sc_on":  gen.isEnableShortCircuit = true; break;
            case "sc_off": gen.isDisableShortCircuit = true; break;
            case "sc_toggle": gen.isToggleShortCircuit = true; break;
            case "r_mode": gen.isToggleResistanceMode = true; break;
            case "tune_u": gen.isTuneU = true; break;
        }
    }

    private GameObject CreateButtonBase(string name, string label)
    {
        if (buttonPrefab != null)
        {
            var prefabGo = Instantiate(buttonPrefab, buttonPanel, false);
            prefabGo.name = name;
            return prefabGo;
        }

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(buttonPanel, false);

        go.AddComponent<CanvasRenderer>();

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = buttonSize;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.8f, 0.8f, 0.8f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGO = new GameObject("Text", typeof(RectTransform));
        var trt = textGO.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        textGO.AddComponent<CanvasRenderer>();
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        return go;
    }

    private void AddSeparator()
    {
        var sep = new GameObject("Separator", typeof(RectTransform));
        sep.transform.SetParent(buttonPanel, false);
        var rt = sep.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(10f, spacing);
    }

    [ContextMenu("Очистить кнопки")]
    public void ClearButtons()
    {
        if (buttonPanel == null)
            buttonPanel = transform as RectTransform;
        if (buttonPanel == null) return;

        RemoveGeneratedChildren();
    }
}
