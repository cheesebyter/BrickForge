using BrickForge.Core.Pipelines;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BrickForge.Api.Workers;

/// <summary>
/// Background service that continuously dequeues job IDs and runs the generation pipeline.
/// Exceptions from individual jobs are logged and do not crash the worker.
/// </summary>
public sealed class GenerationJobWorker : BackgroundService
{
    private readonly IJobQueue _queue;
    private readonly IGenerationPipelineService _pipeline;
    private readonly ILogger<GenerationJobWorker> _logger;

    public GenerationJobWorker(
        IJobQueue queue,
        IGenerationPipelineService pipeline,
        ILogger<GenerationJobWorker> logger)
    {
        _queue = queue;
        _pipeline = pipeline;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GenerationJobWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            string jobId;
            try
            {
                jobId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await _pipeline.RunAsync(jobId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing job {JobId}.", jobId);
            }
        }

        _logger.LogInformation("GenerationJobWorker stopped.");
    }
}
