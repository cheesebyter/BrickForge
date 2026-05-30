using BrickForge.BrickGraph.Model;
using BrickForge.Export;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export.Tests;

public sealed class LDrawExporterTests
{
    private static Graph BuildSinglePartGraph(
        string color = "black",
        float[]? position = null,
        float[]? rotation = null,
        int step = 1)
    {
        var graph = new Graph
        {
            Model = new BrickModelMetadata { Id = "test", Name = "Test Model" }
        };
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "p1",
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = color,
            Position = position ?? [0f, 0f, 0f],
            Rotation = rotation ?? [1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f],
            Step = step
        });
        graph.AddStep(new BrickStep { StepNumber = step, PartInstanceIds = ["p1"] });
        return graph;
    }

    private static readonly LDrawExporter _exporter = new();

    [Fact]
    public void Export_ContainsHeader()
    {
        var graph = BuildSinglePartGraph();
        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        Assert.Contains("0 FILE model.mpd", result.Content);
        Assert.Contains("0 Author: BrickForge MVP0", result.Content);
        Assert.Contains("0 !HISTORY Generated at", result.Content);
    }

    [Fact]
    public void Export_ContainsPartLine()
    {
        var graph = BuildSinglePartGraph();
        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        Assert.Contains("3001.dat", result.Content);
    }

    [Fact]
    public void Export_ContainsStepMarkers()
    {
        var graph = BuildSinglePartGraph();
        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        Assert.Contains("0 STEP", result.Content);
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
    public void Export_PartLineHasCorrectFormat()
    {
        var graph = BuildSinglePartGraph();
        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        var lines = result.Content!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var partLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("1 "));

        Assert.NotNull(partLine);
        // LDraw part line: 1 <color> <x> <y> <z> <9 rotation values> <part>.dat
        var tokens = partLine.Trim().Split(' ');
        Assert.True(tokens.Length >= 14, $"Expected at least 14 tokens, got {tokens.Length}: {partLine}");
        Assert.Equal("1", tokens[0]);
    }

    [Fact]
    public void Export_UnknownColor_UsesFallbackCode()
    {
        var graph = BuildSinglePartGraph(color: "ultraviolet_rainbow");
        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        // Fallback code is 16; line starts with "1 16 ..."
        Assert.Contains("1 16 ", result.Content);
    }

    [Fact]
    public void Export_ContentIsUtf8Compatible()
    {
        var graph = BuildSinglePartGraph();
        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        // Round-trip through UTF-8 should produce identical bytes
        var bytes = System.Text.Encoding.UTF8.GetBytes(result.Content!);
        var roundTripped = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Equal(result.Content, roundTripped);
    }
}
