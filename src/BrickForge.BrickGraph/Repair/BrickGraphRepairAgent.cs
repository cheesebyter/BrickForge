using BrickForge.BrickGraph.Model;
using BrickForge.BrickGraph.Parts;
using BrickForge.Core.Agents;
using Microsoft.Extensions.Logging;


namespace BrickForge.BrickGraph.Repair;

/// <summary>
/// Rule-based agent that repairs a BrickGraph to resolve common validation failures.
///
/// Repairs applied (in order):
/// 1. Replace unsupported colors with the template's default main color.
/// 2. Assign step 1 to any part whose step number is less than 1.
/// 3. Trim parts that exceed <see cref="RepairRequest.MaxParts"/> (keeps earliest steps first).
///
/// Returns a new BrickGraph — the original is never mutated.
/// </summary>
public sealed class BrickGraphRepairAgent : IAgent<RepairRequest, BrickGraph>
{
    private const string FallbackColor = "light_bluish_gray";

    private readonly SupportedPartsRegistry _registry;
    private readonly ILogger<BrickGraphRepairAgent> _logger;

    public string AgentName => "BrickGraphRepairAgent";

    public BrickGraphRepairAgent(SupportedPartsRegistry registry, ILogger<BrickGraphRepairAgent> logger)
    {
        _registry = registry;
        _logger   = logger;
    }

    /// <inheritdoc />
    public Task<AgentResult<BrickGraph>> RunAsync(
        RepairRequest input,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var graph   = input.Graph;
        var jobId   = context.JobId;
        var safeColor = ResolveSafeColor(input);
        var repaired = 0;

        // ── Step 1: Repair colors and steps ──────────────────────────────────
        var repairedParts = graph.Parts
            .Select(p =>
            {
                var needsColorFix = !_registry.IsColorSupported(p.Color);
                var needsStepFix  = p.Step < 1;

                if (!needsColorFix && !needsStepFix)
                    return p;

                repaired++;

                if (needsColorFix)
                    _logger.LogInformation(
                        "[{Agent}] JobId={JobId} Part '{Id}': replacing unsupported color '{Old}' with '{New}'.",
                        AgentName, jobId, p.InstanceId, p.Color, safeColor);

                if (needsStepFix)
                    _logger.LogInformation(
                        "[{Agent}] JobId={JobId} Part '{Id}': fixing invalid step {Step} → 1.",
                        AgentName, jobId, p.InstanceId, p.Step);

                return new BrickPartInstance
                {
                    InstanceId   = p.InstanceId,
                    PartNumber   = p.PartNumber,
                    PartName     = p.PartName,
                    Color        = needsColorFix ? safeColor : p.Color,
                    Position     = p.Position,
                    Rotation     = p.Rotation,
                    Step         = needsStepFix ? 1 : p.Step
                };
            })
            .ToList();

        // ── Step 2: Trim excess parts ─────────────────────────────────────────
        if (repairedParts.Count > input.MaxParts)
        {
            var removed = repairedParts.Count - input.MaxParts;
            // Keep the parts from the lowest step numbers; remove from the end of the ordered list.
            repairedParts = repairedParts
                .OrderBy(p => p.Step)
                .ThenBy(p => p.InstanceId)
                .Take(input.MaxParts)
                .ToList();

            _logger.LogInformation(
                "[{Agent}] JobId={JobId} Trimmed {Removed} excess part(s) to stay within MaxParts={Max}.",
                AgentName, jobId, removed, input.MaxParts);
            repaired += removed;
        }

        // ── Build repaired graph ──────────────────────────────────────────────
        var result = new BrickGraph
        {
            Model = new BrickModelMetadata
            {
                Id          = graph.Model.Id,
                Name        = graph.Model.Name,
                TargetParts = graph.Model.TargetParts,
                ActualParts = repairedParts.Count
            }
        };
        foreach (var p in repairedParts)
            result.Parts.Add(p);
        foreach (var s in graph.Steps)
            result.Steps.Add(s);

        _logger.LogInformation(
            "[{Agent}] JobId={JobId} Repair complete. {Repaired} fix(es), {Total} parts remaining.",
            AgentName, jobId, repaired, result.Parts.Count);

        return Task.FromResult(AgentResult<BrickGraph>.Success(result));
    }

    private string ResolveSafeColor(RepairRequest input)
    {
        var candidate = input.Template?.DefaultMainColor;
        if (candidate is not null && _registry.IsColorSupported(candidate))
            return candidate;

        return _registry.IsColorSupported(FallbackColor) ? FallbackColor : "black";
    }
}
