namespace BrickForge.Core.Agents;

/// <summary>
/// Defines a single-responsibility agent that transforms <typeparamref name="TInput"/>
/// into an <see cref="AgentResult{TOutput}"/>.
/// </summary>
public interface IAgent<TInput, TOutput>
{
    /// <summary>Human-readable name for logging and diagnostics.</summary>
    string AgentName { get; }

    /// <summary>
    /// Executes the agent and returns the result.
    /// </summary>
    Task<AgentResult<TOutput>> RunAsync(
        TInput input,
        AgentContext context,
        CancellationToken cancellationToken = default);
}
