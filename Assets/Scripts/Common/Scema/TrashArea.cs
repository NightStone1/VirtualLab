// Copyright (c) 2026 Бабичева Екатерина Анатольевна,
// Бибко Эдуард Александрович.
//
// Данный программный код разработан в рамках выпускной квалификационной работы
// "Виртуальный методический комплекс по дисциплине "Электрические машины"".
//
// Использование программного комплекса в учебном процессе АМТИ допускается
// в рамках подписанного акта о внедрении.
//
// Дальнейшее распространение, модификация, переработка, передача третьим лицам,
// публикация исходного кода, а также использование за пределами указанного
// внедрения допускаются только с письменного согласия авторов, если иное
// не предусмотрено отдельным соглашением.

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
        // Удаление обрабатывается в CircuitWorkAreaElement
        Debug.Log("Элемент удален через корзину");

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