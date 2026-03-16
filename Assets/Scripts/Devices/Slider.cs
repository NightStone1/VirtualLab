using UnityEngine;

public class Slider : MonoBehaviour
{
    public float minY;
    public float maxY;
    public bool inverted = false; // если true Ч шкала идЄт 100 -> 0

    public event System.Action<float> OnValueChanged;

    private Camera cam;
    private bool isDragging = false;
    private float offsetY;

    public float Percent { get; private set; }

    void Start()
    {
        maxY = transform.position.y;
        minY = transform.position.y - (0.13f -(-0.0775f));
        cam = Camera.main;
    }

    void OnMouseDown()
    {
        isDragging = true;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.WorldToScreenPoint(transform.position).z)
        );

        offsetY = transform.position.y - mouseWorldPos.y;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.WorldToScreenPoint(transform.position).z)
        );

        float newY = mouseWorldPos.y + offsetY;
        newY = Mathf.Clamp(newY, minY, maxY);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // вычисл€ем процент (0Ц100)
        Percent = Mathf.InverseLerp(minY, maxY, newY) * 100f;

        if (inverted)
            Percent = 100f - Percent;

        // уведомл€ем подписчиков об изменении
        OnValueChanged?.Invoke(Percent);

        Debug.Log(gameObject.name + " = " + Mathf.RoundToInt(Percent));
    }

    void OnMouseUp()
    {
        isDragging = false;
    }
}
