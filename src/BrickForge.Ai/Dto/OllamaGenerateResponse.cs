using System.Text.Json.Serialization;

namespace BrickForge.Ai.Dto;

internal sealed class OllamaGenerateResponse
{
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("response")]
    public string? Response { get; init; }

    [JsonPropertyName("done")]
    public bool Done { get; init; }
}
