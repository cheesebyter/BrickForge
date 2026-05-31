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

/// <summary>
/// End-to-end workflow tests implementing BF-MVP1-049.
///
/// Acceptance criteria:
/// - Workflow runs for at least one Golden Sample Prompt.
/// - Job ends with <c>Completed</c> or <c>CompletedWithWarnings</c>.
/// - <c>model.mpd</c>, <c>parts.csv</c>, <c>instructions.md</c>, <c>report.md</c> are created.
/// - ValidationScore >= 0.70 or warning status is cleanly set.
///
/// Uses MockOllamaClient — no live Ollama instance required.
/// </summary>
public sealed class EndToEndWorkflowTests : IDisposable
{
    private readonly string _outputRoot;
    private readonly SqliteJobRepository _repo;
    private readonly GenerationPipelineService _sut;

    // BF-MVP1-048 golden sample: primary prompt.
    private const string KaffeemaschinePrompt =
        "Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel und einer Tasse. Das Modell soll einfach und stabil sein.";

    public EndToEndWorkflowTests()
    {
        _outputRoot = Path.Combine(Path.GetTempPath(), "BrickForge_E2E_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputRoot);

        _repo = new SqliteJobRepository("Data Source=:memory:");

        var (partsRegistry, templateRegistry) = LoadTestRegistries();

        var genOpts = new GenerationOptions
        {
            OutputRoot = _outputRoot,
            MaxParts = 300,
            DefaultTargetParts = 50,
            MaxPromptLength = 2000
        };
        var ollamaOpts = new OllamaOptions { MockMode = true };

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

    // ── §49.1 Full workflow for Kaffeemaschine golden sample ──────────────────

    [Fact]
    public async Task EndToEnd_KaffeemaschineGoldenSample_JobReachesCompletedStatus()
    {
        var job = CreateJob(KaffeemaschinePrompt);
        await _repo.CreateAsync(job);

        await _sut.RunAsync(job.Id, CancellationToken.None);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(
            finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings,
            $"Expected Completed or CompletedWithWarnings, got {finished.Status}. Error: {finished.ErrorMessage}");
    }

    [Fact]
    public async Task EndToEnd_KaffeemaschineGoldenSample_AllRequiredFilesCreated()
    {
        var job = CreateJob(KaffeemaschinePrompt);
        await _repo.CreateAsync(job);

        await _sut.RunAsync(job.Id, CancellationToken.None);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings,
            $"Job failed: {finished.ErrorMessage}");

        var outputDir = Path.Combine(_outputRoot, job.Id);
        Assert.True(File.Exists(Path.Combine(outputDir, "model.mpd")),       "model.mpd missing");
        Assert.True(File.Exists(Path.Combine(outputDir, "parts.csv")),       "parts.csv missing");
        Assert.True(File.Exists(Path.Combine(outputDir, "instructions.md")), "instructions.md missing");
        Assert.True(File.Exists(Path.Combine(outputDir, "report.md")),       "report.md missing");
    }

    [Fact]
    public async Task EndToEnd_KaffeemaschineGoldenSample_ValidationScoreMeetsThreshold()
    {
        var job = CreateJob(KaffeemaschinePrompt);
        await _repo.CreateAsync(job);

        await _sut.RunAsync(job.Id, CancellationToken.None);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings,
            $"Job failed: {finished.ErrorMessage}");

        // §49 Acceptance: ValidationScore >= 0.70 OR job is CompletedWithWarnings (clear warning state).
        if (finished.Status == JobStatus.CompletedWithWarnings)
        {
            // Warning state is acceptable per spec.
            return;
        }

        Assert.True(finished.ValidationScore >= 0.70,
            $"ValidationScore {finished.ValidationScore:F4} is below the 0.70 threshold");
    }

    [Fact]
    public async Task EndToEnd_KaffeemaschineGoldenSample_InstructionsContainDisclaimer()
    {
        var job = CreateJob(KaffeemaschinePrompt);
        await _repo.CreateAsync(job);

        await _sut.RunAsync(job.Id, CancellationToken.None);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings);

        var instructions = await File.ReadAllTextAsync(
            Path.Combine(_outputRoot, job.Id, "instructions.md"));

        Assert.Contains("nicht um eine offizielle LEGO", instructions);
    }

    [Fact]
    public async Task EndToEnd_KaffeemaschineGoldenSample_JobRecordsActualPartCount()
    {
        var job = CreateJob(KaffeemaschinePrompt);
        await _repo.CreateAsync(job);

        await _sut.RunAsync(job.Id, CancellationToken.None);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings);
        Assert.True(finished.ActualParts > 0, "ActualParts must be set after a successful run");
    }

    // ── §49.2 Multiple golden samples ─────────────────────────────────────────

    [Theory]
    [InlineData("Baue ein kleines Gartenhaus mit rotem Dach und weißen Wänden.")]
    [InlineData("Erstelle eine einfache graue Werkbank als Brick-Modell.")]
    [InlineData("Erstelle einen kleinen roten Sportwagen als Brick-Modell.")]
    [InlineData("Erstelle einen gelben Verkaufsstand als Brick-Modell.")]
    public async Task EndToEnd_GoldenSamplePrompts_ReachTerminalStatus(string prompt)
    {
        var job = CreateJob(prompt);
        await _repo.CreateAsync(job);

        await _sut.RunAsync(job.Id, CancellationToken.None);

        var finished = await _repo.GetByIdAsync(job.Id);
        Assert.NotNull(finished);
        Assert.True(
            finished.Status is JobStatus.Completed or JobStatus.CompletedWithWarnings or JobStatus.Failed,
            $"Job must reach a terminal state, got {finished.Status}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GenerationJob CreateJob(string prompt) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        CreatedAt = DateTimeOffset.UtcNow,
        Prompt = prompt
    };

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

    public void Dispose()
    {
        try { Directory.Delete(_outputRoot, recursive: true); }
        catch { /* best effort */ }
    }
}
