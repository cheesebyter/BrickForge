using System.Text.Json;
using System.Text.Json.Serialization;
using BrickForge.BrickGraph.Model;

namespace BrickForge.BrickGraph;

/// <summary>
/// Central internal model for a generated brick model.
/// This is the data structure passed between all pipeline stages.
/// Exporters must not mutate this object.
/// </summary>
public sealed class BrickGraph
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [JsonPropertyName("model")]
    public BrickModelMetadata Model { get; init; } = new();

    [JsonPropertyName("parts")]
    public List<BrickPartInstance> Parts { get; init; } = [];

    [JsonPropertyName("steps")]
    public List<BrickStep> Steps { get; init; } = [];

    // ── Mutation helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Adds a part instance and updates <see cref="BrickModelMetadata.ActualParts"/>.
    /// </summary>
    public void AddPart(BrickPartInstance part)
    {
        Parts.Add(part);
        Model.ActualParts = Parts.Count;
    }

    /// <summary>
    /// Adds a build step.
    /// </summary>
    public void AddStep(BrickStep step)
    {
        Steps.Add(step);
    }

    // ── Serialization ─────────────────────────────────────────────────────────

    /// <summary>
    /// Serialises this graph to a JSON string.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>
    /// Deserialises a BrickGraph from JSON.
    /// Returns null on parse failure.
    /// </summary>
    public static BrickGraph? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<BrickGraph>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
