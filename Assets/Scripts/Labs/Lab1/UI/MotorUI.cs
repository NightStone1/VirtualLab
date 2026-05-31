using UnityEngine;
using UnityEngine.UI; // обязательно для UI

public class MotorUI : MonoBehaviour
{
    public Motor motor;          // ссылка на двигатель
    public Text rpmText;         // текстовое поле в UI

    void Update()
    {
        if (motor != null && rpmText != null)
        {
            // округляем до целых или с 1 знаком
            rpmText.text = $"Обороты: {motor.CurrentRPM:F0} RPM";

            // или с десятичными:
            // rpmText.text = $"Обороты: {motor.CurrentRPM:F1} RPM";
        }
    }
}