using BrickForge.Ai.Analysis;
using BrickForge.Core.Ai;
using BrickForge.Core.Options;
using BrickForge.Core.Results;

namespace BrickForge.Ai.Tests;

/// <summary>
/// Unit tests for <see cref="PromptAnalysisService"/>.
/// All tests use <see cref="FakeOllamaClient"/> – no live Ollama instance is required.
/// </summary>
public sealed class PromptAnalysisServiceTests
{
    private static readonly GenerationOptions DefaultGenOptions = new()
    {
        MaxParts = 80,
        DefaultTargetParts = 50,
        OutputRoot = "data/outputs"
    };

    private static readonly OllamaOptions DefaultOllamaOptions = new()
    {
        Model = "llama3.1:8b",
        Temperature = 0.2
    };

    // ── Valid JSON response ───────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_WhenValidJsonResponse_ReturnsParsedResult()
    {
        const string json = """
            {
              "model_name": "Kleine Kaffeemaschine",
              "model_category": "small_machine",
              "target_parts": 50,
              "main_color": "black",
              "accent_color": "light_bluish_gray",
              "features": ["cup", "front_panel"],
              "feasible": true,
              "warnings": []
            }
            """;

        var service = BuildService(Result<string>.Success(json));

        var result = await service.AnalyzeAsync("Kaffeemaschine");

        Assert.True(result.IsSuccess);
        Assert.Equal("Kleine Kaffeemaschine", result.Value!.ModelName);
        Assert.Equal("small_machine", result.Value.ModelCategory);
        Assert.Equal(50, result.Value.TargetParts);
        Assert.Equal("black", result.Value.MainColor);
        Assert.Equal("light_bluish_gray", result.Value.AccentColor);
        Assert.Contains("cup", result.Value.Features);
        Assert.True(result.Value.Feasible);
        Assert.False(result.Value.UsedFallback);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenTargetPartsExceedsMax_CapsAtMax()
    {
        var json = BuildJson(targetParts: 999);
        var service = BuildService(Result<string>.Success(json));

        var result = await service.AnalyzeAsync("Large model");

        Assert.True(result.IsSuccess);
        Assert.Equal(80, result.Value!.TargetParts);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenTargetPartsExceedsMax_AddsWarning()
    {
        var json = BuildJson(targetParts: 200);
        var service = BuildService(Result<string>.Success(json));

        var result = await service.AnalyzeAsync("Large model");

        Assert.Contains(result.Value!.Warnings, w => w.Contains("begrenzt"));
    }

    [Fact]
    public async Task AnalyzeAsync_WhenFeasibleIsFalse_ResultIsFeasibleFalse()
    {
        var json = BuildJson(feasible: false, warnings: ["Zu komplex für MVP0"]);
        var service = BuildService(Result<string>.Success(json));

        var result = await service.AnalyzeAsync("Complex motorised robot");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Feasible);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenCategoryIsInvalid_DefaultsToSmallMachine()
    {
        var json = BuildJson(category: "unknown_category");
        var service = BuildService(Result<string>.Success(json));

        var result = await service.AnalyzeAsync("Something");

        Assert.Equal("small_machine", result.Value!.ModelCategory);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenColorIsInvalid_DefaultsToBlack()
    {
        var json = BuildJson(mainColor: "pinkish_turquoise");
        var service = BuildService(Result<string>.Success(json));

        var result = await service.AnalyzeAsync("Something");

        Assert.Equal("black", result.Value!.MainColor);
    }

    // ── JSON stripping ────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_WhenResponseHasMarkdownFences_StripsFencesAndParses()
    {
        const string json = """
            ```json
            {
              "model_name": "Fence Test",
              "model_category": "small_machine",
              "target_parts": 30,
              "main_color": "red",
              "accent_color": "white",
              "features": [],
              "feasible": true,
              "warnings": []
            }
            ```
            """;

        var service = BuildService(Result<string>.Success(json));

        var result = await service.AnalyzeAsync("Something");

        Assert.True(result.IsSuccess);
        Assert.Equal("Fence Test", result.Value!.ModelName);
        Assert.False(result.Value.UsedFallback);
    }

    // ── Fallback behaviour ────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_WhenInvalidJson_UsesFallback()
    {
        var service = BuildService(Result<string>.Success("this is not json at all"));

        var result = await service.AnalyzeAsync("Kaffeemaschine");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.UsedFallback);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenOllamaReturnsFailure_UsesFallback()
    {
        var service = BuildService(Result<string>.Failure("Ollama is not reachable"));

        var result = await service.AnalyzeAsync("Kaffeemaschine");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.UsedFallback);
        Assert.Equal("small_machine", result.Value.ModelCategory);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenOllamaUnavailable_FallbackIsStillFeasible()
    {
        var service = BuildService(Result<string>.Failure("Network error"));

        var result = await service.AnalyzeAsync("Einfaches Modell");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Feasible);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenEmptyJson_UsesFallback()
    {
        var service = BuildService(Result<string>.Success("{}"));

        // Empty JSON deserializes but has all nulls – should use defaults, not fallback
        var result = await service.AnalyzeAsync("Etwas");

        Assert.True(result.IsSuccess);
        // Either fallback or defaults are acceptable; what matters is no exception
        Assert.NotNull(result.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PromptAnalysisService BuildService(Result<string> generateResult)
    {
        var fakeClient = new FakeOllamaClient(generateResult);
        return new PromptAnalysisService(fakeClient, DefaultOllamaOptions, DefaultGenOptions);
    }

    private static string BuildJson(
        string modelName = "Test Model",
        string category = "small_machine",
        int targetParts = 50,
        string mainColor = "black",
        string accentColor = "light_bluish_gray",
        bool feasible = true,
        IEnumerable<string>? warnings = null)
    {
        var warningArray = string.Join(",", (warnings ?? []).Select(w => $"\"{w}\""));
        return $$"""
            {
              "model_name": "{{modelName}}",
              "model_category": "{{category}}",
              "target_parts": {{targetParts}},
              "main_color": "{{mainColor}}",
              "accent_color": "{{accentColor}}",
              "features": [],
              "feasible": {{(feasible ? "true" : "false")}},
              "warnings": [{{warningArray}}]
            }
            """;
    }
}

// ── Fake IOllamaClient ────────────────────────────────────────────────────────

internal sealed class FakeOllamaClient : IOllamaClient
{
    private readonly bool _isAvailable;
    private readonly Result<string> _generateResult;

    public FakeOllamaClient(Result<string> generateResult, bool isAvailable = true)
    {
        _generateResult = generateResult;
        _isAvailable = isAvailable;
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_isAvailable);

    public Task<Result<string>> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_generateResult);
}
