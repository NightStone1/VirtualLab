using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Mouse Sensitivity")]
    public float sensX = 200f;
    public float sensY = 200f;

    private const string SensKey = "MouseSensitivity";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void LoadSettings()
    {
        float savedSens = PlayerPrefs.GetFloat(SensKey, 200f);
        sensX = savedSens;
        sensY = savedSens;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(SensKey, sensX);
        PlayerPrefs.Save();
    }
}
