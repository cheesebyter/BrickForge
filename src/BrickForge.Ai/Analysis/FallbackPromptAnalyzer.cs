using BrickForge.Core.Ai;
using BrickForge.Core.Options;
using BrickForge.Core.Results;

namespace BrickForge.Ai.Analysis;

/// <summary>
/// Rule-based fallback analyser used when the AI returns invalid or unparseable JSON.
/// Provides a safe, deterministic result so the pipeline can continue.
/// </summary>
public sealed class FallbackPromptAnalyzer
{
    private readonly GenerationOptions _options;

    public FallbackPromptAnalyzer(GenerationOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Produces a minimal <see cref="PromptAnalysisResult"/> from keyword matching.
    /// The result always has <see cref="PromptAnalysisResult.UsedFallback"/> set to <c>true</c>.
    /// </summary>
    public PromptAnalysisResult Analyze(string userPrompt)
    {
        var lower = userPrompt.ToLowerInvariant();
        var category = DetermineCategory(lower);

        return new PromptAnalysisResult
        {
            ModelName = "Brick Model",
            ModelCategory = category,
            TargetParts = _options.DefaultTargetParts,
            MainColor = "black",
            AccentColor = "light_bluish_gray",
            Features = [],
            Feasible = true,
            Warnings = ["Fallback-Analyse wurde verwendet. Die KI-Antwort konnte nicht verarbeitet werden."],
            UsedFallback = true
        };
    }

    private static string DetermineCategory(string lower)
    {
        if (lower.Contains("kaffee") || lower.Contains("kaffeemaschine") ||
            lower.Contains("maschine") || lower.Contains("drucker") ||
            lower.Contains("toaster") || lower.Contains("gerät"))
            return "small_machine";

        if (lower.Contains("haus") || lower.Contains("gebäude") ||
            lower.Contains("gebaude") || lower.Contains("building") ||
            lower.Contains("hütte") || lower.Contains("huette"))
            return "small_building";

        if (lower.Contains("auto") || lower.Contains("fahrzeug") ||
            lower.Contains("car") || lower.Contains("vehicle") ||
            lower.Contains("bus") || lower.Contains("lkw"))
            return "small_vehicle";

        return "display_object";
    }
}
