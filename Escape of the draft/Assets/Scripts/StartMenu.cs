using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    private const string VolumeKey = "MasterVolume";

    [SerializeField] private GameObject header;
    [SerializeField] private GameObject menuButtons;
    [SerializeField] private GameObject helpPage;
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeLabel;
    [SerializeField] private AudioSource menuSound;
    [SerializeField] private AudioClip previewSound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RestoreVolume()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.7f));
    }

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
        SetPage(helpPage);
    }

    public void ShowSettings()
    {
        volumeSlider.SetValueWithoutNotify(Mathf.RoundToInt(AudioListener.volume * 100f));
        UpdateVolumeLabel(volumeSlider.value);
        SetPage(settingsPage);
    }

    public void SetVolume(float percent)
    {
        AudioListener.volume = percent / 100f;
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        UpdateVolumeLabel(percent);
    }

    public void PreviewVolume()
    {
        menuSound.PlayOneShot(previewSound);
    }

    public void ClosePage()
    {
        if (settingsPage.activeSelf)
            PlayerPrefs.Save();
        SetPage(null);
    }

    private void UpdateVolumeLabel(float percent)
    {
        volumeLabel.text = $"{Mathf.RoundToInt(percent)} %";
    }

    private void SetPage(GameObject page)
    {
        header.SetActive(page == null);
        menuButtons.SetActive(page == null);
        helpPage.SetActive(page == helpPage);
        settingsPage.SetActive(page == settingsPage);
        menuSound.Play();
    }
}
