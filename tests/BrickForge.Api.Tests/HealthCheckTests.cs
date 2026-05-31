using BrickForge.Api.Health;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.Core.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BrickForge.Api.Tests;

/// <summary>
/// Tests for BF-MVP1-045 – Health Checks.
///
/// Acceptance criteria:
/// - Health check detects missing Ollama.
/// - Health check detects database issues.
/// - Agent status is reported.
/// - UI/API can react meaningfully on errors.
/// </summary>
public sealed class HealthCheckTests
{
    // ── DatabaseHealthCheck ───────────────────────────────────────────────────

    [Fact]
    public async Task DatabaseHealthCheck_WithInMemoryDatabase_ReturnsHealthy()
    {
        var opts = Options.Create(new StorageOptions { ConnectionString = "Data Source=:memory:" });
        var check = new DatabaseHealthCheck(opts);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task DatabaseHealthCheck_WithInvalidConnectionString_ReturnsUnhealthy()
    {
        var opts = Options.Create(new StorageOptions
        {
            ConnectionString = "Data Source=/nonexistent_path/brickforge_test.db;Mode=ReadOnly"
        });
        var check = new DatabaseHealthCheck(opts);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // A missing read-only file causes a connection failure.
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }

    // ── AgentsHealthCheck ─────────────────────────────────────────────────────

    [Fact]
    public async Task AgentsHealthCheck_WhenPartsAndTemplatesLoaded_ReturnsHealthy()
    {
        var (partsRegistry, templateRegistry) = BuildLoadedRegistries();
        var check = new AgentsHealthCheck(partsRegistry, templateRegistry);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Parts:", result.Description ?? string.Empty);
        Assert.Contains("Templates:", result.Description ?? string.Empty);
    }

    [Fact]
    public async Task AgentsHealthCheck_WhenNoPartsLoaded_ReturnsDegraded()
    {
        var emptyParts = new SupportedPartsRegistry([], []);
        var (_, templateRegistry) = BuildLoadedRegistries();
        var check = new AgentsHealthCheck(emptyParts, templateRegistry);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("parts", result.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AgentsHealthCheck_WhenNoTemplatesLoaded_ReturnsDegraded()
    {
        var (partsRegistry, _) = BuildLoadedRegistries();
        var emptyTemplates = new TemplateRegistry([]);
        var check = new AgentsHealthCheck(partsRegistry, emptyTemplates);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("template", result.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── OllamaHealthCheck – unchanged, covered by OllamaHealthCheckTests ──────
    // The acceptance criterion "Health Check erkennt fehlendes Ollama" is already
    // covered by OllamaHealthCheckTests.CheckHealthAsync_WhenOllamaIsUnavailable_ReturnsUnhealthy.

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (SupportedPartsRegistry parts, TemplateRegistry templates) BuildLoadedRegistries()
    {
        const string partsJson = """
            [
              { "part_number": "3001", "part_name": "Brick 2 x 4" },
              { "part_number": "3003", "part_name": "Brick 2 x 2" }
            ]
            """;
        const string colorsJson = """["black","white","light_bluish_gray"]""";
        const string templateJson = """
            {
              "template_id": "small_machine",
              "display_name": "Small Machine",
              "width_studs": 6,
              "depth_studs": 4,
              "height_layers": 4,
              "default_main_color": "black",
              "default_accent_color": "light_bluish_gray",
              "subassemblies": []
            }
            """;

        return (
            SupportedPartsRegistry.FromJson(partsJson, colorsJson),
            TemplateRegistry.FromJson(templateJson)
        );
    }
}
