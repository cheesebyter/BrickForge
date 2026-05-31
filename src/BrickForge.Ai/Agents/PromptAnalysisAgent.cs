using BrickForge.Ai.Analysis;
using BrickForge.Core.Agents;
using BrickForge.Core.Ai;
using Microsoft.Extensions.Logging;

namespace BrickForge.Ai.Agents;

/// <summary>
/// Agent that wraps <see cref="IPromptAnalyzer"/> to produce a <see cref="PromptAnalysisResult"/>.
/// This is the first agent in the MVP1 generation pipeline.
/// </summary>
public sealed class PromptAnalysisAgent : IAgent<string, PromptAnalysisResult>
{
    private readonly IPromptAnalyzer _analyzer;
    private readonly ILogger<PromptAnalysisAgent> _logger;

    public string AgentName => "PromptAnalysisAgent";

    public PromptAnalysisAgent(IPromptAnalyzer analyzer, ILogger<PromptAnalysisAgent> logger)
    {
        _analyzer = analyzer;
        _logger   = logger;
    }

    /// <inheritdoc />
    public async Task<AgentResult<PromptAnalysisResult>> RunAsync(
        string input,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{Agent}] JobId={JobId} starting prompt analysis.",
            AgentName, context.JobId);

        var result = await _analyzer.AnalyzeAsync(input, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("[{Agent}] JobId={JobId} analysis failed: {Error}",
                AgentName, context.JobId, result.ErrorMessage);
            return AgentResult<PromptAnalysisResult>.Failure(
                result.ErrorMessage ?? "Prompt analysis returned no result.");
        }

        var analysis = result.Value!;

        if (!analysis.Feasible)
        {
            var reason = string.Join("; ", analysis.Warnings);
            _logger.LogWarning("[{Agent}] JobId={JobId} prompt infeasible: {Reason}",
                AgentName, context.JobId, reason);
            return AgentResult<PromptAnalysisResult>.Failure($"Prompt is not feasible: {reason}");
        }

        _logger.LogInformation("[{Agent}] JobId={JobId} analysis succeeded. Model={Model}, Parts={Parts}",
            AgentName, context.JobId, analysis.ModelName, analysis.TargetParts);

        return AgentResult<PromptAnalysisResult>.Success(analysis);
    }
}
