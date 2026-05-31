using BrickForge.BrickGraph.Agents;
using BrickForge.BrickGraph.Templates;
using BrickForge.Core.Agents;
using BrickForge.Core.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrickForge.BrickGraph.Tests.Agents;

public class TemplateSelectionAgentTests
{
    private static TemplateRegistry BuildRegistry(params string[] templateIds)
    {
        var templates = templateIds.Select(id => new BrickModelTemplate
        {
            TemplateId         = id,
            DisplayName        = id,
            WidthStuds         = 6,
            DepthStuds         = 4,
            HeightLayers       = 4,
            DefaultMainColor   = "black",
            DefaultAccentColor = "light_bluish_gray",
            Subassemblies      = []
        });
        return new TemplateRegistry(templates);
    }

    private static AgentContext MakeContext() => new() { JobId = "test-job-001" };

    [Fact]
    public async Task RunAsync_KnownCategory_ReturnsMatchingTemplate()
    {
        var registry = BuildRegistry("small_machine", "small_building");
        var agent    = new TemplateSelectionAgent(registry, NullLogger<TemplateSelectionAgent>.Instance);
        var analysis = new PromptAnalysisResult { ModelCategory = "small_building" };

        var result = await agent.RunAsync(analysis, MakeContext());

        Assert.True(result.IsSuccess);
        Assert.Equal("small_building", result.Value!.TemplateId);
    }

    [Fact]
    public async Task RunAsync_UnknownCategory_FallsBackToSmallMachine()
    {
        var registry = BuildRegistry("small_machine");
        var agent    = new TemplateSelectionAgent(registry, NullLogger<TemplateSelectionAgent>.Instance);
        var analysis = new PromptAnalysisResult { ModelCategory = "rocket_ship" };

        var result = await agent.RunAsync(analysis, MakeContext());

        Assert.True(result.IsSuccess);
        Assert.Equal("small_machine", result.Value!.TemplateId);
    }

    [Fact]
    public async Task RunAsync_NoTemplatesInRegistry_ReturnsFailure()
    {
        var registry = BuildRegistry(); // empty
        var agent    = new TemplateSelectionAgent(registry, NullLogger<TemplateSelectionAgent>.Instance);
        var analysis = new PromptAnalysisResult { ModelCategory = "small_machine" };

        var result = await agent.RunAsync(analysis, MakeContext());

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Fact]
    public async Task RunAsync_AgentNameIsCorrect()
    {
        var registry = BuildRegistry("small_machine");
        var agent    = new TemplateSelectionAgent(registry, NullLogger<TemplateSelectionAgent>.Instance);
        Assert.Equal("TemplateSelectionAgent", agent.AgentName);
    }

    [Fact]
    public async Task RunAsync_CategoryMatchIsCaseInsensitive()
    {
        var registry = BuildRegistry("small_machine");
        var agent    = new TemplateSelectionAgent(registry, NullLogger<TemplateSelectionAgent>.Instance);
        var analysis = new PromptAnalysisResult { ModelCategory = "SMALL_MACHINE" };

        var result = await agent.RunAsync(analysis, MakeContext());

        Assert.True(result.IsSuccess);
    }
}
