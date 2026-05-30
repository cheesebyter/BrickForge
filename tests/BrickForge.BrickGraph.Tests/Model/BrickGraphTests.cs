using BrickForge.BrickGraph.Model;

namespace BrickForge.BrickGraph.Tests.Model;

/// <summary>
/// Unit tests for the BrickGraph domain model (BF-MVP0-007).
/// </summary>
public sealed class BrickGraphTests
{
    [Fact]
    public void BrickGraph_WhenNew_HasEmptyPartsAndSteps()
    {
        var graph = new BrickGraph();

        Assert.Empty(graph.Parts);
        Assert.Empty(graph.Steps);
    }

    [Fact]
    public void BrickGraph_WhenNew_ActualPartsIsZero()
    {
        var graph = new BrickGraph { Model = new BrickModelMetadata { Id = "m1", Name = "Test" } };

        Assert.Equal(0, graph.Model.ActualParts);
    }

    [Fact]
    public void AddPart_UpdatesActualParts()
    {
        var graph = new BrickGraph();

        graph.AddPart(MakePart("part_001", "3001"));
        graph.AddPart(MakePart("part_002", "3004"));

        Assert.Equal(2, graph.Model.ActualParts);
        Assert.Equal(2, graph.Parts.Count);
    }

    [Fact]
    public void AddStep_IncreasesStepCount()
    {
        var graph = new BrickGraph();
        graph.AddStep(new BrickStep { StepNumber = 1, Label = "Base" });
        graph.AddStep(new BrickStep { StepNumber = 2, Label = "Body" });

        Assert.Equal(2, graph.Steps.Count);
    }

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var graph = new BrickGraph
        {
            Model = new BrickModelMetadata { Id = "m1", Name = "Coffee Machine", TargetParts = 30 }
        };
        graph.AddPart(MakePart("part_001", "3001"));

        var json = graph.ToJson();

        Assert.NotEmpty(json);
        Assert.Contains("\"model\"", json);
        Assert.Contains("\"parts\"", json);
        Assert.Contains("\"steps\"", json);
    }

    [Fact]
    public void FromJson_RoundTrip_PreservesData()
    {
        var original = new BrickGraph
        {
            Model = new BrickModelMetadata { Id = "m1", Name = "Test Model", TargetParts = 50 }
        };
        original.AddPart(MakePart("part_001", "3001"));
        original.AddStep(new BrickStep { StepNumber = 1, PartInstanceIds = ["part_001"] });

        var json = original.ToJson();
        var restored = BrickGraph.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal("m1", restored.Model.Id);
        Assert.Equal("Test Model", restored.Model.Name);
        Assert.Equal(50, restored.Model.TargetParts);
        Assert.Single(restored.Parts);
        Assert.Equal("part_001", restored.Parts[0].InstanceId);
        Assert.Equal("3001", restored.Parts[0].PartNumber);
        Assert.Single(restored.Steps);
        Assert.Equal(1, restored.Steps[0].StepNumber);
    }

    [Fact]
    public void FromJson_WhenInvalidJson_ReturnsNull()
    {
        var result = BrickGraph.FromJson("not valid json {{");

        Assert.Null(result);
    }

    [Fact]
    public void AllPartIds_AreUnique()
    {
        var graph = new BrickGraph();
        for (var i = 1; i <= 5; i++)
            graph.AddPart(MakePart($"part_{i:000}", "3001"));

        var ids = graph.Parts.Select(p => p.InstanceId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void AllStepNumbers_AreAtLeastOne()
    {
        var graph = new BrickGraph();
        graph.AddPart(MakePart("part_001", "3001"));
        graph.AddStep(new BrickStep { StepNumber = 1, PartInstanceIds = ["part_001"] });

        Assert.All(graph.Parts, p => Assert.True(p.Step >= 1));
        Assert.All(graph.Steps, s => Assert.True(s.StepNumber >= 1));
    }

    [Fact]
    public void BrickPartInstance_DefaultRotation_IsIdentityMatrix()
    {
        var part = new BrickPartInstance();

        Assert.Equal(9, part.Rotation.Length);
        Assert.Equal([1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f], part.Rotation);
    }

    [Fact]
    public void BrickPartInstance_DefaultPosition_IsOrigin()
    {
        var part = new BrickPartInstance();

        Assert.Equal(3, part.Position.Length);
        Assert.All(part.Position, v => Assert.Equal(0f, v));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BrickPartInstance MakePart(string instanceId, string partNumber) =>
        new()
        {
            InstanceId = instanceId,
            PartNumber = partNumber,
            PartName = "Test Brick",
            Color = "black",
            Position = [0f, 0f, 0f],
            Step = 1
        };
}
