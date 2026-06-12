using UnityEngine;
[System.Serializable]
public class SliderColors1
{
    public Color color0Percent = Color.green;      // 0% - Зелёный
    public Color color33Percent = Color.yellow;    // 33% - Жёлтый  
    public Color color66Percent = new Color(1f, 0.5f, 0f); // 66% - Оранжевый
    public Color color100Percent = Color.red;      // 100% - Красный
}
public class SliderGor : MonoBehaviour
{
    public float minZ;
    public float maxZ;
    public bool inverted = false;

    // Компоненты для изменения цвета
    public Renderer sliderRenderer;  // Рендерер ползунка (перетаскиваемая часть)
    public bool changeParentColor = false;  // Менять цвет родительского объекта

    public event System.Action<float> OnValueChanged;

    private Camera cam;
    private bool isDragging = false;
    private float offsetZ;
    public SliderColors1 sliderColors;
    public float Percent { get; private set; }

    void Start()
    {
        maxZ = transform.position.z;
        minZ = transform.position.z - (0.13f - (-0.0775f));
        cam = Camera.main;

        // Если рендерер не назначен, пытаемся найти на этом объекте
        if (sliderRenderer == null)
        {
            sliderRenderer = GetComponent<Renderer>();
        }

        // Устанавливаем начальный цвет
        UpdateColor(0f);
    }

    void OnMouseDown()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            return;
        }

        isDragging = true;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.WorldToScreenPoint(transform.position).z)
        );

        offsetZ = transform.position.z - mouseWorldPos.z;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        if (cam == null)
        {
            return;
        }

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.WorldToScreenPoint(transform.position).z)
        );

        float newZ = mouseWorldPos.z + offsetZ;
        newZ = Mathf.Clamp(newZ, minZ, maxZ);
        transform.position = new Vector3(transform.position.x,  transform.position.y, newZ);

        // Расчёт процентов (0-100)
        Percent = Mathf.InverseLerp(minZ, maxZ, newZ) * 100f;

        if (inverted)
            Percent = 100f - Percent;

        // Изменяем цвет в зависимости от процентов
        UpdateColor(Percent);

        OnValueChanged?.Invoke(Percent);
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    // Метод для обновления цвета
    void UpdateColor(float percent)
    {
        if (sliderRenderer == null) return;

        float t = percent / 100f;
        Color targetColor;

        if (t < 0.33f)
        {
            // 0% -> 33%
            float segmentT = t / 0.33f;
            targetColor = Color.Lerp(sliderColors.color0Percent, sliderColors.color33Percent, segmentT);
        }
        else if (t < 0.66f)
        {
            // 33% -> 66%
            float segmentT = (t - 0.33f) / 0.33f;
            targetColor = Color.Lerp(sliderColors.color33Percent, sliderColors.color66Percent, segmentT);
        }
        else
        {
            // 66% -> 100%
            float segmentT = (t - 0.66f) / 0.34f;
            targetColor = Color.Lerp(sliderColors.color66Percent, sliderColors.color100Percent, segmentT);
        }

        sliderRenderer.material.color = targetColor;

        if (changeParentColor && transform.parent != null)
        {
            Renderer parentRenderer = transform.parent.GetComponent<Renderer>();
            if (parentRenderer != null)
            {
                parentRenderer.material.color = targetColor;
            }
        }
    }

    // Публичный метод для ручной установки процентов (например, из другого скрипта)
    public void SetPercent(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);

        // Конвертируем проценты обратно в позицию Y
        float targetPercent = inverted ? 100f - percent : percent;
        float t = targetPercent / 100f;
        float newZ = Mathf.Lerp(minZ, maxZ, t);

        transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
        Percent = percent;

        UpdateColor(percent);
        OnValueChanged?.Invoke(percent);
    }
}