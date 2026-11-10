using TMPro;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private ShiftPuzzleGame game;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text restartButtonText;

    private bool paused;
    private bool confirmingRestart;
    private float previousTimeScale;
    private string restartLabel;

    private void Awake()
    {
        if (game == null || pausePanel == null || restartButtonText == null ||
            pausePanel == gameObject || transform.IsChildOf(pausePanel.transform))
        {
            Debug.LogError("PauseMenu: назначь Game, Pause и текст кнопки перезапуска; компонент должен быть вне окна Pause.", this);
            enabled = false;
            return;
        }

        restartLabel = restartButtonText.text;
        pausePanel.SetActive(false);
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

        confirmingRestart = false;
        restartButtonText.text = restartLabel;
        pausePanel.SetActive(false);
        game.SetPaused(false);
        Time.timeScale = previousTimeScale;
        paused = false;
    }

    public void Restart()
    {
        if (!paused)
            return;

        if (!confirmingRestart)
        {
            confirmingRestart = true;
            restartButtonText.text = "Точно начать заново?";
            return;
        }

        game.StartNewGame();
        Resume();
    }

    private void OnDisable()
    {
        if (paused)
            Resume();
    }
}
