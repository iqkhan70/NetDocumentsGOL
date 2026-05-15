using GameOfLife.Api.Models;

namespace GameOfLife.Api.Services;

public static class BoardValidator
{
    public static bool TryNormalize(UploadBoardRequest request, out bool[][] cells, out string error)
    {
        if (request.Cells is not null)
        {
            cells = request.Cells;
            return TryValidate(cells, out error);
        }

        if (request.Rows is null)
        {
            cells = [];
            error = "Board must include either cells or rows.";
            return false;
        }

        return TryParseRows(request.Rows, out cells, out error);
    }

    public static bool TryValidate(bool[][]? cells, out string error)
    {
        if (cells is null || cells.Length == 0)
        {
            error = "Board must contain at least one row.";
            return false;
        }

        if (cells[0] is null || cells[0].Length == 0)
        {
            error = "Board rows must contain at least one cell.";
            return false;
        }

        var width = cells[0].Length;
        for (var row = 0; row < cells.Length; row++)
        {
            if (cells[row] is null)
            {
                error = $"Row {row} cannot be null.";
                return false;
            }

            if (cells[row].Length != width)
            {
                error = "Board must be rectangular. Every row must have the same number of cells.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool TryParseRows(string[] rows, out bool[][] cells, out string error)
    {
        if (rows.Length == 0)
        {
            cells = [];
            error = "Board must contain at least one row.";
            return false;
        }

        if (string.IsNullOrEmpty(rows[0]))
        {
            cells = [];
            error = "Board rows must contain at least one cell.";
            return false;
        }

        var width = rows[0].Length;
        cells = new bool[rows.Length][];
        error = string.Empty;

        for (var row = 0; row < rows.Length; row++)
        {
            if (rows[row].Length != width)
            {
                cells = [];
                error = "Board must be rectangular. Every row must have the same number of cells.";
                return false;
            }

            cells[row] = new bool[width];
            for (var column = 0; column < width; column++)
            {
                cells[row][column] = rows[row][column] switch
                {
                    '#' => true,
                    '.' => false,
                    _ => InvalidCell(row, column, out error)
                };

                if (!string.IsNullOrEmpty(error))
                {
                    cells = [];
                    return false;
                }
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool InvalidCell(int row, int column, out string error)
    {
        error = $"Invalid board character at row {row}, column {column}. Use '#' for live cells and '.' for dead cells.";
        return false;
    }
}
