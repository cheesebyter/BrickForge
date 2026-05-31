using BrickForge.Api.Workers;
using BrickForge.Core.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrickForge.Api.Tests;

public sealed class GenerationJobWorkerTests
{
    [Fact]
    public async Task Worker_ProcessesSingleJob_ThenStopsOnCancellation()
    {
        var processed = new List<string>();
        var cts = new CancellationTokenSource();
        var queue = new SequentialFakeJobQueue(["job-1"], cts);
        var pipeline = new RecordingPipelineService(processed);

        var worker = new GenerationJobWorker(queue, pipeline, NullLogger<GenerationJobWorker>.Instance);
        await worker.StartAsync(cts.Token);
        await Task.Delay(200); // let the worker pick up the job
        await worker.StopAsync(CancellationToken.None);

        Assert.Contains("job-1", processed);
    }

    [Fact]
    public async Task Worker_DoesNotCrash_WhenPipelineThrows()
    {
        var cts = new CancellationTokenSource();
        var queue = new SequentialFakeJobQueue(["job-error"], cts);
        var pipeline = new ThrowingPipelineService();

        var worker = new GenerationJobWorker(queue, pipeline, NullLogger<GenerationJobWorker>.Instance);
        var ex = await Record.ExceptionAsync(async () =>
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(200);
            await worker.StopAsync(CancellationToken.None);
        });

        Assert.Null(ex);
    }

    [Fact]
    public async Task Worker_ProcessesMultipleJobs_InOrder()
    {
        var processed = new List<string>();
        var cts = new CancellationTokenSource();
        var queue = new SequentialFakeJobQueue(["job-a", "job-b", "job-c"], cts);
        var pipeline = new RecordingPipelineService(processed);

        var worker = new GenerationJobWorker(queue, pipeline, NullLogger<GenerationJobWorker>.Instance);
        await worker.StartAsync(cts.Token);
        await Task.Delay(500);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(3, processed.Count);
        Assert.Equal(new[] { "job-a", "job-b", "job-c" }, processed.ToArray());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class SequentialFakeJobQueue : IJobQueue
    {
        private readonly Queue<string> _jobs;
        private readonly CancellationTokenSource _cts;

        public SequentialFakeJobQueue(IEnumerable<string> jobIds, CancellationTokenSource cts)
        {
            _jobs = new Queue<string>(jobIds);
            _cts = cts;
        }

        public void Enqueue(string jobId) => _jobs.Enqueue(jobId);

        public async Task<string> DequeueAsync(CancellationToken cancellationToken = default)
        {
            if (_jobs.TryDequeue(out var id))
            {
                return id;
            }
            _cts.Cancel();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return string.Empty;
        }
    }

    private sealed class RecordingPipelineService : IGenerationPipelineService
    {
        private readonly List<string> _recorded;
        public RecordingPipelineService(List<string> recorded) => _recorded = recorded;

        public Task RunAsync(string jobId, CancellationToken cancellationToken = default)
        {
            _recorded.Add(jobId);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPipelineService : IGenerationPipelineService
    {
        public Task RunAsync(string jobId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated pipeline failure");
    }
}
