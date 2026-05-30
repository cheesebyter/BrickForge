using BrickForge.Core.Options;

namespace BrickForge.Core.Tests.Options;

/// <summary>
/// Verifies that all typed options classes carry correct default values
/// so that missing config entries never cause null-reference failures.
/// </summary>
public sealed class OptionsDefaultsTests
{
    [Fact]
    public void GenerationOptions_Defaults_AreValid()
    {
        var options = new GenerationOptions();

        Assert.Equal(80, options.MaxParts);
        Assert.Equal(50, options.DefaultTargetParts);
        Assert.Equal("data/outputs", options.OutputRoot);
        Assert.True(options.DefaultTargetParts <= options.MaxParts);
    }

    [Fact]
    public void OllamaOptions_Defaults_AreValid()
    {
        var options = new OllamaOptions();

        Assert.Equal("http://localhost:11434", options.BaseUrl);
        Assert.Equal("llama3.1:8b", options.Model);
        Assert.True(options.TimeoutSeconds > 0);
        Assert.InRange(options.Temperature, 0.0, 1.0);
    }

    [Fact]
    public void ExportOptions_Defaults_AllEnabled()
    {
        var options = new ExportOptions();

        Assert.True(options.GenerateMpd);
        Assert.True(options.GenerateCsv);
        Assert.True(options.GenerateMarkdown);
    }

    [Fact]
    public void OllamaOptions_BaseUrl_IsLocalhost()
    {
        var options = new OllamaOptions();

        // Default must not point to any external service
        Assert.StartsWith("http://localhost", options.BaseUrl);
    }

    [Fact]
    public void GenerationOptions_OutputRoot_IsRelativePath()
    {
        var options = new GenerationOptions();

        Assert.False(Path.IsPathRooted(options.OutputRoot),
            "OutputRoot must be a relative path to prevent path traversal.");
    }
}
