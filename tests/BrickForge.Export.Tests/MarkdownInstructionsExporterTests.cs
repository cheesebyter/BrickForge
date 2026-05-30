using BrickForge.BrickGraph.Model;
using BrickForge.Export;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export.Tests;

public sealed class MarkdownInstructionsExporterTests
{
    private static readonly MarkdownInstructionsExporter _exporter = new();

    private static Graph BuildGraph()
    {
        var graph = new Graph
        {
            Model = new BrickModelMetadata { Id = "test", Name = "Coffee Machine" }
        };
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "p1",
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = "black",
            Step = 1
        });
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "p2",
            PartNumber = "3002",
            PartName = "Brick 2 x 3",
            Color = "light_bluish_gray",
            Step = 2
        });
        graph.AddStep(new BrickStep { StepNumber = 1, Label = "Schritt 1", PartInstanceIds = ["p1"] });
        graph.AddStep(new BrickStep { StepNumber = 2, Label = "Schritt 2", PartInstanceIds = ["p2"] });
        return graph;
    }

    [Fact]
    public void Export_ContainsTitle()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains("# Coffee Machine", result.Content);
    }

    [Fact]
    public void Export_ContainsLegalDisclaimer()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains(
            "Dieses Dokument wurde automatisch durch BrickForge erzeugt.",
            result.Content);
        Assert.Contains(
            "Es handelt sich nicht um eine offizielle LEGO-Bauanleitung",
            result.Content);
    }

    [Fact]
    public void Export_ContainsAllSteps()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains("Schritt 1", result.Content);
        Assert.Contains("Schritt 2", result.Content);
    }

    [Fact]
    public void Export_ContainsPartsList()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains("3001", result.Content);
        Assert.Contains("3002", result.Content);
        Assert.Contains("Teileliste", result.Content);
    }

    [Fact]
    public void Export_DoesNotContainOfficialLegoClaimGerman()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        // Must not positively claim official LEGO status
        Assert.DoesNotContain("offizielle LEGO-Bauanleitung.", result.Content);
        // The disclaimer must negate the claim, not assert it
        Assert.Contains("nicht um eine offizielle LEGO-Bauanleitung", result.Content);
    }

    [Fact]
    public void Export_DoesNotContainOfficialLegoClaimEnglish()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.DoesNotContain("official LEGO instruction", result.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("official LEGO building", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Export_WhenBrickGraphIsEmpty_ReturnsFailure()
    {
        var graph = new Graph
        {
            Model = new BrickModelMetadata { Id = "empty", Name = "Empty" }
        };

        var result = _exporter.Export(graph);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }
}
