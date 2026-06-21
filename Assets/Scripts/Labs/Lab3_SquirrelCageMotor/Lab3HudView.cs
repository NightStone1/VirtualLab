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
    public void ClearAllPoints() => InvokeController(c => c.ClearCurrentStagePoints());
    public void ResetLab() => InvokeController(c => c.ResetLab());
    public void ToggleQ1() => InvokeController(c => c.ToggleQ1());
    public void ToggleQ2() => InvokeController(c => c.ToggleQ2());
    public void ToggleQ3() => InvokeController(c => c.ToggleQ3());
    public void ToggleShortCircuitMode() => InvokeController(c => c.ToggleShortCircuitMode());
    public void ToggleResistanceMeasurementMode() => InvokeController(c => c.ToggleResistanceMeasurementMode());
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
                return "Это подготовительный debug-режим измерения сопротивлений. Включите R mode, изменяйте R1/R2 для получения разных тестовых значений U/I и запишите 5 точек таблицы 1.1. Q1/Q2/Q3 на этом этапе не являются обязательными для записи измерений.";
            case Lab3Stage.CircuitSetup:
                return "Подготовьте схему к опытам. Включите Q1 и Q3. На этом этапе измерения не записываются.";
            case Lab3Stage.NoLoadCharacteristic:
                return "Включите Q1 и Q3, оставьте Q2 выключенным, SC должен быть выключен. Изменяйте R1 и запишите 5 точек Ea=f(If).";
            case Lab3Stage.LoadCharacteristic:
                return "Включите Q1, Q2 и Q3. SC должен быть выключен. Поддерживайте Ia условно постоянным, изменяйте R1 и запишите 5 точек U=f(If).";
            case Lab3Stage.ExternalCharacteristic:
                return "Включите Q1, Q2 и Q3. SC должен быть выключен. Поддерживайте If условно постоянным, изменяйте R2 и запишите 5 точек U=f(Ia).";
            case Lab3Stage.RegulationCharacteristic:
                return "Включите Q1, Q2 и Q3. SC и R mode должны быть выключены. Изменяйте R2, чтобы менять ток якоря Ia. Затем нажмите Tune U, чтобы подстроить возбуждение через R1 и вернуть U к целевому значению. После этого нажмите Record. Запишите 5 точек If=f(Ia).";
            case Lab3Stage.ShortCircuitCharacteristic:
                return "Включите Q1 и режим SC. Напряжение U должно быть около 0. Изменяйте R1 и запишите 5 точек Ik=f(If).";
            case Lab3Stage.Completed:
                return "Работа завершена. Проверьте заполнение таблиц 1.1-1.6. Reset выполняет полный сброс лабораторной работы.";
            default:
                return "Ознакомьтесь со схемой установки. На этом этапе измерения не записываются. Нажмите Next для перехода к измерению сопротивлений.";
        }
    }

    private static string GetStateText(Lab3Controller source)
    {
        return
            $"Q1={OnOff(source.Q1Enabled)}, Q2={OnOff(source.Q2Enabled)}, Q3={OnOff(source.Q3Enabled)}, SC={OnOff(source.ShortCircuitEnabled)}, R mode={OnOff(source.ResistanceMeasurementMode)}\n" +
            $"R1={source.R1Position:F0}%, R2={source.R2Position:F0}%\n" +
            $"U={source.Voltage:F1} В, Ea={source.Emf:F1} В, Ia={source.ArmatureCurrent:F2} А, If={source.FieldCurrent:F3} А, Ik={source.ShortCircuitCurrent:F2} А, omega={source.Omega:F0} рад/с" +
            GetRegulationTargetText(source);
    }

    private static string GetRegulationTargetText(Lab3Controller source)
    {
        if (source.CurrentStage != Lab3Stage.RegulationCharacteristic)
        {
            return string.Empty;
        }

        return $"\nU target={source.TargetRegulationVoltage:F1} В, ΔU={source.RegulationVoltageDelta:F1} В. Для подстройки нажмите Tune U.";
    }

    private static string GetPointsText(Lab3Controller source)
    {
        return
            $"Точек текущего этапа: {source.GetRecordedPointCount(source.CurrentStage)}/5\n" +
            $"1.1 сопротивления: {source.ResistancePoints.Count}/5; 1.2 ХХ: {source.NoLoadPoints.Count}/5; 1.3 нагрузочная: {source.LoadPoints.Count}/5\n" +
            $"1.4 внешняя: {source.ExternalPoints.Count}/5; 1.5 регулировочная: {source.RegulationPoints.Count}/5; 1.6 КЗ: {source.ShortCircuitPoints.Count}/5";
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
