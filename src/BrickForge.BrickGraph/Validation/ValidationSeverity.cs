namespace BrickForge.BrickGraph.Validation;

/// <summary>
/// Severity level of a validation issue.
/// High-severity issues render the model invalid and must block export.
/// </summary>
public enum ValidationSeverity
{
    Low = 0,
    Medium = 1,
    High = 2
}
