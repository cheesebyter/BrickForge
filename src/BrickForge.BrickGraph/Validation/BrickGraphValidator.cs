using BrickForge.BrickGraph.Model;
using BrickForge.BrickGraph.Parts;

namespace BrickForge.BrickGraph.Validation;

/// <summary>
/// Validates a BrickGraph against the rule set.
/// This is a security and quality boundary — export must not proceed if validation fails.
/// </summary>
public sealed class BrickGraphValidator
{
    private const int DefaultMaxAllowedParts = 80;

    /// <summary>Total number of distinct checks. Drives the quality score.</summary>
    private const int TotalChecks = 11;

    private readonly SupportedPartsRegistry _registry;

    public BrickGraphValidator(SupportedPartsRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Runs all validation rules against the graph and returns a result.
    /// </summary>
    /// <param name="graph">The graph to validate.</param>
    /// <param name="maxParts">
    ///   Optional part-count limit. When null the validator uses its built-in default of
    ///   <see cref="DefaultMaxAllowedParts"/>.
    /// </param>
    public ValidationResult Validate(BrickGraph graph, int? maxParts = null)
    {
        var effectiveMaxParts = maxParts is > 0 ? maxParts.Value : DefaultMaxAllowedParts;
        var issues = new List<ValidationIssue>();

        CheckNonEmptyParts(graph, issues);
        CheckMaxParts(graph, effectiveMaxParts, issues);
        CheckSupportedParts(graph, issues);
        CheckAllowedColors(graph, issues);
        CheckStepAssignment(graph, issues);
        CheckPositionsSet(graph, issues);
        CheckCollisions(graph, issues);
        CheckFloatingParts(graph, issues);
        CheckMonotonicSteps(graph, issues);
        CheckConnectedStructure(graph, issues);
        CheckExportSyntax(graph, issues);

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

    private static void CheckMaxParts(BrickGraph graph, int maxParts, List<ValidationIssue> issues)
    {
        if (graph.Parts.Count > maxParts)
        {
            issues.Add(new ValidationIssue
            {
                Code = "TOO_MANY_PARTS",
                Message = $"Das Modell hat {graph.Parts.Count} Teile. Maximum ist {maxParts}.",
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
                    Message = $"Teilenummer '{part.PartNumber}' (Instanz '{part.InstanceId}') ist nicht in der Teileliste.",
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
                    Message = $"Farbe '{part.Color}' (Instanz '{part.InstanceId}') ist nicht in der Farbliste.",
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

    /// <summary>
    /// Detects parts occupying exactly the same position, which would make the model unbuildable.
    /// </summary>
    private static void CheckCollisions(BrickGraph graph, List<ValidationIssue> issues)
    {
        if (graph.Parts.Count < 2) return;

        var positions = new HashSet<string>();
        var collisionFound = false;

        foreach (var part in graph.Parts)
        {
            if (part.Position.Length != 3) continue;

            var key = $"{part.Position[0]:F2},{part.Position[1]:F2},{part.Position[2]:F2}";
            if (!positions.Add(key))
                collisionFound = true;
        }

        if (collisionFound)
        {
            issues.Add(new ValidationIssue
            {
                Code = "POSITION_COLLISION",
                Message = "Mindestens zwei Teile befinden sich an derselben Position. Das Modell ist nicht baubar.",
                Severity = ValidationSeverity.High
            });
        }
    }

    /// <summary>
    /// Checks that at least one part is placed at the base layer (y == 0).
    /// A model with no ground-level part is likely floating and cannot be built from a stable base.
    /// </summary>
    private static void CheckFloatingParts(BrickGraph graph, List<ValidationIssue> issues)
    {
        if (graph.Parts.Count == 0) return;

        var hasBasePart = graph.Parts.Any(p => p.Position.Length == 3 && p.Position[1] == 0f);

        if (!hasBasePart)
        {
            issues.Add(new ValidationIssue
            {
                Code = "NO_BASE_PARTS",
                Message = "Kein Teil befindet sich auf der Grundebene (y = 0). Das Modell hat keine stabile Basis.",
                Severity = ValidationSeverity.Medium
            });
        }
    }

    /// <summary>
    /// Checks that step numbers are monotonically ordered with no unreasonably large gaps.
    /// </summary>
    private static void CheckMonotonicSteps(BrickGraph graph, List<ValidationIssue> issues)
    {
        if (graph.Parts.Count == 0) return;

        var validSteps = graph.Parts.Where(p => p.Step >= 1).Select(p => p.Step).ToHashSet();
        if (validSteps.Count == 0) return;

        var maxStep = validSteps.Max();
        // Unique step count should be close to max step (no huge gaps like step 1 → step 50)
        if (maxStep > validSteps.Count * 3)
        {
            issues.Add(new ValidationIssue
            {
                Code = "NON_MONOTONIC_STEPS",
                Message = $"Step-Nummern enthalten ungewöhnlich große Lücken (max Step: {maxStep}, eindeutige Steps: {validSteps.Count}). Bauanleitung ist möglicherweise nicht nachvollziehbar.",
                Severity = ValidationSeverity.Medium
            });
        }
    }

    /// <summary>
    /// Checks that the model has at least one part in step 1, providing a base anchor.
    /// </summary>
    private static void CheckConnectedStructure(BrickGraph graph, List<ValidationIssue> issues)
    {
        if (graph.Parts.Count == 0) return;

        var hasStep1 = graph.Parts.Any(p => p.Step == 1);
        if (!hasStep1)
        {
            issues.Add(new ValidationIssue
            {
                Code = "NO_BASE_STEP",
                Message = "Kein Teil befindet sich in Schritt 1. Das Modell hat kein Fundamentelement.",
                Severity = ValidationSeverity.Medium
            });
        }
    }

    /// <summary>
    /// Checks that the graph can be serialised to JSON without errors.
    /// </summary>
    private static void CheckExportSyntax(BrickGraph graph, List<ValidationIssue> issues)
    {
        try
        {
            var json = graph.ToJson();
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("Empty serialization result.");
        }
        catch (Exception ex)
        {
            issues.Add(new ValidationIssue
            {
                Code = "EXPORT_SYNTAX_ERROR",
                Message = $"Der BrickGraph kann nicht serialisiert werden: {ex.Message}",
                Severity = ValidationSeverity.High
            });
        }
    }
}
