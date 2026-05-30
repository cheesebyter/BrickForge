namespace BrickForge.Core.Jobs;

/// <summary>
/// Abstraction for storing and retrieving <see cref="GenerationJob"/> instances.
/// </summary>
public interface IJobRepository
{
    Task<GenerationJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<GenerationJob> CreateAsync(GenerationJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(GenerationJob job, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GenerationJob>> ListAsync(CancellationToken cancellationToken = default);
}
