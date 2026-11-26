using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouteRenderer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer grid;
    [SerializeField] private Sprite routeNodeSprite;
    [SerializeField] private Sprite routeLineSprite;
    [SerializeField] private float drawDuration = 0.8f;
    [SerializeField, Min(0f)] private float shiftDuration = 0.22f;

    private static readonly Vector2Int[] Sides =
        { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

    private Sprite firstStrokeHalf;
    private Sprite secondStrokeHalf;

    private void Awake()
    {
        if (routeLineSprite == null)
            return;

        // Keep the same ink contour when a connection separates during a swipe.
        Rect half = routeLineSprite.rect;
        half.width *= 0.5f;
        firstStrokeHalf = CreateStrokeHalf(half);
        half.x += half.width;
        secondStrokeHalf = CreateStrokeHalf(half);
    }

    private Sprite CreateStrokeHalf(Rect rect)
    {
        return Sprite.Create(routeLineSprite.texture, rect, new Vector2(0.5f, 0.5f),
            routeLineSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
    }

    private void OnDestroy()
    {
        Destroy(firstStrokeHalf);
        Destroy(secondStrokeHalf);
    }

    private struct ShiftPart
    {
        public SpriteRenderer sprite;
        public Vector3 start, end, offset, scale;
        public bool leavesLine, visibleBefore, visibleAfter;

        public void Place(float t, Vector3 step)
        {
            Vector3 center = Vector3.Lerp(start, end, t);
            float size = 1f;
            if (leavesLine)
            {
                // Fold into the outgoing edge and grow from the incoming edge.
                center = t < 0.5f ? start + step * t : end - step * (1f - t);
                size = Mathf.Abs(1f - 2f * t);
            }

            sprite.transform.position = center + offset * size;
            sprite.transform.localScale = scale * size;
            float opacity = Mathf.Lerp(visibleBefore ? 1f : 0f, visibleAfter ? 1f : 0f, t);
            sprite.color = new Color(1f, 1f, 1f, opacity);
        }
    }

    public bool IsConfigured =>
        grid != null &&
        routeNodeSprite != null &&
        routeLineSprite != null;

    public Bounds GridBounds => grid.bounds;

    public void Redraw(ShiftPuzzleBoard board, bool animate = false)
    {
        StopAllCoroutines();
        Clear();
        int firstNewPart = transform.childCount;

        for (int cell = 0; cell < board.Columns * board.Rows; cell++)
        {
            if (!board.IsRouteCell(cell))
                continue;

            bool hasConnection = false;

            foreach (int neighbour in board.Neighbours(cell))
            {
                bool connectsToActor =
                    neighbour == board.PlayerCell ||
                    neighbour == board.DoorCell;

                bool connectsToAnotherNode = board.IsRouteCell(neighbour);

                if (connectsToActor || connectsToAnotherNode)
                    hasConnection = true;

                if (connectsToActor || (connectsToAnotherNode && neighbour > cell))
                    CreateConnection(board, cell, neighbour);
            }

            if (!hasConnection)
                CreatePart("Route Node", routeNodeSprite, CellToWorld(board, cell), 2, 0f);
        }

        if (animate && drawDuration > 0f)
        {
            var parts = new List<Transform>();
            for (int i = firstNewPart; i < transform.childCount; i++)
            {
                Transform part = transform.GetChild(i);
                parts.Add(part);
                part.gameObject.SetActive(false);
            }

            if (parts.Count > 0)
                StartCoroutine(DrawParts(parts));
        }
    }

    public IEnumerator AnimateShift(ShiftPuzzleBoard board, Dictionary<int, int> destinations,
        bool shiftRow, int direction)
    {
        if (shiftDuration > 0f)
        {
            StopAllCoroutines();
            Clear();
            Vector3 cellSize = new Vector3(GridBounds.size.x / board.Columns, GridBounds.size.y / board.Rows);
            Vector3 step = (shiftRow ? Vector3.right * cellSize.x : Vector3.up * cellSize.y) * direction;
            var moving = new List<ShiftPart>();

            for (int cell = 0; cell < board.Columns * board.Rows; cell++)
            {
                int target = Destination(cell, destinations);
                bool actor = cell == board.PlayerCell || cell == board.DoorCell;
                if (!actor && !board.IsRouteCell(target))
                    continue;

                int before = ConnectionMask(board, cell, destinations);
                int after = ConnectionMask(board, target, null);
                Vector3 start = CellToWorld(board, cell), end = CellToWorld(board, target);
                start.z = end.z = transform.position.z;
                bool gap = cell != target && (end - start - step).sqrMagnitude > step.sqrMagnitude * 0.01f;
                for (int side = 0; side < Sides.Length; side++)
                {
                    int bit = 1 << side;
                    if (((before | after) & bit) == 0)
                        continue;

                    Vector2Int axis = Sides[side];
                    int neighbour = cell + axis.x + axis.y * board.Columns;
                    if (cell == target && (before & after & bit) != 0 &&
                        Destination(neighbour, destinations) == neighbour)
                    {
                        if (cell < neighbour)
                            CreateConnection(board, cell, neighbour);
                        continue;
                    }

                    Vector3 offset = new Vector3(axis.x * cellSize.x, axis.y * cellSize.y) * 0.25f;
                    Sprite strokeHalf = axis.x > 0 || axis.y > 0 ? firstStrokeHalf : secondStrokeHalf;
                    var arm = CreatePart("Sliding Stroke", strokeHalf, start + offset, 1,
                        axis.x == 0 ? 90f : 0f);
                    moving.Add(new ShiftPart { sprite = arm, start = start, end = end, offset = offset,
                        scale = arm.transform.localScale, leavesLine = gap, visibleBefore = (before & bit) != 0,
                        visibleAfter = (after & bit) != 0 });
                }

                if (!actor && (before == 0 || after == 0))
                {
                    var dot = CreatePart("Sliding Node", routeNodeSprite, start, 2, 0f);
                    moving.Add(new ShiftPart { sprite = dot, start = start, end = end,
                        scale = dot.transform.localScale, leavesLine = gap,
                        visibleBefore = before == 0, visibleAfter = after == 0 });
                }
            }

            for (float elapsed = 0f; elapsed < shiftDuration; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / shiftDuration);
                foreach (ShiftPart part in moving)
                    part.Place(t, step);
                yield return null;
            }
        }
        Redraw(board);
    }

    private static int Destination(int cell, Dictionary<int, int> destinations)
    {
        return destinations != null && destinations.TryGetValue(cell, out int target) ? target : cell;
    }

    private static int ConnectionMask(ShiftPuzzleBoard board, int cell, Dictionary<int, int> destinations)
    {
        int mask = 0;
        for (int i = 0; i < Sides.Length; i++)
        {
            int x = board.Column(cell) + Sides[i].x, y = board.Row(cell) + Sides[i].y;
            if (x < 0 || y < 0 || x >= board.Columns || y >= board.Rows)
                continue;
            int neighbour = board.Cell(x, y);
            // After the board rotates, the old contents of each cell live at its destination.
            if (board.IsRouteCell(Destination(neighbour, destinations)) ||
                (board.IsRouteCell(Destination(cell, destinations)) &&
                (neighbour == board.PlayerCell || neighbour == board.DoorCell)))
                mask |= 1 << i;
        }
        return mask;
    }

    private IEnumerator DrawParts(List<Transform> parts)
    {
        float partDuration = drawDuration / parts.Count;

        foreach (Transform part in parts)
        {
            Vector3 finalScale = part.localScale;
            Vector3 finalPosition = part.position;
            SpriteRenderer spriteRenderer = part.GetComponent<SpriteRenderer>();
            float width = spriteRenderer.sprite.bounds.size.x * Mathf.Abs(part.lossyScale.x);
            Vector3 direction = part.right;
            float elapsed = 0f;

            part.localScale = new Vector3(0f, finalScale.y, finalScale.z);
            part.position = finalPosition - direction * width * 0.5f;
            part.gameObject.SetActive(true);

            while (elapsed < partDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / partDuration);
                part.localScale = new Vector3(finalScale.x * progress, finalScale.y, finalScale.z);
                part.position = finalPosition - direction * width * 0.5f * (1f - progress);
                yield return null;
            }

            part.localScale = finalScale;
            part.position = finalPosition;
        }
    }

    public Vector3 CellToWorld(ShiftPuzzleBoard board, int cell)
    {
        Bounds bounds = grid.bounds;

        return new Vector3(
            bounds.min.x +
            (board.Column(cell) + 0.5f) * bounds.size.x / board.Columns,

            bounds.min.y +
            (board.Row(cell) + 0.5f) * bounds.size.y / board.Rows,

            0f
        );
    }

    private void CreateConnection(
        ShiftPuzzleBoard board,
        int firstCell,
        int secondCell
    )
    {
        Vector3 first = CellToWorld(board, firstCell);
        Vector3 second = CellToWorld(board, secondCell);
        Vector3 middle = (first + second) * 0.5f;

        bool horizontal =
            Mathf.Abs(first.x - second.x) >
            Mathf.Abs(first.y - second.y);

        CreatePart(
            "Route Line",
            routeLineSprite,
            middle,
            1,
            horizontal ? 0f : 90f
        );
    }

    private SpriteRenderer CreatePart(
        string objectName,
        Sprite sprite,
        Vector3 position,
        int sortingOrder,
        float rotationZ
    )
    {
        GameObject routePart = new GameObject(objectName);

        routePart.transform.SetParent(transform, false);
        routePart.transform.position = new Vector3(
            position.x,
            position.y,
            transform.position.z
        );

        routePart.transform.rotation =
            Quaternion.Euler(0f, 0f, rotationZ);

        routePart.transform.localScale =
            Vector3.one * grid.transform.lossyScale.x;

        SpriteRenderer renderer =
            routePart.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject routePart = transform.GetChild(i).gameObject;
            routePart.SetActive(false);
            Destroy(routePart);
        }
    }
}
