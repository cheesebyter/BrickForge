using System.Text.Json.Serialization;

namespace BrickForge.BrickGraph.Model;

/// <summary>
/// A logical build step grouping parts that are placed together.
/// </summary>
public sealed class BrickStep
{
    /// <summary>Step number (1-based, ascending).</summary>
    [JsonPropertyName("step_number")]
    public int StepNumber { get; init; }

    /// <summary>Optional human-readable label shown in instructions.</summary>
    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Instance IDs of parts placed in this step.
    /// References <see cref="BrickPartInstance.InstanceId"/> in the owning graph.
    /// </summary>
    [JsonPropertyName("part_instance_ids")]
    public IReadOnlyList<string> PartInstanceIds { get; init; } = [];
}
