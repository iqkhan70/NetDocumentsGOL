namespace GameOfLife.Api.Services;

public interface IBoardStore
{
    Task<Guid> CreateAsync(bool[][] cells, CancellationToken cancellationToken);

    Task<StoredBoard?> GetAsync(Guid id, CancellationToken cancellationToken);
}
