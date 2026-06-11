using TMPro;
using UnityEngine;

public class Lab6HudView : MonoBehaviour
{
    [SerializeField] private Lab6Controller controller;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private TextMeshProUGUI switchesText;
    [SerializeField] private TextMeshProUGUI measurementsText;
    [SerializeField] private TextMeshProUGUI pointsText;

    private void Awake()
    {
        if (controller == null)
        {
            controller = FindAnyLab6Controller();
        }
    }

    public void SetController(Lab6Controller value)
    {
        controller = value;
    }

    public void BindRuntimeFields(
        TextMeshProUGUI title,
        TextMeshProUGUI stage,
        TextMeshProUGUI instruction,
        TextMeshProUGUI warning,
        TextMeshProUGUI switches,
        TextMeshProUGUI measurements,
        TextMeshProUGUI points)
    {
        titleText = title;
        stageText = stage;
        instructionText = instruction;
        warningText = warning;
        switchesText = switches;
        measurementsText = measurements;
        pointsText = points;
    }

    public void Refresh(Lab6Controller source)
    {
        if (source == null)
        {
            return;
        }

        controller = source;
        Lab6Measurement measurement = source.CurrentMeasurement;
        Lab6Data data = source.Data;
        bool showDebug = source.ShowDebugControls;

        SetText(titleText, "Лабораторная 4. Испытание асинхронного двигателя с короткозамкнутым ротором");
        SetText(stageText, source.CurrentStage == Lab6Stage.Completed ? "Лабораторная завершена" : "Этап: " + GetStageName(source.CurrentStage));
        SetText(instructionText, GetInstruction(source.CurrentStage));
        SetText(warningText, source.LastMessage);
        SetText(switchesText, GetModeText(source.CurrentStage, source.WindingConnectionText));

        if (showDebug && measurement != null)
        {
            SetText(measurementsText,
                $"U = {measurement.voltage:F0} В\n" +
                $"I = {measurement.current:F2} А\n" +
                $"P1 = {measurement.powerInput:F0} Вт\n" +
                $"P2 = {measurement.powerOutput:F0} Вт\n" +
                $"n = {measurement.speed:F0} об/мин\n" +
                $"M = {measurement.torque:F2} Н*м\n" +
                $"cosφ = {measurement.cosPhi:F2}\n" +
                $"η = {measurement.efficiency:P0}\n" +
                $"s = {measurement.slip:P1}\n" +
                $"Za/Zb/Zc/Zср = {measurement.za:F2}/{measurement.zb:F2}/{measurement.zc:F2}/{measurement.zAverage:F2} Ом");
        }
        else
        {
            SetText(measurementsText, string.Empty);
        }

        if (showDebug)
        {
            SetText(pointsText,
                "Условия записи точки:\n" + GetRecordConditions(source.CurrentStage) + "\n" +
                $"Текущее состояние: Q2={source.Q2Position}/7, R={source.LoadStep} ({source.LoadPercent:F0}%), тормоз {OnOff(source.BrakeEnabled)}\n" +
                $"Точки ХХ: {source.GetRecordedPointCount(Lab6Stage.NoLoad)}/{data.requiredNoLoadPoints}\n" +
                $"Точки КЗ: {source.GetRecordedPointCount(Lab6Stage.ShortCircuit)}/{data.requiredShortCircuitPoints}\n" +
                $"Точки нагрузки: {source.GetRecordedPointCount(Lab6Stage.Load)}/{data.requiredLoadPoints}\n" +
                $"Сопротивления: {source.GetRecordedPointCount(Lab6Stage.ResistanceMeasurement)}/1\n" +
                $"Всего записано: {source.RecordedPointCount}");
        }
        else
        {
            SetText(pointsText, GetCurrentProgressText(source, data));
        }
    }

    public void ToggleQ1() => InvokeController(c => c.ToggleQ1());
    public void ToggleQ3() => InvokeController(c => c.ToggleQ3());
    public void ToggleQ4() => InvokeController(c => c.ToggleQ4());
    public void ToggleQ5() => InvokeController(c => c.ToggleQ5());
    public void ToggleQ6() => InvokeController(c => c.ToggleQ6());
    public void Q2Up() => InvokeController(c => c.IncreaseQ2());
    public void Q2Down() => InvokeController(c => c.DecreaseQ2());
    public void ToggleBrake() => InvokeController(c => c.ToggleBrake());
    public void LoadUp() => InvokeController(c => c.ChangeLoadPercent(10f));
    public void LoadDown() => InvokeController(c => c.ChangeLoadPercent(-10f));
    public void RecordPoint() => InvokeController(c => c.RecordPoint());
    public void NextStage() => InvokeController(c => c.NextStage());
    public void EmergencyStop() => InvokeController(c => c.EmergencyStop());

    private void InvokeController(System.Action<Lab6Controller> action)
    {
        if (controller == null)
        {
            controller = FindAnyLab6Controller();
        }

        if (controller == null)
        {
            Debug.LogWarning("Lab6HudView: controller is not assigned.");
            return;
        }

        action(controller);
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
        {
            target.text = value;
            target.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }
    }

    private static Lab6Controller FindAnyLab6Controller()
    {
        Lab6Controller[] controllers = FindObjectsByType<Lab6Controller>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return controllers.Length > 0 ? controllers[0] : null;
    }

    private static string OnOff(bool value)
    {
        return value ? "включён" : "выключен";
    }

    private static string GetStageName(Lab6Stage stage)
    {
        switch (stage)
        {
            case Lab6Stage.NoLoad:
                return "Опыт холостого хода";
            case Lab6Stage.ShortCircuit:
                return "Опыт короткого замыкания";
            case Lab6Stage.Load:
                return "Опыт непосредственной нагрузки";
            case Lab6Stage.ResistanceMeasurement:
                return "Измерение сопротивлений обмоток";
            case Lab6Stage.Completed:
                return "Лабораторная завершена";
            default:
                return "Подготовка";
        }
    }

    private static string GetInstruction(Lab6Stage stage)
    {
        switch (stage)
        {
            case Lab6Stage.NoLoad:
                return "Включите Q1, Q5 и Q6.\nУстановите РНТ Q2 в положение 1-5.\nНажмите «Записать точку» на TV.\nЗапишите 5 разных точек.";
            case Lab6Stage.ShortCircuit:
                return "Ротор заторможен автоматически.\nВключите Q1 и Q5.\nУстановите РНТ Q2 в положение 1-5.\nНажмите «Записать точку» на TV.\nQ2 выше 5 запрещён: ток КЗ превышает 1.2 Iн.";
            case Lab6Stage.Load:
                return "Тормоз отключён автоматически.\nВключите Q1, Q3, Q4, Q5 и Q6.\nУстановите рабочее напряжение РНТ Q2.\nИзменяйте нагрузку реостатом R: 0%, 25%, 50%, 75%, 100%.\nЗапишите 5 разных точек.";
            case Lab6Stage.ResistanceMeasurement:
                return "Нажмите «Записать точку», чтобы измерить сопротивления обмоток статора.\nДопускается одна запись.";
            case Lab6Stage.Completed:
                return "Лабораторная завершена. Результаты доступны на TV.";
            default:
                return "Подготовьте стенд. Для начала опыта холостого хода перейдите к следующему этапу.";
        }
    }

    private static string GetCurrentProgressText(Lab6Controller source, Lab6Data data)
    {
        switch (source.CurrentStage)
        {
            case Lab6Stage.NoLoad:
                return $"Записано точек: {source.GetRecordedPointCount(Lab6Stage.NoLoad)}/{data.requiredNoLoadPoints}";
            case Lab6Stage.ShortCircuit:
                return $"Записано точек: {source.GetRecordedPointCount(Lab6Stage.ShortCircuit)}/{data.requiredShortCircuitPoints}";
            case Lab6Stage.Load:
                return $"Записано точек: {source.GetRecordedPointCount(Lab6Stage.Load)}/{data.requiredLoadPoints}";
            case Lab6Stage.ResistanceMeasurement:
                return $"Запись сопротивлений: {source.GetRecordedPointCount(Lab6Stage.ResistanceMeasurement)}/1";
            default:
                return string.Empty;
        }
    }

    private static string GetModeText(Lab6Stage stage, string windingConnectionText)
    {
        switch (stage)
        {
            case Lab6Stage.ShortCircuit:
                return windingConnectionText + "\nСостояние ротора: заторможен";
            case Lab6Stage.Load:
                return windingConnectionText + "\nСостояние ротора: свободное вращение";
            case Lab6Stage.NoLoad:
            case Lab6Stage.ResistanceMeasurement:
                return windingConnectionText;
            case Lab6Stage.Completed:
                return string.Empty;
            default:
                return "Соединение обмоток: не требуется";
        }
    }

    private static string GetRecordConditions(Lab6Stage stage)
    {
        switch (stage)
        {
            case Lab6Stage.NoLoad:
                return "Q1 on, Q2 > 0, Q5 on, Q6 on. Дубли по Q2/U0 запрещены.";
            case Lab6Stage.ShortCircuit:
                return "Q1 on, Q5 on, Q2 > 0, тормоз on, Ik <= 1.2 Iн. Дубли по Q2/Uk запрещены.";
            case Lab6Stage.Load:
                return "Q1, Q2, Q3, Q4, Q5, Q6 on, тормоз off. Дубли по Load% запрещены.";
            case Lab6Stage.ResistanceMeasurement:
                return "Одна запись сопротивлений обмоток.";
            default:
                return "Перейдите к этапу кнопкой TV: Следующий этап.";
        }
    }
}
