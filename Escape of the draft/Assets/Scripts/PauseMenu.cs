using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private ShiftPuzzleGame game;
    [SerializeField] private GameObject pausePanel;

    private bool paused;
    private float previousTimeScale;

    private void Awake()
    {
        if (game == null || pausePanel == null ||
            pausePanel == gameObject || transform.IsChildOf(pausePanel.transform))
        {
            Debug.LogError("PauseMenu: назначь Game и Pause; компонент должен быть вне окна Pause.", this);
            enabled = false;
            return;
        }

        pausePanel.SetActive(false);
    }

    public void ReturnToMenu()
    {
        Resume();
        SceneManager.LoadScene("StartScene");
    }

    public void OpenPause()
    {
        if (!enabled || paused)
            return;

        paused = true;
        previousTimeScale = Time.timeScale;
        game.SetPaused(true);
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (!paused)
            return;

        pausePanel.SetActive(false);
        game.SetPaused(false);
        Time.timeScale = previousTimeScale;
        paused = false;
    }

    public void Restart()
    {
        if (!paused)
            return;

        game.StartNewGame();
        Resume();
    }

    private void OnDisable()
    {
        if (paused)
            Resume();
    }
}
