using System.Globalization;
using System.Text;
using BrickForge.BrickGraph.Validation;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export;

/// <summary>
/// Produces the technical generation report (<c>report.md</c>) from a BrickGraph
/// and accompanying <see cref="GenerationReportData"/>.
/// Does not mutate the graph.
/// </summary>
public sealed class ReportExporter
{
    private const string Disclaimer =
        "Dieses Dokument wurde automatisch durch BrickForge erzeugt. " +
        "Es handelt sich nicht um eine offizielle LEGO-Bauanleitung und nicht um ein von LEGO geprüftes Modell.";

    private static readonly IReadOnlyList<string> Mvp0Limitations =
    [
        "Nur die Vorlage `small_machine` wird in MVP0 unterstützt.",
        "Kollisionsprüfung und strukturelle Verbindungsprüfung sind in MVP0 nicht implementiert.",
        "Farb- und Teileunterstützung ist auf die definierte Allowlist beschränkt.",
        "Die erzeugten Modelle sind geometrisch vereinfacht und nicht für den physischen Aufbau optimiert.",
        "LDraw-Ausgabe wurde nicht mit einem LDraw-Viewer verifiziert.",
    ];

    /// <summary>
    /// Produces the content of <c>report.md</c>.
    /// </summary>
    public ExportResult Export(Graph graph, GenerationReportData data)
    {
        var sb = new StringBuilder();
        var timestamp = data.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture);

        sb.AppendLine("# BrickForge Generierungsbericht");
        sb.AppendLine();
        sb.AppendLine($"**Erstellt:** {timestamp}");
        sb.AppendLine();

        // Original prompt
        sb.AppendLine("## Eingabe-Prompt");
        sb.AppendLine();
        sb.AppendLine(string.IsNullOrWhiteSpace(data.OriginalPrompt) ? "_kein Prompt angegeben_" : data.OriginalPrompt);
        sb.AppendLine();

        // AI model / analysis method
        sb.AppendLine("## KI-Analyse");
        sb.AppendLine();
        var aiLabel = data.AnalysisResult?.UsedFallback == true || data.AiModelName is null
            ? "Fallback-Analyse"
            : data.AiModelName;
        sb.AppendLine($"**Analysemethode:** {aiLabel}");
        sb.AppendLine();

        if (data.AnalysisResult is { } analysis)
        {
            sb.AppendLine("| Feld | Wert |");
            sb.AppendLine("|------|------|");
            sb.AppendLine($"| Modellname | {analysis.ModelName} |");
            sb.AppendLine($"| Kategorie | {analysis.ModelCategory} |");
            sb.AppendLine($"| Zielteileanzahl | {analysis.TargetParts} |");
            sb.AppendLine($"| Hauptfarbe | {analysis.MainColor} |");
            sb.AppendLine($"| Akzentfarbe | {analysis.AccentColor} |");
            sb.AppendLine();
        }

        // Parts count
        sb.AppendLine("## Teileanzahl");
        sb.AppendLine();
        if (data.AnalysisResult is not null)
            sb.AppendLine($"- Ziel: {data.AnalysisResult.TargetParts}");
        sb.AppendLine($"- Tatsächlich erzeugt: {graph.Model.ActualParts}");
        sb.AppendLine();

        // Validation result
        sb.AppendLine("## Validierungsergebnis");
        sb.AppendLine();
        if (data.ValidationResult is { } validation)
        {
            sb.AppendLine($"- **Gültig:** {(validation.Valid ? "Ja" : "Nein")}");
            sb.AppendLine($"- **Score:** {validation.Score:F4}");

            if (validation.Issues.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Probleme");
                sb.AppendLine();
                foreach (var issue in validation.Issues)
                    sb.AppendLine($"- [{issue.Severity}] `{issue.Code}`: {issue.Message}");
            }
        }
        else
        {
            sb.AppendLine("_Keine Validierung durchgeführt._");
        }
        sb.AppendLine();

        // Generated files
        sb.AppendLine("## Erzeugte Dateien");
        sb.AppendLine();
        if (data.GeneratedFiles.Count == 0)
        {
            sb.AppendLine("_Keine Dateien erzeugt._");
        }
        else
        {
            foreach (var file in data.GeneratedFiles)
                sb.AppendLine($"- `{file}`");
        }
        sb.AppendLine();

        // MVP0 limitations
        sb.AppendLine("## Bekannte MVP0-Einschränkungen");
        sb.AppendLine();
        foreach (var limitation in Mvp0Limitations)
            sb.AppendLine($"- {limitation}");
        sb.AppendLine();

        // Disclaimer
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"_{Disclaimer}_");

        return ExportResult.Ok(sb.ToString());
    }
}
