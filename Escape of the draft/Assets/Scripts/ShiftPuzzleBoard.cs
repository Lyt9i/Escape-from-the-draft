using System.Collections.Generic;
using UnityEngine;

public sealed class ShiftPuzzleBoard
{
    private readonly List<bool[]> history = new List<bool[]>();

    public int Columns { get; }
    public int Rows { get; }
    public int PlayerCell { get; private set; }
    public int DoorCell { get; private set; }
    public int StarCell { get; private set; }
    public int Moves { get; private set; }
    public int Par { get; private set; }

    private bool[] cells;
    private bool[] initialCells;

    public ShiftPuzzleBoard(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
    }

    public void Generate(int sheet)
    {
        Moves = 0;
        history.Clear();

        int playerRow = Random.Range(1, Rows - 1);
        int doorRow;

        do
        {
            doorRow = Random.Range(0, Rows);
        }
        while (doorRow == playerRow);

        PlayerCell = Cell(0, playerRow);
        DoorCell = Cell(Columns - 1, doorRow);

        int pivotColumn = Random.Range(2, Columns - 1);
        int starRow = Mathf.RoundToInt((playerRow + doorRow) * 0.5f);
        StarCell = Cell(pivotColumn, starRow);

        bool[] solvedCells = new bool[Columns * Rows];

        for (int column = 1; column <= pivotColumn; column++)
            solvedCells[Cell(column, playerRow)] = true;

        for (int row = Mathf.Min(playerRow, doorRow); row <= Mathf.Max(playerRow, doorRow); row++)
            solvedCells[Cell(pivotColumn, row)] = true;

        for (int column = pivotColumn; column < Columns - 1; column++)
            solvedCells[Cell(column, doorRow)] = true;

        for (int i = 0; i < 3; i++)
        {
            int randomCell;

            do
            {
                randomCell = Random.Range(0, solvedCells.Length);
            }
            while (randomCell == PlayerCell || randomCell == DoorCell);

            solvedCells[randomCell] = true;
        }

        int scrambleCount = Mathf.Min(5, 2 + sheet / 3);

        for (int attempt = 0; attempt < 100; attempt++)
        {
            cells = Copy(solvedCells);

            for (int i = 0; i < scrambleCount; i++)
            {
                bool shiftRow = Random.value > 0.5f;
                int index = Random.Range(0, shiftRow ? Rows : Columns);
                int direction = Random.value > 0.5f ? 1 : -1;

                RotateLine(shiftRow, index, direction);
            }

            if (!HasPath())
            {
                Par = scrambleCount;
                initialCells = Copy(cells);
                return;
            }
        }

        cells = Copy(solvedCells);
        RotateLine(false, 1, 1);
        Par = 1;
        initialCells = Copy(cells);
    }

    public bool TryShift(bool shiftRow, int index, int direction)
    {
        if (direction != -1 && direction != 1)
            return false;

        int maximumIndex = shiftRow ? Rows : Columns;

        if (index < 0 || index >= maximumIndex)
            return false;

        bool[] beforeMove = Copy(cells);
        RotateLine(shiftRow, index, direction);

        if (AreEqual(beforeMove, cells))
            return false;

        history.Add(beforeMove);
        Moves++;
        return true;
    }

    public void Undo()
    {
        if (history.Count == 0)
            return;

        cells = history[history.Count - 1];
        history.RemoveAt(history.Count - 1);
        Moves++;
    }

    public void Reset()
    {
        cells = Copy(initialCells);
        history.Clear();
        Moves = 0;
    }

    public bool IsRouteCell(int cell)
    {
        return cells[cell];
    }

    public bool HasPath()
    {
        return FindPath() != null;
    }

    public List<int> FindPath()
    {
        int[] previous = new int[cells.Length];

        for (int i = 0; i < previous.Length; i++)
            previous[i] = -2;

        var queue = new Queue<int>();
        queue.Enqueue(PlayerCell);
        previous[PlayerCell] = -1;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == DoorCell)
                break;

            foreach (int neighbour in Neighbours(current))
            {
                bool passable = neighbour == DoorCell || cells[neighbour];

                if (previous[neighbour] == -2 && passable)
                {
                    previous[neighbour] = current;
                    queue.Enqueue(neighbour);
                }
            }
        }

        if (previous[DoorCell] == -2)
            return null;

        var path = new List<int>();

        for (int cell = DoorCell; cell != -1; cell = previous[cell])
            path.Add(cell);

        path.Reverse();
        return path;
    }

    public IEnumerable<int> Neighbours(int cell)
    {
        int column = Column(cell);
        int row = Row(cell);

        if (row > 0)
            yield return Cell(column, row - 1);

        if (column < Columns - 1)
            yield return Cell(column + 1, row);

        if (row < Rows - 1)
            yield return Cell(column, row + 1);

        if (column > 0)
            yield return Cell(column - 1, row);
    }

    public int Cell(int column, int row)
    {
        return row * Columns + column;
    }

    public int Column(int cell)
    {
        return cell % Columns;
    }

    public int Row(int cell)
    {
        return cell / Columns;
    }

    private void RotateLine(bool shiftRow, int index, int direction)
    {
        var line = new List<int>();
        int length = shiftRow ? Columns : Rows;

        for (int i = 0; i < length; i++)
        {
            int cell = shiftRow ? Cell(i, index) : Cell(index, i);

            if (cell != PlayerCell && cell != DoorCell)
                line.Add(cell);
        }

        bool[] values = new bool[line.Count];

        for (int i = 0; i < line.Count; i++)
            values[i] = cells[line[i]];

        for (int i = 0; i < line.Count; i++)
        {
            int sourceIndex = Modulo(i - direction, line.Count);
            cells[line[i]] = values[sourceIndex];
        }
    }

    private static bool[] Copy(bool[] source)
    {
        return (bool[])source.Clone();
    }

    private static bool AreEqual(bool[] first, bool[] second)
    {
        for (int i = 0; i < first.Length; i++)
        {
            if (first[i] != second[i])
                return false;
        }

        return true;
    }

    private static int Modulo(int value, int length)
    {
        return (value % length + length) % length;
    }
}