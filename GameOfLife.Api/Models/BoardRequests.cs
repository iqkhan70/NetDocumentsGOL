namespace GameOfLife.Api.Models;

public sealed record UploadBoardRequest
{
    public bool[][]? Cells { get; init; }

    public string[]? Rows { get; init; }
}

public sealed record BoardCreatedResponse(Guid Id);

public sealed record BoardStateResponse(Guid Id, int Generation, bool[][] Cells, bool IsStable)
{
    public string[] Rows => Cells
        .Select(row => new string(row.Select(cell => cell ? '#' : '.').ToArray()))
        .ToArray();
}

public sealed record ErrorResponse(string Error);
