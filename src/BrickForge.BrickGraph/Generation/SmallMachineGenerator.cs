using BrickForge.BrickGraph.Model;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.Core.Ai;

namespace BrickForge.BrickGraph.Generation;

/// <summary>
/// Generates a BrickGraph for the <c>small_machine</c> template.
///
/// The generator is fully deterministic. It produces a simple rectangular
/// machine model (base → body layers → front panel → top → optional detail).
///
/// LDraw coordinate convention used here:
///   X = left/right (stud columns, 20 units per stud)
///   Y = up/down, positive = DOWN (plate = 8 units, brick = 24 units)
///   Z = front/back (stud rows, 20 units per stud)
/// </summary>
public sealed class SmallMachineGenerator
{
    // LDraw unit constants
    private const float StudSize = 20f;      // 1 stud = 20 LDraw units
    private const float PlateHeight = 8f;    // 1 plate = 8 LDraw units tall
    private const float BrickHeight = 24f;   // 1 brick = 24 LDraw units tall

    private readonly SupportedPartsRegistry _registry;

    public SmallMachineGenerator(SupportedPartsRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Generates a BrickGraph from the given prompt analysis and template.
    /// </summary>
    public BrickGraph Generate(PromptAnalysisResult analysis, BrickModelTemplate template)
    {
        var mainColor = ResolveColor(analysis.MainColor);
        var accentColor = ResolveColor(analysis.AccentColor);
        var modelName = string.IsNullOrWhiteSpace(analysis.ModelName) ? template.DisplayName : analysis.ModelName;

        var graph = new BrickGraph
        {
            Model = new BrickModelMetadata
            {
                Id = Guid.NewGuid().ToString("N")[..12],
                Name = modelName,
                TargetParts = analysis.TargetParts
            }
        };

        int partIndex = 1;
        var stepPartIds = new Dictionary<int, List<string>>();

        // ── Step 1: Base plates ───────────────────────────────────────────────
        var baseParts = BuildBase(template, mainColor, ref partIndex);
        RegisterParts(graph, baseParts, stepPartIds, step: 1);

        // ── Steps 2-N: Main body brick layers ─────────────────────────────────
        int bodyLayers = Math.Max(2, template.HeightLayers);
        float bodyBaseY = -PlateHeight; // just above the base layer
        for (int layer = 0; layer < bodyLayers; layer++)
        {
            float layerY = bodyBaseY - layer * BrickHeight;
            var layerParts = BuildBodyLayer(template, mainColor, layer, layerY, ref partIndex);
            RegisterParts(graph, layerParts, stepPartIds, step: 2 + layer);
        }

        // ── Step bodyLayers+2: Front panel tiles ──────────────────────────────
        int frontStep = bodyLayers + 2;
        float frontPanelY = bodyBaseY - bodyLayers * BrickHeight;
        var frontParts = BuildFrontPanel(template, accentColor, frontPanelY, ref partIndex);
        RegisterParts(graph, frontParts, stepPartIds, frontStep);

        // ── Step bodyLayers+3: Top plates ─────────────────────────────────────
        int topStep = frontStep + 1;
        float topY = frontPanelY - PlateHeight;
        var topParts = BuildTop(template, mainColor, topY, ref partIndex);
        RegisterParts(graph, topParts, stepPartIds, topStep);

        // ── Step bodyLayers+4: Simple detail ──────────────────────────────────
        int detailStep = topStep + 1;
        float detailY = topY - PlateHeight;
        var detailParts = BuildDetail(template, accentColor, detailY, ref partIndex);
        if (detailParts.Count > 0)
            RegisterParts(graph, detailParts, stepPartIds, detailStep);

        // ── Assign steps to graph ─────────────────────────────────────────────
        foreach (var (stepNum, ids) in stepPartIds.OrderBy(kv => kv.Key))
        {
            graph.AddStep(new BrickStep
            {
                StepNumber = stepNum,
                Label = GetStepLabel(stepNum, bodyLayers),
                PartInstanceIds = ids
            });
        }

        return graph;
    }

    // ── Section builders ─────────────────────────────────────────────────────

    /// <summary>Row of Plate 1×2 (3023) covering the full base footprint.</summary>
    private List<BrickPartInstance> BuildBase(BrickModelTemplate template, string color, ref int idx)
    {
        var def = _registry.FindPart("3023")!; // Plate 1×2
        var parts = new List<BrickPartInstance>();
        const float y = 0f;

        for (int row = 0; row < template.DepthStuds; row++)
        {
            int cols = template.WidthStuds / 2;
            for (int col = 0; col < cols; col++)
            {
                float x = col * 2 * StudSize + StudSize; // center of 2-stud-wide plate
                float z = row * StudSize;
                parts.Add(MakePart(def, color, x, y, z, step: 1, ref idx));
            }
        }
        return parts;
    }

    /// <summary>One layer of Brick 1×4 (3010) filling the body width.</summary>
    private List<BrickPartInstance> BuildBodyLayer(
        BrickModelTemplate template, string color, int layer, float layerY, ref int idx)
    {
        var def = _registry.FindPart("3010")!; // Brick 1×4
        var parts = new List<BrickPartInstance>();
        int step = 2 + layer;

        // Place rows: each brick covers 4 studs in Z direction
        int bricksPerRow = template.WidthStuds; // 1 wide
        int rowsNeeded = template.DepthStuds / 4; // each brick is 4 deep
        if (rowsNeeded == 0) rowsNeeded = 1;

        for (int row = 0; row < rowsNeeded; row++)
        {
            float z = row * 4 * StudSize + StudSize * 2; // center of 4-deep brick
            for (int col = 0; col < bricksPerRow; col++)
            {
                float x = col * StudSize;
                parts.Add(MakePart(def, color, x, layerY, z, step, ref idx));
            }
        }
        return parts;
    }

    /// <summary>One row of Tile 1×2 (3069b) on the front face.</summary>
    private List<BrickPartInstance> BuildFrontPanel(
        BrickModelTemplate template, string color, float panelY, ref int idx)
    {
        var def = _registry.FindPart("3069b")!; // Tile 1×2
        var parts = new List<BrickPartInstance>();
        const float z = 0f; // front face
        int step = template.HeightLayers + 2;

        int tilesWide = template.WidthStuds / 2;
        for (int col = 0; col < tilesWide; col++)
        {
            float x = col * 2 * StudSize + StudSize; // center of 2-wide tile
            parts.Add(MakePart(def, color, x, panelY, z, step, ref idx));
        }
        return parts;
    }

    /// <summary>Row of Plate 2×4 (3020) covering the top.</summary>
    private List<BrickPartInstance> BuildTop(
        BrickModelTemplate template, string color, float topY, ref int idx)
    {
        var def = _registry.FindPart("3020")!; // Plate 2×4
        var parts = new List<BrickPartInstance>();
        int step = template.HeightLayers + 3;

        int plates = template.WidthStuds / 2;
        for (int col = 0; col < plates; col++)
        {
            float x = col * 2 * StudSize + StudSize;
            float z = StudSize * 2;
            parts.Add(MakePart(def, color, x, topY, z, step, ref idx));
        }
        return parts;
    }

    /// <summary>Two Plate 1×1 (3024) detail pieces.</summary>
    private List<BrickPartInstance> BuildDetail(
        BrickModelTemplate template, string color, float detailY, ref int idx)
    {
        var def = _registry.FindPart("3024")!; // Plate 1×1
        int step = template.HeightLayers + 4;
        return
        [
            MakePart(def, color, 0f, detailY, 0f, step, ref idx),
            MakePart(def, color, StudSize, detailY, 0f, step, ref idx)
        ];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BrickPartInstance MakePart(
        PartDefinition def, string color, float x, float y, float z, int step, ref int idx)
    {
        var part = new BrickPartInstance
        {
            InstanceId = $"part_{idx:000}",
            PartNumber = def.PartNumber,
            PartName = def.PartName,
            Color = color,
            Position = [x, y, z],
            Rotation = [1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f],
            Step = step
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

    private string ResolveColor(string requested)
        => _registry.IsColorSupported(requested) ? requested : "black";

    private static string GetStepLabel(int step, int bodyLayers) => step switch
    {
        1 => "Grundfläche",
        _ when step <= bodyLayers + 1 => $"Hauptkörper Schicht {step - 1}",
        _ when step == bodyLayers + 2 => "Frontpanel",
        _ when step == bodyLayers + 3 => "Abschlussplatte",
        _ => "Detail"
    };
}
