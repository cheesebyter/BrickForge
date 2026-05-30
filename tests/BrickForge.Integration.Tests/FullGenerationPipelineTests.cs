using BrickForge.Ai;
using BrickForge.Ai.Analysis;
using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Options;
using BrickForge.Core.Results;
using BrickForge.Export;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Integration.Tests;

/// <summary>
/// End-to-end integration tests for the full MVP0 generation pipeline.
/// Uses a mock Ollama client — no live Ollama instance is required.
/// Implements BF-MVP0-019.
/// </summary>
public sealed class FullGenerationPipelineTests : IDisposable
{
    private readonly string _tempDir;

    public FullGenerationPipelineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "brickforge_e2e_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Golden sample: Kaffeemaschine ─────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_KaffeemaschineGoldenSample_ProducesAllSixOutputFiles()
    {
        var (outputDir, _) = await RunPipelineAsync(KaffeemaschinePrompt, KaffeemaschineAnalysisJson);

        Assert.True(File.Exists(Path.Combine(outputDir, "brickgraph.json")), "brickgraph.json missing");
        Assert.True(File.Exists(Path.Combine(outputDir, "validation.json")), "validation.json missing");
        Assert.True(File.Exists(Path.Combine(outputDir, "model.mpd")),       "model.mpd missing");
        Assert.True(File.Exists(Path.Combine(outputDir, "parts.csv")),       "parts.csv missing");
        Assert.True(File.Exists(Path.Combine(outputDir, "instructions.md")), "instructions.md missing");
        Assert.True(File.Exists(Path.Combine(outputDir, "report.md")),       "report.md missing");
    }

    [Fact]
    public async Task FullPipeline_KaffeemaschineGoldenSample_ValidationPasses()
    {
        var (outputDir, _) = await RunPipelineAsync(KaffeemaschinePrompt, KaffeemaschineAnalysisJson);

        var validationJson = await File.ReadAllTextAsync(Path.Combine(outputDir, "validation.json"));
        var validation = ValidationResult.FromJson(validationJson);

        Assert.NotNull(validation);
        Assert.True(validation.Valid, $"Validation should pass. Issues: {string.Join("; ", validation.Issues.Select(i => i.Code))}");
    }

    [Fact]
    public async Task FullPipeline_KaffeemaschineGoldenSample_PartCountInExpectedRange()
    {
        var (outputDir, _) = await RunPipelineAsync(KaffeemaschinePrompt, KaffeemaschineAnalysisJson);

        var graphJson = await File.ReadAllTextAsync(Path.Combine(outputDir, "brickgraph.json"));
        var graph = Graph.FromJson(graphJson);

        Assert.NotNull(graph);
        Assert.InRange(graph.Parts.Count, 20, 80);
    }

    [Fact]
    public async Task FullPipeline_KaffeemaschineGoldenSample_InstructionsContainDisclaimer()
    {
        var (outputDir, _) = await RunPipelineAsync(KaffeemaschinePrompt, KaffeemaschineAnalysisJson);

        var instructions = await File.ReadAllTextAsync(Path.Combine(outputDir, "instructions.md"));

        Assert.Contains("nicht um eine offizielle LEGO", instructions);
    }

    [Fact]
    public async Task FullPipeline_KaffeemaschineGoldenSample_ReportContainsDisclaimer()
    {
        var (outputDir, _) = await RunPipelineAsync(KaffeemaschinePrompt, KaffeemaschineAnalysisJson);

        var report = await File.ReadAllTextAsync(Path.Combine(outputDir, "report.md"));

        Assert.Contains("nicht um eine offizielle LEGO", report);
    }

    [Fact]
    public async Task FullPipeline_KaffeemaschineGoldenSample_MpdContainsPartLines()
    {
        var (outputDir, _) = await RunPipelineAsync(KaffeemaschinePrompt, KaffeemaschineAnalysisJson);

        var mpd = await File.ReadAllTextAsync(Path.Combine(outputDir, "model.mpd"));

        Assert.Contains("1 ", mpd);      // at least one part line
        Assert.Contains("0 STEP", mpd);  // at least one step marker
    }

    [Fact]
    public async Task FullPipeline_KaffeemaschineGoldenSample_CsvContainsHeader()
    {
        var (outputDir, _) = await RunPipelineAsync(KaffeemaschinePrompt, KaffeemaschineAnalysisJson);

        var csv = await File.ReadAllTextAsync(Path.Combine(outputDir, "parts.csv"));

        Assert.StartsWith("quantity,part_number,part_name,color", csv);
    }

    [Fact]
    public async Task FullPipeline_KaffeemaschineGoldenSample_BrickGraphJsonIsValid()
    {
        var (outputDir, _) = await RunPipelineAsync(KaffeemaschinePrompt, KaffeemaschineAnalysisJson);

        var json = await File.ReadAllTextAsync(Path.Combine(outputDir, "brickgraph.json"));
        var graph = Graph.FromJson(json);

        Assert.NotNull(graph);
        Assert.Equal(graph.Parts.Count, graph.Model.ActualParts);
    }

    // ── Mock mode tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_WithMockOllamaClient_CompletesSuccessfully()
    {
        var mockClient = new MockOllamaClient();
        var (outputDir, _) = await RunPipelineWithClientAsync(KaffeemaschinePrompt, mockClient);

        Assert.True(File.Exists(Path.Combine(outputDir, "brickgraph.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, "model.mpd")));
    }

    // ── Infeasible prompt ─────────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_InfeasiblePrompt_ReturnsFailureResult()
    {
        const string infeasibleJson = """
            {
              "model_name": "Ultra Complex Robot",
              "model_category": "small_machine",
              "target_parts": 50,
              "main_color": "black",
              "accent_color": "light_bluish_gray",
              "features": [],
              "feasible": false,
              "warnings": ["Too complex for MVP0"]
            }
            """;

        var (_, success) = await RunPipelineAsync("build an ultra complex robot", infeasibleJson);

        Assert.False(success, "Pipeline should fail for infeasible prompt");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(string OutputDir, bool Success)> RunPipelineAsync(
        string prompt,
        string aiResponseJson)
    {
        var fakeClient = new FixedResponseOllamaClient(Result<string>.Success(aiResponseJson));
        return await RunPipelineWithClientAsync(prompt, fakeClient);
    }

    private async Task<(string OutputDir, bool Success)> RunPipelineWithClientAsync(
        string prompt,
        IOllamaClient ollamaClient)
    {
        var genOptions = new GenerationOptions
        {
            MaxParts = 80,
            DefaultTargetParts = 50,
            OutputRoot = _tempDir
        };

        var ollamaOptions = new OllamaOptions { Model = "test-model", Temperature = 0.2 };

        var promptAnalyzer = new PromptAnalysisService(ollamaClient, ollamaOptions, genOptions);
        var registry = SupportedPartsRegistry.FromJson(SupportedPartsJson, SupportedColorsJson);
        var templateRegistry = TemplateRegistry.FromJson(SmallMachineTemplateJson);
        var generator = new SmallMachineGenerator(registry);
        var validator = new BrickGraphValidator(registry);

        // Capture output directory before running (we use a single output root, list dirs after)
        var existingDirs = Directory.Exists(_tempDir)
            ? new HashSet<string>(Directory.GetDirectories(_tempDir))
            : [];

        var exitCode = await GeneratePipeline.RunAsync(
            prompt,
            promptAnalyzer,
            generator,
            validator,
            templateRegistry,
            genOptions,
            ollamaOptions.Model);

        var success = exitCode == 0;

        // Find the newly created job directory
        var newDirs = Directory.Exists(_tempDir)
            ? Directory.GetDirectories(_tempDir).Except(existingDirs).ToArray()
            : [];

        var outputDir = newDirs.Length == 1 ? newDirs[0] : _tempDir;

        return (outputDir, success);
    }

    // ── Test data ─────────────────────────────────────────────────────────────

    private const string KaffeemaschinePrompt =
        "Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel und einer Tasse. Das Modell soll einfach und stabil sein.";

    private const string KaffeemaschineAnalysisJson = """
        {
          "model_name": "Kleine Kaffeemaschine",
          "model_category": "small_machine",
          "target_parts": 50,
          "main_color": "black",
          "accent_color": "light_bluish_gray",
          "features": ["cup", "front_panel"],
          "feasible": true,
          "warnings": []
        }
        """;

    private const string SupportedPartsJson = """
        [
          { "part_number": "3005", "part_name": "Brick 1 x 1" },
          { "part_number": "3004", "part_name": "Brick 1 x 2" },
          { "part_number": "3622", "part_name": "Brick 1 x 3" },
          { "part_number": "3010", "part_name": "Brick 1 x 4" },
          { "part_number": "3003", "part_name": "Brick 2 x 2" },
          { "part_number": "3002", "part_name": "Brick 2 x 3" },
          { "part_number": "3001", "part_name": "Brick 2 x 4" },
          { "part_number": "3024", "part_name": "Plate 1 x 1" },
          { "part_number": "3023", "part_name": "Plate 1 x 2" },
          { "part_number": "3710", "part_name": "Plate 1 x 4" },
          { "part_number": "3022", "part_name": "Plate 2 x 2" },
          { "part_number": "3020", "part_name": "Plate 2 x 4" },
          { "part_number": "3069b", "part_name": "Tile 1 x 2 with Groove" },
          { "part_number": "2431", "part_name": "Tile 1 x 4 with Groove" }
        ]
        """;

    private const string SupportedColorsJson = """
        ["black","white","red","blue","yellow","light_bluish_gray","dark_bluish_gray","transparent_clear"]
        """;

    private const string SmallMachineTemplateJson = """
        {
          "template_id": "small_machine",
          "display_name": "Small Machine",
          "width_studs": 6,
          "depth_studs": 4,
          "height_layers": 4,
          "default_main_color": "black",
          "default_accent_color": "light_bluish_gray",
          "subassemblies": [
            { "name": "base",          "preferred_part": "3020",  "budget_fraction": 0.20 },
            { "name": "main_body",     "preferred_part": "3001",  "budget_fraction": 0.45 },
            { "name": "front_panel",   "preferred_part": "3069b", "budget_fraction": 0.20 },
            { "name": "top",           "preferred_part": "3020",  "budget_fraction": 0.10 },
            { "name": "simple_detail", "preferred_part": "3024",  "budget_fraction": 0.05 }
          ]
        }
        """;
}

// ── Test-only fake Ollama client ──────────────────────────────────────────────

internal sealed class FixedResponseOllamaClient : IOllamaClient
{
    private readonly Result<string> _response;

    public FixedResponseOllamaClient(Result<string> response) => _response = response;

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<Result<string>> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_response);
}

// ── Internal pipeline helper (wraps GenerateCommand logic for testing) ────────

/// <summary>
/// Thin helper that wires the pipeline components together for integration tests.
/// Mirrors the logic in <c>GenerateCommand.RunAsync</c> without CLI console output concerns.
/// </summary>
internal static class GeneratePipeline
{
    internal static async Task<int> RunAsync(
        string prompt,
        IPromptAnalyzer promptAnalyzer,
        SmallMachineGenerator generator,
        BrickGraphValidator validator,
        TemplateRegistry templateRegistry,
        GenerationOptions generationOptions,
        string modelName)
    {
        var analysisResult = await promptAnalyzer.AnalyzeAsync(prompt);
        if (!analysisResult.IsSuccess || analysisResult.Value is null) return 1;

        var analysis = analysisResult.Value;
        if (!analysis.Feasible) return 1;

        var template = templateRegistry.FindTemplate(analysis.ModelCategory)
                       ?? templateRegistry.FindTemplate("small_machine");
        if (template is null) return 1;

        var graph = generator.Generate(analysis, template);
        var validation = validator.Validate(graph);
        if (!validation.Valid) return 1;

        var jobId = Guid.NewGuid().ToString("N");
        var outputDir = Path.Combine(generationOptions.OutputRoot, jobId);
        Directory.CreateDirectory(outputDir);

        await File.WriteAllTextAsync(Path.Combine(outputDir, "brickgraph.json"), graph.ToJson());
        await File.WriteAllTextAsync(Path.Combine(outputDir, "validation.json"), validation.ToJson());

        var generatedFiles = new List<string> { "brickgraph.json", "validation.json" };

        var ldraw = new LDrawExporter().Export(graph);
        if (ldraw.Success)
        {
            await File.WriteAllTextAsync(Path.Combine(outputDir, "model.mpd"), ldraw.Content!);
            generatedFiles.Add("model.mpd");
        }

        var csv = new CsvPartsExporter().Export(graph);
        if (csv.Success)
        {
            await File.WriteAllTextAsync(Path.Combine(outputDir, "parts.csv"), csv.Content!);
            generatedFiles.Add("parts.csv");
        }

        var md = new MarkdownInstructionsExporter().Export(graph);
        if (md.Success)
        {
            await File.WriteAllTextAsync(Path.Combine(outputDir, "instructions.md"), md.Content!);
            generatedFiles.Add("instructions.md");
        }

        var reportData = new GenerationReportData
        {
            OriginalPrompt = prompt,
            AiModelName = analysis.UsedFallback ? null : modelName,
            AnalysisResult = analysis,
            ValidationResult = validation,
            GeneratedFiles = generatedFiles.AsReadOnly(),
            Timestamp = DateTimeOffset.UtcNow
        };

        var report = new ReportExporter().Export(graph, reportData);
        if (report.Success)
        {
            await File.WriteAllTextAsync(Path.Combine(outputDir, "report.md"), report.Content!);
            generatedFiles.Add("report.md");
        }

        return 0;
    }
}
