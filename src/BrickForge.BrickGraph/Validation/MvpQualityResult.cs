namespace BrickForge.BrickGraph.Validation;

/// <summary>
/// Describes the outcome of an MVP quality check (BF-MVP1-033).
/// A model is MVP-acceptable when all criteria are met.
/// </summary>
public sealed class MvpQualityResult
{
    /// <summary>
    /// True when all MVP quality criteria are satisfied.
    /// </summary>
    public bool IsAcceptable => FailedCriteria.Count == 0;

    /// <summary>
    /// The set of criteria that failed. Empty when the model is acceptable.
    /// </summary>
    public IReadOnlyList<string> FailedCriteria { get; init; } = [];

    /// <summary>
    /// Human-readable descriptions for each failed criterion.
    /// Same order as <see cref="FailedCriteria"/>.
    /// </summary>
    public IReadOnlyList<string> FailureMessages { get; init; } = [];
}
