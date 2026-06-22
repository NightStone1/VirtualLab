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

public class Lab6LoadStepInteractable : MonoBehaviour
{
    [SerializeField] private Lab6Controller controller;
    [SerializeField] private int step;
    [SerializeField] private bool enableDebugLogs;

    private void OnValidate()
    {
        step = Mathf.Clamp(step, 0, 4);
    }

    private void OnMouseDown()
    {
        int clampedStep = Mathf.Clamp(step, 0, 4);
        if (enableDebugLogs)
        {
            Debug.Log($"Lab6 R-block click: object={name}, step={clampedStep}");
        }

        if (controller == null)
        {
            controller = FindAnyLab6Controller();
        }

        if (controller == null)
        {
            Debug.LogWarning($"Lab6LoadStepInteractable: controller is not assigned on {name}");
            return;
        }

        controller.ToggleLoadStep(clampedStep);
    }

    private static Lab6Controller FindAnyLab6Controller()
    {
        Lab6Controller[] controllers = FindObjectsByType<Lab6Controller>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return controllers.Length > 0 ? controllers[0] : null;
    }
}
