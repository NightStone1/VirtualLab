// Copyright (c) 2026 Бабичева Екатерина Анатольевна,
// Бибко Эдуард Александрович.
//
// Данный программный код разработан в рамках выпускной квалификационной работы
// "Виртуальный методический комплекс по дисциплине "Электрические машины"".
//
// Использование программного комплекса в учебном процессе АМТИ допускается
// в рамках подписанного акта о внедрении.
//
// Дальнейшее распространение, модификация, переработка, передача третьим лицам,
// публикация исходного кода, а также использование за пределами указанного
// внедрения допускаются только с письменного согласия авторов, если иное
// не предусмотрено отдельным соглашением.

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
                return "Включите R mode, изменяйте R1/R2 для получения разных тестовых значений U/I и запишите 5 точек таблицы 3.1. Q1/Q2/Q3 на этом этапе не обязательны.";
            case Lab3Stage.CircuitSetup:
                return "Подготовьте схему к опытам. Включите Q1 и Q3. На этом этапе измерения не записываются.";
            case Lab3Stage.NoLoadCharacteristic:
                return "Включите Q1 и Q3, оставьте Q2 выключенным, КЗ должен быть выключен. Изменяйте R2/If и запишите 5 точек Ea=f(If).";
            case Lab3Stage.LoadCharacteristic:
                return "Включите Q1, Q2 и Q3. КЗ должен быть выключен. Поддерживайте Ia через R1 условно постоянным, изменяйте R2/If и запишите 5 точек U=f(If).";
            case Lab3Stage.ExternalCharacteristic:
                return "Включите Q1, Q2 и Q3. КЗ должен быть выключен. После первой точки держите R2/If постоянным, изменяйте R1/Ia и запишите 5 точек U=f(Ia).";
            case Lab3Stage.RegulationCharacteristic:
                return "Включите Q1, Q2 и Q3. КЗ и R mode должны быть выключены. Перед первой точкой установите напряжение генератора PV2 около 220 В через R2/If. После записи первой точки текущее PV2 станет целевым U. Далее изменяйте нагрузку R1/Ia, а R2/If будет автоматически подстраиваться для удержания PV2.";
            case Lab3Stage.ShortCircuitCharacteristic:
                return "Включите Q1 и режим КЗ. Напряжение U должно быть около 0. Изменяйте R2/If и запишите 5 точек Ik=f(If).";
            case Lab3Stage.Completed:
                return "Работа завершена. Проверьте заполнение таблиц 3.1-3.6. Reset выполняет полный сброс лабораторной работы.";
            default:
                return "Ознакомьтесь со схемой установки. На этом этапе измерения не записываются. Нажмите Next для перехода к измерению сопротивлений.";
        }
    }

    private static string GetStateText(Lab3Controller source)
    {
        return
            $"Q1={OnOff(source.Q1Enabled)}, Q2={OnOff(source.Q2Enabled)}, Q3={OnOff(source.Q3Enabled)}, SC={OnOff(source.ShortCircuitEnabled)}, R mode={OnOff(source.ResistanceMeasurementMode)}\n" +
            $"R1/Ia={source.R1Position:F0}%, R2/If={source.R2Position:F0}%\n" +
            $"PV1={220f:F0}В | M1(PA1)={source.MotorCurrent:F2}А\n" +
            $"PV2={source.Voltage:F1}В | G1: Ia(PA2)={source.ArmatureCurrent:F2}А If(PA3)={source.FieldCurrent:F3}А Ik={source.ShortCircuitCurrent:F2}А\n" +
            $"Ea={source.Emf:F1}В omega={source.Omega:F0}рад/с" +
            GetRegulationTargetText(source);
    }

    private static string GetRegulationTargetText(Lab3Controller source)
    {
        if (source.CurrentStage != Lab3Stage.RegulationCharacteristic)
        {
            return string.Empty;
        }

        return $"\nPV2 target={source.TargetRegulationVoltage:F1} В, ΔPV2={source.RegulationVoltageDelta:F1} В. R2/If подстраивается автоматически.";
    }

    private static string GetPointsText(Lab3Controller source)
    {
        return
            $"Точек текущего этапа: {source.GetRecordedPointCount(source.CurrentStage)}/5\n" +
            $"3.1 сопротивления: {source.ResistancePoints.Count}/5; 3.2 ХХ: {source.NoLoadPoints.Count}/5; 3.3 нагрузочная: {source.LoadPoints.Count}/5\n" +
            $"3.4 внешняя: {source.ExternalPoints.Count}/5; 3.5 регулировочная: {source.RegulationPoints.Count}/5; 3.6 КЗ: {source.ShortCircuitPoints.Count}/5";
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
