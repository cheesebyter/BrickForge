using System.Net;
using System.Text;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export;

/// <summary>
/// Exports a BrickGraph to HTML build instructions (<c>instructions.html</c>).
/// Does not mutate the graph.
/// </summary>
public sealed class HtmlInstructionsExporter
{
    private const string LegalDisclaimer =
        "Dieses Dokument wurde automatisch durch BrickForge erzeugt. " +
        "Es handelt sich nicht um eine offizielle LEGO-Bauanleitung und nicht um ein von LEGO geprüftes Modell.";

    private const string InterpretationNotice =
        "Dieses Modell ist eine fanbasierte, stilisierte Interpretation (MOC – My Own Creation). " +
        "Es ist kein lizenziertes LEGO-Produkt.";

    private const string LDrawExportHint =
        "Die Datei <code>model.mpd</code> kann mit einem LDraw-kompatiblen Viewer geöffnet werden " +
        "(z. B. LDraw.org Viewer, LeoCAD, BrickLink Studio – optional).";

    /// <summary>
    /// Produces the content of <c>instructions.html</c> from the given graph.
    /// Returns <see cref="ExportResult.Fail"/> when the graph has no parts.
    /// </summary>
    public ExportResult Export(Graph graph)
    {
        if (graph.Parts.Count == 0)
            return ExportResult.Fail("Cannot export an empty BrickGraph: no parts defined.");

        var modelName = string.IsNullOrWhiteSpace(graph.Model.Name) ? "Brick Model" : graph.Model.Name;
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"de\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>{Encode(modelName)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: sans-serif; max-width: 900px; margin: 2rem auto; color: #222; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
        sb.AppendLine("th, td { border: 1px solid #ccc; padding: 6px 10px; text-align: left; }");
        sb.AppendLine("th { background: #f0f0f0; }");
        sb.AppendLine(".disclaimer { border: 1px solid #f90; background: #fff8e1; padding: 10px; border-radius: 4px; }");
        sb.AppendLine(".notice { color: #555; font-style: italic; margin-top: 2rem; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Title + meta
        sb.AppendLine($"<h1>{Encode(modelName)}</h1>");
        sb.AppendLine("<p>");
        sb.AppendLine($"<strong>Beschreibung:</strong> Klemmbaustein-kompatibles Brick-Modell, erzeugt durch BrickForge.<br>");
        sb.AppendLine($"<strong>Teile gesamt:</strong> {graph.Parts.Count}");
        sb.AppendLine("</p>");

        // Disclaimer
        sb.AppendLine($"<div class=\"disclaimer\"><strong>Hinweis:</strong> {LegalDisclaimer}</div>");
        sb.AppendLine();

        // Parts list
        sb.AppendLine("<h2>Teileliste</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Anzahl</th><th>Teilenummer</th><th>Bezeichnung</th><th>Farbe</th></tr></thead>");
        sb.AppendLine("<tbody>");

        var aggregated = graph.Parts
            .GroupBy(p => (p.PartNumber, p.Color))
            .Select(g => new
            {
                Quantity = g.Count(),
                g.Key.PartNumber,
                PartName = g.First().PartName,
                g.Key.Color
            })
            .OrderBy(r => r.PartNumber)
            .ThenBy(r => r.Color);

        foreach (var row in aggregated)
        {
            sb.AppendLine(
                $"<tr><td>{row.Quantity}</td><td>{Encode(row.PartNumber)}</td>" +
                $"<td>{Encode(row.PartName)}</td><td>{Encode(row.Color)}</td></tr>");
        }

        sb.AppendLine("</tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine();

        // Build steps
        sb.AppendLine("<h2>Bauanleitung</h2>");

        var steps = graph.Steps.OrderBy(s => s.StepNumber).ToList();

        if (steps.Count == 0)
        {
            var partsByStep = graph.Parts
                .GroupBy(p => p.Step)
                .OrderBy(g => g.Key);

            foreach (var stepGroup in partsByStep)
            {
                sb.AppendLine($"<h3>Schritt {stepGroup.Key}</h3>");
                sb.AppendLine("<ul>");
                foreach (var part in stepGroup)
                    sb.AppendLine($"<li>{Encode(part.PartName)} ({Encode(part.PartNumber)}) – {Encode(part.Color)}</li>");
                sb.AppendLine("</ul>");
            }
        }
        else
        {
            var partById = graph.Parts.ToDictionary(p => p.InstanceId);

            foreach (var step in steps)
            {
                var label = string.IsNullOrWhiteSpace(step.Label)
                    ? $"Schritt {step.StepNumber}"
                    : step.Label;

                sb.AppendLine($"<h3>{Encode(label)}</h3>");
                sb.AppendLine("<ul>");

                foreach (var instanceId in step.PartInstanceIds)
                {
                    if (partById.TryGetValue(instanceId, out var part))
                        sb.AppendLine($"<li>{Encode(part.PartName)} ({Encode(part.PartNumber)}) – {Encode(part.Color)}</li>");
                }

                sb.AppendLine("</ul>");
            }
        }

        // LDraw export hint
        sb.AppendLine("<h2>LDraw-Export</h2>");
        sb.AppendLine($"<p>{LDrawExportHint}</p>");

        // Footer notice
        sb.AppendLine($"<p class=\"notice\">{InterpretationNotice}</p>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return ExportResult.Ok(sb.ToString());
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
