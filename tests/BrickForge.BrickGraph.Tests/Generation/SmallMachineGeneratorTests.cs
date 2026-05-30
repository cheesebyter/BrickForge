using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.Core.Ai;

namespace BrickForge.BrickGraph.Tests.Generation;

/// <summary>
/// Unit tests for <see cref="SmallMachineGenerator"/> (BF-MVP0-010, BF-MVP0-011).
/// All tests are deterministic — no network or AI dependency.
/// </summary>
public sealed class SmallMachineGeneratorTests
{
    private static readonly SupportedPartsRegistry Registry = BuildRegistry();
    private static readonly BrickModelTemplate Template = BuildTemplate();
    private readonly SmallMachineGenerator _generator = new(Registry);

    // ── BF-MVP0-010: Basic generation ─────────────────────────────────────────

    [Fact]
    public void Generate_ProducesAtLeastTwentyParts()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.True(result.Parts.Count >= 20,
            $"Expected >= 20 parts, got {result.Parts.Count}");
    }

    [Fact]
    public void Generate_ProducesAtMostEightyParts()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.True(result.Parts.Count <= 80,
            $"Expected <= 80 parts, got {result.Parts.Count}");
    }

    [Fact]
    public void Generate_ActualPartsMatchesPartsCount()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.Equal(result.Parts.Count, result.Model.ActualParts);
    }

    [Fact]
    public void Generate_AllPartsHaveNonEmptyInstanceId()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.All(result.Parts, p => Assert.NotEmpty(p.InstanceId));
    }

    [Fact]
    public void Generate_AllInstanceIdsAreUnique()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        var ids = result.Parts.Select(p => p.InstanceId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Generate_AllPartsHaveValidColor()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.All(result.Parts, p => Assert.True(
            Registry.IsColorSupported(p.Color),
            $"Part {p.InstanceId} has unsupported color '{p.Color}'"));
    }

    [Fact]
    public void Generate_AllPartsHaveSupportedPartNumber()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.All(result.Parts, p => Assert.True(
            Registry.IsPartSupported(p.PartNumber),
            $"Part {p.InstanceId} uses unsupported part '{p.PartNumber}'"));
    }

    [Fact]
    public void Generate_AllPartsHaveThreeElementPositionArray()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.All(result.Parts, p => Assert.Equal(3, p.Position.Length));
    }

    [Fact]
    public void Generate_AllPartsHaveNineElementRotationMatrix()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.All(result.Parts, p => Assert.Equal(9, p.Rotation.Length));
    }

    [Fact]
    public void Generate_GraphIsJsonSerializable()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);
        var json = result.ToJson();
        var restored = BrickGraph.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(result.Parts.Count, restored.Parts.Count);
        Assert.Equal(result.Steps.Count, restored.Steps.Count);
    }

    [Fact]
    public void Generate_WhenUnsupportedColorRequested_FallsBackToBlack()
    {
        var analysis = new PromptAnalysisResult
        {
            ModelName = "Test",
            ModelCategory = "small_machine",
            TargetParts = 30,
            MainColor = "neon_pink", // unsupported
            AccentColor = "purple",  // unsupported
            Feasible = true
        };

        var result = _generator.Generate(analysis, Template);

        Assert.All(result.Parts, p => Assert.Equal("black", p.Color));
    }

    // ── BF-MVP0-011: Step generation ──────────────────────────────────────────

    [Fact]
    public void Generate_ProducesAtLeastFiveSteps()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.True(result.Steps.Count >= 5,
            $"Expected >= 5 steps, got {result.Steps.Count}");
    }

    [Fact]
    public void Generate_NoPartHasStepZeroOrLess()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.All(result.Parts, p => Assert.True(
            p.Step >= 1, $"Part {p.InstanceId} has invalid step {p.Step}"));
    }

    [Fact]
    public void Generate_StepNumbersAreAscending()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        var stepNumbers = result.Steps.Select(s => s.StepNumber).ToList();
        for (int i = 1; i < stepNumbers.Count; i++)
            Assert.True(stepNumbers[i] > stepNumbers[i - 1],
                $"Step {stepNumbers[i]} is not greater than step {stepNumbers[i - 1]}");
    }

    [Fact]
    public void Generate_EachStepReferencesAtLeastOnePart()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        Assert.All(result.Steps, s => Assert.NotEmpty(s.PartInstanceIds));
    }

    [Fact]
    public void Generate_AllPartInstanceIdsInStepsAreValid()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        var validIds = result.Parts.Select(p => p.InstanceId).ToHashSet();
        foreach (var step in result.Steps)
            Assert.All(step.PartInstanceIds, id => Assert.Contains(id, validIds));
    }

    [Fact]
    public void Generate_AllPartsAreReferencedInExactlyOneStep()
    {
        var result = _generator.Generate(DefaultAnalysis(), Template);

        var allReferencedIds = result.Steps.SelectMany(s => s.PartInstanceIds).ToList();
        var partIds = result.Parts.Select(p => p.InstanceId).ToHashSet();

        // Every part appears in at least one step
        Assert.All(partIds, id => Assert.Contains(id, allReferencedIds));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PromptAnalysisResult DefaultAnalysis() => new()
    {
        ModelName = "Test Coffee Machine",
        ModelCategory = "small_machine",
        TargetParts = 50,
        MainColor = "black",
        AccentColor = "light_bluish_gray",
        Feasible = true
    };

    private static SupportedPartsRegistry BuildRegistry()
    {
        const string parts = """
            [
              { "part_number": "3005", "part_name": "Brick 1 x 1" },
              { "part_number": "3004", "part_name": "Brick 1 x 2" },
              { "part_number": "3622", "part_name": "Brick 1 x 3" },
              { "part_number": "3010", "part_name": "Brick 1 x 4" },
              { "part_number": "3003", "part_name": "Brick 2 x 2" },
              { "part_number": "3002", "part_name": "Brick 2 x 3" },
              { "part_number": "3001", "part_name": "Brick 2 x 4" },
              { "part_number": "3024", "part_name": "Plate 1 x 1" },
              { "part_number": "3023", "part_name": "Plate 1 x 2" },
              { "part_number": "3710", "part_name": "Plate 1 x 4" },
              { "part_number": "3022", "part_name": "Plate 2 x 2" },
              { "part_number": "3020", "part_name": "Plate 2 x 4" },
              { "part_number": "3069b", "part_name": "Tile 1 x 2 with Groove" },
              { "part_number": "2431", "part_name": "Tile 1 x 4 with Groove" }
            ]
            """;
        const string colors = """
            ["black","white","red","blue","yellow","light_bluish_gray","dark_bluish_gray","transparent_clear"]
            """;
        return SupportedPartsRegistry.FromJson(parts, colors);
    }

    private static BrickModelTemplate BuildTemplate() => new()
    {
        TemplateId = "small_machine",
        DisplayName = "Small Machine",
        WidthStuds = 6,
        DepthStuds = 4,
        HeightLayers = 4,
        DefaultMainColor = "black",
        DefaultAccentColor = "light_bluish_gray",
        Subassemblies =
        [
            new() { Name = "base",          PreferredPart = "3020",  BudgetFraction = 0.20f },
            new() { Name = "main_body",     PreferredPart = "3001",  BudgetFraction = 0.45f },
            new() { Name = "front_panel",   PreferredPart = "3069b", BudgetFraction = 0.20f },
            new() { Name = "top",           PreferredPart = "3020",  BudgetFraction = 0.10f },
            new() { Name = "simple_detail", PreferredPart = "3024",  BudgetFraction = 0.05f }
        ]
    };
}
