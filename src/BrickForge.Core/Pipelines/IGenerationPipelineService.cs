namespace BrickForge.Core.Pipelines;

public interface IGenerationPipelineService
{
    Task RunAsync(string jobId, CancellationToken cancellationToken = default);
}
