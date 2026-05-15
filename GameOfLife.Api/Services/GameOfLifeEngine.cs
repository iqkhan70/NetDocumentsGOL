namespace GameOfLife.Api.Services;

public sealed class GameOfLifeEngine
{
    public bool[][] Next(bool[][] cells)
    {
        var height = cells.Length;
        var width = cells[0].Length;
        var next = CreateBoard(height, width);

        for (var row = 0; row < height; row++)
        {
            for (var column = 0; column < width; column++)
            {
                var liveNeighbors = CountLiveNeighbors(cells, row, column);
                next[row][column] = cells[row][column]
                    ? liveNeighbors is 2 or 3
                    : liveNeighbors is 3;
            }
        }

        return next;
    }

    public bool[][] Advance(bool[][] cells, int generations)
    {
        var current = Clone(cells);
        for (var generation = 0; generation < generations; generation++)
        {
            current = Next(current);
        }

        return current;
    }

    public bool IsStable(bool[][] cells)
    {
        return AreEqual(cells, Next(cells));
    }

    public FinalStateResult TryFindFinalState(bool[][] cells, int maxAttempts)
    {
        var current = Clone(cells);
        if (IsStable(current))
        {
            return new FinalStateResult(true, 0, current);
        }

        for (var generation = 1; generation <= maxAttempts; generation++)
        {
            var next = Next(current);
            if (AreEqual(current, next))
            {
                return new FinalStateResult(true, generation, next);
            }

            current = next;
        }

        return new FinalStateResult(false, maxAttempts, current);
    }

    public bool AreEqual(bool[][] left, bool[][] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var row = 0; row < left.Length; row++)
        {
            if (left[row].Length != right[row].Length)
            {
                return false;
            }

            for (var column = 0; column < left[row].Length; column++)
            {
                if (left[row][column] != right[row][column])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool[][] Clone(bool[][] cells)
    {
        var clone = new bool[cells.Length][];
        for (var row = 0; row < cells.Length; row++)
        {
            clone[row] = new bool[cells[row].Length];
            Array.Copy(cells[row], clone[row], cells[row].Length);
        }

        return clone;
    }

    private static int CountLiveNeighbors(bool[][] cells, int row, int column)
    {
        var count = 0;

        for (var rowOffset = -1; rowOffset <= 1; rowOffset++)
        {
            for (var columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                if (rowOffset == 0 && columnOffset == 0)
                {
                    continue;
                }

                var neighborRow = row + rowOffset;
                var neighborColumn = column + columnOffset;
                if (neighborRow < 0 ||
                    neighborColumn < 0 ||
                    neighborRow >= cells.Length ||
                    neighborColumn >= cells[neighborRow].Length)
                {
                    continue;
                }

                if (cells[neighborRow][neighborColumn])
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool[][] CreateBoard(int height, int width)
    {
        var cells = new bool[height][];
        for (var row = 0; row < height; row++)
        {
            cells[row] = new bool[width];
        }

        return cells;
    }
}

public sealed record FinalStateResult(bool Found, int Generation, bool[][] Cells);
