using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class DraggableCircuitUIElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;

    private CircuitElementData elementData;
    private Canvas mainCanvas;
    private CanvasGroup canvasGroup;
    private GameObject draggingClone;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null)
            Debug.LogError("Canvas не найден!");
    }

    public void Initialize(CircuitElementData data)
    {
        elementData = data;

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.elementName;

        if (typeText != null)
            typeText.text = data.elementType;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (elementData == null) return;

        Debug.Log($"Начат drag элемента: {elementData.elementName}");

        // Сохраняем данные в статический класс
        DragData.CurrentDraggedElement = elementData;

        // Создаем визуальный клон для перетаскивания
        if (draggingClone == null)
        {
            draggingClone = Instantiate(gameObject, mainCanvas.transform);
            draggingClone.transform.SetAsLastSibling();

            // Настраиваем клон
            RectTransform cloneRect = draggingClone.GetComponent<RectTransform>();
            cloneRect.sizeDelta = rectTransform.sizeDelta;
            cloneRect.position = eventData.position;

            CanvasGroup cloneGroup = draggingClone.GetComponent<CanvasGroup>();
            if (cloneGroup == null)
                cloneGroup = draggingClone.AddComponent<CanvasGroup>();
            cloneGroup.blocksRaycasts = false;
            cloneGroup.alpha = 0.8f;
        }

        // Скрываем оригинал во время перетаскивания
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingClone != null)
        {
            draggingClone.GetComponent<RectTransform>().position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"Закончен drag элемента: {elementData?.elementName}");

        // Восстанавливаем оригинал
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Уничтожаем клон
        if (draggingClone != null)
        {
            Destroy(draggingClone);
            draggingClone = null;
        }

        // НЕ сбрасываем CurrentDraggedElement сразу!
        // WorkArea должен успеть его прочитать в OnDrop
        // Сбрасываем через кадр
        StartCoroutine(ClearDragData());
    }

    private System.Collections.IEnumerator ClearDragData()
    {
        yield return new WaitForEndOfFrame();
        DragData.CurrentDraggedElement = null;
    }

    public CircuitElementData GetElementData()
    {
        return elementData;
    }
}

public static class DragData
{
    public static CircuitElementData CurrentDraggedElement;
}