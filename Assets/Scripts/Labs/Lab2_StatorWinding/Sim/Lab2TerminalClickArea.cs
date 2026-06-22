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

public class Lab2TerminalClickArea : MonoBehaviour
{
    [SerializeField] private Lab2Terminal terminal;

    public void Initialize(Lab2Terminal owner)
    {
        terminal = owner;
    }

    private void Awake()
    {
        if (terminal == null)
            terminal = GetComponentInParent<Lab2Terminal>();
    }

    private void OnMouseDown()
    {
        if (terminal != null)
            terminal.HandleClick();
    }
}
