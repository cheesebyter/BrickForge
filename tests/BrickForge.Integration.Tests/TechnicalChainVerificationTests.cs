using BrickForge.Ai;
using BrickForge.Ai.Analysis;
using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Ai;
using BrickForge.Core.Options;
using BrickForge.Core.Results;
using BrickForge.Export;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Integration.Tests;

/// <summary>
/// Implements BF-MVP1-038 (Schlussbemerkung §38).
///
/// MVP 1 soll beweisen, dass die zentrale technische Kette funktioniert:
///
///   Textbeschreibung
///     -> strukturierter Modellplan
///     -> BrickGraph
///     -> Validierung
///     -> LDraw/MPD/LDR
///     -> Teileliste
///     -> einfache Bauanleitung
///
/// Each test in this class verifies one step of that chain in isolation,
/// providing living documentation that the chain is complete.
///
/// No live Ollama instance is required — a deterministic fake client is used.
/// </summary>
public sealed class TechnicalChainVerificationTests : IDisposable
{
    private readonly string _tempDir;

    public TechnicalChainVerificationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "brickforge_chain_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Chain Step 1: Textbeschreibung → strukturierter Modellplan ────────────

    [Fact]
    public async Task ChainStep1_TextPrompt_ProducesStructuredAnalysisResult()
    {
        var client = new FixedResponseOllamaClient(Result<string>.Success(KaffeemaschineAnalysisJson));
        var analyzer = BuildAnalyzer(client);

        var result = await analyzer.AnalyzeAsync(KaffeemaschinePrompt);

        Assert.True(result.IsSuccess, "Chain step 1 must succeed: prompt → analysis");
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.ModelName));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.ModelCategory));
        Assert.True(result.Value.TargetParts > 0);
    }

    [Fact]
    public async Task ChainStep1_WhenAiUnavailable_FallbackProducesValidAnalysis()
    {
        var client = new SimulatedUnavailableOllamaClient();
        var analyzer = BuildAnalyzer(client);

        var result = await analyzer.AnalyzeAsync(KaffeemaschinePrompt);

        Assert.True(result.IsSuccess, "Chain step 1 must succeed via fallback when AI unavailable");
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Feasible, "Fallback analysis must be feasible for a valid prompt");
    }

    // ── Chain Step 2: strukturierter Modellplan → BrickGraph ─────────────────

    [Fact]
    public void ChainStep2_AnalysisResult_ProducesBrickGraph()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var generator = new SmallMachineGenerator(registry);
        var analysis = BuildAnalysis();

        var graph = generator.Generate(analysis, template);

        Assert.NotNull(graph);
        Assert.True(graph.Parts.Count > 0, "Chain step 2 must produce a non-empty BrickGraph");
        Assert.False(string.IsNullOrWhiteSpace(graph.Model.Name));
    }

    [Fact]
    public void ChainStep2_BrickGraph_AllPartsHaveUniqueIds()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var generator = new SmallMachineGenerator(registry);
        var analysis = BuildAnalysis();

        var graph = generator.Generate(analysis, template);

        var ids = graph.Parts.Select(p => p.InstanceId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void ChainStep2_BrickGraph_AllPartsHaveStepAssigned()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var generator = new SmallMachineGenerator(registry);
        var analysis = BuildAnalysis();

        var graph = generator.Generate(analysis, template);

        Assert.All(graph.Parts, p =>
            Assert.True(p.Step >= 1, $"Part {p.InstanceId} has no valid step (step={p.Step})"));
    }

    // ── Chain Step 3: BrickGraph → Validierung ────────────────────────────────

    [Fact]
    public void ChainStep3_ValidBrickGraph_PassesValidation()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var graph = new SmallMachineGenerator(registry).Generate(BuildAnalysis(), template);
        var validator = new BrickGraphValidator(registry);

        var result = validator.Validate(graph);

        Assert.NotNull(result);
        Assert.True(result.Valid, $"Chain step 3 must pass for a valid graph. Issues: {string.Join(", ", result.Issues.Select(i => i.Code))}");
        Assert.True(result.Score > 0.0, "Validation score must be positive");
    }

    [Fact]
    public void ChainStep3_ValidationResult_IsSerializableToJson()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var graph = new SmallMachineGenerator(registry).Generate(BuildAnalysis(), template);
        var validator = new BrickGraphValidator(registry);

        var result = validator.Validate(graph);
        var json = result.ToJson();
        var restored = ValidationResult.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(result.Valid, restored.Valid);
    }

    // ── Chain Step 4: LDraw/MPD/LDR ───────────────────────────────────────────

    [Fact]
    public void ChainStep4_BrickGraph_ProducesLDrawMpd()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var graph = new SmallMachineGenerator(registry).Generate(BuildAnalysis(), template);

        var export = new LDrawExporter().Export(graph);

        Assert.True(export.Success, "Chain step 4: LDraw export must succeed");
        Assert.False(string.IsNullOrWhiteSpace(export.Content));
    }

    [Fact]
    public void ChainStep4_MpdContent_ContainsPartLinesAndStepMarkers()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var graph = new SmallMachineGenerator(registry).Generate(BuildAnalysis(), template);

        var export = new LDrawExporter().Export(graph);

        Assert.Contains("0 STEP", export.Content);
        Assert.Contains(" 1 ", export.Content); // LDraw part line type flag
    }

    // ── Chain Step 5: Teileliste (CSV) ────────────────────────────────────────

    [Fact]
    public void ChainStep5_BrickGraph_ProducesPartsList()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var graph = new SmallMachineGenerator(registry).Generate(BuildAnalysis(), template);

        var export = new CsvPartsExporter().Export(graph);

        Assert.True(export.Success, "Chain step 5: CSV parts export must succeed");
        Assert.False(string.IsNullOrWhiteSpace(export.Content));
    }

    [Fact]
    public void ChainStep5_CsvContent_HasHeaderAndAtLeastOneDataRow()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var graph = new SmallMachineGenerator(registry).Generate(BuildAnalysis(), template);

        var export = new CsvPartsExporter().Export(graph);
        var lines = export.Content!.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.True(lines.Length >= 2, "CSV must have header row plus at least one data row");
        Assert.StartsWith("quantity,part_number", lines[0]);
    }

    // ── Chain Step 6: einfache Bauanleitung (Markdown) ───────────────────────

    [Fact]
    public void ChainStep6_BrickGraph_ProducesMarkdownInstructions()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var graph = new SmallMachineGenerator(registry).Generate(BuildAnalysis(), template);

        var export = new MarkdownInstructionsExporter().Export(graph);

        Assert.True(export.Success, "Chain step 6: Markdown instructions export must succeed");
        Assert.False(string.IsNullOrWhiteSpace(export.Content));
    }

    [Fact]
    public void ChainStep6_MarkdownInstructions_ContainsLegalDisclaimer()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var graph = new SmallMachineGenerator(registry).Generate(BuildAnalysis(), template);

        var export = new MarkdownInstructionsExporter().Export(graph);

        Assert.Contains("nicht um eine offizielle LEGO", export.Content);
    }

    [Fact]
    public void ChainStep6_MarkdownInstructions_ContainsStepListings()
    {
        var registry = BuildRegistry();
        var template = BuildTemplate();
        var graph = new SmallMachineGenerator(registry).Generate(BuildAnalysis(), template);

        var export = new MarkdownInstructionsExporter().Export(graph);
        var content = export.Content!;

        // When steps have labels the exporter uses the label (e.g. "Basis", "Hauptkörper");
        // when steps are empty it falls back to "Schritt N".  Both cases must produce at
        // least one step heading ("###") in the markdown output.
        var stepHeadingCount = content.Split('\n')
            .Count(line => line.TrimStart().StartsWith("###"));

        var expectedStepCount = graph.Steps.Count > 0 ? graph.Steps.Count : graph.Parts.Select(p => p.Step).Distinct().Count();
        Assert.True(
            stepHeadingCount >= expectedStepCount,
            $"Markdown must contain at least {expectedStepCount} step headings (###), found {stepHeadingCount}");
    }

    // ── Full chain: all six steps in sequence ─────────────────────────────────

    [Fact]
    public async Task FullChain_AllSixSteps_ProduceCompleteOutput()
    {
        var (outputDir, success) = await RunChainAsync(KaffeemaschinePrompt, KaffeemaschineAnalysisJson);

        Assert.True(success, "Full technical chain must succeed end to end");
        Assert.True(File.Exists(Path.Combine(outputDir, "brickgraph.json")), "Step 2 output: brickgraph.json");
        Assert.True(File.Exists(Path.Combine(outputDir, "validation.json")), "Step 3 output: validation.json");
        Assert.True(File.Exists(Path.Combine(outputDir, "model.mpd")),       "Step 4 output: model.mpd");
        Assert.True(File.Exists(Path.Combine(outputDir, "parts.csv")),       "Step 5 output: parts.csv");
        Assert.True(File.Exists(Path.Combine(outputDir, "instructions.md")), "Step 6 output: instructions.md");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PromptAnalysisService BuildAnalyzer(IOllamaClient client)
    {
        var genOptions = new GenerationOptions { MaxParts = 80, DefaultTargetParts = 50 };
        var ollamaOptions = new OllamaOptions { PlanningModel = "test-model", Temperature = 0.2 };
        return new PromptAnalysisService(client, ollamaOptions, genOptions);
    }

    private static SupportedPartsRegistry BuildRegistry() =>
        SupportedPartsRegistry.FromJson(SupportedPartsJson, SupportedColorsJson);

    private static BrickModelTemplate BuildTemplate()
    {
        var registry = TemplateRegistry.FromJson(SmallMachineTemplateJson);
        return registry.FindTemplate("small_machine")!;
    }

    private static PromptAnalysisResult BuildAnalysis() =>
        new()
        {
            ModelName      = "Kleine Kaffeemaschine",
            ModelCategory  = "small_machine",
            TargetParts    = 50,
            MainColor      = "black",
            AccentColor    = "light_bluish_gray",
            Features       = ["cup", "front_panel"],
            Feasible       = true,
            UsedFallback   = false
        };

    private async Task<(string OutputDir, bool Success)> RunChainAsync(
        string prompt,
        string aiResponseJson)
    {
        var client = new FixedResponseOllamaClient(Result<string>.Success(aiResponseJson));
        var genOptions = new GenerationOptions
        {
            MaxParts = 80,
            DefaultTargetParts = 50,
            OutputRoot = _tempDir
        };
        var ollamaOptions = new OllamaOptions { PlanningModel = "test-model", Temperature = 0.2 };
        var registry = BuildRegistry();
        var templateRegistry = TemplateRegistry.FromJson(SmallMachineTemplateJson);
        var generator = new SmallMachineGenerator(registry);
        var validator = new BrickGraphValidator(registry);
        var analyzer = new PromptAnalysisService(client, ollamaOptions, genOptions);

        var existingDirs = new HashSet<string>(Directory.GetDirectories(_tempDir));

        var exitCode = await GeneratePipeline.RunAsync(
            prompt, analyzer, generator, validator, templateRegistry,
            genOptions, ollamaOptions.PlanningModel);

        var newDirs = Directory.GetDirectories(_tempDir).Except(existingDirs).ToArray();
        var outputDir = newDirs.Length == 1 ? newDirs[0] : _tempDir;
        return (outputDir, exitCode == 0);
    }

    // ── Test data ─────────────────────────────────────────────────────────────

    private const string KaffeemaschinePrompt =
        "Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel und einer Tasse. " +
        "Das Modell soll einfach und stabil sein.";

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
          { "part_number": "3005",  "part_name": "Brick 1 x 1" },
          { "part_number": "3004",  "part_name": "Brick 1 x 2" },
          { "part_number": "3622",  "part_name": "Brick 1 x 3" },
          { "part_number": "3010",  "part_name": "Brick 1 x 4" },
          { "part_number": "3003",  "part_name": "Brick 2 x 2" },
          { "part_number": "3002",  "part_name": "Brick 2 x 3" },
          { "part_number": "3001",  "part_name": "Brick 2 x 4" },
          { "part_number": "3024",  "part_name": "Plate 1 x 1" },
          { "part_number": "3023",  "part_name": "Plate 1 x 2" },
          { "part_number": "3710",  "part_name": "Plate 1 x 4" },
          { "part_number": "3022",  "part_name": "Plate 2 x 2" },
          { "part_number": "3020",  "part_name": "Plate 2 x 4" },
          { "part_number": "3069b", "part_name": "Tile 1 x 2 with Groove" },
          { "part_number": "2431",  "part_name": "Tile 1 x 4 with Groove" }
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
