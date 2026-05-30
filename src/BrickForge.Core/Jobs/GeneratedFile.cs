namespace BrickForge.Core.Jobs;

/// <summary>
/// Represents a single file produced by a <see cref="GenerationJob"/>.
/// </summary>
public sealed class GeneratedFile
{
    public required string Id { get; init; }
    public required string JobId { get; init; }

    /// <summary>Logical file type identifier, e.g. "model.mpd", "parts.csv", "validation.json".</summary>
    public required string FileType { get; init; }

    /// <summary>Absolute path to the file on the local file system (internal use only – never exposed to callers).</summary>
    public required string FilePath { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
