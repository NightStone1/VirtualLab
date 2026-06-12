using TMPro;
using UnityEngine;

public class Lab3RpmDisplayView : MonoBehaviour
{
    public Lab3_ElectricCircuit controller;
    public bool autoFindController = true;
    public TMP_Text targetText;
    public bool autoFindText = true;
    public bool showLlr = true;

    private void Awake()
    {
        if (controller == null && autoFindController)
            controller = FindFirstObjectByType<Lab3_ElectricCircuit>();

        if (targetText == null && autoFindText)
        {
            targetText = GetComponent<TMP_Text>();
            if (targetText == null)
                targetText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void Update()
    {
        if (targetText == null)
            return;

        if (controller == null)
        {
            targetText.text = "Нет данных";
            return;
        }

        float rpm = controller.RPMValue;
        float llr = controller.LLRValue;

        if (showLlr)
            targetText.text = $"n = {rpm:F0} об/мин\nLLR = {llr:F0}%";
        else
            targetText.text = $"n = {rpm:F0} об/мин";
    }
}
