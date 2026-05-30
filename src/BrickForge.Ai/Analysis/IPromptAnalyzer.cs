using BrickForge.Core.Ai;
using BrickForge.Core.Results;

namespace BrickForge.Ai.Analysis;

/// <summary>
/// Performs AI-assisted analysis of a user prompt to produce a structured model briefing.
/// </summary>
public interface IPromptAnalyzer
{
    /// <summary>
    /// Analyses the user prompt and returns a validated <see cref="PromptAnalysisResult"/>.
    /// On AI failure or invalid JSON, a fallback result is returned rather than a failure.
    /// Only returns <see cref="Result{T}.IsSuccess"/> = false for configuration or hard errors.
    /// </summary>
    Task<Result<PromptAnalysisResult>> AnalyzeAsync(
        string userPrompt,
        CancellationToken cancellationToken = default);
}
