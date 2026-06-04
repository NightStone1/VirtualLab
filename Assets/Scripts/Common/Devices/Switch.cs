using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch : MonoBehaviour
{
    public Vector3 onEuler;   // углы для включенного состояния
    public Vector3 offEuler;  // углы для выключенного состояния
    public float rotationSpeed = 2f;
    public bool isOn = false;
    private bool isAnimating = false;
    public event System.Action<bool> OnValueChanged;

    public GameObject circleObject;  // Ссылка на объект Circle
    private Renderer circleRenderer;
    private Renderer switchRenderer;

    void Start()
    {
        switchRenderer = GetComponent<Renderer>();

        if (circleObject != null)
        {
            circleRenderer = circleObject.GetComponent<Renderer>();
        }

        // Установим начальный цвет
        SetAllColors(isOn ? Color.green : Color.red);
    }

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

        Color startColor = GetCurrentColor();
        Color endColor = toOn ? Color.green : Color.red;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            transform.localRotation = Quaternion.Slerp(startRot, endRot, t);

            Color currentColor = Color.Lerp(startColor, endColor, t);
            SetAllColors(currentColor);

            yield return null;
        }

        transform.localRotation = endRot;
        SetAllColors(endColor);

        OnValueChanged?.Invoke(toOn);
        isAnimating = false;
    }

    private void SetAllColors(Color color)
    {
        if (switchRenderer != null)
            switchRenderer.material.color = color;

        if (circleRenderer != null)
            circleRenderer.material.color = color;
    }

    private Color GetCurrentColor()
    {
        if (switchRenderer != null)
            return switchRenderer.material.color;
        else if (circleRenderer != null)
            return circleRenderer.material.color;
        else
            return Color.white;
    }
}