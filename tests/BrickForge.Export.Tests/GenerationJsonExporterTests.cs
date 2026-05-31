using BrickForge.BrickGraph.Model;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Ai;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export.Tests;

public sealed class GenerationJsonExporterTests
{
    private static readonly GenerationJsonExporter _exporter = new();

    private static Graph BuildGraph(string color = "black", int partCount = 2)
    {
        var g = new Graph
        {
            Model = new BrickModelMetadata
            {
                Id          = "job_test",
                Name        = "Test Model",
                TargetParts = partCount,
                ActualParts = partCount
            }
        };
        for (var i = 1; i <= partCount; i++)
        {
            g.Parts.Add(new BrickPartInstance
            {
                InstanceId = $"part_{i:D3}",
                PartNumber = "3001",
                PartName   = "Brick 2x4",
                Color      = color,
                Step       = i
            });
        }
        return g;
    }

    private static GenerationJsonData BuildData(
        bool wasRepaired       = false,
        PromptAnalysisResult? analysis    = null,
        ValidationResult?     validation  = null,
        IReadOnlyList<string>? files      = null) =>
        new()
        {
            JobId          = "job_abc",
            OriginalPrompt = "Erstelle eine Kaffeemaschine",
            TemplateName   = "small_machine",
            AnalysisResult = analysis,
            ValidationResult = validation,
            WasRepaired    = wasRepaired,
            GeneratedFiles = files ?? ["model.mpd", "parts.csv", "instructions.md"],
            Timestamp      = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero)
        };

    // ── Success cases ─────────────────────────────────────────────────────────

    [Fact]
    public void Export_WhenGraphHasParts_ReturnsSuccess()
    {
        var result = _exporter.Export(BuildGraph(), BuildData());

        Assert.True(result.Success);
        Assert.NotNull(result.Content);
    }

    [Fact]
    public void Export_Always_ContainsJobId()
    {
        var result = _exporter.Export(BuildGraph(), BuildData());

        Assert.Contains("\"job_id\"", result.Content!);
        Assert.Contains("job_abc", result.Content!);
    }

    [Fact]
    public void Export_Always_ContainsDisclaimer()
    {
        var result = _exporter.Export(BuildGraph(), BuildData());

        Assert.Contains("BrickForge", result.Content!);
        Assert.Contains("nicht um eine offizielle LEGO", result.Content!);
    }

    [Fact]
    public void Export_WhenGraphHasParts_ContainsActualColors()
    {
        var graph  = BuildGraph(color: "red");
        var result = _exporter.Export(graph, BuildData());

        Assert.True(result.Success);
        Assert.Contains("red", result.Content!);
    }

    [Fact]
    public void Export_WhenWasRepaired_SetsWasRepairedTrue()
    {
        var result = _exporter.Export(BuildGraph(), BuildData(wasRepaired: true));

        Assert.True(result.Success);
        Assert.Contains("\"was_repaired\": true", result.Content!);
    }

    [Fact]
    public void Export_WhenNotRepaired_SetsWasRepairedFalse()
    {
        var result = _exporter.Export(BuildGraph(), BuildData(wasRepaired: false));

        Assert.True(result.Success);
        Assert.Contains("\"was_repaired\": false", result.Content!);
    }

    [Fact]
    public void Export_ContainsGeneratedFiles()
    {
        var files  = new List<string> { "model.mpd", "parts.csv" };
        var result = _exporter.Export(BuildGraph(), BuildData(files: files));

        Assert.True(result.Success);
        Assert.Contains("model.mpd", result.Content!);
        Assert.Contains("parts.csv", result.Content!);
    }

    [Fact]
    public void Export_ContainsTemplateName()
    {
        var result = _exporter.Export(BuildGraph(), BuildData());

        Assert.Contains("small_machine", result.Content!);
    }

    [Fact]
    public void Export_ContainsOriginalPrompt()
    {
        var result = _exporter.Export(BuildGraph(), BuildData());

        Assert.Contains("Kaffeemaschine", result.Content!);
    }

    // ── Analysis result ───────────────────────────────────────────────────────

    [Fact]
    public void Export_WhenAnalysisResultProvided_IncludesModelCategory()
    {
        var analysis = new PromptAnalysisResult
        {
            ModelName      = "Coffee Machine",
            ModelCategory  = "small_machine",
            TargetParts    = 50,
            MainColor      = "black",
            AccentColor    = "light_bluish_gray"
        };
        var result = _exporter.Export(BuildGraph(), BuildData(analysis: analysis));

        Assert.True(result.Success);
        Assert.Contains("small_machine", result.Content!);
    }

    [Fact]
    public void Export_WhenAnalysisIsNull_FallsBackToGraphModelName()
    {
        var graph  = BuildGraph();
        var result = _exporter.Export(graph, BuildData(analysis: null));

        Assert.True(result.Success);
        // model_name should be the graph's name
        Assert.Contains("Test Model", result.Content!);
    }

    [Fact]
    public void Export_WhenGraphIsEmpty_ColorsListIsEmpty()
    {
        var g = new Graph
        {
            Model = new BrickModelMetadata { Id = "empty", Name = "Empty", TargetParts = 0, ActualParts = 0 }
        };
        var result = _exporter.Export(g, BuildData(analysis: null));

        Assert.True(result.Success);
        // No colors — the "colors" array should be empty
        Assert.Contains("\"colors\": []", result.Content!);
    }

    [Fact]
    public void Export_WhenGraphIsEmpty_AnalysisColorsUsedAsFallback()
    {
        var g = new Graph
        {
            Model = new BrickModelMetadata { Id = "empty", Name = "Empty", TargetParts = 0, ActualParts = 0 }
        };
        var analysis = new PromptAnalysisResult
        {
            ModelName   = "Test",
            MainColor   = "black",
            AccentColor = "white"
        };
        var result = _exporter.Export(g, BuildData(analysis: analysis));

        Assert.True(result.Success);
        Assert.Contains("black", result.Content!);
        Assert.Contains("white", result.Content!);
    }

    // ── Validation result ─────────────────────────────────────────────────────

    [Fact]
    public void Export_WhenValidationPassed_IsValidTrue()
    {
        var validation = new ValidationResult { Valid = true, Score = 1.0f };
        var result = _exporter.Export(BuildGraph(), BuildData(validation: validation));

        Assert.True(result.Success);
        Assert.Contains("\"is_valid\": true", result.Content!);
    }

    [Fact]
    public void Export_WhenValidationFailed_IsValidFalse()
    {
        var validation = new ValidationResult { Valid = false, Score = 0.5f };
        var result = _exporter.Export(BuildGraph(), BuildData(validation: validation));

        Assert.True(result.Success);
        Assert.Contains("\"is_valid\": false", result.Content!);
    }

    // ── Output format ─────────────────────────────────────────────────────────

    [Fact]
    public void Export_OutputIsValidJson()
    {
        var result = _exporter.Export(BuildGraph(), BuildData());

        Assert.True(result.Success);
        var doc = System.Text.Json.JsonDocument.Parse(result.Content!);
        Assert.NotNull(doc);
    }

    [Fact]
    public void Export_OutputIsIndented()
    {
        var result = _exporter.Export(BuildGraph(), BuildData());

        Assert.True(result.Success);
        Assert.Contains("\n  ", result.Content!);
    }

    [Fact]
    public void Export_ContainsTimestamp()
    {
        var result = _exporter.Export(BuildGraph(), BuildData());

        Assert.True(result.Success);
        Assert.Contains("2025-01-01T12:00:00Z", result.Content!);
    }

    [Fact]
    public void Export_ContainsKnownLimitations()
    {
        var result = _exporter.Export(BuildGraph(), BuildData());

        Assert.True(result.Success);
        Assert.Contains("known_limitations", result.Content!);
    }
}
