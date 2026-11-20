using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public void ContinueGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void ConfirmNewGame()
    {
        ShiftPuzzleGame.ClearSavedRun();
        SceneManager.LoadScene("GameScene");
    }

    public void ShowHelp()
    {
        Debug.Log("Как играть: пока в разработке.");
    }

    public void ShowSettings()
    {
        Debug.Log("Настройки: пока в разработке.");
    }
}
