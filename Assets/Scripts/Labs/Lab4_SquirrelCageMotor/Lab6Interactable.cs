using UnityEngine;

public class Lab6Interactable : MonoBehaviour
{
    [SerializeField] private Lab6SwitchId switchId;
    [SerializeField] private Lab6Controller controller;

    private void OnMouseDown()
    {
        if (controller == null)
        {
            Debug.LogWarning($"Lab6Interactable {name}: controller is not assigned.");
            return;
        }

        switch (switchId)
        {
            case Lab6SwitchId.Q1:
                controller.ToggleQ1();
                break;
            case Lab6SwitchId.Q2Up:
                controller.IncreaseQ2();
                break;
            case Lab6SwitchId.Q2Down:
                controller.DecreaseQ2();
                break;
            case Lab6SwitchId.Q3:
                controller.ToggleQ3();
                break;
            case Lab6SwitchId.Q4:
                controller.ToggleQ4();
                break;
            case Lab6SwitchId.Q5:
                controller.ToggleQ5();
                break;
            case Lab6SwitchId.Q6:
                controller.ToggleQ6();
                break;
            case Lab6SwitchId.Brake:
                controller.ToggleBrake();
                break;
            case Lab6SwitchId.LoadUp:
                controller.ChangeLoadPercent(10f);
                break;
            case Lab6SwitchId.LoadDown:
                controller.ChangeLoadPercent(-10f);
                break;
            case Lab6SwitchId.RecordPoint:
                controller.RecordPoint();
                break;
            case Lab6SwitchId.NextStage:
                controller.NextStage();
                break;
            case Lab6SwitchId.EmergencyStop:
                controller.EmergencyStop();
                break;
        }
    }
}
