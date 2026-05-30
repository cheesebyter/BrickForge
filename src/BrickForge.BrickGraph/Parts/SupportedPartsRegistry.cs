using System.Text.Json;

namespace BrickForge.BrickGraph.Parts;

/// <summary>
/// In-memory registry of supported parts and colours for MVP0.
/// Loaded from JSON files at construction time.
/// </summary>
public sealed class SupportedPartsRegistry
{
    private readonly Dictionary<string, PartDefinition> _parts;
    private readonly HashSet<string> _colors;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SupportedPartsRegistry(IEnumerable<PartDefinition> parts, IEnumerable<string> colors)
    {
        _parts = parts.ToDictionary(p => p.PartNumber, StringComparer.OrdinalIgnoreCase);
        _colors = new HashSet<string>(colors, StringComparer.OrdinalIgnoreCase);
    }

    // ── Static factory ────────────────────────────────────────────────────────

    /// <summary>
    /// Loads registry from JSON strings (e.g. read from embedded resources or files).
    /// </summary>
    public static SupportedPartsRegistry FromJson(string partsJson, string colorsJson)
    {
        var parts = JsonSerializer.Deserialize<List<PartDefinition>>(partsJson, JsonOptions)
                    ?? [];
        var colors = JsonSerializer.Deserialize<List<string>>(colorsJson, JsonOptions)
                     ?? [];
        return new SupportedPartsRegistry(parts, colors);
    }

    // ── Lookup ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="PartDefinition"/> for the given part number, or null if not supported.
    /// </summary>
    public PartDefinition? FindPart(string partNumber)
        => _parts.TryGetValue(partNumber, out var def) ? def : null;

    /// <summary>
    /// Returns true if the part number is in the supported list.
    /// </summary>
    public bool IsPartSupported(string partNumber)
        => _parts.ContainsKey(partNumber);

    /// <summary>
    /// Returns true if the colour name is in the supported list.
    /// </summary>
    public bool IsColorSupported(string color)
        => _colors.Contains(color);

    /// <summary>
    /// Returns all supported part numbers.
    /// </summary>
    public IReadOnlyCollection<string> SupportedPartNumbers => _parts.Keys;

    /// <summary>
    /// Returns all supported colour names.
    /// </summary>
    public IReadOnlyCollection<string> SupportedColors => _colors;
}
