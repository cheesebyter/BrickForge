using System.Text.Json.Serialization;

namespace BrickForge.BrickGraph.Parts;

/// <summary>
/// Describes a brick-compatible part supported by BrickForge MVP0.
/// </summary>
public sealed class PartDefinition
{
    [JsonPropertyName("part_number")]
    public string PartNumber { get; init; } = string.Empty;

    [JsonPropertyName("part_name")]
    public string PartName { get; init; } = string.Empty;
}
