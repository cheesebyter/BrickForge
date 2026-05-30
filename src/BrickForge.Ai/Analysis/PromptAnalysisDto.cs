using System.Text.Json.Serialization;

namespace BrickForge.Ai.Analysis;

/// <summary>
/// Internal DTO for deserialising the raw JSON returned by Ollama during prompt analysis.
/// This is untrusted AI data and must be validated before mapping to <see cref="BrickForge.Core.Ai.PromptAnalysisResult"/>.
/// </summary>
internal sealed class PromptAnalysisDto
{
    [JsonPropertyName("model_name")]
    public string? ModelName { get; init; }

    [JsonPropertyName("model_category")]
    public string? ModelCategory { get; init; }

    [JsonPropertyName("target_parts")]
    public int? TargetParts { get; init; }

    [JsonPropertyName("main_color")]
    public string? MainColor { get; init; }

    [JsonPropertyName("accent_color")]
    public string? AccentColor { get; init; }

    [JsonPropertyName("features")]
    public List<string>? Features { get; init; }

    [JsonPropertyName("feasible")]
    public bool? Feasible { get; init; }

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; init; }
}
