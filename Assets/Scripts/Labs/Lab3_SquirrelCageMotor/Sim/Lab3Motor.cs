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

public class Lab3Motor : MonoBehaviour
{
    public float TargetRPM = 0f;
    public float CurrentRPM = 0f;
    public float acceleration = 200f;
    public float deceleration = 200f;

    public TMP_Text rpmText;

    [Header("Механическая связь M1-G1")]
    public Transform coupledShaft;           // Вал, соединяющий M1 и G1
    public Transform g1Rotor;                // Ротор генератора G1 (опционально)

    private float angle = 0f;

    void Start()
    {
        InvokeRepeating(nameof(Tick), 0f, 1f / 60f);
    }

    void Tick()
    {
        float delta = TargetRPM - CurrentRPM;

        if (Mathf.Abs(delta) > 0.01f)
        {
            float speed = delta > 0 ? acceleration : deceleration;
            CurrentRPM += Mathf.Sign(delta) * speed * Time.deltaTime;
            CurrentRPM = Mathf.Clamp(CurrentRPM, 0, TargetRPM);
        }

        // Обновление текста
        if (rpmText != null)
        {
            rpmText.text = $"{CurrentRPM:F0} об./мин.";
        }

        if (CurrentRPM > 0.01f)
        {
            float rotationDelta = (CurrentRPM / 60f) * 360f * Time.deltaTime;
            angle += rotationDelta;
            transform.localRotation = Quaternion.Euler(0f, -90f, angle);

            // Вал M1-G1 вращается синхронно
            if (coupledShaft != null)
            {
                coupledShaft.Rotate(Vector3.forward, rotationDelta, Space.Self);
            }

            // Ротор генератора G1 вращается синхронно с M1
            if (g1Rotor != null)
            {
                g1Rotor.Rotate(Vector3.forward, rotationDelta, Space.Self);
            }
        }
    }
}
