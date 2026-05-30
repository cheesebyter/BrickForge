using System.Text.Json.Serialization;

namespace BrickForge.Ai.Dto;

internal sealed class OllamaGenerateRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;

    [JsonPropertyName("system")]
    public string System { get; init; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;

    [JsonPropertyName("options")]
    public OllamaModelOptions? Options { get; init; }
}

internal sealed class OllamaModelOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }
}
