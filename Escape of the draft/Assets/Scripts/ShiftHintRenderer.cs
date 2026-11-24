using System.Collections;
using UnityEngine;

public class ShiftHintRenderer : MonoBehaviour
{
    private Sprite sprite;
    private Transform rowHighlight;
    private Transform columnHighlight;
    private Transform inkMarks;
    private Texture2D inkTexture;
    private Sprite inkSprite;

    private void Awake()
    {
        sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        rowHighlight = CreateHighlight("Row Highlight");
        columnHighlight = CreateHighlight("Column Highlight");
    }

    public void Show(Bounds bounds, int columns, int rows, int column, int row)
    {
        float width = bounds.size.x / columns;
        float height = bounds.size.y / rows;
        rowHighlight.position = new Vector3(bounds.center.x, bounds.min.y + (row + 0.5f) * height, bounds.center.z);
        rowHighlight.localScale = new Vector3(bounds.size.x, height, 1f);
        columnHighlight.position = new Vector3(bounds.min.x + (column + 0.5f) * width, bounds.center.y, bounds.center.z);
        columnHighlight.localScale = new Vector3(width, bounds.size.y, 1f);
    }

    private Transform CreateHighlight(string name)
    {
        var part = new GameObject(name);
        part.transform.SetParent(transform, false);
        var renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.16f, 0.31f, 0.61f, 0.12f);
        renderer.sortingOrder = 1;
        return part.transform;
    }

    public void ClearInk()
    {
        StopAllCoroutines();
        if (inkMarks == null)
            return;

        inkMarks.gameObject.SetActive(false);
        Destroy(inkMarks.gameObject);
        inkMarks = null;
    }

    public void ShowInk(ShiftPuzzleBoard board, RouteRenderer route)
    {
        if (board.InkCells.Count == 0)
            return;

        if (inkSprite == null)
            CreateInkSprite();

        inkMarks = new GameObject("Ink Marks").transform;
        inkMarks.SetParent(transform, false);
        Bounds bounds = route.GridBounds;
        float width = bounds.size.x / board.Columns;
        float height = bounds.size.y / board.Rows;

        foreach (int cell in board.InkCells)
        {
            Vector3 target = route.CellToWorld(board, cell);
            var drop = new GameObject("Ink Drop").AddComponent<SpriteRenderer>();
            drop.transform.SetParent(inkMarks, false);
            drop.sprite = inkSprite;
            drop.color = new Color(.20f, .11f, .30f, .68f);
            drop.sortingOrder = 2;
            StartCoroutine(Fall(drop.transform, target, Mathf.Min(width, height)));
        }
    }

    private static IEnumerator Fall(Transform drop, Vector3 target, float size)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / .35f)
        {
            drop.position = target + Vector3.up * ((1f - t * t) * size * 1.2f);
            drop.localScale = new Vector3(.24f, .48f, 1f) * size;
            yield return null;
        }

        drop.position = target;
        drop.localScale = Vector3.one * (.82f * size);
    }

    private void CreateInkSprite()
    {
        const int size = 64;
        inkTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        inkTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new Vector2((x + .5f) / size - .5f, (y + .5f) / size - .5f);
            float angle = Mathf.Atan2(point.y, point.x);
            float radius = .31f + .045f * Mathf.Sin(5f * angle) + .03f * Mathf.Cos(7f * angle);
            float edge = Mathf.Max(radius - point.magnitude,
                .052f - Vector2.Distance(point, new Vector2(.32f, .29f)),
                .035f - Vector2.Distance(point, new Vector2(-.36f, -.23f)));
            inkTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(edge * size + .5f)));
        }
        inkTexture.Apply(false, true);
        inkSprite = Sprite.Create(inkTexture, new Rect(0, 0, size, size), Vector2.one * .5f, size,
            0, SpriteMeshType.FullRect);
    }

    private void OnDestroy()
    {
        if (sprite != null)
            Destroy(sprite);
        if (inkSprite != null)
            Destroy(inkSprite);
        if (inkTexture != null)
            Destroy(inkTexture);
    }
}
