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

public class Lab6Q2PositionInteractable : MonoBehaviour
{
    [SerializeField] private Lab6Controller controller;
    [SerializeField] private int position;

    private void OnValidate()
    {
        position = Mathf.Clamp(position, 0, 7);
    }

    private void OnMouseDown()
    {
        if (controller == null)
        {
            Debug.LogWarning($"Lab6Q2PositionInteractable {name}: controller is not assigned.");
            return;
        }

        controller.SetQ2Position(Mathf.Clamp(position, 0, 7));
    }
}
