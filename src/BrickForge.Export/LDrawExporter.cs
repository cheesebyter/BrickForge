using System.Globalization;
using System.Text;
using BrickForge.BrickGraph.Model;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export;

/// <summary>
/// Exports a BrickGraph to LDraw MPD format (<c>model.mpd</c>).
/// Does not mutate the graph.
/// </summary>
public sealed class LDrawExporter
{
    /// <summary>
    /// Produces the content of <c>model.mpd</c> from the given graph.
    /// Returns <see cref="ExportResult.Fail"/> when the graph has no parts.
    /// </summary>
    public ExportResult Export(Graph graph)
    {
        if (graph.Parts.Count == 0)
            return ExportResult.Fail("Cannot export an empty BrickGraph: no parts defined.");

        var sb = new StringBuilder();
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        sb.AppendLine("0 FILE model.mpd");
        sb.AppendLine("0 BrickForge generated model");
        sb.AppendLine("0 Name: model.mpd");
        sb.AppendLine("0 Author: BrickForge");
        sb.AppendLine($"0 !HISTORY Generated at {timestamp}");
        sb.AppendLine("0 !LICENSE Licensed under CC BY 2.0 (BrickForge generated content)");
        sb.AppendLine("0 LDraw Parts used in this file are from the LDraw Parts Library.");
        sb.AppendLine("0 The LDraw Parts Library is licensed under the Creative Commons Attribution 2.0 license.");
        sb.AppendLine("0 See https://www.ldraw.org/legal.html for details.");
        sb.AppendLine("0 DISCLAIMER: This is not an official LEGO building instruction.");
        sb.AppendLine();

        var partsByStep = graph.Parts
            .GroupBy(p => p.Step)
            .OrderBy(g => g.Key);

        foreach (var stepGroup in partsByStep)
        {
            foreach (var part in stepGroup)
                sb.AppendLine(FormatPartLine(part));

            sb.AppendLine("0 STEP");
        }

        return ExportResult.Ok(sb.ToString());
    }

    private static string FormatPartLine(BrickPartInstance part)
    {
        var colorCode = LDrawColorMap.GetCode(part.Color);

        float x = part.Position.Length > 0 ? part.Position[0] : 0f;
        float y = part.Position.Length > 1 ? part.Position[1] : 0f;
        float z = part.Position.Length > 2 ? part.Position[2] : 0f;

        var rot = part.Rotation.Length >= 9 ? part.Rotation : new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };

        return string.Format(
            CultureInfo.InvariantCulture,
            "1 {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13}.dat",
            colorCode,
            FormatFloat(x), FormatFloat(y), FormatFloat(z),
            FormatFloat(rot[0]), FormatFloat(rot[1]), FormatFloat(rot[2]),
            FormatFloat(rot[3]), FormatFloat(rot[4]), FormatFloat(rot[5]),
            FormatFloat(rot[6]), FormatFloat(rot[7]), FormatFloat(rot[8]),
            part.PartNumber);
    }

    private static string FormatFloat(float value)
    {
        // LDraw conventionally uses integers when the value is whole.
        if (value == MathF.Truncate(value))
            return ((int)value).ToString(CultureInfo.InvariantCulture);
        return value.ToString("G", CultureInfo.InvariantCulture);
    }
}
