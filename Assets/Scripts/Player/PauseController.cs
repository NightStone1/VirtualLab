using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Pause.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Pause.performed -= OnPausePerformed;
        inputActions.Disable();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.State == GameState.Playing)
            Pause();
        else if (GameManager.Instance.State == GameState.Paused)
            Resume();
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