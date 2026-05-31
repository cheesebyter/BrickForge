using BrickForge.BrickGraph.Templates;

namespace BrickForge.BrickGraph.Tests.Templates;

public class TemplateRegistryFromDirectoryTests
{
    private static void WriteTemplate(string dir, string id, string displayName)
    {
        var json = $$"""
            {
              "template_id": "{{id}}",
              "display_name": "{{displayName}}",
              "width_studs": 6,
              "depth_studs": 4,
              "height_layers": 4,
              "default_main_color": "black",
              "default_accent_color": "light_bluish_gray",
              "subassemblies": []
            }
            """;
        File.WriteAllText(Path.Combine(dir, $"{id}_template.json"), json);
    }

    [Fact]
    public void FromDirectory_LoadsAllMatchingTemplates()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            WriteTemplate(dir, "tmpl_a", "Template A");
            WriteTemplate(dir, "tmpl_b", "Template B");
            File.WriteAllText(Path.Combine(dir, "unrelated.json"), "{}");

            var registry = TemplateRegistry.FromDirectory(dir);

            Assert.NotNull(registry.FindTemplate("tmpl_a"));
            Assert.NotNull(registry.FindTemplate("tmpl_b"));
            Assert.Equal(2, registry.TemplateIds.Count);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void FromDirectory_EmptyDirectory_ReturnsEmptyRegistry()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var registry = TemplateRegistry.FromDirectory(dir);
            Assert.Empty(registry.TemplateIds);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void FromDirectory_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var dir = Path.Combine(Path.GetTempPath(), "no_such_dir_" + Guid.NewGuid().ToString("N"));
        Assert.Throws<DirectoryNotFoundException>(() => TemplateRegistry.FromDirectory(dir));
    }

    [Fact]
    public void FindTemplate_IsCaseInsensitive()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            WriteTemplate(dir, "small_machine", "Small Machine");
            var registry = TemplateRegistry.FromDirectory(dir);

            Assert.NotNull(registry.FindTemplate("Small_Machine"));
            Assert.NotNull(registry.FindTemplate("SMALL_MACHINE"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
