using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrickForge.BrickGraph.Validation;

/// <summary>
/// The result of validating a BrickGraph.
/// Serialises to the <c>validation.json</c> output file.
/// </summary>
public sealed class ValidationResult
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// True when there are no High-severity issues.
    /// </summary>
    [JsonPropertyName("valid")]
    public bool Valid { get; init; }

    /// <summary>
    /// Quality score from 0.0 (all checks failed) to 1.0 (all checks passed).
    /// </summary>
    [JsonPropertyName("score")]
    public float Score { get; init; }

    /// <summary>
    /// All issues found. An empty list means a perfect result.
    /// </summary>
    [JsonPropertyName("issues")]
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];

    // ── Serialization ─────────────────────────────────────────────────────────

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static ValidationResult? FromJson(string json)
    {
        try { return JsonSerializer.Deserialize<ValidationResult>(json, JsonOptions); }
        catch { return null; }
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="ValidationResult"/> from a list of issues.
    /// </summary>
    public static ValidationResult FromIssues(IReadOnlyList<ValidationIssue> issues, int totalChecks)
    {
        var failedChecks = issues.Count;
        var passedChecks = Math.Max(0, totalChecks - failedChecks);
        var score = totalChecks == 0 ? 1.0f : (float)passedChecks / totalChecks;
        var valid = issues.All(i => i.Severity < ValidationSeverity.High);

        return new ValidationResult
        {
            Valid = valid,
            Score = MathF.Round(score, 4),
            Issues = issues
        };
    }
}
