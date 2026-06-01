namespace BrickForge.Core.Jobs;

/// <summary>
/// Lifecycle status values for a <see cref="GenerationJob"/>.
/// </summary>
public enum JobStatus
{
    Queued,
    AnalyzingPrompt,
    SelectingTemplate,
    PlanningModel,
    GeneratingBrickGraph,
    Validating,
    Repairing,
    Exporting,
    Completed,
    Failed,
    CompletedWithWarnings
}
