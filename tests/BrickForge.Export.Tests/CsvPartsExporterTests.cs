using BrickForge.BrickGraph.Model;
using BrickForge.Export;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export.Tests;

public sealed class CsvPartsExporterTests
{
    private static readonly CsvPartsExporter _exporter = new();

    private static Graph BuildGraph(params (string PartNumber, string PartName, string Color)[] parts)
    {
        var graph = new Graph
        {
            Model = new BrickModelMetadata { Id = "test", Name = "Test Model" }
        };
        int idx = 1;
        foreach (var (partNumber, partName, color) in parts)
        {
            graph.AddPart(new BrickPartInstance
            {
                InstanceId = $"p{idx++}",
                PartNumber = partNumber,
                PartName = partName,
                Color = color,
                Step = 1
            });
        }
        return graph;
    }

    [Fact]
    public void Export_ContainsHeaderRow()
    {
        var graph = BuildGraph(("3001", "Brick 2 x 4", "black"));
        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        Assert.Contains("quantity,part_number,part_name,color", result.Content);
    }

    [Fact]
    public void Export_AggregatesIdenticalParts()
    {
        var graph = BuildGraph(
            ("3001", "Brick 2 x 4", "black"),
            ("3001", "Brick 2 x 4", "black"),
            ("3001", "Brick 2 x 4", "black"));

        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        var dataLines = result.Content!
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1) // skip header
            .ToList();

        Assert.Single(dataLines);
        Assert.StartsWith("3", dataLines[0]); // quantity = 3
    }

    [Fact]
    public void Export_SeparatesPartsByColor()
    {
        var graph = BuildGraph(
            ("3001", "Brick 2 x 4", "black"),
            ("3001", "Brick 2 x 4", "white"));

        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        var dataLines = result.Content!
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .ToList();

        Assert.Equal(2, dataLines.Count);
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
    public void Export_CsvHasCorrectColumns()
    {
        var graph = BuildGraph(("3001", "Brick 2 x 4", "red"));
        var result = _exporter.Export(graph);

        Assert.True(result.Success);
        var lines = result.Content!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0];
        var dataRow = lines[1];

        var columns = header.Trim().Split(',');
        Assert.Equal(4, columns.Length);
        Assert.Equal("quantity", columns[0].Trim());
        Assert.Equal("part_number", columns[1].Trim());
        Assert.Equal("part_name", columns[2].Trim());
        Assert.Equal("color", columns[3].Trim());

        // Data row should also have 4 fields
        Assert.Equal(4, dataRow.Split(',').Length);
    }
}
