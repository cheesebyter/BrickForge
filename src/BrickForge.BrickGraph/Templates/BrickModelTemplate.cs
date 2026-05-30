using System.Text.Json.Serialization;

namespace BrickForge.BrickGraph.Templates;

/// <summary>
/// Defines a subassembly section within a brick model template.
/// </summary>
public sealed class TemplateSubassembly
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Preferred part number for this section.</summary>
    [JsonPropertyName("preferred_part")]
    public string PreferredPart { get; init; } = string.Empty;

    /// <summary>Colour applied to this section (overrides template default if set).</summary>
    [JsonPropertyName("color")]
    public string? Color { get; init; }

    /// <summary>Fraction of total target parts allocated to this section (0..1).</summary>
    [JsonPropertyName("budget_fraction")]
    public float BudgetFraction { get; init; }
}

/// <summary>
/// Defines a brick model template including dimensions, colours and subassemblies.
/// Templates drive the deterministic BrickGraph generator.
/// </summary>
public sealed class BrickModelTemplate
{
    [JsonPropertyName("template_id")]
    public string TemplateId { get; init; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Width in LDraw units (1 stud = 20 LDraw units).</summary>
    [JsonPropertyName("width_studs")]
    public int WidthStuds { get; init; }

    /// <summary>Depth in LDraw units.</summary>
    [JsonPropertyName("depth_studs")]
    public int DepthStuds { get; init; }

    /// <summary>Height in brick layers (1 brick = 3 plates = 24 LDraw units tall).</summary>
    [JsonPropertyName("height_layers")]
    public int HeightLayers { get; init; }

    [JsonPropertyName("default_main_color")]
    public string DefaultMainColor { get; init; } = "black";

    [JsonPropertyName("default_accent_color")]
    public string DefaultAccentColor { get; init; } = "light_bluish_gray";

    [JsonPropertyName("subassemblies")]
    public IReadOnlyList<TemplateSubassembly> Subassemblies { get; init; } = [];
}
