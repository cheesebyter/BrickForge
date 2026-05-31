using BrickForge.BrickGraph.Templates;
using BrickForge.Core.Agents;
using BrickForge.Core.Ai;
using Microsoft.Extensions.Logging;

namespace BrickForge.BrickGraph.Agents;

/// <summary>
/// Agent that selects the best-matching <see cref="BrickModelTemplate"/> for a given
/// <see cref="PromptAnalysisResult"/>. Falls back to <c>small_machine</c> when no
/// exact match exists in the registry.
/// </summary>
public sealed class TemplateSelectionAgent : IAgent<PromptAnalysisResult, BrickModelTemplate>
{
    private const string FallbackTemplateId = "small_machine";

    private readonly TemplateRegistry _registry;
    private readonly ILogger<TemplateSelectionAgent> _logger;

    public string AgentName => "TemplateSelectionAgent";

    public TemplateSelectionAgent(TemplateRegistry registry, ILogger<TemplateSelectionAgent> logger)
    {
        _registry = registry;
        _logger   = logger;
    }

    /// <inheritdoc />
    public Task<AgentResult<BrickModelTemplate>> RunAsync(
        PromptAnalysisResult input,
        AgentContext context,
        CancellationToken cancellationToken = default)
    {
        var template = _registry.FindTemplate(input.ModelCategory)
                       ?? _registry.FindTemplate(FallbackTemplateId);

        if (template is null)
        {
            var msg = $"No template found for category '{input.ModelCategory}' and fallback '{FallbackTemplateId}' is also unavailable.";
            _logger.LogError("[{Agent}] JobId={JobId} {Error}", AgentName, context.JobId, msg);
            return Task.FromResult(AgentResult<BrickModelTemplate>.Failure(msg));
        }

        _logger.LogInformation(
            "[{Agent}] JobId={JobId} selected template '{Template}' for category '{Category}'.",
            AgentName, context.JobId, template.TemplateId, input.ModelCategory);

        return Task.FromResult(AgentResult<BrickModelTemplate>.Success(template));
    }
}
