using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorkArea : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    [SerializeField] private Transform elementsParent;
    [SerializeField] public float gridSize = 50f;
    [SerializeField] public bool snapToGrid = true;
    [SerializeField] private bool showGrid = true;

    [Header("Visual Feedback")]
    [SerializeField] private Color highlightColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private GameObject gridPrefab; // Префаб для отображения сетки

    private Image backgroundImage;
    private Color originalColor;
    private RectTransform workAreaRect;
    private GameObject gridObject;

    public RectTransform WorkAreaRect => workAreaRect;

    private void Awake()
    {
        workAreaRect = GetComponent<RectTransform>();
        backgroundImage = GetComponent<Image>();

        if (backgroundImage != null)
            originalColor = backgroundImage.color;

        // Создаем контейнер для элементов если его нет
        if (elementsParent == null)
        {
            GameObject parentObj = new GameObject("PlacedElements");
            parentObj.transform.SetParent(transform);
            parentObj.transform.SetAsLastSibling();
            elementsParent = parentObj.transform;

            RectTransform rect = elementsParent as RectTransform;
            if (rect == null)
                rect = elementsParent.gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Создаем визуальную сетку
        if (showGrid)
            CreateGrid();
    }

    private void CreateGrid()
    {
        if (gridPrefab != null)
        {
            gridObject = Instantiate(gridPrefab, transform);
            gridObject.transform.SetAsFirstSibling();
        }
        else
        {
            // Создаем простую сетку через LineRenderer (опционально)
            DrawGridLines();
        }
    }

    private void DrawGridLines()
    {
        // Простая визуальная сетка (можно улучшить)
        GameObject gridLines = new GameObject("GridLines");
        gridLines.transform.SetParent(transform);
        gridLines.transform.SetAsFirstSibling();

        // Здесь можно добавить LineRenderer для отрисовки сетки
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DragData.CurrentDraggedElement != null)
        {
            if (backgroundImage != null)
                backgroundImage.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null)
            backgroundImage.color = originalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Элемент брошен на WorkArea");

        CircuitElementData element = DragData.CurrentDraggedElement;

        if (element == null)
        {
            Debug.LogWarning("DragData.CurrentDraggedElement == null");
            return;
        }

        // Получаем позицию для спавна
        Vector2 spawnPosition = GetSpawnPosition(eventData);

        // Создаем элемент
        GameObject newElement = CreateElement(element, spawnPosition);

        if (newElement != null)
        {
            Debug.Log($"Создан {element.elementName} на позиции {spawnPosition}");
        }

        // Убираем подсветку
        if (backgroundImage != null)
            backgroundImage.color = originalColor;
    }

    private GameObject CreateElement(CircuitElementData element, Vector2 position)
    {
        GameObject elementObj;

        if (element.prefab != null)
        {
            elementObj = Instantiate(element.prefab, elementsParent);
        }
        else
        {
            // Создаем базовый визуальный элемент
            elementObj = new GameObject(element.elementName);
            elementObj.transform.SetParent(elementsParent);

            // Добавляем Image
            Image img = elementObj.AddComponent<Image>();
            if (element.icon != null)
                img.sprite = element.icon;
            else
                img.color = new Color(0.3f, 0.6f, 0.9f, 1f);

            // Устанавливаем размер
            RectTransform rect = elementObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 80);
        }

        // Настраиваем позицию
        RectTransform elementRect = elementObj.GetComponent<RectTransform>();
        elementRect.anchoredPosition = position;
        elementRect.localScale = Vector3.one;

        // Добавляем компонент перемещения
        CircuitWorkAreaElement workElement = elementObj.GetComponent<CircuitWorkAreaElement>();
        if (workElement == null)
            workElement = elementObj.AddComponent<CircuitWorkAreaElement>();

        workElement.Initialize(element);

        // Добавляем BoxCollider2D для лучшего определения кликов
        BoxCollider2D collider = elementObj.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = elementObj.AddComponent<BoxCollider2D>();
            collider.size = elementRect.sizeDelta;
        }

        return elementObj;
    }

    private Vector2 GetSpawnPosition(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            workAreaRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        if (snapToGrid)
        {
            localPoint.x = Mathf.Round(localPoint.x / gridSize) * gridSize;
            localPoint.y = Mathf.Round(localPoint.y / gridSize) * gridSize;
        }

        // Проверяем, не занято ли место (опционально)
        if (IsPositionOccupied(localPoint))
        {
            // Ищем свободное место рядом
            localPoint = FindFreePosition(localPoint);
        }

        return localPoint;
    }

    private bool IsPositionOccupied(Vector2 position)
    {
        // Проверяем, есть ли уже элемент на этой позиции
        foreach (Transform child in elementsParent)
        {
            if (Vector2.Distance(child.GetComponent<RectTransform>().anchoredPosition, position) < 50f)
                return true;
        }
        return false;
    }

    private Vector2 FindFreePosition(Vector2 originalPos)
    {
        Vector2 newPos = originalPos;
        float offset = gridSize;

        for (int i = 0; i < 10; i++)
        {
            // Проверяем соседние позиции по спирали
            Vector2[] offsets = new Vector2[]
            {
                new Vector2(offset, 0),
                new Vector2(-offset, 0),
                new Vector2(0, offset),
                new Vector2(0, -offset),
                new Vector2(offset, offset),
                new Vector2(-offset, offset),
                new Vector2(offset, -offset),
                new Vector2(-offset, -offset)
            };

            foreach (var off in offsets)
            {
                Vector2 testPos = originalPos + off;
                if (!IsPositionOccupied(testPos))
                    return testPos;
            }

            offset += gridSize;
        }

        return originalPos;
    }

    // Метод для получения всех элементов на WorkArea
    public CircuitWorkAreaElement[] GetAllElements()
    {
        return elementsParent.GetComponentsInChildren<CircuitWorkAreaElement>();
    }

    // Метод для очистки всей рабочей области
    public void ClearAllElements()
    {
        foreach (Transform child in elementsParent)
        {
            Destroy(child.gameObject);
        }
    }
}