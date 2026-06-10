using UnityEngine;

public class Lab6Q2PositionInteractable : MonoBehaviour
{
    [SerializeField] private Lab6Controller controller;
    [SerializeField] private int position;

    private void OnValidate()
    {
        position = Mathf.Clamp(position, 0, 7);
    }

    private void OnMouseDown()
    {
        if (controller == null)
        {
            Debug.LogWarning($"Lab6Q2PositionInteractable {name}: controller is not assigned.");
            return;
        }

        controller.SetQ2Position(Mathf.Clamp(position, 0, 7));
    }
}
