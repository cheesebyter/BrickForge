using System.Threading.Channels;
using BrickForge.Core.Pipelines;

namespace BrickForge.Api.Workers;

/// <summary>
/// Channel-based implementation of <see cref="IJobQueue"/>.
/// Uses an unbounded single-reader channel for maximum throughput.
/// </summary>
public sealed class DefaultJobQueue : IJobQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(string jobId)
        => _channel.Writer.TryWrite(jobId);

    public async Task<string> DequeueAsync(CancellationToken cancellationToken = default)
        => await _channel.Reader.ReadAsync(cancellationToken);
}
