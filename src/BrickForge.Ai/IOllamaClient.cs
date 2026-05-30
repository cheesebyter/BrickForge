using BrickForge.Core.Results;

namespace BrickForge.Ai;

/// <summary>
/// Abstraction over the local Ollama HTTP API.
/// Implementations must not call external AI APIs.
/// </summary>
public interface IOllamaClient
{
    /// <summary>
    /// Checks whether the local Ollama service is reachable.
    /// Returns false on any network error; never throws.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a prompt to the configured local model and returns the raw text response.
    /// The caller is responsible for validating and parsing the response.
    /// </summary>
    Task<Result<string>> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
