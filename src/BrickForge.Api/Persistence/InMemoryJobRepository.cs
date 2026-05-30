using System.Collections.Concurrent;
using BrickForge.Core.Jobs;

namespace BrickForge.Api.Persistence;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IJobRepository"/>.
/// Suitable for MVP1; replace with a persistent store for production.
/// </summary>
public sealed class InMemoryJobRepository : IJobRepository
{
    private readonly ConcurrentDictionary<string, GenerationJob> _store = new();

    public Task<GenerationJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var job);
        return Task.FromResult(job);
    }

    public Task<GenerationJob> CreateAsync(GenerationJob job, CancellationToken cancellationToken = default)
    {
        _store[job.Id] = job;
        return Task.FromResult(job);
    }

    public Task UpdateAsync(GenerationJob job, CancellationToken cancellationToken = default)
    {
        _store[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GenerationJob>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GenerationJob> list = _store.Values.OrderByDescending(j => j.CreatedAt).ToList().AsReadOnly();
        return Task.FromResult(list);
    }
}
