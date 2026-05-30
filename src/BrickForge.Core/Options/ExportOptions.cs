namespace BrickForge.Core.Options;

/// <summary>
/// Configuration options that control which output files are generated per job.
/// </summary>
public sealed class ExportOptions
{
    /// <summary>Generate an LDraw MPD model file.</summary>
    public bool GenerateMpd { get; init; } = true;

    /// <summary>Generate a CSV parts list.</summary>
    public bool GenerateCsv { get; init; } = true;

    /// <summary>Generate a Markdown instruction document.</summary>
    public bool GenerateMarkdown { get; init; } = true;
}
