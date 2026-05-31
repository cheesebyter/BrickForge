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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BrickForge.Api.Tests;

public sealed class GenerationPipelineServiceTests : IDisposable
{
    private readonly string _outputRoot;
    private readonly SqliteJobRepository _repo;
    private readonly GenerationPipelineService _sut;

    public GenerationPipelineServiceTests()
    {
        _outputRoot = Path.Combine(Path.GetTempPath(), "BrickForgeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputRoot);

        _repo = new SqliteJobRepository("Data Source=:memory:");

        var (partsRegistry, templateRegistry) = LoadTestRegistries();

        var ollamaOpts = new OllamaOptions { MockMode = true };
        var genOpts = new GenerationOptions { OutputRoot = _outputRoot };

        var ollamaClient = new MockOllamaClient();
        var promptAnalyzer = new PromptAnalysisService(ollamaClient, ollamaOpts, genOpts);
        var generator = new TemplateBasedGenerator(partsRegistry);
        var validator = new BrickGraphValidator(partsRegistry);
        var repairAgent = new BrickGraphRepairAgent(partsRegistry, NullLogger<BrickGraphRepairAgent>.Instance);

        _sut = new GenerationPipelineService(
            _repo,
            promptAnalyzer,
            generator,
            validator,
            repairAgent,
            templateRegistry,
            Options.Create(genOpts),
            NullLogger<GenerationPipelineService>.Instance);
    }

    [Fact]
    public async Task RunAsync_WithMockAi_CompletesJobAndCreatesOutputFiles()
    {
        var job = new GenerationJob
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt = "Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel."
        };
        await _repo.CreateAsync(job);

        await _sut.RunAsync(job.Id, CancellationToken.None);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings,
            $"Expected Completed/CompletedWithWarnings but got {finished.Status}");
        Assert.NotEmpty(finished.Files);
    }

    [Fact]
    public async Task RunAsync_WithUnknownJobId_DoesNotThrow()
    {
        var ex = await Record.ExceptionAsync(() =>
            _sut.RunAsync("nonexistent-job-id", CancellationToken.None));
        Assert.Null(ex);
    }

    [Fact]
    public async Task RunAsync_SetsJobStatusToFailedOrCompletedWithoutThrowing_ForAnyPrompt()
    {
        var job = new GenerationJob
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt = string.Empty
        };
        await _repo.CreateAsync(job);

        var ex = await Record.ExceptionAsync(() =>
            _sut.RunAsync(job.Id, CancellationToken.None));

        Assert.Null(ex);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(finished.Status is JobStatus.Failed or JobStatus.Completed or JobStatus.CompletedWithWarnings,
            $"Job must reach a terminal state, got {finished.Status}");
    }

    [Fact]
    public async Task RunAsync_WhenPromptExceedsMaxLength_FailsJobWithClearMessage()
    {
        // BF-MVP1-019 §19.5: oversized prompts must be rejected gracefully.
        var oversizedPrompt = new string('x', 2001);
        var job = new GenerationJob
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt = oversizedPrompt
        };
        await _repo.CreateAsync(job);

        await _sut.RunAsync(job.Id, CancellationToken.None);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.Equal(JobStatus.Failed, finished.Status);
        Assert.NotNull(finished.ErrorMessage);
        Assert.Contains("maximum length", finished.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_WhenPromptIsAtMaxLength_DoesNotFailOnLengthCheck()
    {
        // Exactly at the limit (2000 chars) should not trigger the length guard.
        var exactPrompt = new string('x', 2000);
        var job = new GenerationJob
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt = exactPrompt
        };
        await _repo.CreateAsync(job);

        await _sut.RunAsync(job.Id, CancellationToken.None);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.NotEqual(JobStatus.Failed, finished.Status);
    }

    public void Dispose()
    {
        try { Directory.Delete(_outputRoot, recursive: true); }
        catch { /* best effort */ }
    }

    private static (SupportedPartsRegistry parts, TemplateRegistry templates) LoadTestRegistries()
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "data", "parts");
            var partsJson = File.ReadAllText(Path.Combine(dir, "supported-parts.json"));
            var colorsJson = File.ReadAllText(Path.Combine(dir, "supported-colors.json"));
            var templateJson = File.ReadAllText(Path.Combine(dir, "small_machine_template.json"));
            return (SupportedPartsRegistry.FromJson(partsJson, colorsJson),
                    TemplateRegistry.FromJson(templateJson));
        }
        catch
        {
            return (new SupportedPartsRegistry([], []), new TemplateRegistry([]));
        }
    }
}

