using System.Text;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export;

/// <summary>
/// Exports a BrickGraph parts list to CSV format (<c>parts.csv</c>).
/// Identical parts (same part_number + same color) are aggregated into one row.
/// Does not mutate the graph.
/// </summary>
public sealed class CsvPartsExporter
{
    private const string Header = "quantity,part_number,part_name,color";

    /// <summary>
    /// Produces the content of <c>parts.csv</c> from the given graph.
    /// Returns <see cref="ExportResult.Fail"/> when the graph has no parts.
    /// </summary>
    public ExportResult Export(Graph graph)
    {
        if (graph.Parts.Count == 0)
            return ExportResult.Fail("Cannot export an empty BrickGraph: no parts defined.");

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

        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var row in aggregated)
        {
            sb.AppendLine(string.Join(",",
                row.Quantity,
                EscapeCsv(row.PartNumber),
                EscapeCsv(row.PartName),
                EscapeCsv(row.Color)));
        }

        return ExportResult.Ok(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
