using BrickForge.Core.Agents;

namespace BrickForge.Core.Jobs;

/// <summary>
/// Represents a single model generation request and its current state.
/// </summary>
public sealed class GenerationJob
{
    public required string Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Prompt { get; init; }

    public JobStatus Status { get; set; } = JobStatus.Queued;

    public string? TemplateName { get; set; }
    public string? Difficulty { get; set; }
    public int? TargetParts { get; set; }
    public int? ActualParts { get; set; }
    public string? OutputPath { get; set; }
    public double? ValidationScore { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Main colour extracted from prompt analysis (BF-MVP1-042).</summary>
    public string? MainColor { get; set; }

    /// <summary>Accent colour extracted from prompt analysis (BF-MVP1-042).</summary>
    public string? AccentColor { get; set; }

    public List<GeneratedFile> Files { get; init; } = [];

    /// <summary>Per-agent and job-level execution metrics (BF-MVP1-044). Null until pipeline completes.</summary>
    public JobMetrics? Metrics { get; set; }
}
