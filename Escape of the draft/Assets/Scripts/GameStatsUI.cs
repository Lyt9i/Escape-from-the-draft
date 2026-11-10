using TMPro;
using UnityEngine;

public class GameStatsUI : MonoBehaviour
{
    [SerializeField] private ShiftPuzzleGame game;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text sheetText;

    private int displayedScore = int.MinValue;
    private int displayedBestScore = int.MinValue;
    private int displayedSheet = int.MinValue;

    private void Start()
    {
        if (game == null || scoreText == null || bestScoreText == null)
        {
            Debug.LogError("GameStatsUI: назначь Game и оба текстовых поля в Inspector.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (displayedScore != game.Score)
        {
            displayedScore = game.Score;
            scoreText.text = displayedScore.ToString();
        }

        if (displayedBestScore != game.BestScore)
        {
            displayedBestScore = game.BestScore;
            bestScoreText.text = displayedBestScore.ToString();
        }

        if (sheetText != null && displayedSheet != game.CurrentSheet)
        {
            displayedSheet = game.CurrentSheet;
            sheetText.text = $"Лист {displayedSheet}";
        }
    }
}
