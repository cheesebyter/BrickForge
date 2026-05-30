using BrickForge.BrickGraph.Parts;

namespace BrickForge.BrickGraph.Tests.Parts;

/// <summary>
/// Unit tests for <see cref="SupportedPartsRegistry"/> (BF-MVP0-008).
/// </summary>
public sealed class SupportedPartsRegistryTests
{
    private static readonly string PartsJson = """
        [
          { "part_number": "3005", "part_name": "Brick 1 x 1" },
          { "part_number": "3004", "part_name": "Brick 1 x 2" },
          { "part_number": "3001", "part_name": "Brick 2 x 4" },
          { "part_number": "3069b", "part_name": "Tile 1 x 2 with Groove" }
        ]
        """;

    private static readonly string ColorsJson = """
        ["black","white","red","blue","light_bluish_gray"]
        """;

    private readonly SupportedPartsRegistry _registry =
        SupportedPartsRegistry.FromJson(PartsJson, ColorsJson);

    [Fact]
    public void FindPart_WhenPartExists_ReturnsDefinition()
    {
        var def = _registry.FindPart("3001");

        Assert.NotNull(def);
        Assert.Equal("3001", def.PartNumber);
        Assert.Equal("Brick 2 x 4", def.PartName);
    }

    [Fact]
    public void FindPart_WhenPartDoesNotExist_ReturnsNull()
    {
        var def = _registry.FindPart("9999");

        Assert.Null(def);
    }

    [Fact]
    public void IsPartSupported_WhenKnownPart_ReturnsTrue()
    {
        Assert.True(_registry.IsPartSupported("3005"));
    }

    [Fact]
    public void IsPartSupported_WhenUnknownPart_ReturnsFalse()
    {
        Assert.False(_registry.IsPartSupported("0000"));
    }

    [Fact]
    public void IsColorSupported_WhenKnownColor_ReturnsTrue()
    {
        Assert.True(_registry.IsColorSupported("black"));
        Assert.True(_registry.IsColorSupported("light_bluish_gray"));
        Assert.True(_registry.IsColorSupported("red"));
    }

    [Fact]
    public void IsColorSupported_WhenUnknownColor_ReturnsFalse()
    {
        Assert.False(_registry.IsColorSupported("neon_pink"));
        Assert.False(_registry.IsColorSupported(""));
    }

    [Fact]
    public void IsPartSupported_IsCaseInsensitive()
    {
        Assert.True(_registry.IsPartSupported("3069B"));
        Assert.True(_registry.IsPartSupported("3069b"));
    }

    [Fact]
    public void IsColorSupported_IsCaseInsensitive()
    {
        Assert.True(_registry.IsColorSupported("BLACK"));
        Assert.True(_registry.IsColorSupported("Black"));
    }

    [Fact]
    public void SupportedPartNumbers_ContainsAllLoadedParts()
    {
        Assert.Contains("3001", _registry.SupportedPartNumbers);
        Assert.Contains("3069b", _registry.SupportedPartNumbers);
    }

    [Fact]
    public void SupportedColors_ContainsAllLoadedColors()
    {
        Assert.Contains("black", _registry.SupportedColors);
        Assert.Contains("light_bluish_gray", _registry.SupportedColors);
    }

    [Fact]
    public void FromJson_WhenFullMvp0List_LoadsAllFourteenPartsAndEightColors()
    {
        // Inline the canonical MVP0 lists to keep the test deterministic and environment-independent.
        const string partsJson = """
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
        const string colorsJson = """
            ["black","white","red","blue","yellow","light_bluish_gray","dark_bluish_gray","transparent_clear"]
            """;

        var registry = SupportedPartsRegistry.FromJson(partsJson, colorsJson);

        Assert.Equal(14, registry.SupportedPartNumbers.Count);
        Assert.Equal(8, registry.SupportedColors.Count);
    }
}
