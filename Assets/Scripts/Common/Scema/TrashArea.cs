using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrashArea : MonoBehaviour, IDropHandler
{
    [Header("Visual")]
    [SerializeField] private Color highlightColor = Color.red;
    [SerializeField] private float highlightAlpha = 0.3f;

    private Image backgroundImage;
    private Color originalColor;

    private void Start()
    {
        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
            originalColor = backgroundImage.color;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // ”даление обрабатываетс€ в CircuitWorkAreaElement
        Debug.Log("Ёлемент удален через корзину");

        if (backgroundImage != null)
            backgroundImage.color = originalColor;
    }

    private void OnDragDropHover(bool isHovering)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isHovering ?
                new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightAlpha) :
                originalColor;
        }
    }
}