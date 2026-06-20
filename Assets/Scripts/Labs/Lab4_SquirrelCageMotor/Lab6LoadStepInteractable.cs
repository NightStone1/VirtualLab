using UnityEngine;

public class Lab6LoadStepInteractable : MonoBehaviour
{
    [SerializeField] private Lab6Controller controller;
    [SerializeField] private int step;
    [SerializeField] private bool enableDebugLogs;

    private void OnValidate()
    {
        step = Mathf.Clamp(step, 0, 4);
    }

    private void OnMouseDown()
    {
        int clampedStep = Mathf.Clamp(step, 0, 4);
        if (enableDebugLogs)
        {
            Debug.Log($"Lab6 R-block click: object={name}, step={clampedStep}");
        }

        if (controller == null)
        {
            controller = FindAnyLab6Controller();
        }

        if (controller == null)
        {
            Debug.LogWarning($"Lab6LoadStepInteractable: controller is not assigned on {name}");
            return;
        }

        controller.ToggleLoadStep(clampedStep);
    }

    private static Lab6Controller FindAnyLab6Controller()
    {
        Lab6Controller[] controllers = FindObjectsByType<Lab6Controller>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return controllers.Length > 0 ? controllers[0] : null;
    }
}
