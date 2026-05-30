using System.Text.Json;

namespace BrickForge.BrickGraph.Templates;

/// <summary>
/// Loads and provides access to brick model templates.
/// </summary>
public sealed class TemplateRegistry
{
    private readonly Dictionary<string, BrickModelTemplate> _templates;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public TemplateRegistry(IEnumerable<BrickModelTemplate> templates)
    {
        _templates = templates.ToDictionary(t => t.TemplateId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads a single template from its JSON representation.
    /// </summary>
    public static TemplateRegistry FromJson(string templateJson)
    {
        var template = JsonSerializer.Deserialize<BrickModelTemplate>(templateJson, JsonOptions)
                       ?? throw new InvalidOperationException("Failed to parse template JSON.");
        return new TemplateRegistry([template]);
    }

    /// <summary>
    /// Loads multiple templates from a collection of JSON strings.
    /// </summary>
    public static TemplateRegistry FromJsonCollection(IEnumerable<string> templateJsons)
    {
        var templates = templateJsons.Select(json =>
            JsonSerializer.Deserialize<BrickModelTemplate>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse template JSON."));
        return new TemplateRegistry(templates);
    }

    /// <summary>
    /// Returns the template for the given ID, or null if not found.
    /// </summary>
    public BrickModelTemplate? FindTemplate(string templateId)
        => _templates.TryGetValue(templateId, out var t) ? t : null;

    /// <summary>
    /// Returns all registered template IDs.
    /// </summary>
    public IReadOnlyCollection<string> TemplateIds => _templates.Keys;
}
