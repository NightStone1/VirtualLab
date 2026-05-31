using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI rpmText;
    [SerializeField] private LabResultsManager labResultsManager;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (labResultsManager == null)
            labResultsManager = FindObjectOfType<LabResultsManager>();

        UpdateRPMDisplay(0f);
    }

    public void UpdateRPMDisplay(float rpm)
    {
        if (rpmText != null)
        {
            rpmText.text = $"Обороты: {rpm:F1} об/мин";
        }
    }

    // Вызовите этот метод по кнопке или таймеру
    public void RefreshRPMFromCurrentMode()
    {
        if (labResultsManager != null)
        {
     
        }
    }
}