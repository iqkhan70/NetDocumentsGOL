namespace GameOfLife.Api.Services;

public sealed class BoardStorageOptions
{
    public const string SectionName = "BoardStorage";

    public string Path { get; set; } = string.Empty;

    public int DefaultFinalStateMaxAttempts { get; set; } = 1_000;
}
