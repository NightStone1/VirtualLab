using System.Collections;
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
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private TMP_Text hudText;
    [SerializeField] private TMP_Text hudActionsText;
    [SerializeField] private Transform wireRoot;
    [SerializeField] private Transform supply36VAnchorA;
    [SerializeField] private Transform supply36VAnchorB;
    [SerializeField] private Transform paAnchorLeft;
    [SerializeField] private Transform paAnchorCenter;
    [SerializeField] private Transform paAnchorRight;
    [SerializeField] private Transform pvAnchorLeft;
    [SerializeField] private Transform pvAnchorRight;
    [SerializeField] private Transform paNeedle;
    [SerializeField] private float paNeedleDeflectionAngle = 18f;
    [SerializeField] private float paNeedleDeflectionDuration = 0.08f;
    [SerializeField] private Button recordPairButton;
    [SerializeField] private Button jumperRoleButton;
    [SerializeField] private Button supplyRoleButton;
    [SerializeField] private Button meterRoleButton;
    [SerializeField] private Button fourthRoleButton;
    [SerializeField] private Button checkMarkingSchemeButton;
    [SerializeField] private Button calculateSpeedButton;
    [SerializeField] private Button resetLabButton;

    private readonly List<Lab2Terminal> selectedTerminals = new();
    private readonly List<RecordedPair> foundPairs = new();
    private readonly List<Lab2WireView> temporaryContinuityWires = new();
    private readonly HashSet<Lab2TerminalId> usedTerminals = new();
    private readonly Dictionary<Lab2ConnectionRole, RecordedPair> markingConnections = new();
    private readonly Dictionary<Lab2ConnectionRole, List<Lab2WireView>> roleWires = new();

    private Lab2Stage currentStage = Lab2Stage.Continuity;
    private Lab2ConnectionRole selectedConnectionRole = Lab2ConnectionRole.None;
    private string lastActionMessage = "Выберите две клеммы";
    private bool paConnected;
    private RecordedPair paConnection;
    private int rotorTurns;
    private int needleDeflections;
    private int calculatedPolePairs;
    private int calculatedSynchronousSpeed;
    private Quaternion paNeedleInitialRotation;
    private Coroutine paNeedleAnimation;
    private bool paNeedleWarningShown;

    private void Start()
    {
        ResolveTerminals();
        ResolvePaNeedle();

        ResolveTemporaryPanels();
        EnsureTemporaryUi();
        EnsureHudUi();

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

        if (resetLabButton != null)
            resetLabButton.onClick.AddListener(ResetLab);

        ClearSelection();
        RefreshTemporaryUi();
        SetResult("Режим: Прозвонка. Выберите две клеммы");
        UpdateFoundPairsText();
    }

    private void Update()
    {
        HandleHudKeyboardActions();
    }

    private void HandleHudKeyboardActions()
    {
        bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        switch (currentStage)
        {
            case Lab2Stage.Continuity:
                if (enterPressed)
                    RecordSelectedPair();
                break;

            case Lab2Stage.DetermineFirstSecondPhase:
            case Lab2Stage.DetermineThirdPhase:
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    SelectConnectionRole(GetRoleForButton(0));
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                    SelectConnectionRole(GetRoleForButton(1));
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                    SelectConnectionRole(GetRoleForButton(2));
                else if (enterPressed)
                    CheckCurrentMarkingScheme();
                break;

            case Lab2Stage.StarConnectionCheck:
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    SelectConnectionRole(GetRoleForButton(0));
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                    SelectConnectionRole(GetRoleForButton(1));
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                    SelectConnectionRole(GetRoleForButton(2));
                else if (Input.GetKeyDown(KeyCode.Alpha4))
                    SelectConnectionRole(GetRoleForButton(3));
                else if (enterPressed)
                    CheckCurrentMarkingScheme();
                break;

            case Lab2Stage.RotationSpeedCalculation:
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    SelectConnectionRole(Lab2ConnectionRole.MicroammeterPA);
                else if (enterPressed)
                    CalculateRotationSpeed();
                break;

            case Lab2Stage.Completed:
                if (Input.GetKeyDown(KeyCode.R))
                    ResetLab();
                break;
        }
    }

    private void ResolveTerminals()
    {
        if (terminals == null || terminals.Length == 0)
            terminals = FindObjectsByType<Lab2Terminal>(FindObjectsSortMode.None);

        if (terminals != null && terminals.Length >= StatorWindingModel.TerminalCount)
            return;

        List<Lab2Terminal> resolvedTerminals = new();

        for (int i = 1; i <= StatorWindingModel.TerminalCount; i++)
        {
            Lab2TerminalId terminalId = (Lab2TerminalId)i;
            GameObject terminalObject = GameObject.Find(terminalId.ToString());

            if (terminalObject == null)
            {
                Debug.LogWarning($"Lab2 terminal object {terminalId} was not found in scene.");
                continue;
            }

            if (!terminalObject.TryGetComponent(out Lab2Terminal terminal))
                terminal = terminalObject.AddComponent<Lab2Terminal>();

            terminal.Initialize(terminalId, this);
            resolvedTerminals.Add(terminal);
        }

        terminals = resolvedTerminals.ToArray();
    }

    private void ResolvePaNeedle()
    {
        if (paNeedle == null)
            paNeedle = FindAnchorByNames("PANeedle", "Needle_PA", "MicroammeterNeedle", "AmmeterNeedle", "PA_Needle");

        if (paNeedle != null)
        {
            paNeedleInitialRotation = paNeedle.localRotation;
            Debug.Log($"Lab2 PA needle found: {paNeedle.name}.");
            return;
        }

        Debug.LogWarning("Lab2 PA needle was not found. Speed counters will work without needle animation.");
        paNeedleWarningShown = true;
    }

    public void SelectTerminal(Lab2Terminal terminal)
    {
        if (terminal == null)
            return;

        if (currentStage == Lab2Stage.RotationSpeedCalculation && selectedConnectionRole != Lab2ConnectionRole.MicroammeterPA)
            selectedConnectionRole = Lab2ConnectionRole.MicroammeterPA;

        if (selectedTerminals.Contains(terminal))
        {
            selectedTerminals.Remove(terminal);
            terminal.SetSelected(false);
            SetResult(GetSelectionPrompt());
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
            SetResult(currentStage == Lab2Stage.RotationSpeedCalculation
                ? $"Выбрана клемма статора {terminal.TerminalId}. Выберите вторую клемму статора для PA"
                : $"Выбрана клемма {terminal.TerminalId}. Выберите вторую клемму");
    }

    private string GetSelectionPrompt()
    {
        if (currentStage == Lab2Stage.Continuity)
            return "Режим: Прозвонка. Выберите две клеммы";

        if (currentStage == Lab2Stage.RotationSpeedCalculation)
            return "Выбрана роль: PA. Выберите две клеммы статора C1-C6";

        return $"Выбрана роль: {GetRoleName(selectedConnectionRole)}. Выберите две клеммы";
    }

    private void CheckContinuity()
    {
        Lab2TerminalId first = selectedTerminals[0].TerminalId;
        Lab2TerminalId second = selectedTerminals[1].TerminalId;
        CreateOrReplaceTemporaryContinuityWires(selectedTerminals[0], selectedTerminals[1]);

        bool hasContinuity = StatorWindingModel.HasContinuity(first, second);
        string result = hasContinuity ? "Цепь есть" : "Цепи нет";

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
            ClearTemporaryContinuityWires();
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
            && currentStage != Lab2Stage.StarConnectionCheck
            && !(currentStage == Lab2Stage.RotationSpeedCalculation && role == Lab2ConnectionRole.MicroammeterPA))
        {
            SetResult("Сначала завершите прозвонку трех фазных обмоток");
            return;
        }

        selectedConnectionRole = role;
        markingConnections.Remove(role);
        RemoveRoleWire(role);

        if (role == Lab2ConnectionRole.MicroammeterPA)
        {
            paConnected = false;
            paConnection = default;
            rotorTurns = 0;
            needleDeflections = 0;
            calculatedPolePairs = 0;
            calculatedSynchronousSpeed = 0;
        }

        ClearSelection();
        UpdateFoundPairsText();
        SetResult(role == Lab2ConnectionRole.MicroammeterPA
            ? "Выбрана роль: PA. Выберите две клеммы статора C1-C6"
            : $"Выбрана роль: {GetRoleName(role)}. Выберите две клеммы");
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
            SetResult("Не подключён вольтметр PV к C3-C6");
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
        ClearRoleWires();
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult($"{FormatInstrumentReading(meterReading)}\nC2 — начало второй фазной обмотки, C5 — конец");
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
            SetResult("Не подключён вольтметр PV к C1-C4");
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
        ClearRoleWires();
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
        selectedConnectionRole = Lab2ConnectionRole.MicroammeterPA;
        markingConnections.Clear();
        paConnected = false;
        paConnection = default;
        rotorTurns = 0;
        needleDeflections = 0;
        calculatedPolePairs = 0;
        calculatedSynchronousSpeed = 0;
        ResetPaNeedle();
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult("Соединение обмоток в звезду выполнено правильно. Подключите PA к выводам статора.");
    }

    public void CalculateRotationSpeed()
    {
        if (currentStage != Lab2Stage.RotationSpeedCalculation)
        {
            SetResult("Расчёт скорости доступен после проверки соединения в звезду");
            return;
        }

        if (!paConnected)
        {
            SetResult("Сначала подключите PA к одной фазной обмотке");
            return;
        }

        if (rotorTurns < StatorWindingModel.TrainingRotorTurns)
        {
            rotorTurns += 1;
            needleDeflections += 3;
            StartPaNeedleAnimation(3);
            UpdateFoundPairsText();
            SetResult($"Ротор провернут: {rotorTurns} / {StatorWindingModel.TrainingRotorTurns}. Отклонения стрелки PA: {needleDeflections}");
        }

        if (rotorTurns >= StatorWindingModel.TrainingRotorTurns)
        {
            calculatedPolePairs = needleDeflections / rotorTurns;
            calculatedSynchronousSpeed = 60 * StatorWindingModel.TrainingSupplyFrequency / calculatedPolePairs;
            currentStage = Lab2Stage.Completed;
            ClearSelection();
            RefreshTemporaryUi();
            UpdateFoundPairsText();
            SetResult($"p = {needleDeflections} / {rotorTurns} = {calculatedPolePairs}\nnc = 60 · 50 / {calculatedPolePairs} = {calculatedSynchronousSpeed} об/мин");
        }
    }

    private void StartPaNeedleAnimation(int deflectionCount)
    {
        if (paNeedle == null)
        {
            if (!paNeedleWarningShown)
            {
                Debug.LogWarning("Lab2 PA needle was not found. Speed counters will work without needle animation.");
                paNeedleWarningShown = true;
            }

            return;
        }

        if (paNeedleAnimation != null)
            StopCoroutine(paNeedleAnimation);

        paNeedleAnimation = StartCoroutine(AnimatePaNeedle(deflectionCount));
    }

    private IEnumerator AnimatePaNeedle(int deflectionCount)
    {
        Quaternion deflectedRotation = paNeedleInitialRotation * Quaternion.Euler(0f, 0f, -paNeedleDeflectionAngle);

        for (int i = 0; i < deflectionCount; i++)
        {
            yield return RotatePaNeedle(paNeedleInitialRotation, deflectedRotation, paNeedleDeflectionDuration);
            yield return RotatePaNeedle(deflectedRotation, paNeedleInitialRotation, paNeedleDeflectionDuration);
        }

        paNeedle.localRotation = paNeedleInitialRotation;
        paNeedleAnimation = null;
    }

    private IEnumerator RotatePaNeedle(Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            paNeedle.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }

        paNeedle.localRotation = to;
    }

    private void ResetPaNeedle()
    {
        if (paNeedleAnimation != null)
        {
            StopCoroutine(paNeedleAnimation);
            paNeedleAnimation = null;
        }

        if (paNeedle != null)
            paNeedle.localRotation = paNeedleInitialRotation;
    }

    public void ResetLab()
    {
        currentStage = Lab2Stage.Continuity;
        foundPairs.Clear();
        usedTerminals.Clear();
        markingConnections.Clear();
        ClearRoleWires();
        ClearTemporaryContinuityWires();
        selectedConnectionRole = Lab2ConnectionRole.None;
        paConnected = false;
        paConnection = default;
        rotorTurns = 0;
        needleDeflections = 0;
        calculatedPolePairs = 0;
        calculatedSynchronousSpeed = 0;
        ResetPaNeedle();
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult("Режим: Прозвонка. Выберите две клеммы");
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
        Lab2TerminalId paPairStart = Lab2TerminalId.None;
        Lab2TerminalId paPairEnd = Lab2TerminalId.None;

        if (selectedConnectionRole == Lab2ConnectionRole.MicroammeterPA
            && !TryGetValidPaPhasePair(first, second, out paPairStart, out paPairEnd))
        {
            paConnected = false;
            paConnection = default;
            rotorTurns = 0;
            needleDeflections = 0;
            calculatedPolePairs = 0;
            calculatedSynchronousSpeed = 0;
            markingConnections.Remove(Lab2ConnectionRole.MicroammeterPA);
            RemoveRoleWire(Lab2ConnectionRole.MicroammeterPA);
            ClearSelection();
            UpdateFoundPairsText();
            SetResult("Ошибка: PA должен быть подключён к выводам одной фазной обмотки. Допустимые пары: C1-C4, C2-C5, C3-C6.");
            return;
        }

        markingConnections[selectedConnectionRole] = new RecordedPair(first, second);
        CreateOrReplaceRoleWire(selectedConnectionRole, selectedTerminals[0], selectedTerminals[1]);

        if (selectedConnectionRole == Lab2ConnectionRole.MicroammeterPA)
        {
            paConnected = true;
            paConnection = new RecordedPair(paPairStart, paPairEnd);
            rotorTurns = 0;
            needleDeflections = 0;
            calculatedPolePairs = 0;
            calculatedSynchronousSpeed = 0;
        }

        ClearSelection();
        UpdateFoundPairsText();

        SetResult(selectedConnectionRole == Lab2ConnectionRole.MicroammeterPA
            ? $"PA подключён к фазной обмотке {paConnection.First}-{paConnection.Second}."
            : $"{GetRoleName(selectedConnectionRole)}: {first} - {second}");
    }

    private bool TryGetValidPaPhasePair(Lab2TerminalId first, Lab2TerminalId second, out Lab2TerminalId pairStart, out Lab2TerminalId pairEnd)
    {
        return StatorWindingModel.TryGetPhasePair(first, second, out pairStart, out pairEnd);
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
        lastActionMessage = message;

        if (resultText != null)
            resultText.text = message;

        UpdateHudText();
    }

    private void CreateOrReplaceRoleWire(Lab2ConnectionRole role, Lab2Terminal first, Lab2Terminal second)
    {
        if (role == Lab2ConnectionRole.None || first == null || second == null)
            return;

        RemoveRoleWire(role);

        Color wireColor = GetWireColor(role);

        if (role == Lab2ConnectionRole.Supply36V
            && TryGetSupply36VAnchors(out Transform supplyA, out Transform supplyB))
        {
            AddRoleWire(role, "A", () => supplyA.position, () => first.VisualConnectionPosition, wireColor, false);
            AddRoleWire(role, "B", () => supplyB.position, () => second.VisualConnectionPosition, wireColor, false);
            Debug.Log($"Lab2 wire: created {role} wires from supply anchors to {first.TerminalId} and {second.TerminalId}.");
            return;
        }

        if (role == Lab2ConnectionRole.Meter
            && TryGetPvAnchors(out Transform pvLeft, out Transform pvRight))
        {
            AddRoleWire(role, "Left", () => pvLeft.position, () => first.VisualConnectionPosition, wireColor, false);
            AddRoleWire(role, "Right", () => pvRight.position, () => second.VisualConnectionPosition, wireColor, false);
            Debug.Log($"Lab2 wire: created {role} wires from LeftPV/RightPV anchors to {first.TerminalId} and {second.TerminalId}.");
            return;
        }

        if (role == Lab2ConnectionRole.MicroammeterPA
            && TryGetSpeedPaAnchors(out Transform paCenterForSpeed, out Transform paLeft))
        {
            AddRoleWire(role, "Center", () => paCenterForSpeed.position, () => first.VisualConnectionPosition, wireColor, false);
            AddRoleWire(role, "Left", () => paLeft.position, () => second.VisualConnectionPosition, wireColor, false);
            Debug.Log($"Lab2 wire: created PA speed wires from CenterPA/LeftPA anchors to {first.TerminalId} and {second.TerminalId}.");
            return;
        }

        if (role == Lab2ConnectionRole.Supply36V)
            Debug.LogWarning("Lab2 wire: Supply36V anchors were not found. Falling back to terminal-to-terminal wire.");

        if (role == Lab2ConnectionRole.Meter)
            Debug.LogWarning("Lab2 wire: PV anchors LeftPV/RightPV were not found. Falling back to terminal-to-terminal wire.");

        if (role == Lab2ConnectionRole.MicroammeterPA)
            Debug.LogWarning("Lab2 wire: PA anchors CenterPA/LeftPA were not found. Falling back to terminal-to-terminal wire.");

        AddRoleWire(role, string.Empty, () => first.VisualConnectionPosition, () => second.VisualConnectionPosition, wireColor, true);

        Vector3 startPosition = first.VisualConnectionPosition;
        Vector3 endPosition = second.VisualConnectionPosition;
        float distance = Vector3.Distance(startPosition, endPosition);
        Debug.Log($"Lab2 wire: created {role} fallback wire between {first.TerminalId} ({first.name}) at {startPosition} and {second.TerminalId} ({second.name}) at {endPosition}. Distance: {distance:F4}.");

        if (distance < 0.01f)
            Debug.LogWarning($"Lab2 wire: {role} wire endpoints are too close. Check ClickArea/VisualConnectionPosition for {first.TerminalId} and {second.TerminalId}.");
    }

    private void AddRoleWire(Lab2ConnectionRole role, string suffix, System.Func<Vector3> startPositionProvider, System.Func<Vector3> endPositionProvider, Color color, bool useCompactTerminalProfile)
    {
        string suffixPart = string.IsNullOrEmpty(suffix) ? string.Empty : $"_{suffix}";
        GameObject wireObject = new($"Lab2Wire_{role}{suffixPart}");
        wireObject.transform.SetParent(GetWireRoot(), false);
        Lab2WireView wireView = wireObject.AddComponent<Lab2WireView>();
        wireView.Initialize(startPositionProvider, endPositionProvider, color, GetWireRoleOffset(role));

        if (useCompactTerminalProfile)
            wireView.SetVisualProfile(0.045f, 0.01f, true, 0.14f, 0.018f);

        if (!roleWires.TryGetValue(role, out List<Lab2WireView> wires))
        {
            wires = new List<Lab2WireView>();
            roleWires[role] = wires;
        }

        wires.Add(wireView);
    }

    private void CreateOrReplaceTemporaryContinuityWires(Lab2Terminal first, Lab2Terminal second)
    {
        ClearTemporaryContinuityWires();

        if (first == null || second == null)
            return;

        Color wireColor = GetWireColor(Lab2ConnectionRole.MicroammeterPA);

        if (TryGetContinuityPaAnchors(out Transform paCenter, out Transform paRight))
        {
            AddTemporaryContinuityWire("Center", () => paCenter.position, () => first.VisualConnectionPosition, wireColor, false);
            AddTemporaryContinuityWire("Right", () => paRight.position, () => second.VisualConnectionPosition, wireColor, false);
            Debug.Log($"Lab2 wire: created continuity PA wires from CenterPA/RightPA anchors to {first.TerminalId} and {second.TerminalId}.");
            return;
        }

        Debug.LogWarning("Lab2 wire: PA anchors CenterPA/RightPA were not found for continuity. Falling back to terminal-to-terminal wire.");
        AddTemporaryContinuityWire(string.Empty, () => first.VisualConnectionPosition, () => second.VisualConnectionPosition, wireColor, true);
    }

    private void AddTemporaryContinuityWire(string suffix, System.Func<Vector3> startPositionProvider, System.Func<Vector3> endPositionProvider, Color color, bool useCompactTerminalProfile)
    {
        string suffixPart = string.IsNullOrEmpty(suffix) ? string.Empty : $"_{suffix}";
        GameObject wireObject = new($"Lab2Wire_ContinuityPA{suffixPart}");
        wireObject.transform.SetParent(GetWireRoot(), false);
        Lab2WireView wireView = wireObject.AddComponent<Lab2WireView>();
        wireView.Initialize(startPositionProvider, endPositionProvider, color, GetWireRoleOffset(Lab2ConnectionRole.MicroammeterPA));

        if (useCompactTerminalProfile)
            wireView.SetVisualProfile(0.045f, 0.01f, true, 0.14f, 0.018f);

        temporaryContinuityWires.Add(wireView);
    }

    private void ClearTemporaryContinuityWires()
    {
        for (int i = 0; i < temporaryContinuityWires.Count; i++)
        {
            if (temporaryContinuityWires[i] != null)
                Destroy(temporaryContinuityWires[i].gameObject);
        }

        temporaryContinuityWires.Clear();
    }

    private bool TryGetSupply36VAnchors(out Transform first, out Transform second)
    {
        supply36VAnchorA ??= FindAnchorByNames("Supply36V_A", "Supply36V_PositiveAnchor", "Left36V", "SupplyLeft");
        supply36VAnchorB ??= FindAnchorByNames("Supply36V_B", "Supply36V_NegativeAnchor", "Right36V", "SupplyRight");

        first = supply36VAnchorA;
        second = supply36VAnchorB;

        bool found = first != null && second != null;

        if (found)
            Debug.Log($"Lab2 wire: Supply36V anchors found: {first.name}, {second.name}.");

        return found;
    }

    private bool TryGetContinuityPaAnchors(out Transform first, out Transform second)
    {
        paAnchorCenter ??= FindAnchorByNames("CenterPA");
        paAnchorRight ??= FindAnchorByNames("RightPA");

        first = paAnchorCenter;
        second = paAnchorRight;

        bool found = first != null && second != null;

        if (found)
            Debug.Log($"Lab2 wire: continuity PA anchors found: {first.name}, {second.name}.");

        return found;
    }

    private bool TryGetPvAnchors(out Transform first, out Transform second)
    {
        pvAnchorLeft ??= FindAnchorByNames("LeftPV", "PV_A", "PV_PositiveAnchor");
        pvAnchorRight ??= FindAnchorByNames("RightPV", "PV_B", "PV_NegativeAnchor");

        first = pvAnchorLeft;
        second = pvAnchorRight;

        bool found = first != null && second != null;

        if (found)
            Debug.Log($"Lab2 wire: PV anchors found: {first.name}, {second.name}.");

        return found;
    }

    private bool TryGetSpeedPaAnchors(out Transform first, out Transform second)
    {
        paAnchorCenter ??= FindAnchorByNames("CenterPA");
        paAnchorLeft ??= FindAnchorByNames("LeftPA");

        first = paAnchorCenter;
        second = paAnchorLeft;

        bool found = first != null && second != null;

        if (found)
            Debug.Log($"Lab2 wire: speed PA anchors found: {first.name}, {second.name}.");

        return found;
    }

    private Transform FindAnchorByNames(params string[] names)
    {
        Transform anchor = FindAnchorInRoot("Stend2", names);

        if (anchor != null)
            return anchor;

        anchor = FindAnchorInRoot("Lab2Systems", names);

        if (anchor != null)
            return anchor;

        for (int i = 0; i < names.Length; i++)
        {
            GameObject anchorObject = GameObject.Find(names[i]);

            if (anchorObject != null)
                return anchorObject.transform;
        }

        return null;
    }

    private Transform FindAnchorInRoot(string rootName, string[] names)
    {
        GameObject rootObject = GameObject.Find(rootName);

        if (rootObject == null)
            return null;

        Transform[] children = rootObject.GetComponentsInChildren<Transform>(true);

        for (int childIndex = 0; childIndex < children.Length; childIndex++)
        {
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (children[childIndex].name == names[nameIndex])
                    return children[childIndex];
            }
        }

        return null;
    }

    private Transform GetWireRoot()
    {
        if (wireRoot != null)
            return wireRoot;

        GameObject wireRootObject = new("Lab2Wires");
        wireRootObject.transform.SetParent(transform, false);
        wireRoot = wireRootObject.transform;

        return wireRoot;
    }

    private void RemoveRoleWire(Lab2ConnectionRole role)
    {
        if (!roleWires.TryGetValue(role, out List<Lab2WireView> wires))
            return;

        for (int i = 0; i < wires.Count; i++)
        {
            if (wires[i] != null)
                Destroy(wires[i].gameObject);
        }

        roleWires.Remove(role);
    }

    private void ClearRoleWires()
    {
        foreach (List<Lab2WireView> wires in roleWires.Values)
        {
            for (int i = 0; i < wires.Count; i++)
            {
                if (wires[i] != null)
                    Destroy(wires[i].gameObject);
            }
        }

        roleWires.Clear();
    }

    private Color GetWireColor(Lab2ConnectionRole role)
    {
        return role switch
        {
            Lab2ConnectionRole.Jumper => new Color(0.9f, 0.9f, 0.9f, 1f),
            Lab2ConnectionRole.Supply36V => new Color(1f, 0.35f, 0.2f, 1f),
            Lab2ConnectionRole.Meter => new Color(0.2f, 0.75f, 1f, 1f),
            Lab2ConnectionRole.StarJumper1 => new Color(0.95f, 0.95f, 0.45f, 1f),
            Lab2ConnectionRole.StarJumper2 => new Color(0.95f, 0.95f, 0.45f, 1f),
            Lab2ConnectionRole.SupplyLine1 => new Color(0.35f, 1f, 0.35f, 1f),
            Lab2ConnectionRole.SupplyLine2 => new Color(0.35f, 1f, 0.35f, 1f),
            Lab2ConnectionRole.MicroammeterPA => new Color(0.75f, 0.75f, 1f, 1f),
            _ => Color.white
        };
    }

    private float GetWireRoleOffset(Lab2ConnectionRole role)
    {
        return role switch
        {
            Lab2ConnectionRole.Jumper => 0f,
            Lab2ConnectionRole.Supply36V => 0.008f,
            Lab2ConnectionRole.Meter => 0.016f,
            Lab2ConnectionRole.StarJumper1 => -0.008f,
            Lab2ConnectionRole.StarJumper2 => -0.016f,
            Lab2ConnectionRole.SupplyLine1 => 0.024f,
            Lab2ConnectionRole.SupplyLine2 => -0.024f,
            Lab2ConnectionRole.MicroammeterPA => 0.016f,
            _ => 0f
        };
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
            foundPairsText.text = BuildRotationSpeedCalculationText(currentStage == Lab2Stage.Completed);
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
            builder.AppendLine($"Вольтметр PV: {GetConnectionText(Lab2ConnectionRole.Meter)}");
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
            meterRoleButton = CreateTemporaryButton(buttonParent, "Lab2MeterRoleButton", "PV", new Vector2(180f, -185f));

        if (fourthRoleButton == null)
            fourthRoleButton = CreateTemporaryButton(buttonParent, "Lab2FourthRoleButton", "Питание: линия 2", new Vector2(180f, -230f));

        if (checkMarkingSchemeButton == null)
            checkMarkingSchemeButton = CreateTemporaryButton(buttonParent, "Lab2CheckMarkingSchemeButton", "Проверить схему", new Vector2(180f, -275f));

        if (calculateSpeedButton == null)
            calculateSpeedButton = CreateTemporaryButton(buttonParent, "Lab2CalculateSpeedButton", "Рассчитать скорость", new Vector2(180f, -95f));

        if (resetLabButton == null)
            resetLabButton = CreateTemporaryButton(buttonParent, "Lab2ResetLabButton", "Начать заново", new Vector2(180f, -95f));

        RefreshTemporaryUi();
    }

    private void EnsureHudUi()
    {
        if (hudText != null)
        {
            EnsureHudActionsText();
            UpdateHudText();
            return;
        }

        GameObject canvasObject = new("Lab2HudCanvas");
        hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new("Lab2HudPanel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(430f, 300f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.62f);
        panelImage.raycastTarget = false;

        GameObject textObject = new("Lab2HudText");
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 10f);
        textRect.offsetMax = new Vector2(-12f, -10f);

        hudText = textObject.AddComponent<TextMeshProUGUI>();
        hudText.fontSize = 20f;
        hudText.alignment = TextAlignmentOptions.TopLeft;
        hudText.color = Color.white;
        hudText.raycastTarget = false;
        hudText.textWrappingMode = TextWrappingModes.Normal;
        hudText.overflowMode = TextOverflowModes.Ellipsis;

        EnsureHudActionsText();
        UpdateHudText();
    }

    private void EnsureHudActionsText()
    {
        if (hudCanvas == null)
            hudCanvas = hudText != null ? hudText.GetComponentInParent<Canvas>() : null;

        if (hudCanvas == null)
            return;

        if (hudActionsText != null)
            return;

        GameObject panelObject = new("Lab2HudActionsPanel");
        panelObject.transform.SetParent(hudCanvas.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -328f);
        panelRect.sizeDelta = new Vector2(430f, 96f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);
        panelImage.raycastTarget = false;

        GameObject textObject = new("Lab2HudActionsText");
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);

        hudActionsText = textObject.AddComponent<TextMeshProUGUI>();
        hudActionsText.fontSize = 18f;
        hudActionsText.alignment = TextAlignmentOptions.TopLeft;
        hudActionsText.color = Color.white;
        hudActionsText.raycastTarget = false;
        hudActionsText.textWrappingMode = TextWrappingModes.Normal;
        hudActionsText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void UpdateHudText()
    {
        if (hudText == null)
            return;

        hudText.text = BuildHudText();

        if (hudActionsText != null)
            hudActionsText.text = BuildHudActionsText();
    }

    private string BuildHudActionsText()
    {
        return currentStage switch
        {
            Lab2Stage.Continuity => "Действия:\nEnter — записать пару",
            Lab2Stage.DetermineFirstSecondPhase => "Действия:\n1 — Перемычка, 2 — ~36 В, 3 — PV, Enter — проверить",
            Lab2Stage.DetermineThirdPhase => "Действия:\n1 — Перемычка, 2 — ~36 В, 3 — PV, Enter — проверить",
            Lab2Stage.StarConnectionCheck => "Действия:\n1 — Звезда 1, 2 — Звезда 2, 3 — Питание 1, 4 — Питание 2, Enter — проверить",
            Lab2Stage.RotationSpeedCalculation => paConnected
                ? "Действия:\nEnter — провернуть ротор"
                : "Действия:\nВыберите две клеммы статора C1-C6 для PA",
            Lab2Stage.Completed => "Действия:\nR — начать заново, T — подробные результаты",
            _ => "Действия: нет"
        };
    }

    private string BuildHudText()
    {
        StringBuilder builder = new();

        switch (currentStage)
        {
            case Lab2Stage.Continuity:
                builder.AppendLine("Этап: Прозвонка");
                builder.AppendLine("Выберите две клеммы");
                builder.AppendLine($"Выбрано: {GetSelectedTerminalsText()}");
                builder.AppendLine($"Найдено пар: {foundPairs.Count} / {StatorWindingModel.PhaseWindingCount}");
                break;

            case Lab2Stage.DetermineFirstSecondPhase:
            case Lab2Stage.DetermineThirdPhase:
                builder.AppendLine($"Этап: {GetStageName(currentStage)}");
                builder.AppendLine($"Активная роль: {GetRoleName(selectedConnectionRole)}");
                builder.AppendLine($"Перемычка: {GetConnectionText(Lab2ConnectionRole.Jumper)}");
                builder.AppendLine($"~36 В: {GetConnectionText(Lab2ConnectionRole.Supply36V)}");
                builder.AppendLine($"PV: {GetConnectionText(Lab2ConnectionRole.Meter)}");
                builder.AppendLine("Подсказка: выберите роль и две клеммы");
                break;

            case Lab2Stage.StarConnectionCheck:
                builder.AppendLine("Этап: Проверка соединения в звезду");
                builder.AppendLine($"Звезда 1: {GetConnectionText(Lab2ConnectionRole.StarJumper1)}");
                builder.AppendLine($"Звезда 2: {GetConnectionText(Lab2ConnectionRole.StarJumper2)}");
                builder.AppendLine($"Питание 1: {GetConnectionText(Lab2ConnectionRole.SupplyLine1)}");
                builder.AppendLine($"Питание 2: {GetConnectionText(Lab2ConnectionRole.SupplyLine2)}");
                break;

            case Lab2Stage.RotationSpeedCalculation:
                builder.AppendLine("Этап: Определение скорости вращения");
                builder.AppendLine("Подключите PA к одной фазной обмотке статора");
                builder.AppendLine("Допустимые пары: C1-C4, C2-C5, C3-C6");
                builder.AppendLine(paConnected ? $"PA подключён: {paConnection.First}-{paConnection.Second}" : "PA подключён: нет");
                builder.AppendLine($"Выбрано: {(paConnected ? $"{paConnection.First} - {paConnection.Second}" : GetSelectedTerminalsText())}");
                builder.AppendLine($"Обороты: {rotorTurns} / {StatorWindingModel.TrainingRotorTurns}");
                builder.AppendLine($"Отклонения стрелки PA: {needleDeflections}");
                builder.AppendLine(paConnected ? "Подсказка: Enter — провернуть ротор" : "Подсказка: выберите две клеммы статора C1-C6 для PA");
                break;

            case Lab2Stage.Completed:
                builder.AppendLine("Лабораторная работа завершена");
                builder.AppendLine("T — открыть подробные результаты");
                builder.AppendLine("Нажмите «Начать заново» для повторного прохождения");
                break;
        }

        builder.AppendLine();
        builder.AppendLine($"Результат: {GetShortHudMessage(lastActionMessage)}");

        return builder.ToString();
    }

    private string GetSelectedTerminalsText()
    {
        if (selectedTerminals.Count == 0)
            return "нет";

        StringBuilder builder = new();

        for (int i = 0; i < selectedTerminals.Count; i++)
        {
            if (i > 0)
                builder.Append(", ");

            builder.Append(selectedTerminals[i] != null ? selectedTerminals[i].TerminalId.ToString() : "?");
        }

        return builder.ToString();
    }

    private string GetShortHudMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "нет";

        string singleLine = message.Replace("\r", " ").Replace("\n", " ");
        const int maxLength = 90;

        return singleLine.Length <= maxLength
            ? singleLine
            : singleLine.Substring(0, maxLength - 3) + "...";
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
        bool isCompleted = currentStage == Lab2Stage.Completed;

        SetButtonActive(recordPairButton, isContinuity);
        SetButtonActive(jumperRoleButton, isMarking || isStarCheck || isRotationSpeedCalculation);
        SetButtonActive(supplyRoleButton, isMarking || isStarCheck);
        SetButtonActive(meterRoleButton, isMarking || isStarCheck);
        SetButtonActive(fourthRoleButton, isStarCheck);
        SetButtonActive(checkMarkingSchemeButton, isMarking || isStarCheck);
        SetButtonActive(calculateSpeedButton, isRotationSpeedCalculation);
        SetButtonActive(resetLabButton, isCompleted);
        RefreshButtonLabels();
        UpdateHudText();
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
            Lab2ConnectionRole.Meter => "PV",
            Lab2ConnectionRole.StarJumper1 => "Звезда: перемычка 1",
            Lab2ConnectionRole.StarJumper2 => "Звезда: перемычка 2",
            Lab2ConnectionRole.SupplyLine1 => "Питание: линия 1",
            Lab2ConnectionRole.SupplyLine2 => "Питание: линия 2",
            Lab2ConnectionRole.MicroammeterPA => "PA",
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

        if (currentStage == Lab2Stage.RotationSpeedCalculation)
            return buttonIndex == 0 ? Lab2ConnectionRole.MicroammeterPA : Lab2ConnectionRole.None;

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

        if (currentStage == Lab2Stage.RotationSpeedCalculation)
        {
            SetButtonLabel(jumperRoleButton, "PA");
            SetButtonLabel(calculateSpeedButton, paConnected ? "Провернуть ротор" : "Подключите PA");
            return;
        }

        SetButtonLabel(jumperRoleButton, "Перемычка");
        SetButtonLabel(supplyRoleButton, "~36 В");
        SetButtonLabel(meterRoleButton, "PV");
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
            return FormatInstrumentReading(secondPhaseReading);

        if (currentStage == Lab2Stage.DetermineThirdPhase
            && StatorWindingModel.TryCheckThirdPhaseMarkingScheme(
                jumper.First,
                jumper.Second,
                supply.First,
                supply.Second,
                meter.First,
                meter.Second,
                out string thirdPhaseReading))
            return FormatInstrumentReading(thirdPhaseReading);

        return "не определено — схема подключения не завершена или содержит ошибку";
    }

    private string FormatInstrumentReading(string reading)
    {
        return reading;
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
        builder.AppendLine("PA использован для определения скорости.");
        builder.AppendLine($"N = {needleDeflections};");
        builder.AppendLine($"n = {rotorTurns};");
        builder.AppendLine($"p = {calculatedPolePairs};");
        builder.AppendLine($"nc = {calculatedSynchronousSpeed} об/мин.");

        return builder.ToString();
    }

    private string BuildRotationSpeedCalculationText(bool includeResult)
    {
        StringBuilder builder = new();
        builder.AppendLine("Определение скорости вращения двигателя");
        builder.AppendLine();
        builder.AppendLine("Подключение PA:");
        builder.AppendLine(paConnected ? $"PA подключён к выводам статора: {paConnection.First} - {paConnection.Second}" : "PA не подключён");
        builder.AppendLine("Используются клеммы микроамперметра PA: LeftPA и CenterPA.");
        builder.AppendLine();
        builder.AppendLine("Учебный пример расчёта по отклонениям PA:");
        builder.AppendLine($"N = {needleDeflections} — число отклонений стрелки PA");
        builder.AppendLine($"n = {rotorTurns} — число оборотов ротора");
        builder.AppendLine("f = 50 Гц — частота сети");
        builder.AppendLine($"Обороты: {rotorTurns} / {StatorWindingModel.TrainingRotorTurns}");
        builder.AppendLine($"Отклонения стрелки PA: {needleDeflections}");
        builder.AppendLine();
        builder.AppendLine("Формулы:");
        builder.AppendLine("p = N / n");
        builder.AppendLine("nc = 60f / p");

        if (includeResult)
        {
            builder.AppendLine();
            builder.AppendLine("Расчёт:");
            builder.AppendLine($"p = {needleDeflections} / {rotorTurns} = {calculatedPolePairs}");
            builder.AppendLine($"nc = 60 · 50 / {calculatedPolePairs} = {calculatedSynchronousSpeed} об/мин");
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
