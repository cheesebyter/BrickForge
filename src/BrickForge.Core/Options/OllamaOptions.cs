namespace BrickForge.Core.Options;

/// <summary>
/// Configuration options for the local Ollama AI runtime.
/// No external AI API is required or configured here.
///
/// <para>
/// <b>BF-MVP1-020 §20.1:</b> Supports separate planning and fallback model names.
/// Set <see cref="PlanningModel"/> for structured-output tasks (prompt analysis, JSON generation)
/// and <see cref="FallbackModel"/> as a lighter model for simpler classification tasks.
/// </para>
/// </summary>
public sealed class OllamaOptions
{
    /// <summary>Base URL of the local Ollama instance.</summary>
    public string BaseUrl { get; init; } = "http://localhost:11434";

    /// <summary>
    /// Primary model used for planning and structured JSON generation tasks.
    /// Defaults to a capable code/JSON-oriented model.
    /// </summary>
    public string PlanningModel { get; init; } = "qwen2.5-coder:14b";

    /// <summary>
    /// Lighter fallback model used when the planning model is unavailable
    /// or for simpler classification tasks.
    /// </summary>
    public string FallbackModel { get; init; } = "llama3.1:8b";

    /// <summary>
    /// Legacy single-model field kept for backward compatibility.
    /// If <see cref="PlanningModel"/> is set to its default, this value is ignored.
    /// Prefer <see cref="PlanningModel"/> in all new configuration.
    /// </summary>
    [Obsolete("Use PlanningModel and FallbackModel instead.")]
    public string Model { get; init; } = string.Empty;

    /// <summary>Maximum seconds to wait for an Ollama response before treating the call as failed.</summary>
    public int TimeoutSeconds { get; init; } = 120;

    /// <summary>Sampling temperature passed to the model (0.0–1.0). Lower values produce more deterministic output.</summary>
    public double Temperature { get; init; } = 0.2;

    /// <summary>
    /// When true, uses a local mock instead of calling the Ollama HTTP API.
    /// No network calls are made. Intended for offline testing and CI runs.
    /// </summary>
    public bool MockMode { get; init; } = false;
}
