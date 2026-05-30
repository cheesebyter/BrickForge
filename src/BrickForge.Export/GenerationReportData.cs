using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Ai;

namespace BrickForge.Export;

/// <summary>
/// Input data for the <see cref="ReportExporter"/>.
/// Aggregates everything needed to produce <c>report.md</c>.
/// </summary>
public sealed class GenerationReportData
{
    /// <summary>The original user prompt text.</summary>
    public string OriginalPrompt { get; init; } = string.Empty;

    /// <summary>Name of the AI model used (e.g. "llama3"), or null when the fallback analyser was used.</summary>
    public string? AiModelName { get; init; }

    /// <summary>Result of prompt analysis. Null when analysis did not run.</summary>
    public PromptAnalysisResult? AnalysisResult { get; init; }

    /// <summary>Result of BrickGraph validation. Null when validation did not run.</summary>
    public ValidationResult? ValidationResult { get; init; }

    /// <summary>List of generated file names (relative, e.g. "model.mpd").</summary>
    public IReadOnlyList<string> GeneratedFiles { get; init; } = [];

    /// <summary>UTC timestamp when this report was generated.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
