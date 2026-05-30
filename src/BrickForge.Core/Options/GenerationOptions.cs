namespace BrickForge.Core.Options;

/// <summary>
/// Configuration options for the BrickForge generation pipeline.
/// </summary>
public sealed class GenerationOptions
{
    /// <summary>Hard cap on the number of parts any generated model may contain.</summary>
    public int MaxParts { get; init; } = 80;

    /// <summary>Default target parts used when the prompt does not specify a count.</summary>
    public int DefaultTargetParts { get; init; } = 50;

    /// <summary>Root directory for all generated output files.</summary>
    public string OutputRoot { get; init; } = "data/outputs";
}
