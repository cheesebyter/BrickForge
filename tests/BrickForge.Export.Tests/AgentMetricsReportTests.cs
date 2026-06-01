using BrickForge.BrickGraph.Model;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Agents;
using BrickForge.Core.Ai;
using BrickForge.Export;
using Graph = BrickForge.BrickGraph.BrickGraph;

namespace BrickForge.Export.Tests;

/// <summary>
/// Tests for BF-MVP1-044: agent metrics captured per stage and rendered in report.
/// </summary>
public sealed class AgentMetricsReportTests
{
    private static readonly ReportExporter _exporter = new();

    private static Graph BuildMinimalGraph()
    {
        var graph = new Graph
        {
            Model = new BrickModelMetadata { Id = "test", Name = "Test", TargetParts = 10 }
        };
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "p1",
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = "black",
            Step = 1
        });
        return graph;
    }

    private static AgentMetrics MakeAgentMetrics(
        string name,
        int llmCalls = 0,
        bool success = true,
        double? confidence = null,
        double? finalScore = null)
    {
        var start = DateTimeOffset.UtcNow.AddMilliseconds(-200);
        return new AgentMetrics
        {
            AgentName = name,
            StartTime = start,
            EndTime = start.AddMilliseconds(200),
            LlmCalls = llmCalls,
            Retries = 0,
            Success = success,
            Confidence = confidence,
            FinalScore = finalScore
        };
    }

    [Fact]
    public void AgentMetrics_DurationMs_IsComputedFromStartAndEnd()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMilliseconds(500);

        var metrics = new AgentMetrics
        {
            AgentName = "TestAgent",
            StartTime = start,
            EndTime = end,
            LlmCalls = 1,
            Retries = 0,
            Success = true
        };

        Assert.Equal(500, metrics.DurationMs);
    }

    [Fact]
    public void JobMetrics_TotalDurationMs_IsComputedFromStartAndEnd()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMilliseconds(1200);

        var jobMetrics = new JobMetrics
        {
            JobStartTime = start,
            JobEndTime = end,
            TotalLlmCalls = 2,
            TotalRetries = 1,
            JobSuccess = true,
            AgentBreakdown = []
        };

        Assert.Equal(1200, jobMetrics.TotalDurationMs);
    }

    [Fact]
    public void Export_WhenAgentMetricsPresent_RendersMetricsSection()
    {
        var agentMetrics = new List<AgentMetrics>
        {
            MakeAgentMetrics("PromptAnalysisAgent", llmCalls: 1, confidence: 0.95),
            MakeAgentMetrics("TemplateSelectionAgent", llmCalls: 0),
            MakeAgentMetrics("BrickGraphGeneratorAgent", llmCalls: 0, finalScore: 1.0)
        };

        var jobStart = DateTimeOffset.UtcNow.AddMilliseconds(-600);
        var jobMetrics = new JobMetrics
        {
            JobStartTime = jobStart,
            JobEndTime = DateTimeOffset.UtcNow,
            TotalLlmCalls = 1,
            TotalRetries = 0,
            JobSuccess = true,
            AgentBreakdown = agentMetrics
        };

        var data = new GenerationReportData
        {
            OriginalPrompt = "Test",
            ValidationResult = ValidationResult.FromIssues([], 3),
            AgentMetrics = agentMetrics,
            JobMetrics = jobMetrics
        };

        var result = _exporter.Export(BuildMinimalGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("Agentenmetriken", result.Content);
        Assert.Contains("PromptAnalysisAgent", result.Content);
        Assert.Contains("TemplateSelectionAgent", result.Content);
        Assert.Contains("BrickGraphGeneratorAgent", result.Content);
        Assert.Contains("LLM-Aufrufe gesamt", result.Content);
    }

    [Fact]
    public void Export_WhenJobMetricsPresent_RendersJobTotals()
    {
        var jobStart = DateTimeOffset.UtcNow.AddMilliseconds(-800);
        var jobMetrics = new JobMetrics
        {
            JobStartTime = jobStart,
            JobEndTime = DateTimeOffset.UtcNow,
            TotalLlmCalls = 3,
            TotalRetries = 1,
            JobSuccess = true,
            AgentBreakdown = [MakeAgentMetrics("Agent1")]
        };

        var data = new GenerationReportData
        {
            OriginalPrompt = "Test",
            ValidationResult = ValidationResult.FromIssues([], 3),
            AgentMetrics = jobMetrics.AgentBreakdown,
            JobMetrics = jobMetrics
        };

        var result = _exporter.Export(BuildMinimalGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("Gesamtdauer", result.Content);
        Assert.Contains("Retries gesamt", result.Content);
    }

    [Fact]
    public void Export_WhenNoAgentMetrics_DoesNotRenderMetricsSection()
    {
        var data = new GenerationReportData
        {
            OriginalPrompt = "Test",
            ValidationResult = ValidationResult.FromIssues([], 3),
            AgentMetrics = [],
            JobMetrics = null
        };

        var result = _exporter.Export(BuildMinimalGraph(), data);

        Assert.True(result.Success);
        Assert.DoesNotContain("Agentenmetriken", result.Content);
    }

    [Fact]
    public void Export_MetricsTable_ContainsSuccessColumn()
    {
        var agentMetrics = new List<AgentMetrics>
        {
            MakeAgentMetrics("Agent1", success: true),
            MakeAgentMetrics("Agent2", success: false)
        };

        var jobStart = DateTimeOffset.UtcNow.AddMilliseconds(-300);
        var jobMetrics = new JobMetrics
        {
            JobStartTime = jobStart,
            JobEndTime = DateTimeOffset.UtcNow,
            TotalLlmCalls = 0,
            TotalRetries = 0,
            JobSuccess = false,
            AgentBreakdown = agentMetrics
        };

        var data = new GenerationReportData
        {
            OriginalPrompt = "Test",
            ValidationResult = ValidationResult.FromIssues([], 3),
            AgentMetrics = agentMetrics,
            JobMetrics = jobMetrics
        };

        var result = _exporter.Export(BuildMinimalGraph(), data);

        Assert.True(result.Success);
        Assert.Contains("Ja", result.Content);
        Assert.Contains("Nein", result.Content);
    }

    [Fact]
    public void AgentMetrics_ConfidenceNull_DoesNotThrow()
    {
        var metrics = MakeAgentMetrics("Agent", confidence: null);

        Assert.Null(metrics.Confidence);
        Assert.NotNull(metrics.AgentName);
    }
}
