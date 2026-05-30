namespace BrickForge.Export;

/// <summary>
/// The result of an export operation. Content is returned as a string; callers are responsible for writing to disk.
/// </summary>
public sealed record ExportResult
{
    public bool Success { get; init; }
    public string? Content { get; init; }
    public string? ErrorMessage { get; init; }

    public static ExportResult Ok(string content) => new() { Success = true, Content = content };
    public static ExportResult Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
