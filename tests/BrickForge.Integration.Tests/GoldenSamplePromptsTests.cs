using BrickForge.Ai;
using BrickForge.Ai.Analysis;
using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Options;
using BrickForge.Core.Results;
using System.Text.Json;
using System.Text.Json.Serialization;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Integration.Tests;

/// <summary>
/// Tests for BF-MVP1-048 – Golden Sample Prompts.
///
/// Acceptance criteria:
/// - Each prompt exists as a physical file.
/// - Expected properties are defined per file.
/// - Tests can automatically run each prompt through the pipeline.
/// - Results are validated against the acceptance criteria defined in each file.
///
/// No live Ollama instance required — mock AI responses are included in each file.
/// </summary>
public sealed class GoldenSamplePromptsTests : IDisposable
{
    private readonly string _tempDir;

    public GoldenSamplePromptsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "brickforge_golden_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Load and verify file presence ─────────────────────────────────────────

    [Theory]
    [InlineData("kaffeemaschine")]
    [InlineData("gartenhaus")]
    [InlineData("werkbank")]
    [InlineData("sportwagen")]
    [InlineData("verkaufsstand")]
    public void GoldenSampleFile_Exists_AndContainsRequiredFields(string sampleId)
    {
        var path = ResolveSamplePath(sampleId);
        Assert.True(File.Exists(path), $"Golden sample file not found: {path}");

        var sample = LoadSample(path);
        Assert.False(string.IsNullOrWhiteSpace(sample.Prompt),
            $"{sampleId}: prompt must not be empty");
        Assert.NotNull(sample.Expected);
        Assert.NotEqual(System.Text.Json.JsonValueKind.Undefined, sample.MockAiResponse.ValueKind);
        Assert.True(sample.Expected.MinParts > 0,
            $"{sampleId}: min_parts must be > 0");
        Assert.True(sample.Expected.MaxParts > sample.Expected.MinParts,
            $"{sampleId}: max_parts must be > min_parts");
        Assert.NotEmpty(sample.Expected.RequiredFiles);
        Assert.True(sample.Expected.MinValidationScore is >= 0 and <= 1.0,
            $"{sampleId}: min_validation_score must be in [0, 1]");
    }

    // ── Run pipeline for each golden sample ───────────────────────────────────

    [Theory]
    [InlineData("kaffeemaschine")]
    [InlineData("gartenhaus")]
    [InlineData("werkbank")]
    [InlineData("sportwagen")]
    [InlineData("verkaufsstand")]
    public async Task GoldenSample_WhenRunThroughPipeline_SucceedsAndCreatesRequiredFiles(string sampleId)
    {
        var sample = LoadSample(ResolveSamplePath(sampleId));
        var aiResponseJson = sample.MockAiResponse.GetRawText();

        var (outputDir, success) = await RunPipelineAsync(sample.Prompt, aiResponseJson);

        Assert.True(success, $"{sampleId}: pipeline must succeed");

        foreach (var requiredFile in sample.Expected.RequiredFiles)
        {
            Assert.True(
                File.Exists(Path.Combine(outputDir, requiredFile)),
                $"{sampleId}: required file '{requiredFile}' not found in output");
        }
    }

    [Theory]
    [InlineData("kaffeemaschine")]
    [InlineData("gartenhaus")]
    [InlineData("werkbank")]
    [InlineData("sportwagen")]
    [InlineData("verkaufsstand")]
    public async Task GoldenSample_WhenRunThroughPipeline_ValidationPasses(string sampleId)
    {
        var sample = LoadSample(ResolveSamplePath(sampleId));
        var aiResponseJson = sample.MockAiResponse.GetRawText();

        var (outputDir, success) = await RunPipelineAsync(sample.Prompt, aiResponseJson);

        Assert.True(success, $"{sampleId}: pipeline must succeed");

        var validationJson = await File.ReadAllTextAsync(Path.Combine(outputDir, "validation.json"));
        var validation = ValidationResult.FromJson(validationJson);

        Assert.NotNull(validation);
        Assert.True(validation.Valid,
            $"{sampleId}: validation must pass. Issues: {string.Join("; ", validation.Issues.Select(i => i.Code))}");
    }

    [Theory]
    [InlineData("kaffeemaschine")]
    [InlineData("gartenhaus")]
    [InlineData("werkbank")]
    [InlineData("sportwagen")]
    [InlineData("verkaufsstand")]
    public async Task GoldenSample_WhenRunThroughPipeline_PartCountInExpectedRange(string sampleId)
    {
        var sample = LoadSample(ResolveSamplePath(sampleId));
        var aiResponseJson = sample.MockAiResponse.GetRawText();

        var (outputDir, success) = await RunPipelineAsync(sample.Prompt, aiResponseJson);

        Assert.True(success, $"{sampleId}: pipeline must succeed");

        var graphJson = await File.ReadAllTextAsync(Path.Combine(outputDir, "brickgraph.json"));
        var graph = Graph.FromJson(graphJson);

        Assert.NotNull(graph);
        Assert.InRange(graph.Parts.Count, sample.Expected.MinParts, sample.Expected.MaxParts);
    }

    [Theory]
    [InlineData("kaffeemaschine")]
    [InlineData("gartenhaus")]
    [InlineData("werkbank")]
    [InlineData("sportwagen")]
    [InlineData("verkaufsstand")]
    public async Task GoldenSample_WhenDisclaimerRequired_InstructionsContainDisclaimer(string sampleId)
    {
        var sample = LoadSample(ResolveSamplePath(sampleId));
        if (!sample.Expected.DisclaimerRequired) return;

        var aiResponseJson = sample.MockAiResponse.GetRawText();
        var (outputDir, success) = await RunPipelineAsync(sample.Prompt, aiResponseJson);

        Assert.True(success, $"{sampleId}: pipeline must succeed");

        var instructions = await File.ReadAllTextAsync(Path.Combine(outputDir, "instructions.md"));
        Assert.Contains(
            sample.Expected.DisclaimerText ?? "nicht um eine offizielle LEGO",
            instructions,
            StringComparison.OrdinalIgnoreCase);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ResolveSamplePath(string sampleId)
    {
        // Look in the golden-samples directory copied to the test output.
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "golden-samples", $"{sampleId}.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "golden-samples", $"{sampleId}.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "golden-samples", $"{sampleId}.json")
        };

        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists)
               ?? candidates[0]; // return first candidate even if not found (test will assert its absence)
    }

    private static GoldenSampleDefinition LoadSample(string path)
    {
        var json = File.ReadAllText(path);
        var sample = JsonSerializer.Deserialize<GoldenSampleDefinition>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return sample ?? throw new InvalidOperationException($"Failed to deserialize golden sample: {path}");
    }

    private async Task<(string OutputDir, bool Success)> RunPipelineAsync(string prompt, string aiResponseJson)
    {
        var uniqueRoot = Path.Combine(_tempDir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(uniqueRoot);

        var genOptions = new GenerationOptions
        {
            MaxParts = 300,
            DefaultTargetParts = 50,
            OutputRoot = uniqueRoot
        };
        var ollamaOptions = new OllamaOptions { PlanningModel = "test-model", Temperature = 0.2 };

        var ollamaClient = new FixedResponseOllamaClient(Result<string>.Success(aiResponseJson));
        var promptAnalyzer = new PromptAnalysisService(ollamaClient, ollamaOptions, genOptions);
        var registry = LoadTestRegistry();
        var templateRegistry = LoadTestTemplateRegistry();
        var generator = new SmallMachineGenerator(registry);
        var validator = new BrickGraphValidator(registry);

        var exitCode = await GeneratePipeline.RunAsync(
            prompt, promptAnalyzer, generator, validator,
            templateRegistry, genOptions, ollamaOptions.PlanningModel);

        var createdDirs = Directory.GetDirectories(uniqueRoot);
        var outputDir = createdDirs.Length == 1 ? createdDirs[0] : uniqueRoot;
        return (outputDir, exitCode == 0);
    }

    private static SupportedPartsRegistry LoadTestRegistry()
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "data", "parts");
            var partsJson = File.ReadAllText(Path.Combine(dir, "supported-parts.json"));
            var colorsJson = File.ReadAllText(Path.Combine(dir, "supported-colors.json"));
            return SupportedPartsRegistry.FromJson(partsJson, colorsJson);
        }
        catch
        {
            return SupportedPartsRegistry.FromJson(FallbackPartsJson, FallbackColorsJson);
        }
    }

    private static TemplateRegistry LoadTestTemplateRegistry()
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "data", "parts");
            var templateJson = File.ReadAllText(Path.Combine(dir, "small_machine_template.json"));
            return TemplateRegistry.FromJson(templateJson);
        }
        catch
        {
            return TemplateRegistry.FromJson(FallbackTemplateJson);
        }
    }

    private const string FallbackPartsJson = """
        [
          { "part_number": "3005", "part_name": "Brick 1 x 1" },
          { "part_number": "3004", "part_name": "Brick 1 x 2" },
          { "part_number": "3010", "part_name": "Brick 1 x 4" },
          { "part_number": "3001", "part_name": "Brick 2 x 4" },
          { "part_number": "3003", "part_name": "Brick 2 x 2" },
          { "part_number": "3023", "part_name": "Plate 1 x 2" },
          { "part_number": "3020", "part_name": "Plate 2 x 4" },
          { "part_number": "3024", "part_name": "Plate 1 x 1" },
          { "part_number": "3069b", "part_name": "Tile 1 x 2 with Groove" }
        ]
        """;

    private const string FallbackColorsJson =
        """["black","white","red","blue","yellow","light_bluish_gray","dark_bluish_gray"]""";

    private const string FallbackTemplateJson = """
        {
          "template_id": "small_machine",
          "display_name": "Small Machine",
          "width_studs": 6,
          "depth_studs": 4,
          "height_layers": 4,
          "default_main_color": "black",
          "default_accent_color": "light_bluish_gray",
          "subassemblies": [
            { "name": "base",        "preferred_part": "3020",  "budget_fraction": 0.20 },
            { "name": "main_body",   "preferred_part": "3001",  "budget_fraction": 0.45 },
            { "name": "front_panel", "preferred_part": "3069b", "budget_fraction": 0.20 },
            { "name": "top",         "preferred_part": "3020",  "budget_fraction": 0.10 },
            { "name": "detail",      "preferred_part": "3005",  "budget_fraction": 0.05 }
          ]
        }
        """;
}

// ── Golden sample domain model ────────────────────────────────────────────────

internal sealed class GoldenSampleDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public GoldenSampleExpected Expected { get; set; } = new();

    [JsonPropertyName("mock_ai_response")]
    public JsonElement MockAiResponse { get; set; }
}

internal sealed class GoldenSampleExpected
{
    [JsonPropertyName("model_category")]
    public string ModelCategory { get; set; } = "small_machine";

    [JsonPropertyName("min_parts")]
    public int MinParts { get; set; } = 1;

    [JsonPropertyName("max_parts")]
    public int MaxParts { get; set; } = 300;

    [JsonPropertyName("required_files")]
    public List<string> RequiredFiles { get; set; } = [];

    [JsonPropertyName("min_validation_score")]
    public double MinValidationScore { get; set; } = 0.70;

    [JsonPropertyName("disclaimer_required")]
    public bool DisclaimerRequired { get; set; } = true;

    [JsonPropertyName("disclaimer_text")]
    public string? DisclaimerText { get; set; }
}
