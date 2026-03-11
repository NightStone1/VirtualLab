using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Switch : MonoBehaviour
{
    public Vector3 onEuler;   // углы для включенного состояния
    public Vector3 offEuler;  // углы для выключенного состояния
    public float rotationSpeed = 2f;
    public bool isOn = false;
    private bool isAnimating = false;
    public event System.Action<bool> OnValueChanged;
    void OnMouseDown()
    {
        if (isAnimating) return;

        isOn = !isOn;
        StartCoroutine(RotateSwitch(isOn));
    }

    IEnumerator RotateSwitch(bool toOn)
    {
        isAnimating = true;

        Quaternion startRot = transform.localRotation;
        Quaternion endRot = Quaternion.Euler(toOn ? onEuler : offEuler);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            transform.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.localRotation = endRot;
        OnValueChanged?.Invoke(toOn);
        isAnimating = false;
    }
}
