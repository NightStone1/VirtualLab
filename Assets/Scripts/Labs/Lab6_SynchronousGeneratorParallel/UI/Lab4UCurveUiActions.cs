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

public class Lab4UCurveUiActions : MonoBehaviour
{
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;

    private void Awake()
    {
        ResolveController();
    }

    public void SelectNoLoadSeries()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.SetUCurveSeries(UCurveSeries.NoLoad);
        }
    }

    public void SelectHalfLoadSeries()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.SetUCurveSeries(UCurveSeries.HalfLoad);
        }
    }

    public void SelectFullLoadSeries()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.SetUCurveSeries(UCurveSeries.FullLoad);
        }
    }

    public void RecordPoint()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.RecordMeasurement();
        }
    }

    public void ClearCurrentSeries()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.ClearUCurveSeries(labController.CurrentUCurveSeries);
        }
    }

    public void ClearNoLoadSeries()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.ClearUCurveSeries(UCurveSeries.NoLoad);
        }
    }

    public void ClearHalfLoadSeries()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.ClearUCurveSeries(UCurveSeries.HalfLoad);
        }
    }

    public void ClearFullLoadSeries()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.ClearUCurveSeries(UCurveSeries.FullLoad);
        }
    }

    public void ClearAllSeries()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.ClearAllUCurvePoints();
        }
    }

    public void ResetLab()
    {
        if (TryGetController(out SyncGeneratorLabController labController))
        {
            labController.ResetLab();
        }
    }

    private bool TryGetController(out SyncGeneratorLabController labController)
    {
        ResolveController();
        labController = controller;
        return labController != null;
    }

    private void ResolveController()
    {
        if (controller != null || !autoFindController)
        {
            return;
        }

        controller = FindFirstObjectByType<SyncGeneratorLabController>();
    }
}
