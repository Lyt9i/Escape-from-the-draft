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
        PlayerPrefs.DeleteKey("draft-shift-score");
        PlayerPrefs.DeleteKey("draft-shift-routes");
        PlayerPrefs.DeleteKey("draft-shift-stars");
        PlayerPrefs.Save();
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
