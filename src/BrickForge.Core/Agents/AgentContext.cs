namespace BrickForge.Core.Agents;

/// <summary>
/// Shared context passed to each agent during execution.
/// Carries cross-cutting concerns such as job identity and additional metadata.
/// Agents obtain loggers through DI rather than through this context.
/// </summary>
public sealed class AgentContext
{
    /// <summary>The job ID that this agent execution belongs to.</summary>
    public required string JobId { get; init; }

    /// <summary>
    /// Optional key/value metadata that higher-level orchestrators may inject
    /// (e.g. temperature override, feature flags). May be empty.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
