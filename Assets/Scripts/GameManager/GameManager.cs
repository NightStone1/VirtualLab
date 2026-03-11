using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetState(GameState newState)
    {
        State = newState;

        switch (State)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                break;

            case GameState.Playing:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Time.timeScale = 1f;
                break;

            case GameState.Paused:
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                Time.timeScale = 0f;
                break;
        }
    }
}

