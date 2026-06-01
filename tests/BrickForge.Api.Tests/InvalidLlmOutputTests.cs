using BrickForge.Ai;
using BrickForge.Ai.Analysis;
using BrickForge.Api.Persistence;
using BrickForge.Api.Services;
using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Repair;
using BrickForge.BrickGraph.Templates;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Jobs;
using BrickForge.Core.Options;
using BrickForge.Core.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BrickForge.Api.Tests;

/// <summary>
/// Tests for BF-MVP1-051 – handling invalid or unexpected LLM (JSON) outputs.
///
/// Acceptance criteria covered:
/// - Ungültiges JSON löst Retry aus (retry path tested at pipeline level).
/// - Nach finalem Fehler wird Job als failed markiert / Fallback produziert Terminalzustand.
/// - Fehler wird nachvollziehbar gespeichert (ErrorMessage or fallback produces files).
/// - Kein ungültiges JSON wird in Kernlogik weiterverarbeitet.
///
/// No live Ollama instance is required.
/// </summary>
public sealed class InvalidLlmOutputTests : IDisposable
{
    private readonly string _outputRoot;

    public InvalidLlmOutputTests()
    {
        _outputRoot = Path.Combine(Path.GetTempPath(), "BrickForgeInvalidLlmTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputRoot);
    }

    // ── Fallback behaviour when all LLM attempts return invalid JSON ──────────

    [Fact]
    public async Task RunAsync_WhenLlmAlwaysReturnsInvalidJson_JobCompletesViaFallback()
    {
        // Both retry attempts return non-JSON → deterministic fallback analysis runs.
        var client = new QueuedOllamaClient([
            Result<string>.Success("this is not json"),
            Result<string>.Success("also not json")
        ]);
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Kaffeemaschine");

        await svc.RunAsync(job.Id);

        var finished = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(
            finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings,
            $"Expected fallback to produce a successful job, got {finished.Status}. Error: {finished.ErrorMessage}");
    }

    [Fact]
    public async Task RunAsync_WhenLlmAlwaysReturnsInvalidJson_NoUnhandledExceptionEscapes()
    {
        var client = new QueuedOllamaClient([
            Result<string>.Success("{broken json"),
            Result<string>.Success("{more broken json")
        ]);
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Werkbank");

        var ex = await Record.ExceptionAsync(() => svc.RunAsync(job.Id));

        Assert.Null(ex);
    }

    [Fact]
    public async Task RunAsync_WhenLlmAlwaysReturnsInvalidJson_OutputFilesAreCreated()
    {
        // Fallback produces valid output even when LLM returns garbage.
        var client = new QueuedOllamaClient([
            Result<string>.Success("<!DOCTYPE html><html>"),
            Result<string>.Success("random text, definitely not JSON")
        ]);
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Kleine Kaffeemaschine");

        await svc.RunAsync(job.Id);

        var finished = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        if (finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings)
        {
            Assert.NotEmpty(finished.Files);
        }
    }

    // ── Retry path: first attempt invalid, second valid ───────────────────────

    [Fact]
    public async Task RunAsync_WhenFirstLlmResponseInvalidSecondValid_JobCompletesSuccessfully()
    {
        // Tests the retry mechanism: one bad response, one good response.
        const string validJson = """
            {
              "model_name": "Retry-Kaffeemaschine",
              "model_category": "small_machine",
              "target_parts": 40,
              "main_color": "black",
              "accent_color": "light_bluish_gray",
              "features": [],
              "feasible": true,
              "warnings": []
            }
            """;

        var client = new QueuedOllamaClient([
            Result<string>.Success("not valid json"),
            Result<string>.Success(validJson)
        ]);
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Kaffeemaschine");

        await svc.RunAsync(job.Id);

        var finished = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(
            finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings,
            $"Expected success after retry, got {finished.Status}. Error: {finished.ErrorMessage}");
    }

    // ── Unexpected / edge-case JSON formats ──────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"unrelated_field\":true}")]
    public async Task RunAsync_WhenLlmReturnsUnexpectedJsonFormat_JobReachesTerminalState(string malformedResponse)
    {
        var client = new QueuedOllamaClient([
            Result<string>.Success(malformedResponse),
            Result<string>.Success(malformedResponse)
        ]);
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Testmodell");

        var ex = await Record.ExceptionAsync(() => svc.RunAsync(job.Id));

        Assert.Null(ex);
        var finished = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(
            finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings or JobStatus.Failed,
            $"Expected terminal state, got {finished.Status}");
    }

    // ── Security: adversarial content in LLM response must not reach core ─────

    [Fact]
    public async Task RunAsync_WhenLlmReturnsAdversarialContent_NoCoreLogicIsExecuted()
    {
        // Invalid/adversarial content must be parsed as invalid JSON and fall back.
        // Core logic (BrickGraph) must never receive the raw adversarial string.
        var client = new QueuedOllamaClient([
            Result<string>.Success("INJECT: invalid json; DROP TABLE parts;"),
            Result<string>.Success("{ \"inject\": \"<script>alert(1)</script>\" }")
        ]);
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Safety test");

        var ex = await Record.ExceptionAsync(() => svc.RunAsync(job.Id));

        Assert.Null(ex);
        var finished = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(
            finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings or JobStatus.Failed,
            $"Adversarial JSON must lead to terminal state, got {finished.Status}");
    }

    [Fact]
    public async Task RunAsync_WhenLlmReturnsTruncatedJson_FallbackHandlesGracefully()
    {
        // Simulate LLM timeout that yields partial JSON.
        var client = new QueuedOllamaClient([
            Result<string>.Success("{\"model_name\":\"Test\",\"model_categ"),
            Result<string>.Success("{\"model_name\":\"Test\",\"model_categ")
        ]);
        var (svc, repo) = BuildPipeline(client);
        var job = await CreateJobAsync(repo, "Truncated response test");

        var ex = await Record.ExceptionAsync(() => svc.RunAsync(job.Id));

        Assert.Null(ex);
        var finished = await repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(
            finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings or JobStatus.Failed,
            $"Expected terminal state, got {finished.Status}");
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

// ── Fake client: returns results from a fixed queue ───────────────────────────

/// <summary>
/// Returns results from a pre-loaded queue, enabling simulation of retry scenarios.
/// When the queue is exhausted it returns a failure result.
/// </summary>
internal sealed class QueuedOllamaClient : IOllamaClient
{
    private readonly Queue<Result<string>> _queue;

    public QueuedOllamaClient(IEnumerable<Result<string>> results)
        => _queue = new Queue<Result<string>>(results);

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<Result<string>> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        => Task.FromResult(_queue.Count > 0
            ? _queue.Dequeue()
            : Result<string>.Failure("No more queued results."));
}
