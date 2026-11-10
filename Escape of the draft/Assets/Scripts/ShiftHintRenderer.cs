using UnityEngine;

public class ShiftHintRenderer : MonoBehaviour
{
    private Sprite sprite;
    private Transform rowHighlight;
    private Transform columnHighlight;

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

    private void OnDestroy()
    {
        if (sprite != null)
            Destroy(sprite);
    }
}
