using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void Start()
    {
        GameManager.Instance.SetState(GameState.MainMenu);
    }
    public void StartLab()
    {
        GameManager.Instance.SetState(GameState.Playing);
        SceneManager.LoadScene("Lab1");
    }

    public void ExitGame()
    {
        Debug.Log("Exit pressed!");
        Application.Quit();
    }
}
