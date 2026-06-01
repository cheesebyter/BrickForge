namespace BrickForge.Core.Agents;

/// <summary>
/// Timing and quality metrics captured for a single agent stage during pipeline execution (BF-MVP1-044).
/// </summary>
public sealed record AgentMetrics
{
    /// <summary>Name of the agent or pipeline stage.</summary>
    public required string AgentName { get; init; }

    /// <summary>UTC timestamp when the stage started.</summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>UTC timestamp when the stage ended.</summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>Wall-clock duration in milliseconds.</summary>
    public long DurationMs => (long)(EndTime - StartTime).TotalMilliseconds;

    /// <summary>Number of LLM HTTP calls made by this stage.</summary>
    public int LlmCalls { get; init; }

    /// <summary>Number of retries attempted by this stage.</summary>
    public int Retries { get; init; }

    /// <summary>Whether the stage completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Optional confidence or quality score produced by this stage (0.0–1.0).</summary>
    public double? Confidence { get; init; }

    /// <summary>Optional final score after repair/validation (0.0–1.0).</summary>
    public double? FinalScore { get; init; }
}
