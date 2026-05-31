using BrickForge.BrickGraph.Model;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.Core.Ai;

namespace BrickForge.BrickGraph.Generation;

/// <summary>
/// Generic deterministic BrickGraph generator that works with any <see cref="BrickModelTemplate"/>.
/// Uses the template's subassemblies to determine preferred part numbers for each build section.
/// Falls back to safe defaults when a preferred part is absent from the registry.
///
/// LDraw coordinate convention:
///   X = left/right (stud columns, 20 units per stud)
///   Y = up/down, positive = DOWN (plate = 8 units, brick = 24 units)
///   Z = front/back (stud rows, 20 units per stud)
/// </summary>
public sealed class TemplateBasedGenerator
{
    private const float StudSize   = 20f;
    private const float PlateHeight = 8f;
    private const float BrickHeight = 24f;

    // Default part numbers used as fallback when template preferred_part is unavailable.
    private const string DefaultBasePart   = "3023";   // Plate 1×2
    private const string DefaultBodyPart   = "3010";   // Brick 1×4
    private const string DefaultPanelPart  = "3069b";  // Tile 1×2
    private const string DefaultTopPart    = "3020";   // Plate 2×4
    private const string DefaultDetailPart = "3024";   // Plate 1×1

    // Width × Depth in studs for all supported parts.
    private static readonly Dictionary<string, (int Width, int Depth)> PartDimensions = new()
    {
        ["3005"]  = (1, 1),  // Brick 1×1
        ["3004"]  = (1, 2),  // Brick 1×2
        ["3622"]  = (1, 3),  // Brick 1×3
        ["3010"]  = (1, 4),  // Brick 1×4
        ["3003"]  = (2, 2),  // Brick 2×2
        ["3002"]  = (2, 3),  // Brick 2×3
        ["3001"]  = (2, 4),  // Brick 2×4
        ["3024"]  = (1, 1),  // Plate 1×1
        ["3023"]  = (1, 2),  // Plate 1×2
        ["3710"]  = (1, 4),  // Plate 1×4
        ["3022"]  = (2, 2),  // Plate 2×2
        ["3020"]  = (2, 4),  // Plate 2×4
        ["3069b"] = (1, 2),  // Tile 1×2
        ["2431"]  = (1, 4),  // Tile 1×4
    };

    private readonly SupportedPartsRegistry _registry;

    public TemplateBasedGenerator(SupportedPartsRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Generates a <see cref="BrickGraph"/> for the supplied template and prompt analysis.
    /// </summary>
    public BrickGraph Generate(PromptAnalysisResult analysis, BrickModelTemplate template)
    {
        var mainColor   = ResolveColor(analysis.MainColor, template.DefaultMainColor);
        var accentColor = ResolveColor(analysis.AccentColor, template.DefaultAccentColor);
        var modelName   = string.IsNullOrWhiteSpace(analysis.ModelName)
            ? template.DisplayName
            : analysis.ModelName;

        var graph = new BrickGraph
        {
            Model = new BrickModelMetadata
            {
                Id          = Guid.NewGuid().ToString("N")[..12],
                Name        = modelName,
                TargetParts = analysis.TargetParts
            }
        };

        var stepPartIds = new Dictionary<int, List<string>>();
        int partIndex   = 1;

        var basePart   = ResolveSubassemblyPart(template, "base",          DefaultBasePart);
        var bodyPart   = ResolveSubassemblyPart(template, "main_body",     DefaultBodyPart);
        var panelPart  = ResolveSubassemblyPart(template, "front_panel",   DefaultPanelPart);
        var topPart    = ResolveSubassemblyPart(template, "top",           DefaultTopPart);
        var detailPart = ResolveSubassemblyPart(template, "simple_detail", DefaultDetailPart);

        // Step 1: Base
        var baseParts = BuildGridLayer(template, mainColor, basePart, y: 0f, step: 1, ref partIndex);
        RegisterParts(graph, baseParts, stepPartIds, step: 1);

        // Steps 2-N: Body layers
        int   bodyLayers = Math.Max(2, template.HeightLayers);
        float bodyBaseY  = -PlateHeight;
        for (int layer = 0; layer < bodyLayers; layer++)
        {
            int   step   = 2 + layer;
            float layerY = bodyBaseY - layer * BrickHeight;
            var   lparts = BuildGridLayer(template, mainColor, bodyPart, layerY, step, ref partIndex);
            RegisterParts(graph, lparts, stepPartIds, step);
        }

        // Front panel (one row, front face)
        int   frontStep   = bodyLayers + 2;
        float frontPanelY = bodyBaseY - bodyLayers * BrickHeight;
        var   frontParts  = BuildFrontRow(template, accentColor, panelPart, frontPanelY, frontStep, ref partIndex);
        RegisterParts(graph, frontParts, stepPartIds, frontStep);

        // Top layer
        int   topStep  = frontStep + 1;
        float topY     = frontPanelY - PlateHeight;
        var   topParts = BuildGridLayer(template, mainColor, topPart, topY, topStep, ref partIndex);
        RegisterParts(graph, topParts, stepPartIds, topStep);

        // Simple detail
        int   detailStep  = topStep + 1;
        float detailY     = topY - PlateHeight;
        var   detailParts = BuildDetail(accentColor, detailPart, detailY, detailStep, ref partIndex);
        if (detailParts.Count > 0)
            RegisterParts(graph, detailParts, stepPartIds, detailStep);

        foreach (var (stepNum, ids) in stepPartIds.OrderBy(kv => kv.Key))
        {
            graph.AddStep(new BrickStep
            {
                StepNumber      = stepNum,
                Label           = GetStepLabel(stepNum, bodyLayers),
                PartInstanceIds = ids
            });
        }

        return graph;
    }

    // ── Section builders ─────────────────────────────────────────────────────

    /// <summary>
    /// Fills the full template footprint with parts of the given type,
    /// placed in a grid based on each part's stud dimensions.
    /// </summary>
    private List<BrickPartInstance> BuildGridLayer(
        BrickModelTemplate template, string color, string partNumber,
        float y, int step, ref int idx)
    {
        var def  = ResolvePart(partNumber, DefaultBasePart);
        var dims = GetDimensions(partNumber);
        var parts = new List<BrickPartInstance>();

        int cols = Math.Max(1, template.WidthStuds / dims.Width);
        int rows = Math.Max(1, template.DepthStuds / dims.Depth);

        for (int row = 0; row < rows; row++)
        {
            float z = row * dims.Depth * StudSize + dims.Depth * StudSize / 2f;
            for (int col = 0; col < cols; col++)
            {
                float x = col * dims.Width * StudSize + dims.Width * StudSize / 2f;
                parts.Add(MakePart(def, color, x, y, z, step, ref idx));
            }
        }
        return parts;
    }

    /// <summary>
    /// Builds a single row of tiles/panels across the front face of the model.
    /// </summary>
    private List<BrickPartInstance> BuildFrontRow(
        BrickModelTemplate template, string color, string partNumber,
        float y, int step, ref int idx)
    {
        var def  = ResolvePart(partNumber, DefaultPanelPart);
        var dims = GetDimensions(partNumber);
        var parts = new List<BrickPartInstance>();

        int   cols = Math.Max(1, template.WidthStuds / dims.Width);
        float z    = dims.Depth * StudSize / 2f; // front face

        for (int col = 0; col < cols; col++)
        {
            float x = col * dims.Width * StudSize + dims.Width * StudSize / 2f;
            parts.Add(MakePart(def, color, x, y, z, step, ref idx));
        }
        return parts;
    }

    /// <summary>Two small detail pieces at the top of the model.</summary>
    private List<BrickPartInstance> BuildDetail(
        string color, string partNumber, float y, int step, ref int idx)
    {
        var def = ResolvePart(partNumber, DefaultDetailPart);
        return
        [
            MakePart(def, color, 0f,       y, 0f, step, ref idx),
            MakePart(def, color, StudSize, y, 0f, step, ref idx)
        ];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BrickPartInstance MakePart(
        PartDefinition def, string color, float x, float y, float z, int step, ref int idx)
    {
        var part = new BrickPartInstance
        {
            InstanceId  = $"part_{idx:000}",
            PartNumber  = def.PartNumber,
            PartName    = def.PartName,
            Color       = color,
            Position    = [x, y, z],
            Rotation    = [1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f],
            Step        = step
        };
        idx++;
        return part;
    }

    private static void RegisterParts(
        BrickGraph graph,
        IEnumerable<BrickPartInstance> parts,
        Dictionary<int, List<string>> stepPartIds,
        int step)
    {
        foreach (var part in parts)
        {
            graph.AddPart(part);
            if (!stepPartIds.TryGetValue(step, out var ids))
            {
                ids = [];
                stepPartIds[step] = ids;
            }
            ids.Add(part.InstanceId);
        }
    }

    private string ResolveColor(string requested, string templateDefault)
    {
        if (_registry.IsColorSupported(requested)) return requested;
        if (_registry.IsColorSupported(templateDefault)) return templateDefault;
        return "black";
    }

    private string ResolveSubassemblyPart(BrickModelTemplate template, string name, string fallback)
    {
        var sub = template.Subassemblies.FirstOrDefault(
            s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        if (sub?.PreferredPart is { Length: > 0 } pref && _registry.FindPart(pref) is not null)
            return pref;
        return fallback;
    }

    private PartDefinition ResolvePart(string partNumber, string fallback)
        => _registry.FindPart(partNumber)
           ?? _registry.FindPart(fallback)
           ?? throw new InvalidOperationException(
               $"Neither part '{partNumber}' nor fallback '{fallback}' found in registry.");

    private static (int Width, int Depth) GetDimensions(string partNumber)
        => PartDimensions.TryGetValue(partNumber, out var dims) ? dims : (1, 1);

    private static string GetStepLabel(int step, int bodyLayers) => step switch
    {
        1 => "Grundfläche",
        _ when step <= bodyLayers + 1 => $"Hauptkörper Schicht {step - 1}",
        _ when step == bodyLayers + 2 => "Frontpanel",
        _ when step == bodyLayers + 3 => "Abschlussplatte",
        _ => "Detail"
    };
}
