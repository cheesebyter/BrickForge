using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Ai;

namespace BrickForge.Export;

/// <summary>
/// Input data for the <see cref="GenerationJsonExporter"/>.
/// Produces the <c>generation.json</c> file required by the output package (Section 17.1).
/// </summary>
public sealed class GenerationJsonData
{
    /// <summary>The internal job identifier.</summary>
    public string JobId { get; init; } = string.Empty;

    /// <summary>The original user prompt text.</summary>
    public string OriginalPrompt { get; init; } = string.Empty;

    /// <summary>Name of the template selected for this job (e.g. "small_machine").</summary>
    public string TemplateName { get; init; } = string.Empty;

    /// <summary>Result of prompt analysis. Null when analysis did not run.</summary>
    public PromptAnalysisResult? AnalysisResult { get; init; }

    /// <summary>Result of BrickGraph validation after any repair attempt.</summary>
    public ValidationResult? ValidationResult { get; init; }

    /// <summary>True when the model was automatically repaired after initial validation failure.</summary>
    public bool WasRepaired { get; init; }

    /// <summary>List of generated file names (relative, e.g. "model.mpd").</summary>
    public IReadOnlyList<string> GeneratedFiles { get; init; } = [];

    /// <summary>UTC timestamp when this generation completed.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
