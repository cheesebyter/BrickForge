namespace BrickForge.Core.Agents;

/// <summary>
/// Aggregated metrics for a complete generation job (BF-MVP1-044).
/// Contains per-agent detail and job-level totals.
/// </summary>
public sealed record JobMetrics
{
    /// <summary>UTC timestamp when the overall pipeline started.</summary>
    public required DateTimeOffset JobStartTime { get; init; }

    /// <summary>UTC timestamp when the overall pipeline ended.</summary>
    public required DateTimeOffset JobEndTime { get; init; }

    /// <summary>Total wall-clock duration for the entire job in milliseconds.</summary>
    public long TotalDurationMs => (long)(JobEndTime - JobStartTime).TotalMilliseconds;

    /// <summary>Sum of LLM calls across all agent stages.</summary>
    public int TotalLlmCalls { get; init; }

    /// <summary>Sum of retries across all agent stages.</summary>
    public int TotalRetries { get; init; }

    /// <summary>Whether the overall job succeeded.</summary>
    public bool JobSuccess { get; init; }

    /// <summary>Per-agent breakdown of metrics.</summary>
    public IReadOnlyList<AgentMetrics> AgentBreakdown { get; init; } = [];
}
