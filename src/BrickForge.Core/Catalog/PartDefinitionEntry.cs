namespace BrickForge.Core.Catalog;

/// <summary>
/// Catalog entry for a supported brick part stored in the database.
/// </summary>
public sealed record PartDefinitionEntry(
    string Id,
    string PartNumber,
    string Name,
    string? Category,
    bool Supported);
