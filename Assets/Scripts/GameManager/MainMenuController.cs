using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private GameObject labsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Labs UI")]
    [SerializeField] private Transform labsContainer;
    [SerializeField] private LabButtonUI labButtonPrefab;

    private List<LabData> labs = new List<LabData>();

    private void Start()
    {
        if (labButtonPrefab == null)
        {
            Debug.LogError("Lab Button Prefab is not assigned!");
            return;
        }

        if (labsContainer == null)
        {
            Debug.LogError("Labs Container is not assigned!");
            return;
        }

        GameManager.Instance.SetState(GameState.MainMenu);

        LoadLabs();
        ShowMainMenu();
        GenerateLabButtons();
    }

    private void LoadLabs()
    {
        labs = Resources.LoadAll<LabData>("Labs")
            .Where(lab => lab != null)
            .OrderBy(lab => lab.order)
            .ToList();

        if (labs.Count == 0)
        {
            Debug.LogWarning("No LabData found in Resources/Labs!");
        }

        foreach (LabData lab in labs)
        {
            if (string.IsNullOrWhiteSpace(lab.sceneName))
            {
                Debug.LogWarning($"LabData '{lab.name}' has empty sceneName.");
            }
        }
    }


    private void GenerateLabButtons()
    {

        foreach (LabData lab in labs)
        {
            if (lab == null)
                continue;

            string sceneName = lab.sceneName;
            string title = lab.title;
            string theme = lab.theme;

            LabButtonUI labButton = Instantiate(labButtonPrefab, labsContainer);
            labButton.Init(title, theme, () => StartLab(sceneName));
        }
    }

    public void ShowMainMenu()
    {
        mainButtonsPanel.SetActive(true);
        labsPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void OpenLabs()
    {
        mainButtonsPanel.SetActive(false);
        labsPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        mainButtonsPanel.SetActive(false);
        labsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void StartLab(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Scene name is empty!");
            return;
        }

        GameManager.Instance.SetState(GameState.Playing);
        SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exit pressed!");
        Application.Quit();
    }
}
