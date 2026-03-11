using System;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.UIElements;

public class Rotator : MonoBehaviour
{
    [Header("Rotation limits (in degrees)")]
    public float minAngle = 150f;   // при 0%
    public float maxAngle = -150f;  // при 100%

    [Header("Sensitivity")]
    public float sensitivity = 3f;

    [Header("Current value (0Ц100%)")]
    [Range(0, 100)]
    private float value = 0f;
    [Header("LLR value (0Ц250)")]
    [Range(0, 250)]
    private float LLRvalue = 0f;
    public event System.Action<float> OnValueChanged;
    public bool isLLR;
    private bool isDragging = false;
    private float startAngle;
    private float startValue;
    private float screenZ;
    void OnMouseDown()
    {
        isDragging = true;
        screenZ = Camera.main.WorldToScreenPoint(transform.position).z;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenZ));
        Vector3 dir = mouseWorld - transform.position;
        // дл€ вращени€ вокруг Y Ч считаем угол в плоскости XZ
        startAngle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        if (isLLR )
        {
            startValue = LLRvalue;
        }
        else
        {
            startValue = value;
        }        
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
           new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenZ));

        Vector3 dir = mouseWorld - transform.position;
        float currentAngle = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(startAngle, currentAngle);
        startAngle = currentAngle; // добавь сюда
        if (isLLR)
        {
            LLRvalue = Mathf.Clamp(LLRvalue + delta * sensitivity, 0f, 250f);
            float angle = Mathf.Lerp(minAngle, maxAngle, LLRvalue / 250f);
            transform.localRotation = Quaternion.Euler(angle, 90f, -90f);
            OnValueChanged?.Invoke(LLRvalue);
        }
        else
        {
            value = Mathf.Clamp(value + delta * sensitivity, 0f, 100f);
            float angle = Mathf.Lerp(minAngle, maxAngle, value / 100f);
            transform.localRotation = Quaternion.Euler(angle, 90f, -90f);
            OnValueChanged?.Invoke(value);
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
    }
}
