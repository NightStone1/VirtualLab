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
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;
    [SerializeField] private Button recordPairButton;
    [SerializeField] private Button jumperRoleButton;
    [SerializeField] private Button supplyRoleButton;
    [SerializeField] private Button meterRoleButton;
    [SerializeField] private Button fourthRoleButton;
    [SerializeField] private Button checkMarkingSchemeButton;
    [SerializeField] private Button calculateSpeedButton;

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

        ResolveTemporaryPanels();
        EnsureTemporaryUi();

        if (recordPairButton != null)
            recordPairButton.onClick.AddListener(RecordSelectedPair);

        if (jumperRoleButton != null)
            jumperRoleButton.onClick.AddListener(() => SelectConnectionRole(GetRoleForButton(0)));

        if (supplyRoleButton != null)
            supplyRoleButton.onClick.AddListener(() => SelectConnectionRole(GetRoleForButton(1)));

        if (meterRoleButton != null)
            meterRoleButton.onClick.AddListener(() => SelectConnectionRole(GetRoleForButton(2)));

        if (fourthRoleButton != null)
            fourthRoleButton.onClick.AddListener(() => SelectConnectionRole(GetRoleForButton(3)));

        if (checkMarkingSchemeButton != null)
            checkMarkingSchemeButton.onClick.AddListener(CheckCurrentMarkingScheme);

        if (calculateSpeedButton != null)
            calculateSpeedButton.onClick.AddListener(CalculateRotationSpeed);

        ClearSelection();
        RefreshTemporaryUi();
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
            SetResult(currentStage == Lab2Stage.Continuity
                ? "Режим: Прозвонка. Выберите две клеммы"
                : $"Выбрана роль: {GetRoleName(selectedConnectionRole)}. Выберите две клеммы");
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
            RefreshTemporaryUi();
            UpdateFoundPairsText();
            SetResult("Фазные обмотки найдены. Перейдите к определению начал и концов");
        }
        else
        {
            SetResult("Пара записана");
        }
    }

    public void SelectConnectionRole(Lab2ConnectionRole role)
    {
        if (currentStage != Lab2Stage.DetermineFirstSecondPhase
            && currentStage != Lab2Stage.DetermineThirdPhase
            && currentStage != Lab2Stage.StarConnectionCheck)
        {
            SetResult("Сначала завершите прозвонку трех фазных обмоток");
            return;
        }

        selectedConnectionRole = role;
        markingConnections.Remove(role);
        ClearSelection();
        UpdateFoundPairsText();
        SetResult($"Выбрана роль: {GetRoleName(role)}. Выберите две клеммы");
    }

    public void CheckCurrentMarkingScheme()
    {
        if (currentStage == Lab2Stage.DetermineFirstSecondPhase)
        {
            CheckFirstSecondPhaseMarkingScheme();
            return;
        }

        if (currentStage == Lab2Stage.DetermineThirdPhase)
        {
            CheckThirdPhaseMarkingScheme();
            return;
        }

        if (currentStage == Lab2Stage.Completed)
        {
            SetResult("Лабораторная работа завершена");
            return;
        }

        if (currentStage == Lab2Stage.StarConnectionCheck)
        {
            CheckStarConnectionScheme();
            return;
        }

        if (currentStage == Lab2Stage.RotationSpeedCalculation)
        {
            CalculateRotationSpeed();
            return;
        }

        SetResult("Сначала завершите прозвонку трех фазных обмоток");
    }

    private void CheckFirstSecondPhaseMarkingScheme()
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

        if (!markingConnections.TryGetValue(Lab2ConnectionRole.Meter, out RecordedPair meter))
        {
            SetResult("Не подключен прибор PV к C3-C6");
            return;
        }

        if (!StatorWindingModel.TryCheckFirstSecondPhaseMarkingScheme(
            jumper.First,
            jumper.Second,
            supply.First,
            supply.Second,
            meter.First,
            meter.Second,
            out string meterReading))
        {
            Debug.Log("Lab2 marking: valid second-phase schemes are C1-C2 + C4-C5 + PV C3-C6 or C1-C5 + C4-C2 + PV C3-C6.");
            SetResult("Ошибка подключения. Для определения начала и конца второй фазы соедините начало первой фазы с одним из выводов второй фазы, подайте ~36 В на оставшиеся свободные выводы первой и второй фаз и подключите PV к третьей фазе.");
            return;
        }

        currentStage = Lab2Stage.DetermineThirdPhase;
        selectedConnectionRole = Lab2ConnectionRole.None;
        markingConnections.Clear();
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult($"{meterReading}\nC2 — начало второй фазной обмотки, C5 — конец");
    }

    private void CheckThirdPhaseMarkingScheme()
    {
        if (!markingConnections.TryGetValue(Lab2ConnectionRole.Jumper, out RecordedPair jumper))
        {
            SetResult("Не выбрана перемычка C2-C3");
            return;
        }

        if (!markingConnections.TryGetValue(Lab2ConnectionRole.Supply36V, out RecordedPair supply))
        {
            SetResult("Не выбрано питание ~36 В на C5-C6");
            return;
        }

        if (!markingConnections.TryGetValue(Lab2ConnectionRole.Meter, out RecordedPair meter))
        {
            SetResult("Не подключен прибор PV к C1-C4");
            return;
        }

        if (!StatorWindingModel.TryCheckThirdPhaseMarkingScheme(
            jumper.First,
            jumper.Second,
            supply.First,
            supply.Second,
            meter.First,
            meter.Second,
            out string meterReading))
        {
            Debug.Log("Lab2 marking: valid third-phase schemes are C2-C3 + C5-C6 + PV C1-C4 or C2-C6 + C5-C3 + PV C1-C4.");
            SetResult("Ошибка подключения. Для определения третьей фазы соедините начало второй фазы с одним из выводов третьей фазы, подайте ~36 В на оставшиеся свободные выводы второй и третьей фаз и подключите PV к первой фазе.");
            return;
        }

        currentStage = Lab2Stage.StarConnectionCheck;
        selectedConnectionRole = Lab2ConnectionRole.None;
        markingConnections.Clear();
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult("Этап маркировки выводов завершён. Соберите соединение обмоток в звезду");
    }

    private void CheckStarConnectionScheme()
    {
        if (!markingConnections.TryGetValue(Lab2ConnectionRole.StarJumper1, out RecordedPair starJumper1)
            || !markingConnections.TryGetValue(Lab2ConnectionRole.StarJumper2, out RecordedPair starJumper2)
            || !markingConnections.TryGetValue(Lab2ConnectionRole.SupplyLine1, out RecordedPair supplyLine1)
            || !markingConnections.TryGetValue(Lab2ConnectionRole.SupplyLine2, out RecordedPair supplyLine2))
        {
            SetResult("Ошибка соединения. Проверьте, что концы фазных обмоток объединены в общую точку звезды, а начала фаз подключены к питающим линиям.");
            return;
        }

        if (!StatorWindingModel.IsStarConnectionScheme(
            starJumper1.First,
            starJumper1.Second,
            starJumper2.First,
            starJumper2.Second,
            supplyLine1.First,
            supplyLine1.Second,
            supplyLine2.First,
            supplyLine2.Second))
        {
            Debug.Log("Lab2 star check: valid star jumpers connect C4, C5, C6 as one group; valid supply lines include C1, C2, C3.");
            SetResult("Ошибка соединения. Проверьте, что концы фазных обмоток объединены в общую точку звезды, а начала фаз подключены к питающим линиям.");
            return;
        }

        currentStage = Lab2Stage.RotationSpeedCalculation;
        selectedConnectionRole = Lab2ConnectionRole.None;
        markingConnections.Clear();
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult("Соединение обмоток в звезду выполнено правильно. Перейдите к учебному расчёту скорости вращения.");
    }

    public void CalculateRotationSpeed()
    {
        if (currentStage != Lab2Stage.RotationSpeedCalculation)
        {
            SetResult("Расчёт скорости доступен после проверки соединения в звезду");
            return;
        }

        StatorWindingModel.CalculateTrainingRotationSpeed(out int polePairs, out int synchronousSpeed);
        currentStage = Lab2Stage.Completed;
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult($"p = 30 / 10 = {polePairs}\nnс = 60 · 50 / {polePairs} = {synchronousSpeed} об/мин");
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
        ClearSelection();
        UpdateFoundPairsText();

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

        if (currentStage == Lab2Stage.Completed)
        {
            foundPairsText.text = BuildCompletedText();
            return;
        }

        if (currentStage == Lab2Stage.RotationSpeedCalculation)
        {
            foundPairsText.text = BuildRotationSpeedCalculationText(false);
            return;
        }

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

        builder.AppendLine();
        if (currentStage == Lab2Stage.StarConnectionCheck)
        {
            builder.AppendLine("Проверка соединения в звезду:");
            builder.AppendLine($"Этап: {GetStageName(currentStage)}");
            builder.AppendLine($"Активная роль: {GetRoleName(selectedConnectionRole)}");
            builder.AppendLine($"Звезда: перемычка 1: {GetConnectionText(Lab2ConnectionRole.StarJumper1)}");
            builder.AppendLine($"Звезда: перемычка 2: {GetConnectionText(Lab2ConnectionRole.StarJumper2)}");
            builder.AppendLine($"Питание: линия 1: {GetConnectionText(Lab2ConnectionRole.SupplyLine1)}");
            builder.AppendLine($"Питание: линия 2: {GetConnectionText(Lab2ConnectionRole.SupplyLine2)}");
            builder.AppendLine("Подсказка: объедините концы фазных обмоток в общую точку звезды, а начала фаз подключите к питающим линиям.");
        }
        else
        {
            builder.AppendLine("Состояние подключения:");
            builder.AppendLine($"Этап: {GetStageName(currentStage)}");
            builder.AppendLine($"Активная роль: {GetRoleName(selectedConnectionRole)}");
            builder.AppendLine($"Перемычка: {GetConnectionText(Lab2ConnectionRole.Jumper)}");
            builder.AppendLine($"Питание ~36 В: {GetConnectionText(Lab2ConnectionRole.Supply36V)}");
            builder.AppendLine($"Прибор PV: {GetConnectionText(Lab2ConnectionRole.Meter)}");
            builder.AppendLine($"Ожидаемое показание: {GetExpectedMeterReadingText()}");
        }

        foundPairsText.text = builder.ToString();
    }

    private void EnsureTemporaryUi()
    {
        if (resultText == null)
            return;

        Transform fallbackParent = resultText.transform.parent;
        Transform textParent = leftPanel != null ? leftPanel : fallbackParent;
        Transform buttonParent = rightPanel != null ? rightPanel : fallbackParent;

        if (leftPanel != null && resultText.transform.parent != leftPanel)
        {
            resultText.transform.SetParent(leftPanel, false);
            ConfigureLayoutElement(resultText.gameObject, 460f, 70f);
        }

        if (foundPairsText == null)
            foundPairsText = CreateTemporaryText(textParent, "Lab2FoundPairsText", new Vector2(40f, -95f), new Vector2(470f, 260f), 18f);

        if (recordPairButton == null)
            recordPairButton = CreateTemporaryButton(buttonParent, "Lab2RecordPairButton", "Записать пару", new Vector2(180f, -95f));

        if (jumperRoleButton == null)
            jumperRoleButton = CreateTemporaryButton(buttonParent, "Lab2JumperRoleButton", "Перемычка", new Vector2(180f, -95f));

        if (supplyRoleButton == null)
            supplyRoleButton = CreateTemporaryButton(buttonParent, "Lab2SupplyRoleButton", "~36 В", new Vector2(180f, -140f));

        if (meterRoleButton == null)
            meterRoleButton = CreateTemporaryButton(buttonParent, "Lab2MeterRoleButton", "Прибор", new Vector2(180f, -185f));

        if (fourthRoleButton == null)
            fourthRoleButton = CreateTemporaryButton(buttonParent, "Lab2FourthRoleButton", "Питание: линия 2", new Vector2(180f, -230f));

        if (checkMarkingSchemeButton == null)
            checkMarkingSchemeButton = CreateTemporaryButton(buttonParent, "Lab2CheckMarkingSchemeButton", "Проверить схему", new Vector2(180f, -275f));

        if (calculateSpeedButton == null)
            calculateSpeedButton = CreateTemporaryButton(buttonParent, "Lab2CalculateSpeedButton", "Рассчитать скорость", new Vector2(180f, -95f));

        RefreshTemporaryUi();
    }

    private TMP_Text CreateTemporaryText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        GameObject textObject = new(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        bool useLayout = parent != null && parent.GetComponent<VerticalLayoutGroup>() != null;
        rectTransform.anchorMin = useLayout ? new Vector2(0f, 1f) : new Vector2(0f, 1f);
        rectTransform.anchorMax = useLayout ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        if (useLayout)
            ConfigureLayoutElement(textObject, size.x, size.y);

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
        bool useLayout = parent != null && parent.GetComponent<VerticalLayoutGroup>() != null;
        rectTransform.anchorMin = useLayout ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
        rectTransform.anchorMax = useLayout ? new Vector2(1f, 1f) : new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(180f, 36f);

        if (useLayout)
            ConfigureLayoutElement(buttonObject, 180f, 36f);

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

    private void ResolveTemporaryPanels()
    {
        if (leftPanel == null)
        {
            GameObject leftPanelObject = GameObject.Find("LeftPanel");

            if (leftPanelObject != null)
                leftPanel = leftPanelObject.transform;
        }

        if (rightPanel == null)
        {
            GameObject rightPanelObject = GameObject.Find("RightPanel");

            if (rightPanelObject != null)
                rightPanel = rightPanelObject.transform;
        }
    }

    private void ConfigureLayoutElement(GameObject target, float preferredWidth, float preferredHeight)
    {
        if (target == null)
            return;

        LayoutElement layoutElement = target.GetComponent<LayoutElement>();

        if (layoutElement == null)
            layoutElement = target.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = preferredWidth;
        layoutElement.preferredHeight = preferredHeight;
    }

    private void RefreshTemporaryUi()
    {
        bool isContinuity = currentStage == Lab2Stage.Continuity;
        bool isMarking = currentStage == Lab2Stage.DetermineFirstSecondPhase
            || currentStage == Lab2Stage.DetermineThirdPhase;
        bool isStarCheck = currentStage == Lab2Stage.StarConnectionCheck;
        bool isRotationSpeedCalculation = currentStage == Lab2Stage.RotationSpeedCalculation;

        SetButtonActive(recordPairButton, isContinuity);
        SetButtonActive(jumperRoleButton, isMarking || isStarCheck);
        SetButtonActive(supplyRoleButton, isMarking || isStarCheck);
        SetButtonActive(meterRoleButton, isMarking || isStarCheck);
        SetButtonActive(fourthRoleButton, isStarCheck);
        SetButtonActive(checkMarkingSchemeButton, isMarking || isStarCheck);
        SetButtonActive(calculateSpeedButton, isRotationSpeedCalculation);
        RefreshButtonLabels();
    }

    private void SetButtonActive(Button button, bool active)
    {
        if (button != null)
            button.gameObject.SetActive(active);
    }

    private string GetRoleName(Lab2ConnectionRole role)
    {
        return role switch
        {
            Lab2ConnectionRole.Jumper => "Перемычка",
            Lab2ConnectionRole.Supply36V => "Питание ~36 В",
            Lab2ConnectionRole.Meter => "Прибор",
            Lab2ConnectionRole.StarJumper1 => "Звезда: перемычка 1",
            Lab2ConnectionRole.StarJumper2 => "Звезда: перемычка 2",
            Lab2ConnectionRole.SupplyLine1 => "Питание: линия 1",
            Lab2ConnectionRole.SupplyLine2 => "Питание: линия 2",
            _ => "Не выбрано"
        };
    }

    private Lab2ConnectionRole GetRoleForButton(int buttonIndex)
    {
        if (currentStage == Lab2Stage.StarConnectionCheck)
        {
            return buttonIndex switch
            {
                0 => Lab2ConnectionRole.StarJumper1,
                1 => Lab2ConnectionRole.StarJumper2,
                2 => Lab2ConnectionRole.SupplyLine1,
                3 => Lab2ConnectionRole.SupplyLine2,
                _ => Lab2ConnectionRole.None
            };
        }

        return buttonIndex switch
        {
            0 => Lab2ConnectionRole.Jumper,
            1 => Lab2ConnectionRole.Supply36V,
            2 => Lab2ConnectionRole.Meter,
            _ => Lab2ConnectionRole.None
        };
    }

    private void RefreshButtonLabels()
    {
        if (currentStage == Lab2Stage.StarConnectionCheck)
        {
            SetButtonLabel(jumperRoleButton, "Звезда: перемычка 1");
            SetButtonLabel(supplyRoleButton, "Звезда: перемычка 2");
            SetButtonLabel(meterRoleButton, "Питание: линия 1");
            SetButtonLabel(fourthRoleButton, "Питание: линия 2");
            return;
        }

        SetButtonLabel(jumperRoleButton, "Перемычка");
        SetButtonLabel(supplyRoleButton, "~36 В");
        SetButtonLabel(meterRoleButton, "Прибор");
        SetButtonLabel(fourthRoleButton, "Питание: линия 2");
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
            text.text = label;
    }

    private string GetStageName(Lab2Stage stage)
    {
        return stage switch
        {
            Lab2Stage.Continuity => "Прозвонка",
            Lab2Stage.DetermineFirstSecondPhase => "Определение начал первой и второй фаз",
            Lab2Stage.DetermineThirdPhase => "Определение начала третьей фазы",
            Lab2Stage.StarConnectionCheck => "Проверка соединения в звезду",
            Lab2Stage.RotationSpeedCalculation => "Определение скорости вращения двигателя",
            Lab2Stage.Completed => "Лабораторная работа завершена",
            _ => "Неизвестно"
        };
    }

    private string GetConnectionText(Lab2ConnectionRole role)
    {
        return markingConnections.TryGetValue(role, out RecordedPair pair)
            ? $"{pair.First} - {pair.Second}"
            : "не выбрано";
    }

    private string GetExpectedMeterReadingText()
    {
        if (!markingConnections.TryGetValue(Lab2ConnectionRole.Jumper, out RecordedPair jumper)
            || !markingConnections.TryGetValue(Lab2ConnectionRole.Supply36V, out RecordedPair supply)
            || !markingConnections.TryGetValue(Lab2ConnectionRole.Meter, out RecordedPair meter))
            return "не определено — схема подключения не завершена или содержит ошибку";

        if (currentStage == Lab2Stage.DetermineFirstSecondPhase
            && StatorWindingModel.TryCheckFirstSecondPhaseMarkingScheme(
                jumper.First,
                jumper.Second,
                supply.First,
                supply.Second,
                meter.First,
                meter.Second,
                out string secondPhaseReading))
            return secondPhaseReading;

        if (currentStage == Lab2Stage.DetermineThirdPhase
            && StatorWindingModel.TryCheckThirdPhaseMarkingScheme(
                jumper.First,
                jumper.Second,
                supply.First,
                supply.Second,
                meter.First,
                meter.Second,
                out string thirdPhaseReading))
            return thirdPhaseReading;

        return "не определено — схема подключения не завершена или содержит ошибку";
    }

    private string GetFinalMarkingMessage()
    {
        return "Итоговая маркировка:\nC1, C2, C3 — начала фазных обмоток;\nC4, C5, C6 — концы фазных обмоток.";
    }

    private string BuildCompletedText()
    {
        StringBuilder builder = new();
        builder.AppendLine("Лабораторная работа завершена");
        builder.AppendLine();
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

        builder.AppendLine();
        builder.AppendLine(GetFinalMarkingMessage());
        builder.AppendLine();
        builder.AppendLine("Соединение обмоток в звезду проверено.");
        builder.AppendLine("Расчёт скорости вращения выполнен на учебном примере:");
        builder.AppendLine("p = 3;");
        builder.AppendLine("nс = 1000 об/мин.");

        return builder.ToString();
    }

    private string BuildRotationSpeedCalculationText(bool includeResult)
    {
        StringBuilder builder = new();
        builder.AppendLine("Определение скорости вращения двигателя");
        builder.AppendLine();
        builder.AppendLine("Учебный пример расчёта:");
        builder.AppendLine("N = 30 — число отклонений стрелки гальванометра");
        builder.AppendLine("n = 10 — число оборотов ротора");
        builder.AppendLine("f = 50 Гц — частота сети");
        builder.AppendLine();
        builder.AppendLine("Формулы:");
        builder.AppendLine("p = N / n");
        builder.AppendLine("nс = 60f / p");

        if (includeResult)
        {
            builder.AppendLine();
            builder.AppendLine("Расчёт:");
            builder.AppendLine("p = 30 / 10 = 3");
            builder.AppendLine("nс = 60 · 50 / 3 = 1000 об/мин");
        }

        return builder.ToString();
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
