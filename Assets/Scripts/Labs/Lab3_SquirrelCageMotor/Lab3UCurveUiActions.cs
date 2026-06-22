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

public class Lab3UCurveUiActions : MonoBehaviour
{
    public Lab3_ElectricCircuit controller;
    public bool autoFindController = true;

    private void Awake()
    {
        ResolveController();
    }

    public void RecordNoLoadPoint()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.RecordNoLoadPoint();
    }

    public void RecordLoadPoint()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.RecordLoadPoint();
    }

    public void RecordExternalPoint()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.RecordExternalPoint();
    }

    public void RecordRegulatingPoint()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.RecordRegulatingPoint();
    }

    public void RecordShortCircuitPoint()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.RecordShortCircuitPoint();
    }

    public void EnableShortCircuitMode()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.EnableShortCircuitMode();
    }

    public void DisableShortCircuitMode()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.DisableShortCircuitMode();
    }

    public void ClearAllCharacteristicData()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.ClearAllCharacteristicData();
    }

    public void ResetCircuit()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.ResetCircuit();
    }

    public void EmergencyStop()
    {
        if (TryGetController(out Lab3_ElectricCircuit c))
            c.EmergencyStop();
    }

    private bool TryGetController(out Lab3_ElectricCircuit c)
    {
        ResolveController();
        c = controller;
        return c != null;
    }

    private void ResolveController()
    {
        if (controller != null || !autoFindController)
            return;

        controller = FindFirstObjectByType<Lab3_ElectricCircuit>();
    }
}
