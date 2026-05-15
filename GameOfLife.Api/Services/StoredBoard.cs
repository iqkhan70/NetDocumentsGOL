namespace GameOfLife.Api.Services;

public sealed record StoredBoard(Guid Id, DateTimeOffset CreatedAt, bool[][] Cells);
