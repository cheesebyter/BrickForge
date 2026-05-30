using BrickForge.BrickGraph.Templates;

namespace BrickForge.BrickGraph.Tests.Templates;

/// <summary>
/// Unit tests for the small_machine template (BF-MVP0-009).
/// </summary>
public sealed class SmallMachineTemplateTests
{
    private static readonly string TemplateJson = """
        {
          "template_id": "small_machine",
          "display_name": "Small Machine (Kaffeemaschine / Werkzeugmaschine)",
          "width_studs": 6,
          "depth_studs": 4,
          "height_layers": 4,
          "default_main_color": "black",
          "default_accent_color": "light_bluish_gray",
          "subassemblies": [
            { "name": "base",         "preferred_part": "3020",  "color": null, "budget_fraction": 0.20 },
            { "name": "main_body",    "preferred_part": "3001",  "color": null, "budget_fraction": 0.45 },
            { "name": "front_panel",  "preferred_part": "3069b", "color": null, "budget_fraction": 0.20 },
            { "name": "top",          "preferred_part": "3020",  "color": null, "budget_fraction": 0.10 },
            { "name": "simple_detail","preferred_part": "3024",  "color": null, "budget_fraction": 0.05 }
          ]
        }
        """;

    private readonly TemplateRegistry _registry = TemplateRegistry.FromJson(TemplateJson);

    [Fact]
    public void Template_CanBeFoundById()
    {
        var template = _registry.FindTemplate("small_machine");

        Assert.NotNull(template);
    }

    [Fact]
    public void Template_HasExpectedDimensions()
    {
        var template = _registry.FindTemplate("small_machine")!;

        Assert.Equal(6, template.WidthStuds);
        Assert.Equal(4, template.DepthStuds);
        Assert.Equal(4, template.HeightLayers);
    }

    [Fact]
    public void Template_HasExpectedDefaultColors()
    {
        var template = _registry.FindTemplate("small_machine")!;

        Assert.Equal("black", template.DefaultMainColor);
        Assert.Equal("light_bluish_gray", template.DefaultAccentColor);
    }

    [Fact]
    public void Template_HasFiveSubassemblies()
    {
        var template = _registry.FindTemplate("small_machine")!;

        Assert.Equal(5, template.Subassemblies.Count);
    }

    [Fact]
    public void Template_SubassembliesHaveExpectedNames()
    {
        var template = _registry.FindTemplate("small_machine")!;
        var names = template.Subassemblies.Select(s => s.Name).ToList();

        Assert.Contains("base", names);
        Assert.Contains("main_body", names);
        Assert.Contains("front_panel", names);
        Assert.Contains("top", names);
        Assert.Contains("simple_detail", names);
    }

    [Fact]
    public void Template_BudgetFractionsAddUpToOne()
    {
        var template = _registry.FindTemplate("small_machine")!;
        var total = template.Subassemblies.Sum(s => s.BudgetFraction);

        Assert.Equal(1.0f, total, precision: 5);
    }

    [Fact]
    public void Template_AllSubassembliesHavePreferredPart()
    {
        var template = _registry.FindTemplate("small_machine")!;

        Assert.All(template.Subassemblies, s => Assert.NotEmpty(s.PreferredPart));
    }

    [Fact]
    public void FindTemplate_WhenIdNotFound_ReturnsNull()
    {
        var result = _registry.FindTemplate("unknown_template");

        Assert.Null(result);
    }

    [Fact]
    public void TemplateRegistry_TemplateIds_ContainsSmallMachine()
    {
        Assert.Contains("small_machine", _registry.TemplateIds);
    }
}
