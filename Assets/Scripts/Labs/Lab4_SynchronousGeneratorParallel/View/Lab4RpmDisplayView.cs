using TMPro;
using UnityEngine;

public class Lab4RpmDisplayView : MonoBehaviour
{
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;
    public TMP_Text targetText;
    public bool autoFindText = true;
    public string title = "ОБОРОТЫ";
    public string unit = "об/мин";
    public string stoppedText = "0 об/мин";
    public int decimals = 0;
    public bool showFrequency = true;
    public bool clearWhenNoController = false;

    private void Awake()
    {
        if (controller == null && autoFindController)
        {
            controller = FindFirstObjectByType<SyncGeneratorLabController>();
        }

        if (targetText == null && autoFindText)
        {
            targetText = GetComponent<TMP_Text>();
            if (targetText == null)
            {
                targetText = GetComponentInChildren<TMP_Text>(true);
            }
        }
    }

    private void Update()
    {
        if (targetText == null)
        {
            return;
        }

        if (controller == null)
        {
            targetText.text = clearWhenNoController ? string.Empty : "Нет данных";
            return;
        }

        float rpm = controller.IsQ3Enabled ? controller.RotorSpeedRpm : 0f;
        float frequency = controller.IsQ3Enabled ? controller.GeneratorFrequency : 0f;

        if (controller.IsConnectedToGrid)
        {
            frequency = controller.GridFrequency;
            rpm = frequency * 60f;
        }

        string rpmText = rpm <= 0.01f
            ? stoppedText
            : string.Format("{0:F" + Mathf.Max(0, decimals) + "} {1}", rpm, unit);

        if (showFrequency)
        {
            targetText.text = string.Format("{0}\n{1}\nf = {2:0.0} Гц", title, rpmText, frequency);
            return;
        }

        targetText.text = string.Format("{0}\n{1}", title, rpmText);
    }
}
