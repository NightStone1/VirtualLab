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

using UnityEngine;

public enum Lab4ControlAction
{
    ToggleQ1,
    ToggleQ2,
    ToggleQ3,
    ToggleQ4,
    ToggleQ5,
    IncreaseR1,
    DecreaseR1,
    IncreaseDriveRegulator,
    DecreaseDriveRegulator,
    RecordMeasurement,
    ResetLab
}

public class Lab4InteractableControl : MonoBehaviour
{
    public Lab4ControlAction action;
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;
    public bool logDebug = true;

    private void Awake()
    {
        ResolveController();
    }

    public void Execute()
    {
        ResolveController();

        if (controller == null)
        {
            Debug.LogWarning($"Lab4 action skipped: controller not found for {action}.", this);
            return;
        }

        switch (action)
        {
            case Lab4ControlAction.ToggleQ1:
                controller.ToggleQ1();
                break;
            case Lab4ControlAction.ToggleQ2:
                controller.ToggleQ2();
                break;
            case Lab4ControlAction.ToggleQ3:
                controller.ToggleQ3();
                break;
            case Lab4ControlAction.ToggleQ4:
                controller.ToggleQ4();
                break;
            case Lab4ControlAction.ToggleQ5:
                controller.ToggleQ5();
                break;
            case Lab4ControlAction.IncreaseR1:
                controller.IncreaseR1();
                break;
            case Lab4ControlAction.DecreaseR1:
                controller.DecreaseR1();
                break;
            case Lab4ControlAction.IncreaseDriveRegulator:
                controller.IncreaseDriveRegulator();
                break;
            case Lab4ControlAction.DecreaseDriveRegulator:
                controller.DecreaseDriveRegulator();
                break;
            case Lab4ControlAction.RecordMeasurement:
                controller.RecordMeasurement();
                break;
            case Lab4ControlAction.ResetLab:
                controller.ResetLab();
                break;
        }

        if (logDebug)
        {
            Debug.Log($"Lab4 action executed: {action}", this);
        }
    }

    private void OnMouseDown()
    {
        Execute();
    }

    private void ResolveController()
    {
        if (controller != null || !autoFindController)
        {
            return;
        }

        controller = Object.FindFirstObjectByType<SyncGeneratorLabController>();
    }
}
