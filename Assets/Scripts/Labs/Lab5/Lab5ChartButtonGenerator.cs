using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab5ChartButtonGenerator : MonoBehaviour
{
    public RectTransform buttonPanel;

    [Header("Префаб кнопки (опционально)")]
    public GameObject buttonPrefab;

    [Header("Настройки")]
    public Vector2 buttonSize = new Vector2(180f, 30f);
    public float spacing = 4f;

    [Header("Runtime")]
    public bool generateOnStart = false;

    private void Start()
    {
        if (generateOnStart)
            GenerateButtons();
    }

    [ContextMenu("Создать кнопки")]
    public void GenerateButtons()
    {
        if (buttonPanel == null)
            buttonPanel = transform as RectTransform;

        if (buttonPanel == null)
        {
            Debug.LogWarning("Lab5ChartButtonGenerator: buttonPanel не назначен");
            return;
        }

        SetupLayout();
        RemoveGeneratedChildren();

        var tableTypes = new (string label, Lab5ChartTableView.TableType type)[]
        {
            ("Т5.1 ХХХ",            Lab5ChartTableView.TableType.Table5_1_NoLoad),
            ("Т5.2 Нагрузочная",    Lab5ChartTableView.TableType.Table5_2_InductiveLoad),
            ("Т5.3 Внешняя",        Lab5ChartTableView.TableType.Table5_3_External),
            ("Т5.4 Регулировочная", Lab5ChartTableView.TableType.Table5_4_Regulating),
            ("Т5.5 КЗ",             Lab5ChartTableView.TableType.Table5_5_ShortCircuit),
            ("Т5.6 Треугольник",    Lab5ChartTableView.TableType.Table5_6_ReactiveTriangle),
        };

        foreach (var (label, type) in tableTypes)
            CreateSwitchButton(label, type);

        AddSeparator();
        CreateActionButton("Записать точку", "record");
        CreateActionButton("Удалить последнюю точку", "remove");
        CreateActionButton("Следующий этап", "next");
        CreateActionButton("Сброс схемы", "reset");

        AddSeparator();
        CreateActionButton("3ф КЗ", "sc_toggle");
        CreateActionButton("2ф КЗ", "sc2_toggle");
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
        var childrenToRemove = new List<GameObject>();
        for (int i = buttonPanel.childCount - 1; i >= 0; i--)
        {
            var child = buttonPanel.GetChild(i);
            if (child.name.StartsWith("Btn_") || child.name == "Separator" || child.GetComponent<Lab5ChartButtonGeneratorRef>() != null)
                childrenToRemove.Add(child.gameObject);
        }

        foreach (var child in childrenToRemove)
        {
            child.SetActive(false);
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void CreateSwitchButton(string label, Lab5ChartTableView.TableType type)
    {
        var go = CreateButtonBase("Btn_" + label.Replace(" ", "_").Replace(".", "_"), label);

        var gen = go.AddComponent<Lab5ChartButtonGeneratorRef>();
        gen.tableView = GetComponent<Lab5ChartTableView>();
        gen.graphView = FindFirstObjectByType<Lab5ChartGraphView>();
        gen.controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();
        gen.isSwitchToTable = true;
        gen.switchToTable = (Lab5ChartButtonGeneratorRef.TargetTable)(int)type;
    }

    private void CreateActionButton(string label, string action)
    {
        var go = CreateButtonBase("Btn_" + label.Replace(" ", "_").Replace(".", "_"), label);

        var gen = go.AddComponent<Lab5ChartButtonGeneratorRef>();
        gen.tableView = GetComponent<Lab5ChartTableView>();
        gen.graphView = FindFirstObjectByType<Lab5ChartGraphView>();
        gen.controller = FindFirstObjectByType<Lab5SyncGeneratorModel>();

        switch (action)
        {
            case "record": gen.isRecordToCurrentTable = true; break;
            case "remove": gen.isRemoveCurrentPoint = true; break;
            case "next":   gen.isNextStage = true; break;
            case "reset":  gen.isResetCircuit = true; break;
            case "sc_toggle":  gen.isToggleShortCircuit = true; break;
            case "sc2_toggle": gen.isToggleShortCircuit2Phase = true; break;
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
        var builtinSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (builtinSprite != null)
        {
            img.sprite = builtinSprite;
            img.type = Image.Type.Sliced;
        }
        else
        {
            img.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        }

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
