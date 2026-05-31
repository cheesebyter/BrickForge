using BrickForge.BrickGraph.Model;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Repair;
using BrickForge.BrickGraph.Templates;
using BrickForge.Core.Agents;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrickForge.BrickGraph.Tests.Repair;

public sealed class BrickGraphRepairAgentTests
{
    private static readonly string[] SupportedColors = ["black", "white", "light_bluish_gray", "red", "blue"];
    private static readonly PartDefinition[] SupportedParts =
    [
        new PartDefinition { PartNumber = "3001", PartName = "Brick 2x4" },
        new PartDefinition { PartNumber = "3005", PartName = "Brick 1x1" },
    ];

    private static SupportedPartsRegistry BuildRegistry() =>
        new(SupportedParts, SupportedColors);

    private static BrickGraphRepairAgent BuildAgent(SupportedPartsRegistry? registry = null) =>
        new(registry ?? BuildRegistry(), NullLogger<BrickGraphRepairAgent>.Instance);

    private static BrickPartInstance MakePart(
        string id       = "part_001",
        string color    = "black",
        int    step     = 1,
        string partNum  = "3001") =>
        new()
        {
            InstanceId  = id,
            PartNumber  = partNum,
            PartName    = "Brick",
            Color       = color,
            Step        = step,
            Position    = [0f, 0f, 0f],
            Rotation    = [1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f]
        };

    private static BrickGraph BuildGraph(params BrickPartInstance[] parts)
    {
        var g = new BrickGraph
        {
            Model = new BrickModelMetadata
            {
                Id          = "test",
                Name        = "Test",
                TargetParts = parts.Length,
                ActualParts = parts.Length
            }
        };
        foreach (var p in parts)
            g.Parts.Add(p);
        return g;
    }

    private static AgentContext Ctx() => new() { JobId = "job_test" };

    // ── Color repair ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RepairAsync_WhenColorIsUnsupported_ReplacesWithFallbackColor()
    {
        var agent = BuildAgent();
        var graph = BuildGraph(MakePart(color: "pink_unsupported"));
        var req   = new RepairRequest(graph);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        var repairedColor = result.Value!.Parts[0].Color;
        Assert.Equal("light_bluish_gray", repairedColor);
    }

    [Fact]
    public async Task RepairAsync_WhenTemplateHasSupportedMainColor_UsesTemplateColor()
    {
        var registry = BuildRegistry();
        var agent    = BuildAgent(registry);
        var template = new BrickModelTemplate { TemplateId = "t1", DefaultMainColor = "red" };
        var graph    = BuildGraph(MakePart(color: "invalid_color"));
        var req      = new RepairRequest(graph, template);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal("red", result.Value!.Parts[0].Color);
    }

    [Fact]
    public async Task RepairAsync_WhenColorIsSupported_DoesNotChangeColor()
    {
        var agent = BuildAgent();
        var graph = BuildGraph(MakePart(color: "black"));
        var req   = new RepairRequest(graph);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal("black", result.Value!.Parts[0].Color);
    }

    // ── Step repair ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RepairAsync_WhenStepIsZero_SetsStepToOne()
    {
        var agent = BuildAgent();
        var graph = BuildGraph(MakePart(step: 0));
        var req   = new RepairRequest(graph);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Parts[0].Step);
    }

    [Fact]
    public async Task RepairAsync_WhenStepIsNegative_SetsStepToOne()
    {
        var agent = BuildAgent();
        var graph = BuildGraph(MakePart(step: -5));
        var req   = new RepairRequest(graph);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Parts[0].Step);
    }

    [Fact]
    public async Task RepairAsync_WhenStepIsValid_DoesNotChangeStep()
    {
        var agent = BuildAgent();
        var graph = BuildGraph(MakePart(step: 3));
        var req   = new RepairRequest(graph);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Parts[0].Step);
    }

    // ── Trim excess ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RepairAsync_WhenPartCountExceedsMax_TrimsToLimit()
    {
        var agent = BuildAgent();
        var parts = Enumerable.Range(1, 10)
            .Select(i => MakePart(id: $"part_{i:D3}", step: i))
            .ToArray();
        var graph = BuildGraph(parts);
        var req   = new RepairRequest(graph, MaxParts: 5);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.Parts.Count);
    }

    [Fact]
    public async Task RepairAsync_WhenTrimming_KeepsLowestStepParts()
    {
        var agent = BuildAgent();
        // Steps: 5, 1, 3, 2, 4 — after sort keep steps 1, 2 when MaxParts=2
        var parts = new[]
        {
            MakePart("p5", step: 5),
            MakePart("p1", step: 1),
            MakePart("p3", step: 3),
            MakePart("p2", step: 2),
            MakePart("p4", step: 4),
        };
        var graph = BuildGraph(parts);
        var req   = new RepairRequest(graph, MaxParts: 2);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        var remaining = result.Value!.Parts;
        Assert.Equal(2, remaining.Count);
        Assert.Contains(remaining, p => p.InstanceId == "p1");
        Assert.Contains(remaining, p => p.InstanceId == "p2");
    }

    [Fact]
    public async Task RepairAsync_WhenPartCountIsWithinLimit_DoesNotTrim()
    {
        var agent = BuildAgent();
        var graph = BuildGraph(MakePart("p1"), MakePart("p2"));
        var req   = new RepairRequest(graph, MaxParts: 80);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Parts.Count);
    }

    // ── Empty graph ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RepairAsync_WhenGraphHasNoParts_ReturnsEmptyGraph()
    {
        var agent = BuildAgent();
        var graph = BuildGraph();
        var req   = new RepairRequest(graph);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Parts);
    }

    // ── Does not mutate original ──────────────────────────────────────────────

    [Fact]
    public async Task RepairAsync_NeverMutatesOriginalGraph()
    {
        var agent = BuildAgent();
        var original = BuildGraph(MakePart(color: "invalid_color", step: 0));
        var req      = new RepairRequest(original);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        // Original part should be unchanged
        Assert.Equal("invalid_color", original.Parts[0].Color);
        Assert.Equal(0, original.Parts[0].Step);
    }

    // ── ActualParts metadata ──────────────────────────────────────────────────

    [Fact]
    public async Task RepairAsync_WhenTrimming_UpdatesActualPartsInMetadata()
    {
        var agent = BuildAgent();
        var parts = Enumerable.Range(1, 10)
            .Select(i => MakePart(id: $"part_{i:D3}", step: i))
            .ToArray();
        var graph = BuildGraph(parts);
        var req   = new RepairRequest(graph, MaxParts: 3);

        var result = await agent.RunAsync(req, Ctx());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Model.ActualParts);
    }
}
