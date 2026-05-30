namespace BrickForge.Core.Options;

/// <summary>
/// Configuration options for the local Ollama AI runtime.
/// No external AI API is required or configured here.
/// </summary>
public sealed class OllamaOptions
{
    /// <summary>Base URL of the local Ollama instance.</summary>
    public string BaseUrl { get; init; } = "http://localhost:11434";

    /// <summary>Name of the locally available Ollama model to use.</summary>
    public string Model { get; init; } = "llama3.1:8b";

    /// <summary>Maximum seconds to wait for an Ollama response before treating the call as failed.</summary>
    public int TimeoutSeconds { get; init; } = 120;

    /// <summary>Sampling temperature passed to the model (0.0–1.0). Lower values produce more deterministic output.</summary>
    public double Temperature { get; init; } = 0.2;
}
