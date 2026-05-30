using BrickForge.Ai.Analysis;
using BrickForge.Core.Options;

namespace BrickForge.Ai.Tests;

/// <summary>
/// Unit tests for <see cref="FallbackPromptAnalyzer"/>.
/// These tests run deterministically without any AI or network dependency.
/// </summary>
public sealed class FallbackPromptAnalyzerTests
{
    private readonly FallbackPromptAnalyzer _analyzer = new(new GenerationOptions
    {
        MaxParts = 80,
        DefaultTargetParts = 50,
        OutputRoot = "data/outputs"
    });

    [Theory]
    [InlineData("Erstelle eine kleine Kaffeemaschine", "small_machine")]
    [InlineData("Kaffeemaschine mit Tasse", "small_machine")]
    [InlineData("Eine grosse Maschine bauen", "small_machine")]
    public void Analyze_WhenPromptContainsMachineKeyword_ReturnsSmallMachine(
        string prompt, string expectedCategory)
    {
        var result = _analyzer.Analyze(prompt);

        Assert.Equal(expectedCategory, result.ModelCategory);
    }

    [Theory]
    [InlineData("Ein kleines Haus bauen")]
    [InlineData("Gebäude mit rotem Dach")]
    [InlineData("Kleines Wohngebäude")]
    public void Analyze_WhenPromptContainsBuildingKeyword_ReturnsSmallBuilding(string prompt)
    {
        var result = _analyzer.Analyze(prompt);

        Assert.Equal("small_building", result.ModelCategory);
    }

    [Theory]
    [InlineData("Ein rotes Auto")]
    [InlineData("Kleines Fahrzeug mit vier Rädern")]
    [InlineData("Gelber Bus")]
    public void Analyze_WhenPromptContainsVehicleKeyword_ReturnsSmallVehicle(string prompt)
    {
        var result = _analyzer.Analyze(prompt);

        Assert.Equal("small_vehicle", result.ModelCategory);
    }

    [Theory]
    [InlineData("Ein buntes Objekt")]
    [InlineData("Irgendwas Schönes")]
    [InlineData("Dekoration für den Tisch")]
    public void Analyze_WhenNoKnownKeyword_ReturnsDisplayObject(string prompt)
    {
        var result = _analyzer.Analyze(prompt);

        Assert.Equal("display_object", result.ModelCategory);
    }

    [Fact]
    public void Analyze_AlwaysSetsUsedFallbackTrue()
    {
        var result = _analyzer.Analyze("anything");

        Assert.True(result.UsedFallback);
    }

    [Fact]
    public void Analyze_AlwaysFeasibleTrue()
    {
        var result = _analyzer.Analyze("anything");

        Assert.True(result.Feasible);
    }

    [Fact]
    public void Analyze_TargetPartsEqualsDefaultTargetParts()
    {
        var result = _analyzer.Analyze("anything");

        Assert.Equal(50, result.TargetParts);
    }

    [Fact]
    public void Analyze_WarningsContainsFallbackNotice()
    {
        var result = _analyzer.Analyze("anything");

        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void Analyze_DefaultColorsAreBlackAndLightBluishGray()
    {
        var result = _analyzer.Analyze("anything");

        Assert.Equal("black", result.MainColor);
        Assert.Equal("light_bluish_gray", result.AccentColor);
    }
}
