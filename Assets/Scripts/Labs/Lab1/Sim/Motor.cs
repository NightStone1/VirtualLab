using UnityEngine;

public class Motor : MonoBehaviour
{
    public float TargetRPM = 0f;      // RPM, который должен быть
    public float CurrentRPM = 0f;     // RPM, который сейчас
    public float acceleration = 200f; // насколько быстро крутится до цели
    public float deceleration = 200f;

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

        if (CurrentRPM > 0.01f)
        {
            angle += (CurrentRPM / 60f) * 360f * Time.deltaTime;
            transform.localRotation = Quaternion.Euler(0f, -90f, angle);
        }
    }
}