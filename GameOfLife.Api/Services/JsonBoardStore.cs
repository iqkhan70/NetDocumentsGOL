using System.Text.Json;
using Microsoft.Extensions.Options;

namespace GameOfLife.Api.Services;

public sealed class JsonBoardStore(IOptions<BoardStorageOptions> options, GameOfLifeEngine engine) : IBoardStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = options.Value.Path;

    public async Task<Guid> CreateAsync(bool[][] cells, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var storage = await LoadAsync(cancellationToken);
            var board = new StoredBoard(Guid.NewGuid(), DateTimeOffset.UtcNow, engine.Clone(cells));
            storage.Boards.Add(board);
            await SaveAsync(storage, cancellationToken);

            return board.Id;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<StoredBoard?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var storage = await LoadAsync(cancellationToken);
            var board = storage.Boards.FirstOrDefault(candidate => candidate.Id == id);

            return board is null
                ? null
                : board with { Cells = engine.Clone(board.Cells) };
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<StorageFile> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return new StorageFile([]);
        }

        await using var stream = File.OpenRead(_path);
        var storage = await JsonSerializer.DeserializeAsync<StorageFile>(stream, SerializerOptions, cancellationToken);

        return storage ?? new StorageFile([]);
    }

    private async Task SaveAsync(StorageFile storage, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_path}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, storage, SerializerOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, _path, overwrite: true);
    }

    private sealed record StorageFile(List<StoredBoard> Boards);
}
