using BrickForge.BrickGraph.Model;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Ai;
using BrickForge.Export;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export.Tests;

public sealed class ReportExporterTests
{
    private static readonly ReportExporter _exporter = new();

    private static Graph BuildGraph()
    {
        var graph = new Graph
        {
            Model = new BrickModelMetadata { Id = "test", Name = "Coffee Machine", TargetParts = 50 }
        };
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "p1",
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = "black",
            Step = 1
        });
        return graph;
    }

    private static GenerationReportData BuildData(
        string prompt = "Erstelle eine Kaffeemaschine",
        bool useFallback = false,
        ValidationResult? validation = null) => new()
        {
            OriginalPrompt = prompt,
            AiModelName = useFallback ? null : "llama3",
            AnalysisResult = new PromptAnalysisResult
            {
                ModelName = "Coffee Machine",
                ModelCategory = "small_machine",
                TargetParts = 50,
                MainColor = "black",
                AccentColor = "light_bluish_gray",
                UsedFallback = useFallback
            },
            ValidationResult = validation ?? ValidationResult.FromIssues([], 6),
            GeneratedFiles = ["model.mpd", "parts.csv", "instructions.md", "Graph.json", "validation.json", "report.md"],
            Timestamp = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero)
        };

    [Fact]
    public void Export_ContainsPrompt()
    {
        var data = BuildData(prompt: "Erstelle eine kleine Kaffeemaschine");
        var result = _exporter.Export(BuildGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("Erstelle eine kleine Kaffeemaschine", result.Content);
    }

    [Fact]
    public void Export_ContainsModelNameOrFallback()
    {
        var data = BuildData();
        var result = _exporter.Export(BuildGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("Coffee Machine", result.Content);
    }

    [Fact]
    public void Export_ContainsModelNameOrFallback_WhenFallbackUsed()
    {
        var data = BuildData(useFallback: true);
        var result = _exporter.Export(BuildGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("Fallback-Analyse", result.Content);
    }

    [Fact]
    public void Export_ContainsValidationResult()
    {
        var validation = ValidationResult.FromIssues([], 6);
        var data = BuildData(validation: validation);
        var result = _exporter.Export(BuildGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("Validierungsergebnis", result.Content);
        Assert.Contains("Ja", result.Content); // valid = true
    }

    [Fact]
    public void Export_ContainsValidationResult_WithIssues()
    {
        var issues = new List<ValidationIssue>
        {
            new() { Code = "EMPTY_PARTS", Message = "No parts", Severity = ValidationSeverity.High }
        };
        var validation = ValidationResult.FromIssues(issues, 6);
        var data = BuildData(validation: validation);
        var result = _exporter.Export(BuildGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("EMPTY_PARTS", result.Content);
    }

    [Fact]
    public void Export_ContainsTargetAndActualParts()
    {
        var data = BuildData();
        var result = _exporter.Export(BuildGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("50", result.Content);  // target parts
        Assert.Contains("1", result.Content);   // actual parts (graph has 1 part)
    }

    [Fact]
    public void Export_ContainsGeneratedFilesList()
    {
        var data = BuildData();
        var result = _exporter.Export(BuildGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("model.mpd", result.Content);
        Assert.Contains("parts.csv", result.Content);
        Assert.Contains("instructions.md", result.Content);
    }

    [Fact]
    public void Export_ContainsMvp0Limitations()
    {
        var data = BuildData();
        var result = _exporter.Export(BuildGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("MVP0", result.Content);
        Assert.Contains("Einschränkungen", result.Content);
    }

    [Fact]
    public void Export_ContainsDisclaimer()
    {
        var data = BuildData();
        var result = _exporter.Export(BuildGraph(), data);

        Assert.True(result.Success);
        Assert.Contains(
            "Dieses Dokument wurde automatisch durch BrickForge erzeugt.",
            result.Content);
        Assert.Contains(
            "nicht um eine offizielle LEGO-Bauanleitung",
            result.Content);
    }
}
