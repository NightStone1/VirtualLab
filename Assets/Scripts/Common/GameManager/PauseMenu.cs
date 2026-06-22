// Copyright (c) 2026 Бабичева Екатерина Анатольевна,
// Бибко Эдуард Александрович.
//
// Данный программный код разработан в рамках выпускной квалификационной работы
// "Виртуальный методический комплекс по дисциплине "Электрические машины"".
//
// Использование программного комплекса в учебном процессе АМТИ допускается
// в рамках подписанного акта о внедрении.
//
// Дальнейшее распространение, модификация, переработка, передача третьим лицам,
// публикация исходного кода, а также использование за пределами указанного
// внедрения допускаются только с письменного согласия авторов, если иное
// не предусмотрено отдельным соглашением.

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
