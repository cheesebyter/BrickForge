using BrickForge.BrickGraph.Parts;

namespace BrickForge.BrickGraph.Validation;

/// <summary>
/// Validates a BrickGraph against the MVP0 rule set.
/// This is a security and quality boundary — export must not proceed if validation fails.
/// </summary>
public sealed class BrickGraphValidator
{
    private const int MaxAllowedParts = 80;
    private const int TotalChecks = 6;

    private readonly SupportedPartsRegistry _registry;

    public BrickGraphValidator(SupportedPartsRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Runs all MVP0 validation rules against the graph and returns a result.
    /// </summary>
    public ValidationResult Validate(BrickGraph graph)
    {
        var issues = new List<ValidationIssue>();

        CheckNonEmptyParts(graph, issues);
        CheckMaxParts(graph, issues);
        CheckSupportedParts(graph, issues);
        CheckAllowedColors(graph, issues);
        CheckStepAssignment(graph, issues);
        CheckPositionsSet(graph, issues);

        return ValidationResult.FromIssues(issues, TotalChecks);
    }

    // ── Individual rule checks ────────────────────────────────────────────────

    private static void CheckNonEmptyParts(BrickGraph graph, List<ValidationIssue> issues)
    {
        if (graph.Parts.Count == 0)
        {
            issues.Add(new ValidationIssue
            {
                Code = "EMPTY_PARTS",
                Message = "Das Modell enthält keine Teile. Ein leeres Modell kann nicht exportiert werden.",
                Severity = ValidationSeverity.High
            });
        }
    }

    private static void CheckMaxParts(BrickGraph graph, List<ValidationIssue> issues)
    {
        if (graph.Parts.Count > MaxAllowedParts)
        {
            issues.Add(new ValidationIssue
            {
                Code = "TOO_MANY_PARTS",
                Message = $"Das Modell hat {graph.Parts.Count} Teile. Maximum ist {MaxAllowedParts}.",
                Severity = ValidationSeverity.High
            });
        }
    }

    private void CheckSupportedParts(BrickGraph graph, List<ValidationIssue> issues)
    {
        foreach (var part in graph.Parts)
        {
            if (!_registry.IsPartSupported(part.PartNumber))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "UNSUPPORTED_PART",
                    Message = $"Teilenummer '{part.PartNumber}' (Instanz '{part.InstanceId}') ist nicht in der MVP0-Teileliste.",
                    Severity = ValidationSeverity.High
                });
            }
        }
    }

    private void CheckAllowedColors(BrickGraph graph, List<ValidationIssue> issues)
    {
        foreach (var part in graph.Parts)
        {
            if (!_registry.IsColorSupported(part.Color))
            {
                issues.Add(new ValidationIssue
                {
                    Code = "UNSUPPORTED_COLOR",
                    Message = $"Farbe '{part.Color}' (Instanz '{part.InstanceId}') ist nicht in der MVP0-Farbliste.",
                    Severity = ValidationSeverity.High
                });
            }
        }
    }

    private static void CheckStepAssignment(BrickGraph graph, List<ValidationIssue> issues)
    {
        foreach (var part in graph.Parts)
        {
            if (part.Step < 1)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_STEP",
                    Message = $"Teil '{part.InstanceId}' hat einen ungültigen Step-Wert ({part.Step}). Step muss >= 1 sein.",
                    Severity = ValidationSeverity.High
                });
            }
        }
    }

    private static void CheckPositionsSet(BrickGraph graph, List<ValidationIssue> issues)
    {
        foreach (var part in graph.Parts)
        {
            if (part.Position.Length != 3)
            {
                issues.Add(new ValidationIssue
                {
                    Code = "INVALID_POSITION",
                    Message = $"Teil '{part.InstanceId}' hat kein gültiges 3D-Positionsarray.",
                    Severity = ValidationSeverity.Medium
                });
            }
        }
    }
}
