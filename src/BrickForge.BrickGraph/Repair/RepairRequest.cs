using BrickForge.BrickGraph.Templates;

namespace BrickForge.BrickGraph.Repair;

/// <summary>
/// Input for <see cref="BrickGraphRepairAgent"/>.
/// Carries the graph to repair plus the constraints the repair must respect.
/// </summary>
/// <param name="Graph">The BrickGraph that failed validation.</param>
/// <param name="Template">
///   The template used to generate the graph.
///   When provided its <see cref="BrickModelTemplate.DefaultMainColor"/> is used as the fallback color replacement.
/// </param>
/// <param name="MaxParts">
///   Maximum number of parts allowed. Defaults to 80 (MVP0 limit).
///   Override with the template's own limit if available.
/// </param>
public sealed record RepairRequest(
    BrickGraph Graph,
    BrickModelTemplate? Template = null,
    int MaxParts = 80);
