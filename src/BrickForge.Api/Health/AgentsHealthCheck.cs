using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BrickForge.Api.Health;

/// <summary>
/// Verifies that generation agents are operational by checking that the
/// supported-parts catalog and template registry are loaded and non-empty.
/// Implements BF-MVP1-045.
/// </summary>
public sealed class AgentsHealthCheck : IHealthCheck
{
    private readonly SupportedPartsRegistry _partsRegistry;
    private readonly TemplateRegistry _templateRegistry;

    public AgentsHealthCheck(SupportedPartsRegistry partsRegistry, TemplateRegistry templateRegistry)
    {
        _partsRegistry = partsRegistry;
        _templateRegistry = templateRegistry;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var partCount = _partsRegistry.SupportedPartNumbers.Count();
        var templateCount = _templateRegistry.TemplateIds.Count();

        if (partCount == 0)
            return Task.FromResult(HealthCheckResult.Degraded(
                "No supported parts loaded. Generation agents are not fully operational."));

        if (templateCount == 0)
            return Task.FromResult(HealthCheckResult.Degraded(
                "No templates loaded. Generation agents are not fully operational."));

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Generation agents ready. Parts: {partCount}, Templates: {templateCount}."));
    }
}
