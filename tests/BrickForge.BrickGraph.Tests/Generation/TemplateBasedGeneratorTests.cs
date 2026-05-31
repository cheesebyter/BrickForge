using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.Core.Ai;

namespace BrickForge.BrickGraph.Tests.Generation;

public class TemplateBasedGeneratorTests
{
    private static SupportedPartsRegistry BuildRegistry()
    {
        var partsJson = """
            [
              {"part_number":"3005","part_name":"Brick 1x1"},
              {"part_number":"3004","part_name":"Brick 1x2"},
              {"part_number":"3622","part_name":"Brick 1x3"},
              {"part_number":"3010","part_name":"Brick 1x4"},
              {"part_number":"3003","part_name":"Brick 2x2"},
              {"part_number":"3002","part_name":"Brick 2x3"},
              {"part_number":"3001","part_name":"Brick 2x4"},
              {"part_number":"3024","part_name":"Plate 1x1"},
              {"part_number":"3023","part_name":"Plate 1x2"},
              {"part_number":"3710","part_name":"Plate 1x4"},
              {"part_number":"3022","part_name":"Plate 2x2"},
              {"part_number":"3020","part_name":"Plate 2x4"},
              {"part_number":"3069b","part_name":"Tile 1x2"},
              {"part_number":"2431","part_name":"Tile 1x4"}
            ]
            """;
        var colorsJson = """["black","white","red","blue","yellow","light_bluish_gray","dark_bluish_gray","transparent_clear"]""";
        return SupportedPartsRegistry.FromJson(partsJson, colorsJson);
    }

    private static BrickModelTemplate BuildTemplate(string id = "small_machine",
        int width = 6, int depth = 4, int height = 4,
        string mainColor = "black", string accentColor = "light_bluish_gray")
        => new()
        {
            TemplateId        = id,
            DisplayName       = id,
            WidthStuds        = width,
            DepthStuds        = depth,
            HeightLayers      = height,
            DefaultMainColor  = mainColor,
            DefaultAccentColor = accentColor,
            Subassemblies     =
            [
                new() { Name = "base",          PreferredPart = "3020" },
                new() { Name = "main_body",     PreferredPart = "3001" },
                new() { Name = "front_panel",   PreferredPart = "3069b" },
                new() { Name = "top",           PreferredPart = "3020" },
                new() { Name = "simple_detail", PreferredPart = "3024" }
            ]
        };

    private static PromptAnalysisResult BuildAnalysis(
        string category = "small_machine",
        string mainColor = "black",
        string accentColor = "light_bluish_gray")
        => new()
        {
            ModelName     = "Test Model",
            ModelCategory = category,
            TargetParts   = 50,
            MainColor     = mainColor,
            AccentColor   = accentColor,
            Features      = [],
            Feasible      = true
        };

    [Fact]
    public void Generate_SmallMachineTemplate_ProducesNonEmptyGraph()
    {
        var gen    = new TemplateBasedGenerator(BuildRegistry());
        var graph  = gen.Generate(BuildAnalysis(), BuildTemplate());
        Assert.NotEmpty(graph.Parts);
    }

    [Fact]
    public void Generate_AllPartIdsAreUnique()
    {
        var gen   = new TemplateBasedGenerator(BuildRegistry());
        var graph = gen.Generate(BuildAnalysis(), BuildTemplate());
        var ids   = graph.Parts.Select(p => p.InstanceId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Generate_AllPartsHavePositiveStepNumber()
    {
        var gen   = new TemplateBasedGenerator(BuildRegistry());
        var graph = gen.Generate(BuildAnalysis(), BuildTemplate());
        Assert.All(graph.Parts, p => Assert.True(p.Step >= 1));
    }

    [Fact]
    public void Generate_AllPartsHavePosition()
    {
        var gen   = new TemplateBasedGenerator(BuildRegistry());
        var graph = gen.Generate(BuildAnalysis(), BuildTemplate());
        Assert.All(graph.Parts, p =>
        {
            Assert.NotNull(p.Position);
            Assert.Equal(3, p.Position.Length);
        });
    }

    [Fact]
    public void Generate_AllPartsHaveNonEmptyColor()
    {
        var gen   = new TemplateBasedGenerator(BuildRegistry());
        var graph = gen.Generate(BuildAnalysis(), BuildTemplate());
        Assert.All(graph.Parts, p => Assert.False(string.IsNullOrEmpty(p.Color)));
    }

    [Fact]
    public void Generate_StepsAreRecorded()
    {
        var gen   = new TemplateBasedGenerator(BuildRegistry());
        var graph = gen.Generate(BuildAnalysis(), BuildTemplate());
        Assert.NotEmpty(graph.Steps);
    }

    [Fact]
    public void Generate_StepPartIdsReferenceExistingParts()
    {
        var gen    = new TemplateBasedGenerator(BuildRegistry());
        var graph  = gen.Generate(BuildAnalysis(), BuildTemplate());
        var partIds = graph.Parts.Select(p => p.InstanceId).ToHashSet();
        foreach (var step in graph.Steps)
            Assert.All(step.PartInstanceIds, id => Assert.Contains(id, partIds));
    }

    [Fact]
    public void Generate_UsesMainColorFromAnalysis()
    {
        var gen      = new TemplateBasedGenerator(BuildRegistry());
        var analysis = BuildAnalysis(mainColor: "red");
        var graph    = gen.Generate(analysis, BuildTemplate(mainColor: "black"));
        Assert.Contains(graph.Parts, p => p.Color == "red");
    }

    [Fact]
    public void Generate_FallsBackToTemplateColor_WhenAnalysisColorUnsupported()
    {
        var gen      = new TemplateBasedGenerator(BuildRegistry());
        var analysis = BuildAnalysis(mainColor: "magenta"); // unsupported
        var graph    = gen.Generate(analysis, BuildTemplate(mainColor: "blue"));
        // should fall back to "blue" or ultimately "black"
        Assert.All(graph.Parts, p => Assert.NotEqual("magenta", p.Color));
    }

    [Theory]
    [InlineData("small_machine", 6, 4, 4, "black")]
    [InlineData("small_building", 8, 6, 5, "red")]
    [InlineData("small_vehicle", 6, 8, 3, "blue")]
    [InlineData("furniture", 6, 4, 3, "light_bluish_gray")]
    [InlineData("display_object", 4, 4, 6, "yellow")]
    public void Generate_AllFiveTemplates_ProduceValidGraphs(
        string id, int w, int d, int h, string color)
    {
        var gen      = new TemplateBasedGenerator(BuildRegistry());
        var template = BuildTemplate(id, w, d, h, color, "white");
        var analysis = BuildAnalysis(id, color);
        var graph    = gen.Generate(analysis, template);

        Assert.NotEmpty(graph.Parts);
        Assert.NotEmpty(graph.Steps);
        var ids = graph.Parts.Select(p => p.InstanceId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(graph.Parts, p => Assert.True(p.Step >= 1));
    }
}
