namespace BrickForge.Core.Pipelines;

public interface IJobQueue
{
    void Enqueue(string jobId);
    Task<string> DequeueAsync(CancellationToken cancellationToken = default);
}
