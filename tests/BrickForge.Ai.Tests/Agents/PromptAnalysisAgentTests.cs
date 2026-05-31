using BrickForge.Ai.Agents;
using BrickForge.Ai.Analysis;
using BrickForge.Core.Agents;
using BrickForge.Core.Ai;
using BrickForge.Core.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrickForge.Ai.Tests.Agents;

public class PromptAnalysisAgentTests
{
    private sealed class FakeAnalyzer : IPromptAnalyzer
    {
        private readonly Result<PromptAnalysisResult> _result;
        public FakeAnalyzer(Result<PromptAnalysisResult> result) => _result = result;
        public Task<Result<PromptAnalysisResult>> AnalyzeAsync(
            string userPrompt, CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    private static AgentContext MakeContext() => new() { JobId = "test-job-pai" };

    [Fact]
    public async Task RunAsync_SuccessfulAnalysis_ReturnsSuccessResult()
    {
        var analysis = new PromptAnalysisResult
        {
            ModelName     = "Kaffeemaschine",
            ModelCategory = "small_machine",
            TargetParts   = 50,
            Feasible      = true
        };
        var agent  = new PromptAnalysisAgent(
            new FakeAnalyzer(Result<PromptAnalysisResult>.Success(analysis)),
            NullLogger<PromptAnalysisAgent>.Instance);

        var result = await agent.RunAsync("Eine Kaffeemaschine", MakeContext());

        Assert.True(result.IsSuccess);
        Assert.Equal("Kaffeemaschine", result.Value!.ModelName);
    }

    [Fact]
    public async Task RunAsync_AnalyzerFailure_ReturnsAgentFailure()
    {
        var agent = new PromptAnalysisAgent(
            new FakeAnalyzer(Result<PromptAnalysisResult>.Failure("Ollama unavailable")),
            NullLogger<PromptAnalysisAgent>.Instance);

        var result = await agent.RunAsync("some prompt", MakeContext());

        Assert.False(result.IsSuccess);
        Assert.Contains("Ollama unavailable", result.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_InfeasibleAnalysis_ReturnsAgentFailure()
    {
        var analysis = new PromptAnalysisResult
        {
            Feasible  = false,
            Warnings  = ["Too many parts requested"],
            ModelName = "ImpossibleModel"
        };
        var agent = new PromptAnalysisAgent(
            new FakeAnalyzer(Result<PromptAnalysisResult>.Success(analysis)),
            NullLogger<PromptAnalysisAgent>.Instance);

        var result = await agent.RunAsync("impossible prompt", MakeContext());

        Assert.False(result.IsSuccess);
        Assert.Contains("not feasible", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_CancellationRequested_PropagatesCancellation()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var analyzer = new CancellationAwareAnalyzer();
        var agent    = new PromptAnalysisAgent(analyzer, NullLogger<PromptAnalysisAgent>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => agent.RunAsync("cancel me", MakeContext(), cts.Token));
    }

    [Fact]
    public void AgentName_IsCorrect()
    {
        var agent = new PromptAnalysisAgent(
            new FakeAnalyzer(Result<PromptAnalysisResult>.Failure("x")),
            NullLogger<PromptAnalysisAgent>.Instance);
        Assert.Equal("PromptAnalysisAgent", agent.AgentName);
    }

    // Helper that respects cancellation
    private sealed class CancellationAwareAnalyzer : IPromptAnalyzer
    {
        public Task<Result<PromptAnalysisResult>> AnalyzeAsync(string userPrompt, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(Result<PromptAnalysisResult>.Failure("never reached"));
        }
    }
}
