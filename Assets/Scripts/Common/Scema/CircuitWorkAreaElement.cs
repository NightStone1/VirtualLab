using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CircuitWorkAreaElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("References")]
    private CircuitElementData elementData;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform workAreaRect;

    [Header("Settings")]
    [SerializeField] private bool isDraggable = true;
    [SerializeField] private bool canBeDeleted = true;
    [SerializeField] private float snapDistance = 20f; // Расстояние для магнитной привязки

    private Vector2 originalPosition;
    private bool isDragging = false;
    private Vector2 dragOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        FindWorkArea();
    }

    private void FindWorkArea()
    {
        // Находим WorkArea для ограничения движения
        WorkArea workArea = GetComponentInParent<WorkArea>();
        if (workArea != null)
        {
            workAreaRect = workArea.GetComponent<RectTransform>();
        }
        else
        {
            // Если не нашли, ищем по имени
            GameObject workAreaObj = GameObject.Find("WorkArea");
            if (workAreaObj != null)
                workAreaRect = workAreaObj.GetComponent<RectTransform>();
        }
    }

    public void Initialize(CircuitElementData data)
    {
        elementData = data;
        gameObject.name = data.elementName;

        // Настраиваем визуал
        Image img = GetComponent<Image>();
        if (img != null && data.icon != null)
        {
            img.sprite = data.icon;
        }
        else if (img != null)
        {
            // Случайный цвет если нет иконки
            img.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f);
        }

        // Добавляем тень для эффекта (опционально)
        Shadow shadow = GetComponent<Shadow>();
        if (shadow == null)
            shadow = gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(2, -2);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        isDragging = true;
        originalPosition = rectTransform.anchoredPosition;

        // Вычисляем смещение от точки клика до центра элемента
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        dragOffset = rectTransform.anchoredPosition - localPoint;

        // Делаем полупрозрачным при перетаскивании
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

        // Поднимаем элемент над другими
        transform.SetAsLastSibling();

        Debug.Log($"Начало перемещения: {gameObject.name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable || !isDragging) return;

        // Получаем новую позицию
        Vector2 newPosition = GetConstrainedPosition(eventData);

        // Применяем новую позицию
        rectTransform.anchoredPosition = newPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        isDragging = false;

        // Восстанавливаем прозрачность
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Проверяем, не бросили ли элемент в корзину
        if (canBeDeleted && eventData.pointerEnter != null)
        {
            if (eventData.pointerEnter.CompareTag("TrashArea") ||
                eventData.pointerEnter.GetComponent<TrashArea>() != null)
            {
                Destroy(gameObject);
                Debug.Log($"Удален {gameObject.name}");
                return;
            }
        }

        // Магнитная привязка к сетке (опционально)
        SnapToGrid();

        // Проверяем границы после перемещения
        ClampToWorkAreaBounds();

        Debug.Log($"Конец перемещения: {gameObject.name} на позиции {rectTransform.anchoredPosition}");
    }

    private Vector2 GetConstrainedPosition(PointerEventData eventData)
    {
        // Получаем позицию курсора в локальных координатах родителя
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        // Добавляем смещение
        Vector2 newPosition = localPoint + dragOffset;

        // Ограничиваем границами WorkArea
        if (workAreaRect != null)
        {
            newPosition = ClampToBounds(newPosition);
        }

        return newPosition;
    }

    private Vector2 ClampToBounds(Vector2 position)
    {
        // Получаем размеры элемента
        Vector2 elementSize = rectTransform.sizeDelta;

        // Получаем размеры WorkArea
        Vector2 workAreaSize = workAreaRect.rect.size;

        // Вычисляем границы (с учетом, что центр элемента - точка привязки)
        float minX = -workAreaSize.x / 2 + elementSize.x / 2;
        float maxX = workAreaSize.x / 2 - elementSize.x / 2;
        float minY = -workAreaSize.y / 2 + elementSize.y / 2;
        float maxY = workAreaSize.y / 2 - elementSize.y / 2;

        // Применяем ограничения
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    private void ClampToWorkAreaBounds()
    {
        if (workAreaRect != null)
        {
            Vector2 currentPos = rectTransform.anchoredPosition;
            Vector2 clampedPos = ClampToBounds(currentPos);

            if (clampedPos != currentPos)
            {
                rectTransform.anchoredPosition = clampedPos;
                Debug.Log($"Элемент {gameObject.name} скорректирован по границам");
            }
        }
    }

    private void SnapToGrid()
    {
        WorkArea workArea = GetComponentInParent<WorkArea>();
        if (workArea != null && workArea.snapToGrid)
        {
            Vector2 currentPos = rectTransform.anchoredPosition;
            float gridSize = workArea.gridSize;

            currentPos.x = Mathf.Round(currentPos.x / gridSize) * gridSize;
            currentPos.y = Mathf.Round(currentPos.y / gridSize) * gridSize;

            rectTransform.anchoredPosition = currentPos;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {

    }

    // Публичный метод для получения данных элемента
    public CircuitElementData GetElementData()
    {
        return elementData;
    }

    // Публичный метод для программного перемещения
    public void MoveToPosition(Vector2 newPosition)
    {
        rectTransform.anchoredPosition = newPosition;
        ClampToWorkAreaBounds();
    }
}