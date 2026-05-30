using System.Text.Json.Serialization;

namespace BrickForge.BrickGraph.Validation;

/// <summary>
/// A single issue found during BrickGraph validation.
/// </summary>
public sealed class ValidationIssue
{
    /// <summary>Short machine-readable code (e.g. "EMPTY_PARTS").</summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>Human-readable description of the issue.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>Severity of the issue.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("severity")]
    public ValidationSeverity Severity { get; init; }
}
