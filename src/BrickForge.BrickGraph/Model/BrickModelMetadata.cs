using System.Text.Json.Serialization;

namespace BrickForge.BrickGraph.Model;

/// <summary>
/// Metadata header for a BrickGraph model.
/// </summary>
public sealed class BrickModelMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("target_parts")]
    public int TargetParts { get; init; }

    [JsonPropertyName("actual_parts")]
    public int ActualParts { get; set; }
}
