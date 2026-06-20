using TMPro;
using UnityEngine;

public class Lab3HudView : MonoBehaviour
{
    [SerializeField] private Lab3Controller controller;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI messageText;

    private void Awake()
    {
        if (controller == null)
        {
            controller = FindAnyLab3Controller();
        }
    }

    public void SetController(Lab3Controller value)
    {
        controller = value;
    }

    public void BindRuntimeFields(
        TextMeshProUGUI title,
        TextMeshProUGUI stage,
        TextMeshProUGUI instruction,
        TextMeshProUGUI state,
        TextMeshProUGUI points,
        TextMeshProUGUI message)
    {
        titleText = title;
        stageText = stage;
        instructionText = instruction;
        stateText = state;
        pointsText = points;
        messageText = message;
    }

    public void Refresh(Lab3Controller source)
    {
        if (source == null)
        {
            return;
        }

        controller = source;
        SetText(titleText, "Лабораторная 3. Испытание генератора постоянного тока независимого возбуждения");
        SetText(stageText, "Этап: " + source.GetStageName(source.CurrentStage));
        SetText(instructionText, GetInstruction(source.CurrentStage));
        SetText(stateText, GetStateText(source));
        SetText(pointsText, GetPointsText(source));
        SetText(messageText, source.LastMessage);
    }

    public void NextStage() => InvokeController(c => c.NextStage());
    public void PreviousStage() => InvokeController(c => c.PreviousStage());
    public void RecordPoint() => InvokeController(c => c.RecordPoint());
    public void RemoveLastPoint() => InvokeController(c => c.RemoveLastPointInCurrentStage());
    public void ClearAllPoints() => InvokeController(c => c.ClearAllPoints());
    public void ResetLab() => InvokeController(c => c.ResetLab());
    public void ToggleQ1() => InvokeController(c => c.ToggleQ1());
    public void ToggleQ2() => InvokeController(c => c.ToggleQ2());
    public void ToggleQ3() => InvokeController(c => c.ToggleQ3());
    public void ToggleShortCircuitMode() => InvokeController(c => c.ToggleShortCircuitMode());
    public void R1Up() => InvokeController(c => c.IncreaseR1());
    public void R1Down() => InvokeController(c => c.DecreaseR1());
    public void R2Up() => InvokeController(c => c.IncreaseR2());
    public void R2Down() => InvokeController(c => c.DecreaseR2());

    private void InvokeController(System.Action<Lab3Controller> action)
    {
        if (controller == null)
        {
            controller = FindAnyLab3Controller();
        }

        if (controller == null)
        {
            Debug.LogWarning("Lab3HudView: controller is not assigned.");
            return;
        }

        action(controller);
    }

    private static string GetInstruction(Lab3Stage stage)
    {
        switch (stage)
        {
            case Lab3Stage.ResistanceMeasurement:
                return "Запишите тестовое измерение U/I для таблицы 1.1. Для MVP значения задаются синтетически через R1/R2.";
            case Lab3Stage.CircuitSetup:
                return "Подготовьте схему: Q1 - питание привода, Q2 - нагрузка генератора, Q3 - возбуждение генератора.";
            case Lab3Stage.NoLoadCharacteristic:
                return "Холостой ход: включите Q1 и Q3, выключите Q2. Меняйте R1 и записывайте Ea = f(If) при Ia около 0.";
            case Lab3Stage.LoadCharacteristic:
                return "Нагрузочная характеристика: включите Q1, Q2, Q3. Удерживайте Ia условно постоянным, меняйте R1 и записывайте U = f(If).";
            case Lab3Stage.ExternalCharacteristic:
                return "Внешняя характеристика: включите Q1, Q2, Q3. Удерживайте If условно постоянным, меняйте R2 и записывайте U = f(Ia).";
            case Lab3Stage.RegulationCharacteristic:
                return "Регулировочная характеристика: включите Q1, Q2, Q3. Поддерживайте U условно постоянным и записывайте If = f(Ia).";
            case Lab3Stage.ShortCircuitCharacteristic:
                return "Короткое замыкание: включите Q1, включите режим КЗ. Меняйте R1 и записывайте Ik = f(If) при U около 0.";
            case Lab3Stage.Completed:
                return "Проверьте записанные точки таблиц 1.1-1.6 и переходите к построению характеристик и сравнению.";
            default:
                return "Осмотрите стенд: M1, G1, R1, R2, Q1, Q2, Q3, PV1/PV2 и PA1/PA2/PA3. Затем перейдите к следующему этапу.";
        }
    }

    private static string GetStateText(Lab3Controller source)
    {
        return
            $"Q1={OnOff(source.Q1Enabled)}, Q2={OnOff(source.Q2Enabled)}, Q3={OnOff(source.Q3Enabled)}, КЗ={OnOff(source.ShortCircuitEnabled)}\n" +
            $"R1={source.R1Position:F0}%, R2={source.R2Position:F0}%\n" +
            $"U={source.Voltage:F1} В, Ea={source.Emf:F1} В, Ia={source.ArmatureCurrent:F2} А, If={source.FieldCurrent:F3} А, Ik={source.ShortCircuitCurrent:F2} А, omega={source.Omega:F0} рад/с";
    }

    private static string GetPointsText(Lab3Controller source)
    {
        return
            $"Точек текущего этапа: {source.GetRecordedPointCount(source.CurrentStage)}\n" +
            $"1.1 сопротивления: {source.ResistancePoints.Count}; 1.2 ХХ: {source.NoLoadPoints.Count}; 1.3 нагрузочная: {source.LoadPoints.Count}\n" +
            $"1.4 внешняя: {source.ExternalPoints.Count}; 1.5 регулировочная: {source.RegulationPoints.Count}; 1.6 КЗ: {source.ShortCircuitPoints.Count}";
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
        {
            target.text = value;
            target.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }
    }

    private static string OnOff(bool value)
    {
        return value ? "on" : "off";
    }

    private static Lab3Controller FindAnyLab3Controller()
    {
        Lab3Controller[] controllers = FindObjectsByType<Lab3Controller>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return controllers.Length > 0 ? controllers[0] : null;
    }
}
