namespace BrickForge.Core.Catalog;

/// <summary>
/// Abstraction for persisting and querying the template and part catalog.
/// </summary>
public interface ICatalogRepository
{
    Task SeedTemplatesAsync(IEnumerable<TemplateDefinitionEntry> templates, CancellationToken ct = default);
    Task SeedPartsAsync(IEnumerable<PartDefinitionEntry> parts, CancellationToken ct = default);
    Task<IReadOnlyList<TemplateDefinitionEntry>> ListTemplatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PartDefinitionEntry>> ListPartsAsync(CancellationToken ct = default);
}
