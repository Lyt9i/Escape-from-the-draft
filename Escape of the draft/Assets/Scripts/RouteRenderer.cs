using UnityEngine;

public class RouteRenderer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer grid;
    [SerializeField] private Sprite routeNodeSprite;
    [SerializeField] private Sprite routeLineSprite;

    public bool IsConfigured =>
        grid != null &&
        routeNodeSprite != null &&
        routeLineSprite != null;

    public Bounds GridBounds => grid.bounds;

    public void Redraw(ShiftPuzzleBoard board)
    {
        Clear();

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

    private void CreatePart(
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
