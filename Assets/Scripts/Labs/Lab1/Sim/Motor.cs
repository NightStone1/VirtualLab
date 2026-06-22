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
using TMPro;

public class Motor : MonoBehaviour
{
    public float TargetRPM = 0f;
    public float CurrentRPM = 0f;
    public float acceleration = 200f;
    public float deceleration = 200f;

    public TMP_Text rpmText;

    private float angle = 0f;

    void Start()
    {
        InvokeRepeating(nameof(Tick), 0f, 1f / 60f);

        // Поиск TMP_Text на сцене, если не назначен вручную
        if (rpmText == null)
        {
            rpmText = FindObjectOfType<TMP_Text>();
            Debug.Log(rpmText != null ? "TMP_Text найден!" : "TMP_Text НЕ найден!");
        }
    }

    void Tick()
    {
        float delta = TargetRPM - CurrentRPM;

        if (Mathf.Abs(delta) > 0.01f)
        {
            float speed = delta > 0 ? acceleration : deceleration;
            CurrentRPM += Mathf.Sign(delta) * speed * Time.deltaTime;
            CurrentRPM = Mathf.Max(0f, CurrentRPM);
            if (delta > 0f && CurrentRPM > TargetRPM)
                CurrentRPM = TargetRPM;
        }

        // Обновление текста
        if (rpmText != null)
        {
            rpmText.text = $"{CurrentRPM:F0} об./мин.";
            Debug.Log($"Текст обновлён: {rpmText.text}"); // Проверка в консоли
        }
        else
        {
            Debug.LogWarning("rpmText = null! Не назначен в инспекторе или не найден на сцене.");
        }

        if (CurrentRPM > 0.01f)
        {
            angle += (CurrentRPM / 60f) * 360f * Time.deltaTime;
            transform.localRotation = Quaternion.Euler(0f, -90f, angle);
        }
    }
}