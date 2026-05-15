using GameOfLife.Api.Models;
using GameOfLife.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<GameOfLifeEngine>();
builder.Services.AddSingleton<IBoardStore, JsonBoardStore>();
builder.Services.Configure<BoardStorageOptions>(builder.Configuration.GetSection(BoardStorageOptions.SectionName));
builder.Services.PostConfigure<BoardStorageOptions>(options =>
{
    options.Path = string.IsNullOrWhiteSpace(options.Path)
        ? Path.Combine(builder.Environment.ContentRootPath, "Data", "boards.json")
        : Path.GetFullPath(options.Path, builder.Environment.ContentRootPath);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

var boards = app.MapGroup("/boards").WithTags("Boards");

boards.MapPost("/", async (UploadBoardRequest request, IBoardStore store, CancellationToken cancellationToken) =>
{
    if (!BoardValidator.TryNormalize(request, out var cells, out var error))
    {
        return Results.BadRequest(new ErrorResponse(error));
    }

    var id = await store.CreateAsync(cells, cancellationToken);

    return Results.Created($"/boards/{id}", new BoardCreatedResponse(id));
})
.WithName("UploadBoard")
.WithSummary("Uploads a new board state and returns its id.");

boards.MapGet("/{id:guid}", async (Guid id, IBoardStore store, CancellationToken cancellationToken) =>
{
    var board = await store.GetAsync(id, cancellationToken);

    return board is null
        ? Results.NotFound(new ErrorResponse("Board not found."))
        : Results.Ok(new BoardStateResponse(board.Id, 0, board.Cells, IsStable: false));
})
.WithName("GetBoard")
.WithSummary("Gets the uploaded board state.");

boards.MapGet("/{id:guid}/next", async (
    Guid id,
    IBoardStore store,
    GameOfLifeEngine engine,
    CancellationToken cancellationToken) =>
{
    var board = await store.GetAsync(id, cancellationToken);
    if (board is null)
    {
        return Results.NotFound(new ErrorResponse("Board not found."));
    }

    var next = engine.Next(board.Cells);

    return Results.Ok(new BoardStateResponse(board.Id, 1, next, engine.AreEqual(board.Cells, next)));
})
.WithName("GetNextState")
.WithSummary("Gets the next state for a board.");

boards.MapGet("/{id:guid}/states/{generations:int}", async (
    Guid id,
    int generations,
    IBoardStore store,
    GameOfLifeEngine engine,
    CancellationToken cancellationToken) =>
{
    if (generations < 0)
    {
        return Results.BadRequest(new ErrorResponse("Generations must be greater than or equal to zero."));
    }

    var board = await store.GetAsync(id, cancellationToken);
    if (board is null)
    {
        return Results.NotFound(new ErrorResponse("Board not found."));
    }

    var state = engine.Advance(board.Cells, generations);

    return Results.Ok(new BoardStateResponse(board.Id, generations, state, engine.IsStable(state)));
})
.WithName("GetStateAfterGenerations")
.WithSummary("Gets a board state a specified number of generations away.");

boards.MapGet("/{id:guid}/final", async (
    Guid id,
    int? maxAttempts,
    IBoardStore store,
    GameOfLifeEngine engine,
    IOptions<BoardStorageOptions> options,
    CancellationToken cancellationToken) =>
{
    var attempts = maxAttempts ?? options.Value.DefaultFinalStateMaxAttempts;
    if (attempts < 0)
    {
        return Results.BadRequest(new ErrorResponse("maxAttempts must be greater than or equal to zero."));
    }

    var board = await store.GetAsync(id, cancellationToken);
    if (board is null)
    {
        return Results.NotFound(new ErrorResponse("Board not found."));
    }

    var result = engine.TryFindFinalState(board.Cells, attempts);
    if (!result.Found)
    {
        return Results.UnprocessableEntity(new ErrorResponse($"Board did not reach a final stable state within {attempts} attempts."));
    }

    return Results.Ok(new BoardStateResponse(board.Id, result.Generation, result.Cells, IsStable: true));
})
.WithName("GetFinalState")
.WithSummary("Gets the final stable state for a board within a maximum number of attempts.");

app.Run();

public partial class Program;
