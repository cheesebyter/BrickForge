namespace BrickForge.Core.Ai;

/// <summary>
/// The structured result of an AI-assisted prompt analysis.
/// This is a domain object produced after validating the raw AI response.
/// All values are safe to use by downstream pipeline stages.
/// </summary>
public sealed class PromptAnalysisResult
{
    /// <summary>Human-readable name for the model derived from the prompt.</summary>
    public string ModelName { get; init; } = "Brick Model";

    /// <summary>Category that determines which template is selected.</summary>
    public string ModelCategory { get; init; } = "small_machine";

    /// <summary>Target part count, already capped at the configured maximum.</summary>
    public int TargetParts { get; init; } = 50;

    /// <summary>Primary colour of the model.</summary>
    public string MainColor { get; init; } = "black";

    /// <summary>Accent/secondary colour of the model.</summary>
    public string AccentColor { get; init; } = "light_bluish_gray";

    /// <summary>Requested model features (e.g. cup, front_panel).</summary>
    public IReadOnlyList<string> Features { get; init; } = [];

    /// <summary>
    /// Whether the prompt can be fulfilled within MVP0 constraints.
    /// A value of false must stop the generation pipeline.
    /// </summary>
    public bool Feasible { get; init; } = true;

    /// <summary>Non-fatal advisory messages returned by the AI or the validator.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>True when the result was produced by the rule-based fallback analyser, not by the AI.</summary>
    public bool UsedFallback { get; init; } = false;
}
