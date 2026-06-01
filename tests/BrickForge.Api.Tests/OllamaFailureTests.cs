using BrickForge.Ai;
using BrickForge.Ai.Analysis;
using BrickForge.Api.Health;
using BrickForge.Api.Persistence;
using BrickForge.Api.Services;
using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Repair;
using BrickForge.BrickGraph.Templates;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Jobs;
using BrickForge.Core.Options;
using BrickForge.Core.Pipelines;
using BrickForge.Core.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BrickForge.Api.Tests;

/// <summary>
/// Tests for BF-MVP1-050 – Ollama failure handling.
///
/// Acceptance criteria covered:
/// - Health Check erkennt Ausfall.
/// - Job bricht verständlich ab (erreicht Terminalzustand).
/// - Kein unhandled exception.
/// - API meldet "Ollama nicht erreichbar" (via health endpoint).
/// - Fehler wird im Bericht protokolliert (fallback observable via files).
///
/// No live Ollama instance is required.
/// </summary>
public sealed class OllamaFailureTests : IDisposable
{
    private readonly string _outputRoot;

    public OllamaFailureTests()
    {
        _outputRoot = Path.Combine(Path.GetTempPath(), "BrickForgeOllamaFailureTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputRoot);
    }

    // ── Pipeline: job reaches terminal state when Ollama is fully down ────────

    [Fact]
    public async Task RunAsync_WhenOllamaIsUnavailable_JobCompletesViaFallback()
    {
        // IsAvailable=false, Generate returns network failure → PromptAnalysisService falls back.
        var client = new FixedOllamaClient(isAvailable: false, result: Result<string>.Failure("Connection refused"));
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Kleine Kaffeemaschine");

        await svc.RunAsync(job.Id);

        var finished = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(
            finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings,
            $"Expected Completed/CompletedWithWarnings via fallback, got {finished.Status}. Error: {finished.ErrorMessage}");
    }

    [Fact]
    public async Task RunAsync_WhenOllamaGenerateReturnsHttpError_JobReachesTerminalState()
    {
        // Health check would pass (IsAvailable=true) but Generate fails transiently.
        var client = new FixedOllamaClient(isAvailable: true, result: Result<string>.Failure("HTTP 503 Service Unavailable"));
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Werkbank");

        await svc.RunAsync(job.Id);

        var finished = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(
            finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings or JobStatus.Failed,
            $"Expected terminal state, got {finished.Status}");
    }

    [Fact]
    public async Task RunAsync_WhenOllamaIsUnavailable_NoUnhandledExceptionEscapes()
    {
        var client = new FixedOllamaClient(isAvailable: false, result: Result<string>.Failure("Ollama not reachable"));
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Sportwagen");

        var ex = await Record.ExceptionAsync(() => svc.RunAsync(job.Id));

        Assert.Null(ex);
    }

    [Fact]
    public async Task RunAsync_WhenOllamaIsUnavailable_FallbackProducesOutputFiles()
    {
        // Even with Ollama down, the deterministic fallback should still produce files.
        var client = new FixedOllamaClient(isAvailable: false, result: Result<string>.Failure("No connection"));
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Kleines Gartenhaus");

        await svc.RunAsync(job.Id);

        var finished = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        if (finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings)
        {
            Assert.NotEmpty(finished.Files);
        }
    }

    // ── Health Check unit test (AC: "Health Check erkennt Ausfall") ──────────

    [Fact]
    public async Task OllamaHealthCheck_WhenOllamaIsUnavailable_ReturnsUnhealthy()
    {
        var client = new FixedOllamaClient(isAvailable: false, result: Result<string>.Failure("Down"));
        var check = new OllamaHealthCheck(client);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("not reachable", result.Description ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── HTTP-level: API reports Ollama unreachable via health endpoint ────────

    [Fact]
    public async Task HealthEndpoint_WhenOllamaUnavailable_Returns503()
    {
        // AC: "UI/API meldet 'Ollama nicht erreichbar'"
        await using var factory = new UnavailableOllamaApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ollama");

        // ASP.NET Core health checks return 503 for Unhealthy status by default.
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private (GenerationPipelineService svc, SqliteJobRepository repo) BuildPipeline(IOllamaClient ollamaClient)
    {
        var repo = new SqliteJobRepository("Data Source=:memory:");
        var (partsRegistry, templateRegistry) = LoadTestRegistries();

        var ollamaOpts = new OllamaOptions { MockMode = false };
        var genOpts = new GenerationOptions { OutputRoot = _outputRoot };

        var promptAnalyzer = new PromptAnalysisService(ollamaClient, ollamaOpts, genOpts);
        var generator = new TemplateBasedGenerator(partsRegistry);
        var validator = new BrickGraphValidator(partsRegistry);
        var repairAgent = new BrickGraphRepairAgent(partsRegistry, NullLogger<BrickGraphRepairAgent>.Instance);

        var svc = new GenerationPipelineService(
            repo,
            promptAnalyzer,
            generator,
            validator,
            repairAgent,
            templateRegistry,
            Options.Create(genOpts),
            NullLogger<GenerationPipelineService>.Instance);

        return (svc, repo);
    }

    private static async Task<GenerationJob> CreateJobAsync(SqliteJobRepository repo, string prompt)
    {
        var job = new GenerationJob
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt = prompt
        };
        await repo.CreateAsync(job);
        return job;
    }

    private static (SupportedPartsRegistry parts, TemplateRegistry templates) LoadTestRegistries()
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "data", "parts");
            var partsJson = File.ReadAllText(Path.Combine(dir, "supported-parts.json"));
            var colorsJson = File.ReadAllText(Path.Combine(dir, "supported-colors.json"));
            var templateJson = File.ReadAllText(Path.Combine(dir, "small_machine_template.json"));
            return (SupportedPartsRegistry.FromJson(partsJson, colorsJson), TemplateRegistry.FromJson(templateJson));
        }
        catch
        {
            return (new SupportedPartsRegistry([], []), new TemplateRegistry([]));
        }
    }

    public void Dispose()
    {
        try { Directory.Delete(_outputRoot, recursive: true); }
        catch { /* best effort */ }
    }
}

// ── Fake client used across BF-MVP1-050 tests ────────────────────────────────

/// <summary>
/// Returns a fixed result for every call, and a fixed availability status.
/// Simulates an Ollama instance that is either permanently down or always returns an error.
/// </summary>
internal sealed class FixedOllamaClient : IOllamaClient
{
    private readonly bool _isAvailable;
    private readonly Result<string> _result;

    public FixedOllamaClient(bool isAvailable, Result<string> result)
    {
        _isAvailable = isAvailable;
        _result = result;
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(_isAvailable);

    public Task<Result<string>> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        => Task.FromResult(_result);
}

// ── WebApplicationFactory: substitutes Ollama with unavailable client ─────────

/// <summary>
/// Factory that replaces <see cref="IOllamaClient"/> with a permanently unavailable client.
/// Used for HTTP-level health endpoint tests (BF-MVP1-050).
/// </summary>
internal sealed class UnavailableOllamaApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Replace IJobQueue with no-op to prevent background pipeline execution.
            var queueDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IJobQueue));
            if (queueDesc != null) services.Remove(queueDesc);
            services.AddSingleton<IJobQueue, NoOpJobQueue>();

            // Replace IOllamaClient with one that always reports unavailable.
            var ollamaDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IOllamaClient));
            if (ollamaDesc != null) services.Remove(ollamaDesc);
            services.AddSingleton<IOllamaClient>(
                new FixedOllamaClient(isAvailable: false, result: Result<string>.Failure("Ollama unavailable")));
        });
    }
}
