using System;
using System.Collections;
using UnityEngine;

public class BookPageTransitionTest : MonoBehaviour
{
    [SerializeField] private Book book;
    [SerializeField] private AutoFlip autoFlip;
    [SerializeField] private Sprite paperSprite;

    private bool finished;
    private Texture2D screenshot;
    private Sprite screenshotSprite;
    private Sprite originalPage;
    private Texture2D nextScreenshot;
    private Sprite nextScreenshotSprite;
    private Sprite originalBackground;
    private Sprite originalNextPage;
    private Canvas bookCanvas;
    private RectTransform bookRect;
    private RectTransform bookParent;
    private Vector2 lastCanvasSize;

    public bool IsConfigured => book != null && autoFlip != null &&
                                autoFlip.gameObject == book.gameObject &&
                                book.bookPages != null && book.bookPages.Length >= 2;

    private void Start()
    {
        if (!IsConfigured)
        {
            Debug.LogWarning("BookPageTransitionTest: назначь Book и AutoFlip с минимум двумя страницами.", this);
            return;
        }

        autoFlip.AutoStartFlip = false;
        book.interactable = false;
        bookCanvas = book.GetComponentInParent<Canvas>(true);
        bookRect = book.transform as RectTransform;
        bookParent = book.transform.parent as RectTransform;
        if (paperSprite == null)
        {
            BackgroundFit background = FindObjectOfType<BackgroundFit>();
            if (background != null && background.TryGetComponent(out SpriteRenderer backgroundRenderer))
                paperSprite = backgroundRenderer.sprite;
        }

        if (paperSprite != null)
        {
            book.background = paperSprite;
            for (int i = 0; i < book.bookPages.Length; i++)
                book.bookPages[i] = paperSprite;
        }
        else
        {
            Debug.LogWarning("BookPageTransitionTest: назначь спрайт тетрадного листа в Paper Sprite.", this);
        }
        book.gameObject.SetActive(false);
    }

    private bool FitBook()
    {
        if (bookCanvas == null || bookRect == null || bookParent == null)
        {
            Debug.LogWarning("BookPageTransitionTest: книга должна находиться внутри Canvas.", this);
            return false;
        }

        var canvasRect = bookCanvas.transform as RectTransform;
        var corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);
        Vector3 bottomLeft = bookParent.InverseTransformPoint(corners[0]);
        Vector3 topRight = bookParent.InverseTransformPoint(corners[2]);
        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;
        if (width <= 0f || height <= 0f)
            return false;

        bookRect.anchorMin = Vector2.zero;
        bookRect.anchorMax = Vector2.zero;
        bookRect.pivot = new Vector2(0.5f, 0.5f);
        bookRect.localScale = Vector3.one;
        bookRect.localRotation = Quaternion.identity;
        bookRect.sizeDelta = new Vector2(width * 2f, height);
        bookRect.anchoredPosition = new Vector2(
            bottomLeft.x - bookParent.rect.xMin,
            (bottomLeft.y + topRight.y) * 0.5f - bookParent.rect.yMin
        );
        lastCanvasSize = canvasRect.rect.size;
        return true;
    }

    private void LateUpdate()
    {
        if (book != null && book.gameObject.activeInHierarchy && bookCanvas != null &&
            ((RectTransform)bookCanvas.transform).rect.size != lastCanvasSize && FitBook())
            book.RefreshLayout();
    }

    public IEnumerator Play(Action prepareNextPage)
    {
        if (!IsConfigured)
            yield break;

        Canvas.ForceUpdateCanvases();
        if (!FitBook())
        {
            prepareNextPage();
            yield break;
        }

        yield return new WaitForEndOfFrame();
        screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshot == null)
        {
            Debug.LogWarning("BookPageTransitionTest: не удалось сделать снимок экрана.", this);
            prepareNextPage();
            yield break;
        }

        screenshotSprite = Sprite.Create(screenshot, new Rect(0, 0, screenshot.width, screenshot.height), new Vector2(0.5f, 0.5f));
        originalPage = book.bookPages[0];
        prepareNextPage();
        yield return null;
        yield return new WaitForEndOfFrame();
        nextScreenshot = ScreenCapture.CaptureScreenshotAsTexture();
        if (nextScreenshot != null)
            nextScreenshotSprite = Sprite.Create(nextScreenshot, new Rect(0, 0, nextScreenshot.width, nextScreenshot.height), new Vector2(0.5f, 0.5f));
        else
            Debug.LogWarning("BookPageTransitionTest: не удалось снять следующий лист.", this);

        book.bookPages[0] = screenshotSprite;
        originalBackground = book.background;
        if (nextScreenshotSprite != null)
        {
            book.background = nextScreenshotSprite;
            if (book.bookPages.Length > 2)
            {
                originalNextPage = book.bookPages[2];
                book.bookPages[2] = nextScreenshotSprite;
            }
        }
        finished = false;
        book.currentPage = 0;
        book.OnFlip.AddListener(OnFlip);
        book.gameObject.SetActive(true);
        yield return null;
        Canvas.ForceUpdateCanvases();
        book.RefreshLayout();
        if (paperSprite != null)
            book.LeftNext.sprite = paperSprite;
        book.RightNext.sprite = screenshotSprite;
        autoFlip.FlipRightPage();

        float elapsed = 0f;
        float timeout = Mathf.Max(3f, autoFlip.PageFlipTime * 3f);
        while (!finished && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!finished)
            Debug.LogWarning("BookPageTransitionTest: переворот не завершился вовремя.", this);

        Cancel();
    }

    public void Cancel()
    {
        if (book != null)
        {
            book.OnFlip.RemoveListener(OnFlip);
            book.gameObject.SetActive(false);
            if (screenshotSprite != null)
            {
                book.bookPages[0] = originalPage;
                if (book.Left.sprite == screenshotSprite) book.Left.sprite = originalPage;
                if (book.Right.sprite == screenshotSprite) book.Right.sprite = originalPage;
                if (book.LeftNext.sprite == screenshotSprite) book.LeftNext.sprite = originalPage;
                if (book.RightNext.sprite == screenshotSprite) book.RightNext.sprite = originalPage;
            }
            if (nextScreenshotSprite != null)
            {
                book.background = originalBackground;
                if (book.bookPages.Length > 2) book.bookPages[2] = originalNextPage;
                if (book.Left.sprite == nextScreenshotSprite) book.Left.sprite = originalBackground;
                if (book.Right.sprite == nextScreenshotSprite) book.Right.sprite = originalBackground;
                if (book.LeftNext.sprite == nextScreenshotSprite) book.LeftNext.sprite = originalBackground;
                if (book.RightNext.sprite == nextScreenshotSprite) book.RightNext.sprite = originalBackground;
            }
        }

        if (screenshotSprite != null) Destroy(screenshotSprite);
        if (screenshot != null) Destroy(screenshot);
        if (nextScreenshotSprite != null) Destroy(nextScreenshotSprite);
        if (nextScreenshot != null) Destroy(nextScreenshot);
        screenshotSprite = null;
        screenshot = null;
        originalPage = null;
        nextScreenshotSprite = null;
        nextScreenshot = null;
        originalBackground = null;
        originalNextPage = null;
    }

    private void OnDestroy()
    {
        Cancel();
    }

    private void OnFlip()
    {
        finished = true;
    }
}
