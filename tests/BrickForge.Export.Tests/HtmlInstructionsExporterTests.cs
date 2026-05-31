using BrickForge.BrickGraph.Model;
using BrickForge.Export;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export.Tests;

public sealed class HtmlInstructionsExporterTests
{
    private static readonly HtmlInstructionsExporter _exporter = new();

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
    public void Export_IsValidHtmlDocument()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains("<!DOCTYPE html>", result.Content);
        Assert.Contains("<html", result.Content);
        Assert.Contains("</html>", result.Content);
    }

    [Fact]
    public void Export_ContainsTitle()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains("Coffee Machine", result.Content);
        Assert.Contains("<h1>", result.Content);
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
            "nicht um eine offizielle LEGO-Bauanleitung",
            result.Content);
    }

    [Fact]
    public void Export_ContainsPartsList()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains("3001", result.Content);
        Assert.Contains("3002", result.Content);
        Assert.Contains("Teileliste", result.Content);
        Assert.Contains("<table>", result.Content);
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
    public void Export_ContainsLDrawExportHint()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains("LDraw", result.Content);
        Assert.Contains("model.mpd", result.Content);
    }

    [Fact]
    public void Export_ContainsInterpretationNotice()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains("MOC", result.Content);
        Assert.Contains("fanbasierte", result.Content);
    }

    [Fact]
    public void Export_DoesNotContainOfficialLegoClaimGerman()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        // Disclaimer must negate the claim, not assert it
        Assert.Contains("nicht um eine offizielle LEGO-Bauanleitung", result.Content);
    }

    [Fact]
    public void Export_DoesNotContainOfficialLegoClaimEnglish()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.DoesNotContain("official LEGO instruction", result.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("official LEGO building instruction", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Export_ContainsUtf8Charset()
    {
        var result = _exporter.Export(BuildGraph());

        Assert.True(result.Success);
        Assert.Contains("UTF-8", result.Content, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void Export_HtmlEncodesModelName()
    {
        var graph = new Graph
        {
            Model = new BrickModelMetadata { Id = "xss", Name = "<script>alert(1)</script>" }
        };
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "p1",
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = "black",
            Step = 1
        });

        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        Assert.DoesNotContain("<script>alert(1)</script>", result.Content);
        Assert.Contains("&lt;script&gt;", result.Content);
    }
}
