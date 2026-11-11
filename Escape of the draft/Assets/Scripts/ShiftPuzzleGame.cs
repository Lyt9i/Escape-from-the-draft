using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShiftPuzzleGame : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Transform hero;
    [SerializeField] private Transform door;
    [SerializeField] private Transform star;
    [SerializeField] private RouteRenderer routeRenderer;
    [SerializeField] private BookPageTransitionTest testPageTransition;
    [SerializeField] private GameAudio gameAudio;

    [Header("Board")]
    [SerializeField] private int columns = 6;
    [SerializeField] private int rows = 6;
    [SerializeField] private float heroStepDuration = 0.13f;

    public bool CanAcceptInput => !running && !completed && !paused && !changingPage;
    public int Score => score;
    public int BestScore => bestScore;
    public int CurrentSheet => sheet + 1;
    public int CompletedRoutes => completedRoutes;
    public int CollectedStars => collectedStars;
    public int Moves => board == null ? 0 : board.Moves;
    public int Par => board == null ? 0 : board.Par;

    private const string ScoreKey = "draft-shift-score";
    private const string BestScoreKey = "draft-shift-best";
    private const string RoutesKey = "draft-shift-routes";
    private const string StarsKey = "draft-shift-stars";

    private ShiftPuzzleBoard board;
    private Camera mainCamera;
    private ShiftHintRenderer hintRenderer;

    private int sheet;
    private int score;
    private int bestScore;
    private int completedRoutes;
    private int collectedStars;

    private bool running;
    private bool completed;
    private bool paused;
    private bool changingPage;

    public void SetPaused(bool value)
    {
        paused = value;
        if (gameAudio != null)
            gameAudio.SetPaused(value);
    }

    private void Awake()
    {
        if (!IsConfigured())
        {
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        board = new ShiftPuzzleBoard(columns, rows);
        hintRenderer = gameObject.AddComponent<ShiftHintRenderer>();
        hintRenderer.Show(routeRenderer.GridBounds, columns, rows, columns / 2, rows / 2);

        score = PlayerPrefs.GetInt(ScoreKey);
        bestScore = PlayerPrefs.GetInt(BestScoreKey);
        completedRoutes = PlayerPrefs.GetInt(RoutesKey);
        sheet = completedRoutes;
        collectedStars = PlayerPrefs.GetInt(StarsKey);

        GeneratePuzzle();
    }

    public void UndoShift()
    {
        if (!CanAcceptInput)
            return;

        board.Undo();
        routeRenderer.Redraw(board);
    }

    public void ResetPuzzle()
    {
        if (!CanAcceptInput)
            return;

        board.Reset();
        routeRenderer.Redraw(board);
    }

    public void NextPuzzle()
    {
        if (!completed || changingPage)
            return;

        if (testPageTransition != null && testPageTransition.IsConfigured)
            StartCoroutine(ChangePage());
        else
            GeneratePuzzle();
    }

    private IEnumerator ChangePage()
    {
        changingPage = true;
        yield return testPageTransition.Play(() =>
        {
            routeRenderer.gameObject.SetActive(false);
            GeneratePuzzle(false);
        }, () => { if (gameAudio != null) gameAudio.PlayPaperTurn(); });
        routeRenderer.gameObject.SetActive(true);
        routeRenderer.Redraw(board, true);
        changingPage = false;
    }

    public void StartNewGame()
    {
        StopAllCoroutines();
        if (gameAudio != null)
            gameAudio.StopAll();
        if (testPageTransition != null)
            testPageTransition.Cancel();
        routeRenderer.gameObject.SetActive(true);
        changingPage = false;
        score = 0;
        completedRoutes = 0;
        collectedStars = 0;
        sheet = 0;

        SaveStats();
        GeneratePuzzle();
    }

    public void TryShiftFromSwipe(Vector2 startScreen, Vector2 endScreen)
    {
        if (!CanAcceptInput)
            return;

        if (!TryGetCell(startScreen, out int column, out int row))
            return;

        Vector2 swipe = endScreen - startScreen;
        bool shiftRow = Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y);
        int lineIndex = shiftRow ? row : column;
        int direction = shiftRow
            ? (swipe.x > 0f ? 1 : -1)
            : (swipe.y > 0f ? 1 : -1);

        if (!board.TryShift(shiftRow, lineIndex, direction))
            return;

        if (gameAudio != null)
            gameAudio.PlayPaperMove();
        routeRenderer.Redraw(board);

        List<int> path = board.FindPath();

        if (path != null)
            StartCoroutine(RunHero(path));
    }

    public void SelectCell(Vector2 screenPosition)
    {
        if (CanAcceptInput && TryGetCell(screenPosition, out int column, out int row))
            hintRenderer.Show(routeRenderer.GridBounds, columns, rows, column, row);
    }

    private bool TryGetCell(Vector2 screenPosition, out int column, out int row)
    {
        column = 0;
        row = 0;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                Mathf.Abs(
                    routeRenderer.GridBounds.center.z -
                    mainCamera.transform.position.z
                )
            )
        );

        Bounds bounds = routeRenderer.GridBounds;

        if (worldPosition.x < bounds.min.x || worldPosition.x > bounds.max.x ||
            worldPosition.y < bounds.min.y || worldPosition.y > bounds.max.y)
        {
            return false;
        }

        column = Mathf.Clamp(
            Mathf.FloorToInt(
                (worldPosition.x - bounds.min.x) /
                bounds.size.x *
                columns
            ),
            0,
            columns - 1
        );

        row = Mathf.Clamp(
            Mathf.FloorToInt(
                (worldPosition.y - bounds.min.y) /
                bounds.size.y *
                rows
            ),
            0,
            rows - 1
        );

        return true;
    }

    private void GeneratePuzzle(bool animateRoute = true)
    {
        running = false;
        completed = false;

        board.Generate(sheet);

        Place(hero, board.PlayerCell);
        Place(door, board.DoorCell);
        Place(star, board.StarCell);

        star.gameObject.SetActive(true);

        routeRenderer.Redraw(board, animateRoute);
    }

    private IEnumerator RunHero(List<int> path)
    {
        running = true;
        bool gotStar = false;
        if (gameAudio != null)
            gameAudio.StartSteps();

        foreach (int cell in path)
        {
            Vector3 start = hero.position;
            Vector3 target = routeRenderer.CellToWorld(board, cell);
            target.z = hero.position.z;

            float time = 0f;

            while (time < heroStepDuration)
            {
                time += Time.deltaTime;

                hero.position = Vector3.Lerp(
                    start,
                    target,
                    time / heroStepDuration
                );

                yield return null;
            }

            hero.position = target;

            if (cell == board.StarCell)
            {
                gotStar = true;
                star.gameObject.SetActive(false);
                if (gameAudio != null)
                    gameAudio.PlayStar();
            }
        }

        if (gameAudio != null)
        {
            gameAudio.StopSteps();
            gameAudio.PlayDoor();
        }

        int reward = Mathf.Max(
            20,
            160 - 10 * Mathf.Max(0, board.Moves - board.Par)
        );

        if (gotStar)
        {
            reward += 60;
            collectedStars++;
        }

        score += reward;
        completedRoutes++;
        bestScore = Mathf.Max(bestScore, score);

        sheet++;
        completed = true;
        running = false;

        SaveStats();

        Debug.Log(
            $"Маршрут собран. Награда: {reward}. " +
            $"Очки: {score}. Следующий лист."
        );

        NextPuzzle();
    }

    private void Place(Transform target, int cell)
    {
        Vector3 position = routeRenderer.CellToWorld(board, cell);
        position.z = target.position.z;
        target.position = position;
    }

    private bool IsConfigured()
    {
        bool configured =
            hero != null &&
            door != null &&
            star != null &&
            routeRenderer != null &&
            routeRenderer.IsConfigured &&
            columns >= 3 &&
            rows >= 3;

        if (!configured)
            Debug.LogError("ShiftPuzzleGame: назначь все ссылки в Inspector.");

        return configured;
    }

    private void SaveStats()
    {
        PlayerPrefs.SetInt(ScoreKey, score);
        PlayerPrefs.SetInt(BestScoreKey, bestScore);
        PlayerPrefs.SetInt(RoutesKey, completedRoutes);
        PlayerPrefs.SetInt(StarsKey, collectedStars);
        PlayerPrefs.Save();
    }
}
