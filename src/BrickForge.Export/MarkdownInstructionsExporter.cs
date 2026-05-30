using System.Text;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export;

/// <summary>
/// Exports a BrickGraph to Markdown build instructions (<c>instructions.md</c>).
/// Does not mutate the graph.
/// </summary>
public sealed class MarkdownInstructionsExporter
{
    private const string LegalDisclaimer =
        "Dieses Dokument wurde automatisch durch BrickForge erzeugt. " +
        "Es handelt sich nicht um eine offizielle LEGO-Bauanleitung und nicht um ein von LEGO geprüftes Modell.";

    /// <summary>
    /// Produces the content of <c>instructions.md</c> from the given graph.
    /// Returns <see cref="ExportResult.Fail"/> when the graph has no parts.
    /// </summary>
    public ExportResult Export(Graph graph)
    {
        if (graph.Parts.Count == 0)
            return ExportResult.Fail("Cannot export an empty BrickGraph: no parts defined.");

        var sb = new StringBuilder();
        var modelName = string.IsNullOrWhiteSpace(graph.Model.Name) ? "Brick Model" : graph.Model.Name;

        sb.AppendLine($"# {modelName}");
        sb.AppendLine();
        sb.AppendLine($"> {LegalDisclaimer}");
        sb.AppendLine();

        // Parts list section
        sb.AppendLine("## Teileliste");
        sb.AppendLine();
        sb.AppendLine("| Anzahl | Teilenummer | Bezeichnung | Farbe |");
        sb.AppendLine("|--------|-------------|-------------|-------|");

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
            sb.AppendLine($"| {row.Quantity} | {row.PartNumber} | {row.PartName} | {row.Color} |");

        sb.AppendLine();

        // Build steps section
        sb.AppendLine("## Bauanleitung");
        sb.AppendLine();

        var steps = graph.Steps.OrderBy(s => s.StepNumber).ToList();

        if (steps.Count == 0)
        {
            // Fallback: group by step number from parts
            var partsByStep = graph.Parts
                .GroupBy(p => p.Step)
                .OrderBy(g => g.Key);

            foreach (var stepGroup in partsByStep)
            {
                sb.AppendLine($"### Schritt {stepGroup.Key}");
                sb.AppendLine();
                foreach (var part in stepGroup)
                    sb.AppendLine($"- {part.PartName} ({part.PartNumber}) – {part.Color}");
                sb.AppendLine();
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

                sb.AppendLine($"### {label}");
                sb.AppendLine();

                foreach (var instanceId in step.PartInstanceIds)
                {
                    if (partById.TryGetValue(instanceId, out var part))
                        sb.AppendLine($"- {part.PartName} ({part.PartNumber}) – {part.Color}");
                }

                sb.AppendLine();
            }
        }

        return ExportResult.Ok(sb.ToString());
    }
}
