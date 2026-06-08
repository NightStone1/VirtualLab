using UnityEngine;

[CreateAssetMenu(fileName = "NewCircuitElement", menuName = "Circuit/Element")]
public class CircuitElementData : ScriptableObject
{
    public string elementName;
    public string elementType;
    public GameObject prefab;
    public Sprite icon;
}