using BrickForge.Ai;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BrickForge.Api.Health;

public sealed class OllamaHealthCheck : IHealthCheck
{
    private readonly IOllamaClient _client;

    public OllamaHealthCheck(IOllamaClient client) => _client = client;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var available = await _client.IsAvailableAsync(cancellationToken);
        return available
            ? HealthCheckResult.Healthy("Ollama is reachable.")
            : HealthCheckResult.Unhealthy("Ollama is not reachable.");
    }
}
