using UnityEngine;
using System.Collections.Generic;

public class CircuitUIPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform elementsContainer;
    [SerializeField] private GameObject draggableElementPrefab;

    [Header("Elements List")]
    [SerializeField] private List<CircuitElementData> availableElements;

    private List<DraggableCircuitUIElement> spawnedElements = new List<DraggableCircuitUIElement>();

    private void Start()
    {
        PopulatePanel();
    }

    public void PopulatePanel()
    {
        ClearPanel();

        foreach (var element in availableElements)
        {
            if (element == null) continue;

            GameObject elementObj = Instantiate(draggableElementPrefab, elementsContainer);
            DraggableCircuitUIElement uiElement = elementObj.GetComponent<DraggableCircuitUIElement>();

            if (uiElement != null)
            {
                uiElement.Initialize(element);
                spawnedElements.Add(uiElement);
            }
        }
    }

    private void ClearPanel()
    {
        foreach (var element in spawnedElements)
        {
            if (element != null)
                Destroy(element.gameObject);
        }
        spawnedElements.Clear();

        foreach (Transform child in elementsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void AddNewElement(CircuitElementData newElement)
    {
        if (!availableElements.Contains(newElement))
            availableElements.Add(newElement);
        PopulatePanel();
    }
}