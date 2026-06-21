using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch : MonoBehaviour
{
    public Vector3 onEuler;   // ���� ��� ����������� ���������
    public Vector3 offEuler;  // ���� ��� ������������ ���������
    public float rotationSpeed = 2f;
    public bool isOn = false;
    private bool isAnimating = false;
    public event System.Action<bool> OnValueChanged;

    public GameObject circleObject;  // ������ �� ������ Circle
    private Renderer circleRenderer;
    private Renderer switchRenderer;

    void Start()
    {
        switchRenderer = GetComponent<Renderer>();

        if (circleObject != null)
        {
            circleRenderer = circleObject.GetComponent<Renderer>();
        }

        // ��������� ��������� ����
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

    /// Установить состояние без анимации (для кнопок с самовозвратом)
    public void SetStateImmediate(bool state)
    {
        if (isAnimating) return;
        isOn = state;
        transform.localRotation = Quaternion.Euler(state ? onEuler : offEuler);
        SetAllColors(state ? Color.green : Color.red);
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