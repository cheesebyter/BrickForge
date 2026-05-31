using BrickForge.Ai;
using BrickForge.Api.Health;
using BrickForge.Core.Results;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BrickForge.Api.Tests;

public sealed class OllamaHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenOllamaIsAvailable_ReturnsHealthy()
    {
        var client = new MockOllamaClient(); // always reports available
        var check = new OllamaHealthCheck(client);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOllamaIsUnavailable_ReturnsUnhealthy()
    {
        var client = new UnavailableOllamaClient();
        var check = new OllamaHealthCheck(client);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class UnavailableOllamaClient : IOllamaClient
    {
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Result<string>> GenerateAsync(string systemPrompt, string userPrompt,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<string>.Failure("Unavailable"));
    }
}

