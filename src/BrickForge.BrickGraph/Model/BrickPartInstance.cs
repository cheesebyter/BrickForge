using System.Text.Json.Serialization;

namespace BrickForge.BrickGraph.Model;

/// <summary>
/// A single placed part instance inside a BrickGraph.
/// </summary>
public sealed class BrickPartInstance
{
    /// <summary>Unique instance identifier within this model (e.g. "part_001").</summary>
    [JsonPropertyName("instance_id")]
    public string InstanceId { get; init; } = string.Empty;

    /// <summary>LDraw-compatible part number (e.g. "3001").</summary>
    [JsonPropertyName("part_number")]
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>Human-readable part name (e.g. "Brick 2 x 4").</summary>
    [JsonPropertyName("part_name")]
    public string PartName { get; init; } = string.Empty;

    /// <summary>Colour name from the allowed colour list.</summary>
    [JsonPropertyName("color")]
    public string Color { get; init; } = "black";

    /// <summary>
    /// Position as [x, y, z] in LDraw units.
    /// Y-axis points downward in LDraw coordinate space.
    /// </summary>
    [JsonPropertyName("position")]
    public float[] Position { get; init; } = [0f, 0f, 0f];

    /// <summary>
    /// 3×3 rotation matrix flattened to 9 values (row-major).
    /// Identity matrix is [1,0,0, 0,1,0, 0,0,1].
    /// </summary>
    [JsonPropertyName("rotation")]
    public float[] Rotation { get; init; } = [1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f];

    /// <summary>Build step number. Must be >= 1.</summary>
    [JsonPropertyName("step")]
    public int Step { get; init; } = 1;
}
