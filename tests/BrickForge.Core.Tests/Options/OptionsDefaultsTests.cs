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
    public void GenerationOptions_MaxPromptLength_DefaultIsPositive()
    {
        // BF-MVP1-019 §19.5: prompt length guard must have a positive default.
        var options = new GenerationOptions();

        Assert.True(options.MaxPromptLength > 0, "MaxPromptLength must be a positive integer.");
    }

    [Fact]
    public void OllamaOptions_Defaults_AreValid()
    {
        var options = new OllamaOptions();

        Assert.Equal("http://localhost:11434", options.BaseUrl);
        Assert.False(string.IsNullOrWhiteSpace(options.PlanningModel));
        Assert.False(string.IsNullOrWhiteSpace(options.FallbackModel));
        Assert.True(options.TimeoutSeconds > 0);
        Assert.InRange(options.Temperature, 0.0, 1.0);
    }

    [Fact]
    public void OllamaOptions_PlanningModel_DefaultIsNotEmpty()
    {
        // BF-MVP1-020 §20.1: planning model must always have a configured default.
        var options = new OllamaOptions();

        Assert.False(string.IsNullOrWhiteSpace(options.PlanningModel));
    }

    [Fact]
    public void OllamaOptions_FallbackModel_DefaultIsNotEmpty()
    {
        // BF-MVP1-020 §20.1: fallback model must always have a configured default.
        var options = new OllamaOptions();

        Assert.False(string.IsNullOrWhiteSpace(options.FallbackModel));
    }

    [Fact]
    public void ExternalAiOptions_Defaults_AreDisabled()
    {
        // BF-MVP1-020 §20.4: external AI must be disabled by default.
        var options = new ExternalAiOptions();

        Assert.False(options.Enabled, "External AI must be disabled by default.");
    }

    [Fact]
    public void ExternalAiOptions_LogExternalUsage_DefaultIsTrue()
    {
        // BF-MVP1-020 §20.4: logging of external usage must be enabled by default when the feature is turned on.
        var options = new ExternalAiOptions();

        Assert.True(options.LogExternalUsage);
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
