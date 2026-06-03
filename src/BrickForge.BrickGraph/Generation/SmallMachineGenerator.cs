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
///   X = left/right (stud columns, 20 LDU per stud)
///   Y = up/down, positive = DOWN (plate = 8 LDU, brick = 24 LDU)
///   Z = front/back (stud rows, 20 LDU per stud)
///
/// Part origin = stud surface (top face of part body).
/// The part body hangs DOWNWARD (positive Y) from the origin.
/// To stack part B on part A: B.Y = A.Y - B.Height.
///
/// Stud alignment strategy:
///   Base uses Plate 1×2 (3023): 2 studs in X, 1 stud in Z.
///   Columns at X = 20, 60, 100  (centers of 40-LDU-wide plates).
///   Rows    at Z = 0, 20, 40, 60 (centers of 20-LDU-deep plates).
///   Body uses Brick 1×2 (3004): identical X/Z footprint → perfect stud alignment.
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
        // First brick sits on the base plate: brick.Y = basePlate.Y - BrickHeight
        // basePlate.Y = -PlateHeight, so bodyBaseY = -PlateHeight - BrickHeight = -32
        float bodyBaseY = -(PlateHeight + BrickHeight);
        for (int layer = 0; layer < bodyLayers; layer++)
        {
            float layerY = bodyBaseY - layer * BrickHeight;
            var layerParts = BuildBodyLayer(template, mainColor, layer, layerY, ref partIndex);
            RegisterParts(graph, layerParts, stepPartIds, step: 2 + layer);
        }

        // ── Step bodyLayers+2: Front panel tiles ──────────────────────────────
        int frontStep = bodyLayers + 2;
        // Tile sits on top of last body layer: tile.Y = lastBrick.Y - PlateHeight
        // lastBrick.Y = bodyBaseY - (bodyLayers-1)*BrickHeight
        float frontPanelY = bodyBaseY - (bodyLayers - 1) * BrickHeight - PlateHeight;
        var frontParts = BuildFrontPanel(template, accentColor, frontPanelY, ref partIndex);
        RegisterParts(graph, frontParts, stepPartIds, frontStep);

        // ── Step bodyLayers+3: Top plates ─────────────────────────────────────
        int topStep = frontStep + 1;
        // Top plates sit on the SAME body surface as the front panel tiles, covering
        // the remaining Z rows (rows 1..depth-1). frontPanelY is already the correct
        // Y for parts resting on the body top (body.Y - PlateHeight).
        float topY = frontPanelY;
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
        // Base plate stud surface: Y = -PlateHeight (body bottom at Y=0 = floor)
        const float y = -PlateHeight;

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

    /// <summary>
    /// One layer of Brick 1×2 (3004) filling the body footprint.
    ///
    /// Uses the SAME X/Z grid as the base plates so every brick's anti-stud
    /// holes sit directly above the plate (or previous-layer brick) studs:
    ///   X columns: 20, 60, 100  (WidthStuds/2 columns of 2-stud-wide bricks)
    ///   Z rows   : 0, 20, 40, 60  (DepthStuds rows of 1-stud-deep bricks)
    /// </summary>
    private List<BrickPartInstance> BuildBodyLayer(
        BrickModelTemplate template, string color, int layer, float layerY, ref int idx)
    {
        var def = _registry.FindPart("3004")!; // Brick 1×2 — same footprint as Plate 1×2
        var parts = new List<BrickPartInstance>();
        int step = 2 + layer;
        int cols = template.WidthStuds / 2; // e.g. 6/2 = 3 columns

        for (int row = 0; row < template.DepthStuds; row++)
        {
            float z = row * StudSize;                    // 0, 20, 40, 60
            for (int col = 0; col < cols; col++)
            {
                float x = col * 2 * StudSize + StudSize; // 20, 60, 100
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

    /// <summary>
    /// Top surface: Plate 1×2 (3023) covering all rows except the front row (Z=0),
    /// which is covered by front-panel tiles.
    ///
    /// All top plates share the same Y as the front panel tiles — they sit directly
    /// on the last body layer at the same elevation.
    /// </summary>
    private List<BrickPartInstance> BuildTop(
        BrickModelTemplate template, string color, float topY, ref int idx)
    {
        var def = _registry.FindPart("3023")!; // Plate 1×2
        var parts = new List<BrickPartInstance>();
        int step = template.HeightLayers + 3;
        int cols = template.WidthStuds / 2;

        // Start at row 1: row 0 (Z=0) is covered by the front panel tiles.
        for (int row = 1; row < template.DepthStuds; row++)
        {
            float z = row * StudSize; // 20, 40, 60
            for (int col = 0; col < cols; col++)
            {
                float x = col * 2 * StudSize + StudSize; // 20, 60, 100
                parts.Add(MakePart(def, color, x, topY, z, step, ref idx));
            }
        }
        return parts;
    }

    /// <summary>
    /// Two Plate 1×1 (3024) detail pieces on the top surface.
    /// Placed at Z=20 (first top-plate row) at X=10, X=30, which are the two
    /// stud positions of the leftmost top plate at X=20.
    /// </summary>
    private List<BrickPartInstance> BuildDetail(
        BrickModelTemplate template, string color, float detailY, ref int idx)
    {
        var def = _registry.FindPart("3024")!; // Plate 1×1
        int step = template.HeightLayers + 4;
        const float z = StudSize; // Z=20 (first top-plate row)
        return
        [
            MakePart(def, color, StudSize / 2f,            detailY, z, step, ref idx), // X=10
            MakePart(def, color, StudSize + StudSize / 2f, detailY, z, step, ref idx)  // X=30
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
