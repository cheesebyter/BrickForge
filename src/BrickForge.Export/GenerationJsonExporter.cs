using System.Text.Json;
using System.Text.Json.Serialization;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export;

/// <summary>
/// Produces the <c>generation.json</c> file required in each job output package (Section 17.1).
///
/// The file contains machine-readable metadata: prompt, template, analysis summary,
/// validation outcome, generated files, and a legal disclaimer.
/// Does not mutate the BrickGraph.
/// </summary>
public sealed class GenerationJsonExporter
{
    private const string Disclaimer =
        "Dieses Dokument wurde automatisch durch BrickForge erzeugt. " +
        "Es handelt sich nicht um eine offizielle LEGO-Bauanleitung " +
        "und nicht um ein von LEGO geprüftes Modell.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Serialises the generation metadata to a JSON string.
    /// </summary>
    public ExportResult Export(Graph graph, GenerationJsonData data)
    {
        var analysis = data.AnalysisResult;
        var validation = data.ValidationResult;

        var doc = new GenerationJsonDocument
        {
            JobId          = data.JobId,
            Timestamp      = data.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            OriginalPrompt = data.OriginalPrompt,
            TemplateName   = data.TemplateName,
            ModelCategory  = analysis?.ModelCategory ?? string.Empty,
            ModelName      = analysis?.ModelName ?? graph.Model.Name,
            TargetParts    = analysis?.TargetParts ?? graph.Model.TargetParts,
            ActualParts    = graph.Model.ActualParts,
            Colors         = BuildColorList(graph, analysis),
            ValidationScore = validation?.Score ?? 0f,
            IsValid         = validation?.Valid ?? false,
            WasRepaired     = data.WasRepaired,
            GeneratedFiles  = data.GeneratedFiles,
            KnownLimitations =
            [
                "Nur unterstützte Teile aus der MVP-Allowlist werden verwendet.",
                "Kollisionsprüfung und Verbindungsprüfung sind vereinfacht.",
                "Geometrie ist stilisiert und nicht für physischen Aufbau optimiert.",
            ],
            Disclaimer = Disclaimer
        };

        try
        {
            return ExportResult.Ok(JsonSerializer.Serialize(doc, JsonOptions));
        }
        catch (Exception ex)
        {
            return ExportResult.Fail($"generation.json serialization failed: {ex.Message}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IReadOnlyList<string> BuildColorList(
        Graph graph,
        BrickForge.Core.Ai.PromptAnalysisResult? analysis)
    {
        // Prefer actual colors used in the graph (more accurate than analysis)
        var usedColors = graph.Parts
            .Select(p => p.Color)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        if (usedColors.Count > 0)
            return usedColors;

        // Fall back to analysis
        if (analysis is not null)
        {
            var fallback = new List<string>();
            if (!string.IsNullOrWhiteSpace(analysis.MainColor))
                fallback.Add(analysis.MainColor);
            if (!string.IsNullOrWhiteSpace(analysis.AccentColor)
                && analysis.AccentColor != analysis.MainColor)
                fallback.Add(analysis.AccentColor);
            return fallback;
        }

        return [];
    }

    // ── Private DTO ───────────────────────────────────────────────────────────

    private sealed class GenerationJsonDocument
    {
        [JsonPropertyName("job_id")]
        public string JobId { get; init; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; init; } = string.Empty;

        [JsonPropertyName("original_prompt")]
        public string OriginalPrompt { get; init; } = string.Empty;

        [JsonPropertyName("template_name")]
        public string TemplateName { get; init; } = string.Empty;

        [JsonPropertyName("model_category")]
        public string ModelCategory { get; init; } = string.Empty;

        [JsonPropertyName("model_name")]
        public string ModelName { get; init; } = string.Empty;

        [JsonPropertyName("target_parts")]
        public int TargetParts { get; init; }

        [JsonPropertyName("actual_parts")]
        public int ActualParts { get; init; }

        [JsonPropertyName("colors")]
        public IReadOnlyList<string> Colors { get; init; } = [];

        [JsonPropertyName("validation_score")]
        public float ValidationScore { get; init; }

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; init; }

        [JsonPropertyName("was_repaired")]
        public bool WasRepaired { get; init; }

        [JsonPropertyName("generated_files")]
        public IReadOnlyList<string> GeneratedFiles { get; init; } = [];

        [JsonPropertyName("known_limitations")]
        public IReadOnlyList<string> KnownLimitations { get; init; } = [];

        [JsonPropertyName("disclaimer")]
        public string Disclaimer { get; init; } = string.Empty;
    }
}
