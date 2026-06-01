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

    private const string InterpretationHint =
        "Die erzeugten Modelle sind stilisierte Interpretationen eines Nutzer-Prompts. " +
        "Sie sind keine originalgetreuen Nachbauten und wurden nicht für den physischen Aufbau optimiert.";

    private static readonly IReadOnlyList<string> Mvp1Limitations =
    [
        "Kollisionsprüfung und strukturelle Verbindungsprüfung können in komplexen Modellen unvollständig sein.",
        "Farb- und Teileunterstützung ist auf die definierte Allowlist beschränkt.",
        "Die erzeugten Modelle sind geometrisch vereinfacht.",
        "LDraw-Ausgabe wurde nicht mit einem LDraw-Viewer automatisch verifiziert.",
        "LPub3D-Export ist optional und nicht Bestandteil der Standardausgabe.",
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

        // Template
        if (!string.IsNullOrWhiteSpace(data.TemplateName))
        {
            sb.AppendLine("## Gewähltes Template");
            sb.AppendLine();
            sb.AppendLine($"- **Template:** `{data.TemplateName}`");
            sb.AppendLine();
        }

        // Color list
        sb.AppendLine("## Farbliste");
        sb.AppendLine();
        var colors = graph.Parts
            .Select(p => p.Color)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        if (colors.Count == 0)
        {
            sb.AppendLine("_Keine Farben ermittelt._");
        }
        else
        {
            foreach (var color in colors)
                sb.AppendLine($"- {color}");
        }
        sb.AppendLine();

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

        // Agent metrics
        if (data.AgentMetrics.Count > 0 || data.JobMetrics is not null)
        {
            sb.AppendLine("## Agentenmetriken");
            sb.AppendLine();

            if (data.JobMetrics is { } jm)
            {
                sb.AppendLine($"- **Gesamtdauer:** {jm.TotalDurationMs} ms");
                sb.AppendLine($"- **LLM-Aufrufe gesamt:** {jm.TotalLlmCalls}");
                sb.AppendLine($"- **Retries gesamt:** {jm.TotalRetries}");
                sb.AppendLine($"- **Erfolg:** {(jm.JobSuccess ? "Ja" : "Nein")}");
                sb.AppendLine();
            }

            if (data.AgentMetrics.Count > 0)
            {
                sb.AppendLine("| Agent | Dauer (ms) | LLM-Aufrufe | Retries | Erfolg | Konfidenz |");
                sb.AppendLine("|-------|-----------|-------------|---------|--------|-----------|");
                foreach (var m in data.AgentMetrics)
                {
                    var conf = m.Confidence.HasValue
                        ? m.Confidence.Value.ToString("F2", CultureInfo.InvariantCulture)
                        : "–";
                    var ok = m.Success ? "Ja" : "Nein";
                    sb.AppendLine($"| {m.AgentName} | {m.DurationMs} | {m.LlmCalls} | {m.Retries} | {ok} | {conf} |");
                }
                sb.AppendLine();
            }
        }

        // Interpretation hint
        sb.AppendLine("## Stilisierte Interpretation");
        sb.AppendLine();
        sb.AppendLine(InterpretationHint);
        sb.AppendLine();

        // Limitations
        sb.AppendLine("## Bekannte Einschränkungen");
        sb.AppendLine();
        foreach (var limitation in Mvp1Limitations)
            sb.AppendLine($"- {limitation}");
        sb.AppendLine();

        // Disclaimer
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"_{Disclaimer}_");

        return ExportResult.Ok(sb.ToString());
    }
}
