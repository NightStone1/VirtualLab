using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private Transform ammeterNeedle;
    [SerializeField] private Transform centerPA;
    [SerializeField] private Transform leftPA;
    [SerializeField] private Transform rightPA;
    [SerializeField] private Transform voltNeedle;
    [SerializeField] private Transform leftPV;
    [SerializeField] private Transform rightPV;
    [SerializeField] private Vector3 paNeedleAxis = Vector3.forward;
    [SerializeField] private float paNeedleDirection = 1f;
    [SerializeField] private float paContinuityDeflectionAngle = 20f;
    [SerializeField] private float paSpeedDeflectionAngle = 20f;
    [SerializeField] private float paNeedleDeflectionDuration = 0.18f;
    [SerializeField] private Vector3 voltNeedleAxis = Vector3.forward;
    [SerializeField] private float voltNeedleDirection = 1f;
    [SerializeField] private float voltDeflectionAngle = 25f;
    [SerializeField] private float voltNeedleDeflectionDuration = 0.18f;
    [SerializeField] private Transform switcherQ1;
    [SerializeField] private Transform switcherQ2;
    [SerializeField] private Transform startEngine;
    [SerializeField] private Transform stopEngine;
    [SerializeField] private Transform rotor;
    [SerializeField] private Vector3 rotorRotationAxis = Vector3.up;
    [Tooltip("Visual continuous motor rotation speed in degrees per second.")]
    [SerializeField] private float motorRotationSpeed = 360f;
    [Tooltip("Duration of one manual 360-degree rotor turn during speed calculation.")]
    [SerializeField] private float manualRotorTurnDuration = 1f;
    [SerializeField] private float switchClickRadius = 0.00025f;
    [SerializeField] private float buttonClickRadius = 0.0007f;
    [SerializeField] private Vector3 switchRotationAxis = Vector3.right;
    [SerializeField] private float switchOnAngle = 20f;
    [SerializeField] private float switchOffAngle = -20f;
    [SerializeField] private Vector3 buttonPressDirection = Vector3.back;
    [SerializeField] private float buttonPressDistance = 0.0005f;
    [SerializeField] private float buttonPressDuration = 0.06f;
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
    private Quaternion voltNeedleInitialRotation;
    private Coroutine paNeedleAnimation;
    private Coroutine voltNeedleAnimation;
    private Coroutine rotorTurnAnimation;
    private bool paNeedleWarningShown;
    private Lab2InteractiveElement q1Element;
    private Lab2InteractiveElement q2Element;
    private Lab2InteractiveElement startEngineElement;
    private Lab2InteractiveElement stopEngineElement;
    private bool q1Enabled;
    private bool q2Enabled;
    private bool motorRunning;
    private bool motorWasStartedSuccessfully;
    private bool isRotorTurnAnimationRunning;
    private bool rotorWarningShown;
    private RectTransform hudPanelRect;
    private RectTransform hudActionsPanelRect;
    private string lastPvPreviewKey = string.Empty;

    private const float HudPanelWidth = 430f;
    private const float HudPanelMinHeight = 300f;
    private const float HudPanelHorizontalPadding = 24f;
    private const float HudPanelVerticalPadding = 20f;
    private const float HudActionsMinHeight = 96f;
    private const float HudActionsHorizontalPadding = 24f;
    private const float HudActionsVerticalPadding = 16f;
    private const float HudPanelsGap = 12f;

    private void Start()
    {
        ResolveTerminals();
        ResolveInstrumentSceneObjects();
        ResolvePaNeedle();
        ResolvePvNeedle();
        ResolveMotorInteractiveElements();

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
        RotateMotorRotor();
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

            case Lab2Stage.MotorStartCheck:
                if (enterPressed)
                    ContinueAfterMotorStartCheck();
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
        if (ammeterNeedle == null)
            ammeterNeedle = FindAnchorByNames("AmmeterNeddle", "PANeedle", "Needle_PA", "MicroammeterNeedle", "AmmeterNeedle", "PA_Needle");
        else
            LogAssignedReference(nameof(ammeterNeedle), ammeterNeedle);

        if (ammeterNeedle != null)
        {
            paNeedleInitialRotation = ammeterNeedle.localRotation;
            Debug.Log($"Lab2 PA needle found: {ammeterNeedle.name}.");
            return;
        }

        Debug.LogWarning("Lab2 PA needle was not found. Speed counters will work without needle animation.");
        paNeedleWarningShown = true;
    }

    private void ResolveInstrumentSceneObjects()
    {
        centerPA = ResolveInspectorOrFallback(nameof(centerPA), centerPA, "Stend2/CenterPA", "CenterPA");
        rightPA = ResolveInspectorOrFallback(nameof(rightPA), rightPA, "Stend2/RightPA", "RightPA");
        leftPA = ResolveInspectorOrFallback(nameof(leftPA), leftPA, "Stend2/LeftPA", "LeftPA");
        leftPV = ResolveInspectorOrFallback(nameof(leftPV), leftPV, "Stend2/LeftPV", "LeftPV", "PV_A", "PV_PositiveAnchor");
        rightPV = ResolveInspectorOrFallback(nameof(rightPV), rightPV, "Stend2/RightPV", "RightPV", "PV_B", "PV_NegativeAnchor");
    }

    private void ResolvePvNeedle()
    {
        if (voltNeedle == null)
            voltNeedle = FindAnchorByNames("VoltNeedle");
        else
            LogAssignedReference(nameof(voltNeedle), voltNeedle);

        if (voltNeedle != null)
        {
            voltNeedleInitialRotation = voltNeedle.localRotation;
            Debug.Log($"Lab2 PV needle found: {voltNeedle.name}.");
        }
    }

    private void ResolveMotorInteractiveElements()
    {
        switcherQ1 = ResolveInspectorOrFallback(nameof(switcherQ1), switcherQ1, "Stend2/switcherQ1", "switcherQ1");
        switcherQ2 = ResolveInspectorOrFallback(nameof(switcherQ2), switcherQ2, "Stend2/switcherQ2", "switcherQ2");
        startEngine = ResolveInspectorOrFallback(nameof(startEngine), startEngine, "Stend2/StartEngine", "StartEngine");
        stopEngine = ResolveInspectorOrFallback(nameof(stopEngine), stopEngine, "Stend2/StopEngine", "StopEngine");
        rotor = ResolveInspectorOrFallback(nameof(rotor), rotor, "dvgatelstend2/rotor", "rotor");

        q1Element = EnsureInteractiveElement(switcherQ1, Lab2InteractiveElement.ElementType.Q1, "ClickArea_Q1", switchClickRadius);
        q2Element = EnsureInteractiveElement(switcherQ2, Lab2InteractiveElement.ElementType.Q2, "ClickArea_Q2", switchClickRadius);
        startEngineElement = EnsureInteractiveElement(startEngine, Lab2InteractiveElement.ElementType.StartButton, "ClickArea_Start", buttonClickRadius);
        stopEngineElement = EnsureInteractiveElement(stopEngine, Lab2InteractiveElement.ElementType.StopButton, "ClickArea_Stop", buttonClickRadius);

        if (rotor == null)
        {
            Debug.LogWarning("Lab2 motor: rotor object 'rotor' was not found. Motor state will work without rotor animation.");
            rotorWarningShown = true;
        }

        ResetMotorStartState();
    }

    private Transform FindSceneObjectTransform(string objectName)
    {
        return objectName switch
        {
            "switcherQ1" => FindTransformByPathOrNames("Stend2/switcherQ1", objectName),
            "switcherQ2" => FindTransformByPathOrNames("Stend2/switcherQ2", objectName),
            "StartEngine" => FindTransformByPathOrNames("Stend2/StartEngine", objectName),
            "StopEngine" => FindTransformByPathOrNames("Stend2/StopEngine", objectName),
            _ => FindTransformByNames(objectName)
        };
    }

    private Transform ResolveInspectorOrFallback(string fieldName, Transform assignedTransform, string fallbackPath, params string[] fallbackNames)
    {
        if (assignedTransform != null)
        {
            LogAssignedReference(fieldName, assignedTransform);
            return assignedTransform;
        }

        Transform fallback = FindTransformByPathOrNames(fallbackPath, fallbackNames);

        if (fallback != null)
            Debug.Log($"Lab2 refs: {fieldName} fallback = {GetTransformPath(fallback)}");

        return fallback;
    }

    private void LogAssignedReference(string fieldName, Transform assignedTransform)
    {
        Debug.Log($"Lab2 refs: {fieldName} assigned = {GetTransformPath(assignedTransform)}");
    }

    private Lab2InteractiveElement EnsureInteractiveElement(Transform target, Lab2InteractiveElement.ElementType type, string clickAreaName, float clickRadius)
    {
        if (target == null)
            return null;

        DisableVisualObjectColliders(target);

        Transform clickArea = FindChildByName(target, clickAreaName);

        if (clickArea == null)
        {
            GameObject clickAreaObject = new(clickAreaName);
            clickAreaObject.transform.SetParent(target, false);
            clickAreaObject.transform.localPosition = Vector3.zero;
            clickAreaObject.transform.localRotation = Quaternion.identity;
            clickAreaObject.transform.localScale = Vector3.one;
            clickArea = clickAreaObject.transform;
        }

        clickArea.localScale = new Vector3(
            Mathf.Abs(clickArea.localScale.x) <= 0.0001f ? 1f : Mathf.Abs(clickArea.localScale.x),
            Mathf.Abs(clickArea.localScale.y) <= 0.0001f ? 1f : Mathf.Abs(clickArea.localScale.y),
            Mathf.Abs(clickArea.localScale.z) <= 0.0001f ? 1f : Mathf.Abs(clickArea.localScale.z));

        if (!clickArea.TryGetComponent(out Lab2InteractiveElement element))
            element = clickArea.gameObject.AddComponent<Lab2InteractiveElement>();

        element.Initialize(
            type,
            this,
            target,
            clickRadius,
            switchRotationAxis,
            switchOnAngle,
            switchOffAngle,
            buttonPressDirection,
            buttonPressDistance,
            buttonPressDuration);
        return element;
    }

    private void DisableVisualObjectColliders(Transform visualTarget)
    {
        Collider[] colliders = visualTarget.GetComponents<Collider>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                Destroy(colliders[i]);
        }
    }

    public void HandleInteractiveElementClick(Lab2InteractiveElement.ElementType elementType)
    {
        switch (elementType)
        {
            case Lab2InteractiveElement.ElementType.Q1:
                ToggleQ1();
                break;

            case Lab2InteractiveElement.ElementType.Q2:
                ToggleQ2();
                break;

            case Lab2InteractiveElement.ElementType.StartButton:
                PressStartEngine();
                break;

            case Lab2InteractiveElement.ElementType.StopButton:
                PressStopEngine();
                break;
        }
    }

    private void ToggleQ1()
    {
        q1Enabled = !q1Enabled;

        if (!q1Enabled)
        {
            motorRunning = false;
            StopManualRotorTurnAnimation();
        }

        q1Element?.SetSwitchState(q1Enabled);
        PreviewVoltNeedleForCompleteMarkingScheme();
        SetResult(q1Enabled ? "Q1 включён" : "Q1 выключен");
    }

    private void ToggleQ2()
    {
        q2Enabled = !q2Enabled;

        if (!q2Enabled)
            motorRunning = false;

        q2Element?.SetSwitchState(q2Enabled);
        PreviewVoltNeedleForCompleteMarkingScheme();
        SetResult(q2Enabled ? "Q2 включён" : "Q2 выключен");
    }

    private void PressStartEngine()
    {
        startEngineElement?.PlayPressFeedback();

        if (currentStage != Lab2Stage.MotorStartCheck)
            return;

        if (!q1Enabled || !q2Enabled)
        {
            SetResult("Перед пуском включите Q1 и Q2.");
            return;
        }

        motorRunning = true;
        motorWasStartedSuccessfully = true;
        SetResult("Двигатель запущен.");
    }

    private void PressStopEngine()
    {
        stopEngineElement?.PlayPressFeedback();
        motorRunning = false;

        if (currentStage == Lab2Stage.MotorStartCheck)
            SetResult(motorWasStartedSuccessfully
                ? "Двигатель остановлен. Можно перейти к определению скорости."
                : "Двигатель остановлен.");
        else
            UpdateHudText();
    }

    private void ContinueAfterMotorStartCheck()
    {
        if (currentStage != Lab2Stage.MotorStartCheck)
            return;

        if (!motorWasStartedSuccessfully)
        {
            SetResult("Сначала запустите двигатель.");
            return;
        }

        if (motorRunning)
        {
            SetResult("Остановите двигатель кнопкой Стоп перед определением скорости.");
            return;
        }

        EnterRotationSpeedCalculationStage();
    }

    private void RotateMotorRotor()
    {
        if (!motorRunning)
            return;

        if (rotor == null)
        {
            if (!rotorWarningShown)
            {
                Debug.LogWarning("Lab2 motor: rotor object 'rotor' was not found. Motor state will work without rotor animation.");
                rotorWarningShown = true;
            }

            return;
        }

        Vector3 axis = rotorRotationAxis.sqrMagnitude > 0f ? rotorRotationAxis.normalized : Vector3.forward;
        rotor.Rotate(axis, motorRotationSpeed * Time.deltaTime, Space.Self);
    }

    private void ResetMotorStartState()
    {
        q1Enabled = false;
        q2Enabled = false;
        motorRunning = false;
        motorWasStartedSuccessfully = false;
        q1Element?.ResetVisualState();
        q2Element?.ResetVisualState();
        startEngineElement?.ResetVisualState();
        stopEngineElement?.ResetVisualState();
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

    private bool EnsureQ1ForContinuity()
    {
        if (q1Enabled)
            return true;

        SetResult("Включите Q1 для питания стенда и выполнения прозвонки.");
        return false;
    }

    private bool EnsureQ1Q2For36V()
    {
        if (!q1Enabled)
        {
            SetResult("Включите Q1 для подачи питания на стенд.");
            return false;
        }

        if (!q2Enabled)
        {
            SetResult("Включите Q2 для подачи напряжения ~36 В.");
            return false;
        }

        return true;
    }

    private bool EnsureQ1ForStarCheck()
    {
        if (q1Enabled)
            return true;

        SetResult("Включите Q1 перед проверкой соединения обмоток.");
        return false;
    }

    private bool EnsureQ1ForPaSpeed()
    {
        if (q1Enabled)
            return true;

        SetResult("Включите Q1 для работы микроамперметра PA.");
        return false;
    }

    private void CheckContinuity()
    {
        if (!EnsureQ1ForContinuity())
            return;

        Lab2TerminalId first = selectedTerminals[0].TerminalId;
        Lab2TerminalId second = selectedTerminals[1].TerminalId;
        CreateOrReplaceTemporaryContinuityWires(selectedTerminals[0], selectedTerminals[1]);

        bool hasContinuity = StatorWindingModel.HasContinuity(first, second);
        string result = hasContinuity ? "Цепь есть" : "Цепи нет";

        if (hasContinuity)
            StartPaNeedleAnimation(1, paContinuityDeflectionAngle);

        SetResult($"{first} - {second}: {result}");
    }

    public void RecordSelectedPair()
    {
        if (!EnsureQ1ForContinuity())
            return;

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
            ResetPvPreviewState();
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
        PreviewVoltNeedleForCompleteMarkingScheme();
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

        if (!EnsureQ1Q2For36V())
            return;

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
        ResetPvPreviewGuard();
        StartVoltNeedleAnimationIfNeeded(meterReading);
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
        if (!EnsureQ1Q2For36V())
            return;

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
        ResetPvPreviewGuard();
        StartVoltNeedleAnimationIfNeeded(meterReading);
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
        if (!EnsureQ1ForStarCheck())
            return;

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

        currentStage = Lab2Stage.MotorStartCheck;
        ResetPvPreviewState();
        selectedConnectionRole = Lab2ConnectionRole.None;
        markingConnections.Clear();
        ResetMotorStartState();
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult("Включите Q1 и Q2, затем нажмите Пуск.");
    }

    private void EnterRotationSpeedCalculationStage()
    {
        currentStage = Lab2Stage.RotationSpeedCalculation;
        selectedConnectionRole = Lab2ConnectionRole.MicroammeterPA;
        paConnected = false;
        paConnection = default;
        rotorTurns = 0;
        needleDeflections = 0;
        calculatedPolePairs = 0;
        calculatedSynchronousSpeed = 0;
        StopManualRotorTurnAnimation();
        ResetPaNeedle();
        ResetPvPreviewState();
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult("Двигатель запущен. Подключите PA к выводам статора для определения скорости.");
    }

    public void CalculateRotationSpeed()
    {
        if (currentStage != Lab2Stage.RotationSpeedCalculation)
        {
            SetResult("Расчёт скорости доступен после проверки соединения в звезду");
            return;
        }

        if (!EnsureQ1ForPaSpeed())
            return;

        if (motorRunning)
        {
            SetResult("Остановите двигатель перед определением скорости.");
            return;
        }

        if (isRotorTurnAnimationRunning)
            return;

        if (!paConnected)
        {
            SetResult("Сначала подключите PA к одной фазной обмотке");
            return;
        }

        if (rotorTurns < StatorWindingModel.TrainingRotorTurns)
        {
            rotorTurnAnimation = StartCoroutine(AnimateManualRotorTurn());
            SetResult("Выполняется один полный оборот ротора");
        }
    }

    private IEnumerator AnimateManualRotorTurn()
    {
        isRotorTurnAnimationRunning = true;
        Vector3 axis = rotorRotationAxis.sqrMagnitude > 0f ? rotorRotationAxis.normalized : Vector3.up;
        Quaternion startRotation = rotor != null ? rotor.localRotation : Quaternion.identity;
        float duration = Mathf.Max(0.01f, manualRotorTurnDuration);
        float elapsed = 0f;

        ResetPaNeedle();
        Vector3 paAxis = paNeedleAxis.sqrMagnitude > 0f ? paNeedleAxis.normalized : Vector3.forward;
        float paDirection = Mathf.Approximately(paNeedleDirection, 0f) ? 1f : Mathf.Sign(paNeedleDirection);

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float angle = Mathf.Lerp(0f, 360f, t);

            if (rotor != null)
                rotor.localRotation = startRotation * Quaternion.AngleAxis(angle, axis);

            if (ammeterNeedle != null)
            {
                float deflectionPhase = t * 3f;
                float pulse = Mathf.Sin((deflectionPhase - Mathf.Floor(deflectionPhase)) * Mathf.PI);
                ammeterNeedle.localRotation = paNeedleInitialRotation
                    * Quaternion.AngleAxis(paDirection * paSpeedDeflectionAngle * pulse, paAxis);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rotor != null)
            rotor.localRotation = startRotation * Quaternion.AngleAxis(360f, axis);

        if (ammeterNeedle != null)
            ammeterNeedle.localRotation = paNeedleInitialRotation;

        rotorTurns += 1;
        needleDeflections += 3;
        isRotorTurnAnimationRunning = false;
        rotorTurnAnimation = null;
        UpdateFoundPairsText();
        SetResult($"Ротор провернут: {rotorTurns} / {StatorWindingModel.TrainingRotorTurns}. Отклонения стрелки PA: {needleDeflections}");

        if (rotorTurns >= StatorWindingModel.TrainingRotorTurns)
            CompleteRotationSpeedCalculation();
    }

    private void CompleteRotationSpeedCalculation()
    {
        calculatedPolePairs = needleDeflections / rotorTurns;
        calculatedSynchronousSpeed = 60 * StatorWindingModel.TrainingSupplyFrequency / calculatedPolePairs;
        currentStage = Lab2Stage.Completed;
        motorRunning = false;
        ResetPvPreviewState();
        ClearSelection();
        RefreshTemporaryUi();
        UpdateFoundPairsText();
        SetResult($"p = {needleDeflections} / {rotorTurns} = {calculatedPolePairs}\nnc = 60 · 50 / {calculatedPolePairs} = {calculatedSynchronousSpeed} об/мин");
    }

    private void StartPaNeedleAnimation(int deflectionCount, float deflectionAngle)
    {
        if (ammeterNeedle == null)
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

        paNeedleAnimation = StartCoroutine(AnimatePaNeedle(deflectionCount, deflectionAngle));
    }

    private IEnumerator AnimatePaNeedle(int deflectionCount, float deflectionAngle)
    {
        Vector3 axis = paNeedleAxis.sqrMagnitude > 0f ? paNeedleAxis.normalized : Vector3.forward;
        float direction = Mathf.Approximately(paNeedleDirection, 0f) ? 1f : Mathf.Sign(paNeedleDirection);
        Quaternion deflectedRotation = paNeedleInitialRotation * Quaternion.AngleAxis(direction * deflectionAngle, axis);
        ammeterNeedle.localRotation = paNeedleInitialRotation;

        for (int i = 0; i < deflectionCount; i++)
        {
            ammeterNeedle.localRotation = paNeedleInitialRotation;
            yield return RotateNeedle(ammeterNeedle, paNeedleInitialRotation, deflectedRotation, paNeedleDeflectionDuration);
            yield return RotateNeedle(ammeterNeedle, deflectedRotation, paNeedleInitialRotation, paNeedleDeflectionDuration);
        }

        ammeterNeedle.localRotation = paNeedleInitialRotation;
        paNeedleAnimation = null;
    }

    private IEnumerator RotateNeedle(Transform needle, Quaternion from, Quaternion to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            needle.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }

        needle.localRotation = to;
    }

    private void ResetPaNeedle()
    {
        if (paNeedleAnimation != null)
        {
            StopCoroutine(paNeedleAnimation);
            paNeedleAnimation = null;
        }

        if (ammeterNeedle != null)
            ammeterNeedle.localRotation = paNeedleInitialRotation;
    }

    private void StartVoltNeedleAnimationIfNeeded(string meterReading)
    {
        if (string.IsNullOrEmpty(meterReading) || !meterReading.Contains("стрелка отклоняется"))
        {
            ResetVoltNeedle();
            return;
        }

        if (voltNeedle == null)
            return;

        if (voltNeedleAnimation != null)
            StopCoroutine(voltNeedleAnimation);

        voltNeedleAnimation = StartCoroutine(AnimateVoltNeedle());
    }

    private IEnumerator AnimateVoltNeedle()
    {
        Vector3 axis = voltNeedleAxis.sqrMagnitude > 0f ? voltNeedleAxis.normalized : Vector3.forward;
        float direction = Mathf.Approximately(voltNeedleDirection, 0f) ? 1f : Mathf.Sign(voltNeedleDirection);
        Quaternion deflectedRotation = voltNeedleInitialRotation * Quaternion.AngleAxis(direction * voltDeflectionAngle, axis);
        voltNeedle.localRotation = voltNeedleInitialRotation;
        yield return RotateNeedle(voltNeedle, voltNeedleInitialRotation, deflectedRotation, voltNeedleDeflectionDuration);
        yield return RotateNeedle(voltNeedle, deflectedRotation, voltNeedleInitialRotation, voltNeedleDeflectionDuration);
        voltNeedle.localRotation = voltNeedleInitialRotation;
        voltNeedleAnimation = null;
    }

    private void ResetVoltNeedle()
    {
        if (voltNeedleAnimation != null)
        {
            StopCoroutine(voltNeedleAnimation);
            voltNeedleAnimation = null;
        }

        if (voltNeedle != null)
            voltNeedle.localRotation = voltNeedleInitialRotation;
    }

    private void StopManualRotorTurnAnimation()
    {
        if (rotorTurnAnimation != null)
        {
            StopCoroutine(rotorTurnAnimation);
            rotorTurnAnimation = null;
        }

        isRotorTurnAnimationRunning = false;
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
        ResetMotorStartState();
        paConnected = false;
        paConnection = default;
        rotorTurns = 0;
        needleDeflections = 0;
        calculatedPolePairs = 0;
        calculatedSynchronousSpeed = 0;
        ResetPaNeedle();
        ResetPvPreviewState();
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
        PreviewVoltNeedleForCompleteMarkingScheme();

        SetResult(selectedConnectionRole == Lab2ConnectionRole.MicroammeterPA
            ? $"PA подключён к фазной обмотке {paConnection.First}-{paConnection.Second}."
            : $"{GetRoleName(selectedConnectionRole)}: {first} - {second}");
    }

    private void PreviewVoltNeedleForCompleteMarkingScheme()
    {
        if (currentStage != Lab2Stage.DetermineFirstSecondPhase && currentStage != Lab2Stage.DetermineThirdPhase)
        {
            ResetPvPreviewGuard();
            return;
        }

        if (!markingConnections.TryGetValue(Lab2ConnectionRole.Jumper, out RecordedPair jumper)
            || !markingConnections.TryGetValue(Lab2ConnectionRole.Supply36V, out RecordedPair supply)
            || !markingConnections.TryGetValue(Lab2ConnectionRole.Meter, out RecordedPair meter))
        {
            SetInactivePvPreviewKey("incomplete");
            return;
        }

        string baseKey = BuildPvPreviewKey(jumper, supply, meter);

        if (!q1Enabled || !q2Enabled)
        {
            SetInactivePvPreviewKey(baseKey);
            return;
        }

        bool validScheme;
        string meterReading;

        if (currentStage == Lab2Stage.DetermineFirstSecondPhase)
        {
            validScheme = StatorWindingModel.TryCheckFirstSecondPhaseMarkingScheme(
                jumper.First,
                jumper.Second,
                supply.First,
                supply.Second,
                meter.First,
                meter.Second,
                out meterReading);
        }
        else
        {
            validScheme = StatorWindingModel.TryCheckThirdPhaseMarkingScheme(
                jumper.First,
                jumper.Second,
                supply.First,
                supply.Second,
                meter.First,
                meter.Second,
                out meterReading);
        }

        string previewKey = $"{baseKey}|valid:{validScheme}|reading:{meterReading}";

        if (lastPvPreviewKey == previewKey)
            return;

        lastPvPreviewKey = previewKey;

        if (validScheme)
            StartVoltNeedleAnimationIfNeeded(meterReading);
        else
            ResetVoltNeedle();
    }

    private string BuildPvPreviewKey(RecordedPair jumper, RecordedPair supply, RecordedPair meter)
    {
        return $"stage:{currentStage}|q1:{q1Enabled}|q2:{q2Enabled}|jumper:{jumper.First}-{jumper.Second}|supply:{supply.First}-{supply.Second}|meter:{meter.First}-{meter.Second}";
    }

    private void SetInactivePvPreviewKey(string reason)
    {
        string previewKey = $"stage:{currentStage}|q1:{q1Enabled}|q2:{q2Enabled}|{reason}";

        if (lastPvPreviewKey == previewKey)
            return;

        lastPvPreviewKey = previewKey;
        ResetVoltNeedle();
    }

    private void ResetPvPreviewGuard()
    {
        lastPvPreviewKey = string.Empty;
    }

    private void ResetPvPreviewState()
    {
        ResetPvPreviewGuard();
        ResetVoltNeedle();
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
        centerPA ??= FindAnchorByNames("CenterPA");
        rightPA ??= FindAnchorByNames("RightPA");

        first = centerPA;
        second = rightPA;

        bool found = first != null && second != null;

        if (found)
            Debug.Log($"Lab2 wire: continuity PA anchors found: {first.name}, {second.name}.");

        return found;
    }

    private bool TryGetPvAnchors(out Transform first, out Transform second)
    {
        leftPV ??= FindAnchorByNames("LeftPV", "PV_A", "PV_PositiveAnchor");
        rightPV ??= FindAnchorByNames("RightPV", "PV_B", "PV_NegativeAnchor");

        first = leftPV;
        second = rightPV;

        bool found = first != null && second != null;

        if (found)
            Debug.Log($"Lab2 wire: PV anchors found: {first.name}, {second.name}.");

        return found;
    }

    private bool TryGetSpeedPaAnchors(out Transform first, out Transform second)
    {
        centerPA ??= FindAnchorByNames("CenterPA");
        leftPA ??= FindAnchorByNames("LeftPA");

        first = centerPA;
        second = leftPA;

        bool found = first != null && second != null;

        if (found)
            Debug.Log($"Lab2 wire: speed PA anchors found: {first.name}, {second.name}.");

        return found;
    }

    private Transform FindAnchorByNames(params string[] names)
    {
        if (names == null || names.Length == 0)
            return null;

        string exactPath = GetExactScenePath(names[0]);
        return string.IsNullOrEmpty(exactPath)
            ? FindTransformByNames(names)
            : FindTransformByPathOrNames(exactPath, names);
    }

    private Transform FindTransformByPathOrNames(string path, params string[] fallbackNames)
    {
        Transform foundByPath = FindTransformByPath(path);

        if (foundByPath != null)
        {
            Debug.Log($"Lab2 scene lookup: {GetLookupLabel(path, fallbackNames)} resolved to {GetTransformPath(foundByPath)}.");
            return foundByPath;
        }

        Debug.LogWarning($"Lab2 scene lookup: exact path '{path}' was not found.");
        return FindTransformByNames(fallbackNames);
    }

    private string GetExactScenePath(string name)
    {
        return name switch
        {
            "AmmeterNeddle" => "Stend2/AmmeterNeddle",
            "CenterPA" => "Stend2/CenterPA",
            "LeftPA" => "Stend2/LeftPA",
            "RightPA" => "Stend2/RightPA",
            "LeftPV" => "Stend2/LeftPV",
            "RightPV" => "Stend2/RightPV",
            "VoltNeedle" => "Stend2/VoltNeedle",
            "switcherQ1" => "Stend2/switcherQ1",
            "switcherQ2" => "Stend2/switcherQ2",
            "StartEngine" => "Stend2/StartEngine",
            "StopEngine" => "Stend2/StopEngine",
            "rotor" => "dvgatelstend2/rotor",
            _ => string.Empty
        };
    }

    private string GetLookupLabel(string path, string[] fallbackNames)
    {
        if (fallbackNames != null && fallbackNames.Length > 0 && !string.IsNullOrEmpty(fallbackNames[0]))
            return fallbackNames[0];

        int slashIndex = path.LastIndexOf('/');
        return slashIndex >= 0 ? path[(slashIndex + 1)..] : path;
    }

    private Transform FindTransformByNames(params string[] names)
    {
        if (names == null || names.Length == 0)
            return null;

        Transform stendRoot = FindTransformInLoadedScenes(new[] { "Stend2" }, false);

        if (stendRoot != null)
        {
            Transform foundInStend = FindChildByAnyName(stendRoot, names);

            if (foundInStend != null)
            {
                Debug.Log($"Lab2 scene lookup: {names[0]} resolved to {GetTransformPath(foundInStend)}.");
                return foundInStend;
            }
        }

        Transform found = FindTransformInLoadedScenes(names, false);

        if (found != null)
        {
            Debug.Log($"Lab2 scene lookup: {names[0]} resolved to {GetTransformPath(found)}.");
            return found;
        }

        Debug.LogWarning($"Lab2 scene lookup: object '{names[0]}' was not found.");
        return null;
    }

    private Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string[] parts = path.Split('/');

        if (parts.Length == 0)
            return null;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);

            if (!scene.isLoaded)
                continue;

            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
            {
                if (rootObjects[rootIndex] == null
                    || !string.Equals(rootObjects[rootIndex].name, parts[0], System.StringComparison.OrdinalIgnoreCase))
                    continue;

                Transform current = rootObjects[rootIndex].transform;

                for (int partIndex = 1; partIndex < parts.Length; partIndex++)
                {
                    current = FindDirectChildByName(current, parts[partIndex]);

                    if (current == null)
                        break;
                }

                if (current != null)
                    return current;
            }
        }

        return null;
    }

    private Transform FindDirectChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    private string GetTransformPath(Transform target)
    {
        if (target == null)
            return string.Empty;

        Stack<string> parts = new();
        Transform current = target;

        while (current != null)
        {
            parts.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", parts);
    }

    private Transform FindTransformInLoadedScenes(string[] names, bool skipStendChildren)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);

            if (!scene.isLoaded)
                continue;

            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
            {
                if (rootObjects[rootIndex] == null)
                    continue;

                if (skipStendChildren && string.Equals(rootObjects[rootIndex].name, "Stend2", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                Transform found = FindChildByAnyName(rootObjects[rootIndex].transform, names);

                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private Transform FindChildByAnyName(Transform root, string[] names)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int childIndex = 0; childIndex < children.Length; childIndex++)
        {
            for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
            {
                if (string.Equals(children[childIndex].name, names[nameIndex], System.StringComparison.OrdinalIgnoreCase))
                    return children[childIndex];
            }
        }

        return null;
    }

    private Transform FindChildByName(Transform root, string name)
    {
        return FindChildByAnyName(root, new[] { name });
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

        if (currentStage == Lab2Stage.MotorStartCheck)
        {
            foundPairsText.text = BuildMotorStartCheckText();
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
            hudPanelRect ??= hudText.transform.parent as RectTransform;
            hudText.textWrappingMode = TextWrappingModes.Normal;
            hudText.overflowMode = TextOverflowModes.Overflow;
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
        panelRect.sizeDelta = new Vector2(HudPanelWidth, HudPanelMinHeight);
        hudPanelRect = panelRect;

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
        hudText.overflowMode = TextOverflowModes.Overflow;

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
        {
            hudActionsPanelRect ??= hudActionsText.transform.parent as RectTransform;
            hudActionsText.textWrappingMode = TextWrappingModes.Normal;
            hudActionsText.overflowMode = TextOverflowModes.Overflow;
            return;
        }

        GameObject panelObject = new("Lab2HudActionsPanel");
        panelObject.transform.SetParent(hudCanvas.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -328f);
        panelRect.sizeDelta = new Vector2(HudPanelWidth, HudActionsMinHeight);
        hudActionsPanelRect = panelRect;

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
        hudActionsText.overflowMode = TextOverflowModes.Overflow;
    }

    private void UpdateHudText()
    {
        if (hudText == null)
            return;

        hudText.text = BuildHudText();

        if (hudActionsText != null)
            hudActionsText.text = BuildHudActionsText();

        ResizeHudPanels();
    }

    private void ResizeHudPanels()
    {
        if (hudText != null && hudPanelRect != null)
        {
            float contentWidth = Mathf.Max(1f, HudPanelWidth - HudPanelHorizontalPadding);
            float preferredHeight = hudText.GetPreferredValues(hudText.text, contentWidth, 0f).y;
            float panelHeight = Mathf.Ceil(Mathf.Max(HudPanelMinHeight, preferredHeight + HudPanelVerticalPadding));
            hudPanelRect.sizeDelta = new Vector2(HudPanelWidth, panelHeight);
        }

        if (hudActionsText == null || hudActionsPanelRect == null)
            return;

        float actionsContentWidth = Mathf.Max(1f, HudPanelWidth - HudActionsHorizontalPadding);
        float actionsPreferredHeight = hudActionsText.GetPreferredValues(hudActionsText.text, actionsContentWidth, 0f).y;
        float actionsHeight = Mathf.Ceil(Mathf.Max(HudActionsMinHeight, actionsPreferredHeight + HudActionsVerticalPadding));
        hudActionsPanelRect.sizeDelta = new Vector2(HudPanelWidth, actionsHeight);

        if (hudPanelRect != null)
            hudActionsPanelRect.anchoredPosition = new Vector2(16f, -16f - hudPanelRect.sizeDelta.y - HudPanelsGap);
    }

    private string BuildHudActionsText()
    {
        return currentStage switch
        {
            Lab2Stage.Continuity => "Действия:\nEnter — записать пару",
            Lab2Stage.DetermineFirstSecondPhase => "Действия:\n1 — Перемычка, 2 — ~36 В, 3 — PV, Enter — проверить",
            Lab2Stage.DetermineThirdPhase => "Действия:\n1 — Перемычка, 2 — ~36 В, 3 — PV, Enter — проверить",
            Lab2Stage.StarConnectionCheck => "Действия:\n1 — Звезда 1, 2 — Звезда 2, 3 — Питание 1, 4 — Питание 2, Enter — проверить",
            Lab2Stage.MotorStartCheck => GetMotorStartActionsText(),
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

            case Lab2Stage.MotorStartCheck:
                builder.AppendLine("Этап: Проверка запуска двигателя");
                builder.AppendLine($"Q1: {GetSwitchStateText(q1Enabled)}");
                builder.AppendLine($"Q2: {GetSwitchStateText(q2Enabled)}");
                builder.AppendLine($"Двигатель: {GetMotorStateText()}");
                builder.AppendLine($"Подсказка: {GetMotorStartHintText()}");
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

        builder.AppendLine($"Q1: {GetSwitchStateText(q1Enabled)}; Q2: {GetSwitchStateText(q2Enabled)}");
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
            Lab2Stage.MotorStartCheck => "Проверка запуска двигателя",
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

    private string GetSwitchStateText(bool enabled)
    {
        return enabled ? "включён" : "выключен";
    }

    private string GetMotorStateText()
    {
        return motorRunning ? "запущен" : "остановлен";
    }

    private string GetMotorStartActionsText()
    {
        if (motorRunning)
            return "Действия:\nНажмите Стоп перед определением скорости";

        if (motorWasStartedSuccessfully)
            return "Действия:\nEnter — перейти к определению скорости";

        return "Действия:\nВключите Q1 и Q2, затем нажмите Пуск";
    }

    private string GetMotorStartHintText()
    {
        if (motorRunning)
            return "Нажмите Стоп перед определением скорости";

        if (motorWasStartedSuccessfully)
            return "Enter — перейти к определению скорости";

        return "Включите Q1 и Q2, затем нажмите Пуск";
    }

    private string BuildMotorStartCheckText()
    {
        StringBuilder builder = new();
        builder.AppendLine("Проверка запуска двигателя");
        builder.AppendLine($"Q1: {GetSwitchStateText(q1Enabled)}");
        builder.AppendLine($"Q2: {GetSwitchStateText(q2Enabled)}");
        builder.AppendLine($"Двигатель: {GetMotorStateText()}");
        builder.AppendLine();
        builder.AppendLine(GetMotorStartHintText());

        return builder.ToString();
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
