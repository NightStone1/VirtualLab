using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab2CircuitController : MonoBehaviour
{
    [SerializeField] private Lab2Terminal[] terminals;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text foundPairsText;
    [SerializeField] private Button recordPairButton;
    [SerializeField] private Button jumperRoleButton;
    [SerializeField] private Button supplyRoleButton;
    [SerializeField] private Button meterRoleButton;
    [SerializeField] private Button checkMarkingSchemeButton;

    private readonly List<Lab2Terminal> selectedTerminals = new();
    private readonly List<RecordedPair> foundPairs = new();
    private readonly HashSet<Lab2TerminalId> usedTerminals = new();
    private readonly Dictionary<Lab2ConnectionRole, RecordedPair> markingConnections = new();

    private Lab2Stage currentStage = Lab2Stage.Continuity;
    private Lab2ConnectionRole selectedConnectionRole = Lab2ConnectionRole.None;

    private void Start()
    {
        if (terminals == null || terminals.Length == 0)
            terminals = FindObjectsByType<Lab2Terminal>(FindObjectsSortMode.None);

        EnsureTemporaryUi();

        if (recordPairButton != null)
            recordPairButton.onClick.AddListener(RecordSelectedPair);

        if (jumperRoleButton != null)
            jumperRoleButton.onClick.AddListener(() => SelectConnectionRole(Lab2ConnectionRole.Jumper));

        if (supplyRoleButton != null)
            supplyRoleButton.onClick.AddListener(() => SelectConnectionRole(Lab2ConnectionRole.Supply36V));

        if (meterRoleButton != null)
            meterRoleButton.onClick.AddListener(() => SelectConnectionRole(Lab2ConnectionRole.Meter));

        if (checkMarkingSchemeButton != null)
            checkMarkingSchemeButton.onClick.AddListener(CheckFirstSecondPhaseMarkingScheme);

        ClearSelection();
        SetResult("Режим: Прозвонка. Выберите две клеммы");
        UpdateFoundPairsText();
    }

    public void SelectTerminal(Lab2Terminal terminal)
    {
        if (terminal == null)
            return;

        if (selectedTerminals.Contains(terminal))
        {
            selectedTerminals.Remove(terminal);
            terminal.SetSelected(false);
            SetResult("Режим: Прозвонка. Выберите две клеммы");
            return;
        }

        if (selectedTerminals.Count >= 2)
            ClearSelection();

        selectedTerminals.Add(terminal);
        terminal.SetSelected(true);

        if (selectedTerminals.Count == 2 && currentStage == Lab2Stage.Continuity)
            CheckContinuity();
        else if (selectedTerminals.Count == 2)
            RecordRoleConnection();
        else
            SetResult($"Выбрана клемма {terminal.TerminalId}. Выберите вторую клемму");
    }

    private void CheckContinuity()
    {
        Lab2TerminalId first = selectedTerminals[0].TerminalId;
        Lab2TerminalId second = selectedTerminals[1].TerminalId;

        bool hasContinuity = StatorWindingModel.HasContinuity(first, second);
        string result = hasContinuity ? "Цепь есть" : "Обрыв";

        SetResult($"{first} - {second}: {result}");
    }

    public void RecordSelectedPair()
    {
        if (selectedTerminals.Count != 2)
        {
            SetResult("Выберите две клеммы для записи пары");
            return;
        }

        Lab2TerminalId first = selectedTerminals[0].TerminalId;
        Lab2TerminalId second = selectedTerminals[1].TerminalId;

        if (!StatorWindingModel.TryGetPhasePair(first, second, out Lab2TerminalId pairStart, out Lab2TerminalId pairEnd))
        {
            SetResult("Нельзя записать пару: цепь отсутствует");
            return;
        }

        if (ContainsRecordedPair(pairStart, pairEnd))
        {
            SetResult("Эта пара уже записана");
            return;
        }

        if (usedTerminals.Contains(pairStart) || usedTerminals.Contains(pairEnd))
        {
            SetResult("Одна из клемм уже используется");
            return;
        }

        foundPairs.Add(new RecordedPair(pairStart, pairEnd));
        usedTerminals.Add(pairStart);
        usedTerminals.Add(pairEnd);
        UpdateFoundPairsText();

        if (foundPairs.Count >= StatorWindingModel.PhaseWindingCount)
        {
            currentStage = Lab2Stage.DetermineFirstSecondPhase;
            SetResult("Фазные обмотки найдены. Перейдите к определению начал и концов");
        }
        else
        {
            SetResult("Пара записана");
        }
    }

    public void SelectConnectionRole(Lab2ConnectionRole role)
    {
        if (currentStage != Lab2Stage.DetermineFirstSecondPhase)
        {
            SetResult("Сначала завершите прозвонку трех фазных обмоток");
            return;
        }

        selectedConnectionRole = role;
        ClearSelection();
        SetResult($"Выбрана роль: {GetRoleName(role)}. Выберите две клеммы");
    }

    public void CheckFirstSecondPhaseMarkingScheme()
    {
        if (currentStage != Lab2Stage.DetermineFirstSecondPhase)
        {
            SetResult("Сначала завершите прозвонку трех фазных обмоток");
            return;
        }

        if (!markingConnections.TryGetValue(Lab2ConnectionRole.Jumper, out RecordedPair jumper))
        {
            SetResult("Не выбрана перемычка C1-C2");
            return;
        }

        if (!markingConnections.TryGetValue(Lab2ConnectionRole.Supply36V, out RecordedPair supply))
        {
            SetResult("Не выбрано питание ~36 В на C4-C5");
            return;
        }

        if (!StatorWindingModel.IsFirstSecondPhaseMarkingScheme(jumper.First, jumper.Second, supply.First, supply.Second))
        {
            SetResult("Схема неправильная: нужна перемычка C1-C2 и питание ~36 В на C4-C5");
            return;
        }

        SetResult("C1 и C2 определены как начала фазных обмоток");
    }

    private void RecordRoleConnection()
    {
        if (selectedConnectionRole == Lab2ConnectionRole.None)
        {
            SetResult("Выберите роль подключения: перемычка, питание ~36 В или прибор");
            return;
        }

        Lab2TerminalId first = selectedTerminals[0].TerminalId;
        Lab2TerminalId second = selectedTerminals[1].TerminalId;
        markingConnections[selectedConnectionRole] = new RecordedPair(first, second);

        SetResult($"{GetRoleName(selectedConnectionRole)}: {first} - {second}");
    }

    private void ClearSelection()
    {
        for (int i = 0; i < selectedTerminals.Count; i++)
        {
            if (selectedTerminals[i] != null)
                selectedTerminals[i].SetSelected(false);
        }

        selectedTerminals.Clear();
    }

    private void SetResult(string message)
    {
        Debug.Log($"Lab2 continuity: {message}");

        if (resultText != null)
            resultText.text = message;
    }

    private bool ContainsRecordedPair(Lab2TerminalId first, Lab2TerminalId second)
    {
        for (int i = 0; i < foundPairs.Count; i++)
        {
            if (foundPairs[i].First == first && foundPairs[i].Second == second)
                return true;
        }

        return false;
    }

    private void UpdateFoundPairsText()
    {
        if (foundPairsText == null)
            return;

        StringBuilder builder = new();
        builder.AppendLine("Найденные фазные пары:");

        if (foundPairs.Count == 0)
        {
            builder.AppendLine("- нет записанных пар");
        }
        else
        {
            for (int i = 0; i < foundPairs.Count; i++)
                builder.AppendLine($"{i + 1}. {foundPairs[i].First} - {foundPairs[i].Second}");
        }

        foundPairsText.text = builder.ToString();
    }

    private void EnsureTemporaryUi()
    {
        if (resultText == null)
            return;

        Transform parent = resultText.transform.parent;

        if (foundPairsText == null)
            foundPairsText = CreateTemporaryText(parent, "Lab2FoundPairsText", new Vector2(260f, -115f), new Vector2(500f, 120f), 20f);

        if (recordPairButton == null)
            recordPairButton = CreateTemporaryButton(parent, "Lab2RecordPairButton", "Записать пару", new Vector2(260f, -185f));

        if (jumperRoleButton == null)
            jumperRoleButton = CreateTemporaryButton(parent, "Lab2JumperRoleButton", "Перемычка", new Vector2(460f, -185f));

        if (supplyRoleButton == null)
            supplyRoleButton = CreateTemporaryButton(parent, "Lab2SupplyRoleButton", "~36 В", new Vector2(660f, -185f));

        if (meterRoleButton == null)
            meterRoleButton = CreateTemporaryButton(parent, "Lab2MeterRoleButton", "Прибор", new Vector2(460f, -230f));

        if (checkMarkingSchemeButton == null)
            checkMarkingSchemeButton = CreateTemporaryButton(parent, "Lab2CheckMarkingSchemeButton", "Проверить схему", new Vector2(660f, -230f));
    }

    private TMP_Text CreateTemporaryText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        GameObject textObject = new(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private Button CreateTemporaryButton(Transform parent, string objectName, string labelText, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(180f, 36f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text label = CreateTemporaryText(buttonObject.transform, "Text", Vector2.zero, rectTransform.sizeDelta, 18f);
        label.text = labelText;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = Vector2.zero;

        return button;
    }

    private string GetRoleName(Lab2ConnectionRole role)
    {
        return role switch
        {
            Lab2ConnectionRole.Jumper => "Перемычка",
            Lab2ConnectionRole.Supply36V => "Питание ~36 В",
            Lab2ConnectionRole.Meter => "Прибор",
            _ => "Не выбрано"
        };
    }

    private readonly struct RecordedPair
    {
        public RecordedPair(Lab2TerminalId first, Lab2TerminalId second)
        {
            First = first;
            Second = second;
        }

        public Lab2TerminalId First { get; }
        public Lab2TerminalId Second { get; }
    }
}
