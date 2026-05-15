using System.Net;
using System.Net.Http.Json;
using GameOfLife.Api.Models;
using GameOfLife.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GameOfLife.Tests;

public sealed class GameOfLifeEngineTests
{
    [Fact]
    public void Next_AppliesConwaysRules()
    {
        var engine = new GameOfLifeEngine();
        var verticalBlinker = new[]
        {
            new[] { false, true, false },
            new[] { false, true, false },
            new[] { false, true, false }
        };

        var next = engine.Next(verticalBlinker);

        Assert.Equal(
            [
                [false, false, false],
                [true, true, true],
                [false, false, false]
            ],
            next);
    }

    [Fact]
    public void TryFindFinalState_ReturnsStableBoard()
    {
        var engine = new GameOfLifeEngine();
        var block = new[]
        {
            new[] { true, true },
            new[] { true, true }
        };

        var result = engine.TryFindFinalState(block, maxAttempts: 5);

        Assert.True(result.Found);
        Assert.Equal(0, result.Generation);
        Assert.Equal(block, result.Cells);
    }
}

public sealed class JsonBoardStoreTests
{
    [Fact]
    public async Task CreateAsync_PersistsBoardForNewStoreInstance()
    {
        var storagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "boards.json");
        var engine = new GameOfLifeEngine();
        var options = Options.Create(new BoardStorageOptions { Path = storagePath });
        var cells = new[]
        {
            new[] { true, false },
            new[] { false, true }
        };

        var firstStore = new JsonBoardStore(options, engine);
        var id = await firstStore.CreateAsync(cells, CancellationToken.None);

        var secondStore = new JsonBoardStore(options, engine);
        var stored = await secondStore.GetAsync(id, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(cells, stored.Cells);
    }
}

public sealed class BoardApiTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly WebApplicationFactory<Program> _factory;

    public BoardApiTests()
    {
        var storagePath = Path.Combine(_temporaryDirectory, "boards.json");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["BoardStorage:Path"] = storagePath,
                        ["BoardStorage:DefaultFinalStateMaxAttempts"] = "4"
                    });
                });
            });
    }

    [Fact]
    public async Task UploadAndGetNext_ReturnsNextBoardState()
    {
        var client = _factory.CreateClient();
        var request = new UploadBoardRequest
        {
            Cells =
            [
                [false, true, false],
                [false, true, false],
                [false, true, false]
            ]
        };

        var createResponse = await client.PostAsJsonAsync("/boards", request, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<BoardCreatedResponse>(CancellationToken.None);
        Assert.NotNull(created);

        var next = await client.GetFromJsonAsync<BoardStateResponse>(
            $"/boards/{created.Id}/next",
            CancellationToken.None);

        Assert.NotNull(next);
        Assert.Equal(1, next.Generation);
        Assert.Equal(
            [
                [false, false, false],
                [true, true, true],
                [false, false, false]
            ],
            next.Cells);
        Assert.Equal(["...", "###", "..."], next.Rows);
    }

    [Fact]
    public async Task Upload_WithRowsFormat_ReturnsCreatedBoard()
    {
        var client = _factory.CreateClient();
        var request = new UploadBoardRequest
        {
            Rows =
            [
                ".....",
                "..#..",
                "..#..",
                "..#..",
                "....."
            ]
        };

        var createResponse = await client.PostAsJsonAsync("/boards", request, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<BoardCreatedResponse>(CancellationToken.None);
        Assert.NotNull(created);

        var next = await client.GetFromJsonAsync<BoardStateResponse>(
            $"/boards/{created.Id}/next",
            CancellationToken.None);

        Assert.NotNull(next);
        Assert.Equal(
            [
                [false, false, false, false, false],
                [false, false, false, false, false],
                [false, true, true, true, false],
                [false, false, false, false, false],
                [false, false, false, false, false]
            ],
            next.Cells);
        Assert.Equal([".....", ".....", ".###.", ".....", "....."], next.Rows);
    }

    [Fact]
    public async Task GetFinal_ReturnsErrorWhenBoardDoesNotStabilizeInAttempts()
    {
        var client = _factory.CreateClient();
        var request = new UploadBoardRequest
        {
            Cells =
            [
                [false, true, false],
                [false, true, false],
                [false, true, false]
            ]
        };

        var createResponse = await client.PostAsJsonAsync("/boards", request, CancellationToken.None);
        var created = await createResponse.Content.ReadFromJsonAsync<BoardCreatedResponse>(CancellationToken.None);
        Assert.NotNull(created);

        var finalResponse = await client.GetAsync($"/boards/{created.Id}/final?maxAttempts=4", CancellationToken.None);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, finalResponse.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
