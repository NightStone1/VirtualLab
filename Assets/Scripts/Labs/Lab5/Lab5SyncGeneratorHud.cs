using TMPro;
using UnityEngine;

public class Lab5SyncGeneratorHud : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI messageText;

    public void BindRuntimeFields(
        TextMeshProUGUI title,
        TextMeshProUGUI stage,
        TextMeshProUGUI instruction,
        TextMeshProUGUI state,
        TextMeshProUGUI points,
        TextMeshProUGUI message)
    {
        titleText = title;
        stageText = stage;
        instructionText = instruction;
        stateText = state;
        pointsText = points;
        messageText = message;

        DisableRaycasts(titleText);
        DisableRaycasts(stageText);
        DisableRaycasts(instructionText);
        DisableRaycasts(stateText);
        DisableRaycasts(pointsText);
        DisableRaycasts(messageText);
    }

    public void SetHudVisible(bool visible)
    {
        gameObject.SetActive(true);
        SetFieldVisible(titleText, visible);
        SetFieldVisible(stageText, visible);
        SetFieldVisible(instructionText, visible);
        SetFieldVisible(stateText, visible);
        SetFieldVisible(pointsText, visible);
        SetFieldVisible(messageText, visible);
    }

    private static void SetFieldVisible(TextMeshProUGUI target, bool visible)
    {
        if (target == null) return;

        target.gameObject.SetActive(visible && !string.IsNullOrEmpty(target.text));
        target.enabled = visible;
        target.raycastTarget = false;
    }

    private static void DisableRaycasts(TextMeshProUGUI target)
    {
        if (target != null)
            target.raycastTarget = false;
    }
}
