namespace BrickForge.Core.Options;

/// <summary>
/// Configuration options for the BrickForge persistence layer.
/// </summary>
public sealed class StorageOptions
{
    public string ConnectionString { get; init; } = "Data Source=data/brickforge.db";
}
