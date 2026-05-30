using BrickForge.Core.Results;

namespace BrickForge.Ai;

/// <summary>
/// A local mock implementation of <see cref="IOllamaClient"/> that returns a fixed,
/// valid JSON analysis without making any HTTP calls.
///
/// Activated by setting <c>Ollama:MockMode = true</c> in configuration.
/// Intended for offline development and CI environments where Ollama is unavailable.
/// </summary>
public sealed class MockOllamaClient : IOllamaClient
{
    /// <summary>
    /// Fixed analysis JSON returned by the mock.
    /// Represents a small black coffee machine — the MVP0 golden sample.
    /// </summary>
    private const string FixedAnalysisJson = """
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

    /// <summary>
    /// Always reports the mock as available — no network check is performed.
    /// </summary>
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    /// <summary>
    /// Returns the fixed <see cref="FixedAnalysisJson"/> regardless of the prompt.
    /// No network call is made.
    /// </summary>
    public Task<Result<string>> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Result<string>.Success(FixedAnalysisJson));
}
