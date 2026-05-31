namespace BrickForge.Core.Catalog;

/// <summary>
/// Catalog entry for an available model template stored in the database.
/// </summary>
public sealed record TemplateDefinitionEntry(
    string Id,
    string Name,
    string Version,
    string? Description);
