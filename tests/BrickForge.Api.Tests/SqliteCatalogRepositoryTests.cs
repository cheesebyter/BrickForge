using BrickForge.Api.Persistence;
using BrickForge.Core.Catalog;

namespace BrickForge.Api.Tests;

/// <summary>
/// Tests for <see cref="SqliteJobRepository"/> acting as <see cref="ICatalogRepository"/>.
/// </summary>
public sealed class SqliteCatalogRepositoryTests
{
    private static SqliteJobRepository CreateRepo() =>
        new("Data Source=:memory:");

    // ── BF-MVP1-021: ModelTemplates table ─────────────────────────────────────

    [Fact]
    public async Task SeedTemplatesAsync_AndListTemplatesAsync_ReturnsSeededTemplates()
    {
        ICatalogRepository repo = CreateRepo();
        var entries = new[]
        {
            new TemplateDefinitionEntry("small_machine", "Small Machine", "1.0", "A small machine template"),
            new TemplateDefinitionEntry("small_building", "Small Building", "1.0", null)
        };

        await repo.SeedTemplatesAsync(entries);

        var result = await repo.ListTemplatesAsync();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == "small_machine" && t.Name == "Small Machine");
        Assert.Contains(result, t => t.Id == "small_building" && t.Description == null);
    }

    [Fact]
    public async Task SeedTemplatesAsync_WhenCalledTwice_UpsertsMaintainsUniqueness()
    {
        ICatalogRepository repo = CreateRepo();
        var initial = new[] { new TemplateDefinitionEntry("small_machine", "Small Machine", "1.0", null) };
        await repo.SeedTemplatesAsync(initial);

        var updated = new[] { new TemplateDefinitionEntry("small_machine", "Small Machine v2", "2.0", "Updated") };
        await repo.SeedTemplatesAsync(updated);

        var result = await repo.ListTemplatesAsync();
        Assert.Single(result);
        Assert.Equal("Small Machine v2", result[0].Name);
        Assert.Equal("2.0", result[0].Version);
    }

    [Fact]
    public async Task ListTemplatesAsync_WhenEmpty_ReturnsEmptyList()
    {
        ICatalogRepository repo = CreateRepo();

        var result = await repo.ListTemplatesAsync();

        Assert.Empty(result);
    }

    // ── BF-MVP1-021: PartDefinitions table ────────────────────────────────────

    [Fact]
    public async Task SeedPartsAsync_AndListPartsAsync_ReturnsSeededParts()
    {
        ICatalogRepository repo = CreateRepo();
        var entries = new[]
        {
            new PartDefinitionEntry("3001", "3001", "Brick 2x4", "brick", true),
            new PartDefinitionEntry("3024", "3024", "Plate 1x1", "plate", true)
        };

        await repo.SeedPartsAsync(entries);

        var result = await repo.ListPartsAsync();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.PartNumber == "3001" && p.Supported);
        Assert.Contains(result, p => p.PartNumber == "3024" && p.Category == "plate");
    }

    [Fact]
    public async Task SeedPartsAsync_WhenCalledTwice_UpsertsMaintainsUniqueness()
    {
        ICatalogRepository repo = CreateRepo();
        await repo.SeedPartsAsync([new PartDefinitionEntry("3001", "3001", "Brick 2x4", "brick", true)]);
        await repo.SeedPartsAsync([new PartDefinitionEntry("3001", "3001", "Brick 2x4 Updated", "brick", false)]);

        var result = await repo.ListPartsAsync();
        Assert.Single(result);
        Assert.Equal("Brick 2x4 Updated", result[0].Name);
        Assert.False(result[0].Supported);
    }

    [Fact]
    public async Task ListPartsAsync_WhenEmpty_ReturnsEmptyList()
    {
        ICatalogRepository repo = CreateRepo();

        var result = await repo.ListPartsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task SeedPartsAsync_WithNullCategory_RoundTripsCorrectly()
    {
        ICatalogRepository repo = CreateRepo();
        await repo.SeedPartsAsync([new PartDefinitionEntry("9999", "9999", "Special Part", null, true)]);

        var result = await repo.ListPartsAsync();
        Assert.Single(result);
        Assert.Null(result[0].Category);
    }
}
