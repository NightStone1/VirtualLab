using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance.State == GameState.Playing)
                Pause();
            else if (GameManager.Instance.State == GameState.Paused)
                Resume();
        }
    }

    public void Pause()
    {
        pausePanel.SetActive(true);
        GameManager.Instance.SetState(GameState.Paused);
    }

    public void Resume()
    {
        pausePanel.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        GameManager.Instance.SetState(GameState.MainMenu);
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
